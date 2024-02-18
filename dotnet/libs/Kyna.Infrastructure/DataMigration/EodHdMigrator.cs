using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class EodHdMigrator(DbDef sourceDef, DbDef targetDef,
EodHdMigrator.MigrationConfiguration configuration, Guid? processId = null,
    bool dryRun = false) : ImportsMigratorBase(sourceDef, targetDef, processId, dryRun), IImportsMigrator
{
    public override string Source => SourceName;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public const string SourceName = "eodhd.com";

    private readonly MigrationConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public async Task<TimeSpan> MigrateAsync(CancellationToken cancellationToken = default)
    {
        var timer = Stopwatch.StartNew();

        var itemsArray = GetTransactionsToMigrate().ToArray();

        HashSet<ApiTransactionForMigration> actions = new(20_000);

        var itemsToMigrate = itemsArray.GroupBy(g => new { g.Category, g.SubCategory })
            .Select(g => new
            {
                g.Key,
                Items = g.OrderBy(i => i.Id).ToArray()
            }).Where(i => i?.Items != null &&
                (_configuration.Categories.Length == 0
                || _configuration.Categories.Contains(i.Key.Category))).ToArray();

        foreach (var itemGrp in itemsToMigrate.Where(i => (i.Items?.Length ?? 0) > 0))
        {
            var last = itemGrp.Items.Last();
            foreach (var item in itemGrp.Items.Where(i => i != null))
            {
                if ((_configuration.Mode == MigrationSourceMode.Latest && item.Equals(last))
                    || _configuration.Mode == MigrationSourceMode.Rolling)
                {
                    item.DoMigrate = true;
                }

                if (_configuration.SourceDeletionMode == SourceDeletionMode.All ||
                    (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest &&
                        !item.Equals(last)))
                {
                    item.DeleteFromSource = true;
                }

                actions.Add(item);
            }
        }

        foreach (var t in new[] {
                EodHdImporter.Constants.Actions.Splits,
                EodHdImporter.Constants.Actions.EndOfDayPrices,
            })
        {
            if (_configuration.MaxParallelization > 1)
            {
                await Parallel.ForEachAsync(actions.Where(a => a.Category.Equals(t) && a.DoMigrate),
                    new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = _configuration.MaxParallelization,
                        CancellationToken = cancellationToken
                    },
                    async (item, ct) =>
                    {
                        string msg =
                            $"Migrate {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}\tDelete from source: {item.DeleteFromSource}";

                        Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(EodHdMigrator)));

                        if (!_dryRun)
                        {
                            await MigrateItemAsync(item, ct);
                        }
                    });
            }
            else
            {
                foreach (var item in actions.Where(a => a.Category.Equals(t) && a.DoMigrate))
                {
                    string msg =
                        $"Migrate {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}\tDelete from source: {item.DeleteFromSource}";

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(EodHdMigrator)));

                    if (!_dryRun)
                    {
                        await MigrateItemAsync(item, cancellationToken);
                    }
                }
            }

            if (_configuration.SourceDeletionMode != SourceDeletionMode.None)
            {
                foreach (var item in actions.Where(a => a.Category.Equals(t) && a.DeleteFromSource))
                {
                    string msg =
                        $"Delete {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}";

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(EodHdMigrator)));
                }

                if (!_dryRun)
                {

                }
            }
        }

        timer.Stop();
        return timer.Elapsed;
    }

    public Task<string> GetInfoAsync()
    {
        throw new NotImplementedException();
    }

    private Task MigrateItemAsync(ApiTransactionForMigration item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var responseBody = _sourceContext.QueryFirstOrDefault<string>(
            _sourceContext.Sql.ApiTransactions.FetchResponseBodyForId,
            new { item.Id });

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            if (item.Category.Equals(EodHdImporter.Constants.Actions.EndOfDayPrices))
            {
                return MigrateEodPricesAsync(item, responseBody);
            }

            if (item.Category.Equals(EodHdImporter.Constants.Actions.Splits))
            {
                return MigrateSplitsAsync(item, responseBody);
            }
        }

        return Task.CompletedTask;
    }
    private IEnumerable<ApiTransactionForMigration> GetTransactionsToMigrate()
    {
        return _sourceContext.Query<ApiTransactionForMigration>(BuildFetchForMigrationSql(),
            new { _configuration.Source, _configuration.Categories });
    }

    private string BuildFetchForMigrationSql()
    {
        StringBuilder sb = new(_sourceContext.Sql.ApiTransactions.FetchForMigration);
        sb.AppendLine();
        sb.AppendLine("WHERE source = @Source");
        if ((_configuration.Categories?.Length ?? 0) > 0)
        {
            sb.AppendLine($"AND category {_sourceContext.Sql.GetInCollectionSql("Categories")}");
        }
        sb.AppendLine($"AND response_status_code = '200'");
        return sb.ToString();
    }

    private async Task MigrateEodPricesAsync(ApiTransactionForMigration item, string responseBody)
    {
        if (_configuration.PriceMigrationMode != PriceMigrationMode.None)
        {
            var eodPriceActions = JsonSerializer.Deserialize<EodHistoricalData.PriceAction[]>(
                responseBody, JsonOptionsRepository.DefaultSerializerOptions);

            if ((eodPriceActions?.Length ?? 0) > 0)
            {
                var eodPrices = eodPriceActions!.Select(p => new EodPrice(item.Source,
                        item.SubCategory,
                        p.Date, p.Open, p.High, p.Low, p.Close, p.Volume,
                        DateTime.UtcNow.Ticks,
                        DateTime.UtcNow.Ticks,
                        _processId)).ToArray();

                if (_configuration.PriceMigrationMode.HasFlag(PriceMigrationMode.Raw))
                {
                    await _targetContext.ExecuteAsync(_targetContext.Sql.EodPrices.Upsert, eodPrices);
                }

                var adjPrices = Array.Empty<AdjustedEodPrice>();
                if (_configuration.PriceMigrationMode.HasFlag(PriceMigrationMode.Adjusted))
                {
                    string splitSql = $"{_targetContext.Sql.Splits.Fetch} WHERE source = @Source AND code = @Code";
                    var splits = _targetContext.Query<Split>(splitSql, new
                    {
                        item.Source,
                        Code = item.SubCategory
                    }).ToArray();

                    adjPrices = SplitAdjustedPriceCalculator.Calculate(eodPrices, splits).ToArray();

                    if (adjPrices.Length > 0)
                    {
                        await _targetContext.ExecuteAsync(_targetContext.Sql.AdjustedEodPrices.Upsert, adjPrices);
                    }
                }
            }
        }
    }

    private Task MigrateSplitsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var splits = JsonSerializer.Deserialize<EodHistoricalData.Split[]>(
            responseBody, JsonOptionsRepository.DefaultSerializerOptions);

        if ((splits?.Length ?? 0) > 0)
        {
            return _targetContext.ExecuteAsync(_sourceContext.Sql.Splits.Upsert,
                splits!.Select(s => new Split(item.Source, item.SubCategory,
                s.Date, s.SplitText, _processId)));
        }

        return Task.CompletedTask;
    }

    public class MigrationConfiguration(MigrationSourceMode mode, string source)
    {
        public string Source { get; init; } = source;
        public string[] Categories { get; init; } = [];

        public MigrationSourceMode Mode { get; init; } = mode;

        [JsonPropertyName("Source Deletion Mode")]
        public SourceDeletionMode SourceDeletionMode { get; init; } = SourceDeletionMode.None;

        [JsonPropertyName("Price Migration Mode")]
        public PriceMigrationMode PriceMigrationMode { get; init; } = PriceMigrationMode.None;

        public int MaxParallelization { get; init; }
    }

    /// <summary>
    /// References the mode in which data is migrated from the imports database to the financials database.
    /// </summary>
    public enum MigrationSourceMode
    {
        /// <summary>
        /// Migrate only the latest for each category/sub-category for the source
        /// </summary>
        Latest = 0,
        /// <summary>
        /// Migrate all identified records in chronological order
        /// </summary>
        Rolling
    }

    /// <summary>
    /// Represents the rules for deletion of API transactions in the imports database.
    /// </summary>
    public enum SourceDeletionMode
    {
        /// <summary>
        /// Delete NONE of the imported transactions.
        /// </summary>
        None = 0,
        /// <summary>
        /// Delete ALL imported transactions (after migration).
        /// </summary>
        All,
        /// <summary>
        /// Delete all imported transactions EXCEPT the latest.
        /// </summary>
        [Description("All Except Latest")]
        AllExceptLatest
    }

    /// <summary>
    /// Represents whether we should preserve raw data, adjusted data, or both.
    /// </summary>
    [Flags]
    public enum PriceMigrationMode
    {
        None = 0,
        Raw = 1 << 0,
        Adjusted = 1 << 1
    }
}
