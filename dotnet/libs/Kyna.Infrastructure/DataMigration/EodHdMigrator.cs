using Kyna.Common;
using Kyna.DataProviders.EodHistoricalData.Models;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;
using Kyna.Infrastructure.Events;
using Kyna.Infrastructure.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class EodHdMigrator : ImportsMigratorBase, IImportsMigrator
{
    public override string Source => SourceName;
    public const string SourceName = "eodhd.com";

    public event EventHandler<CommunicationEventArgs>? Communicate;

    private readonly MigrationConfiguration _configuration;

    public EodHdMigrator(DbDef sourceDef, DbDef targetDef,
        MigrationConfiguration configuration, Guid? processId = null, bool dryRun = false)
        : base(sourceDef, targetDef, processId, dryRun)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<TimeSpan> MigrateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
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
                    (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest && !item.Equals(last)))
                {
                    item.DeleteFromSource = true;
                }

                actions.Add(item);
            }
        }

        foreach (var actionType in new[] {
            EodHdImporter.Constants.Actions.Fundamentals,
            EodHdImporter.Constants.Actions.Splits,
            EodHdImporter.Constants.Actions.Dividends,
            EodHdImporter.Constants.Actions.EndOfDayPrices
        })
        {
            if (_configuration.MaxParallelization > 1)
            {
                await Parallel.ForEachAsync(actions.Where(a => a.Category.Equals(actionType) && a.DoMigrate),
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
                            await MigrateItemAsync(item, ct).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
            }
            else
            {
                foreach (var item in actions.Where(a => a.Category.Equals(actionType) && a.DoMigrate))
                {
                    string msg =
                        $"Migrate {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}\tDelete from source: {item.DeleteFromSource}";

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(EodHdMigrator)));

                    if (!_dryRun)
                    {
                        await MigrateItemAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (_configuration.SourceDeletionMode != SourceDeletionMode.None)
            {
                foreach (var item in actions.Where(a => a.Category.Equals(actionType) && a.DeleteFromSource))
                {
                    string msg =
                        $"Delete {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}";

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(EodHdMigrator)));

                    if (!_dryRun)
                    {

                        string sql = $"{_sourceDbDef.Sql.GetSql(SqlKeys.DeleteApiTransactions, "id = @Id")}";
                        using var conn = _sourceDbDef.GetConnection();
                        await conn.ExecuteAsync(sql, new { item.Id },
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                        conn.Close();
                    }
                }
            }
        }

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Hydrating missing entities", nameof(EodHdMigrator)));
        using var targetConn = _targetDbDef.GetConnection();
        await targetConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.HydrateMissingEntities),
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));

        Communicate?.Invoke(this, new CommunicationEventArgs("Setting split indicator for entities", nameof(EodHdMigrator)));
        await targetConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetSplitIndicatorForEntities),
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Setting price action indicator for entities", nameof(EodHdMigrator)));
        await targetConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetPriceActionIndicatorForEntities),
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Setting last price actions for entities", nameof(EodHdMigrator)));
        await targetConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetLastPriceActionForEntities), commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        timer.Stop();
        return timer.Elapsed;
    }

    public string GetInfo()
    {
        StringBuilder result = new();

        result.AppendLine($"Mode                 : {_configuration.Mode.GetEnumDescription()}");
        result.AppendLine($"Source               : {_configuration.Source}");
        result.AppendLine($"Price Migration Mode : {_configuration.PriceMigrationMode.GetEnumDescription()}");
        result.AppendLine($"Source Deletion Mode : {_configuration.SourceDeletionMode.GetEnumDescription()}");
        result.AppendLine($"Max Parallelization  : {_configuration.MaxParallelization}");

        return result.ToString();
    }

    private async Task MigrateItemAsync(ApiTransactionForMigration item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcConn = _sourceDbDef.GetConnection();
        var responseBody = srcConn.QueryFirstOrDefault<string>(
            _sourceDbDef.Sql.GetSql(SqlKeys.FetchApiResponseBodyForId),
            new { item.Id });

        if (!string.IsNullOrWhiteSpace(responseBody) &&
            responseBody != "[]" && responseBody != "{}")
        {
            if (item.Category.Equals(EodHdImporter.Constants.Actions.EndOfDayPrices))
            {
                await MigrateEodPricesAsync(item, responseBody).ConfigureAwait(false);
            }

            if (item.Category.Equals(EodHdImporter.Constants.Actions.Splits))
            {
                await MigrateSplitsAsync(item, responseBody).ConfigureAwait(false);
            }

            if (item.Category.Equals(EodHdImporter.Constants.Actions.Dividends))
            {
                await MigrateDividendsAsync(item, responseBody).ConfigureAwait(false);
            }

            if (item.Category.Equals(EodHdImporter.Constants.Actions.Fundamentals))
            {
                if (!(responseBody == "{}" || responseBody == "[]"))
                {
                    if (!await TryMigrateFundamentalsForCommonStock(item, responseBody).ConfigureAwait(false))
                    {
                        if (!await TryMigrateFundamentalsForEtf(item, responseBody).ConfigureAwait(false))
                        {
                            KyLogger.LogError($"Unable to process {item.Category} for {item.SubCategory}");
                        }
                    }
                }
            }
        }
    }
    private IEnumerable<ApiTransactionForMigration> GetTransactionsToMigrate()
    {
        var sql = _sourceDbDef.Sql.GetSql(SqlKeys.FetchApiTransactionsForMigration,
            "source = @Source", "response_status_code = '200'");
        using var conn = _sourceDbDef.GetConnection();
        return conn.Query<ApiTransactionForMigration>(BuildFetchForMigrationSql(),
            new { _configuration.Source, _configuration.Categories });
    }

    private string? BuildFetchForMigrationSql()
    {
        List<string> whereClauses = new(3)
        {
            "source = @Source",
            "response_status_code = '200'"
        };
        if ((_configuration.Categories?.Length ?? 0) > 0)
        {
            whereClauses.Add($"category {SqlCollection.GetSqlSyntaxForInCollection("Categories", _sourceDbDef.Engine)}");
        }

        return _sourceDbDef.Sql.GetSql(SqlKeys.FetchApiTransactionsForMigration, [.. whereClauses]);
    }

    private async Task MigrateEodPricesAsync(ApiTransactionForMigration item, string responseBody)
    {
        if (_configuration.PriceMigrationMode != PriceMigrationMode.None)
        {
            var eodPriceActions = JsonSerializer.Deserialize<PriceAction[]>(
                responseBody, JsonSerializerOptionsRepository.Custom);

            if ((eodPriceActions?.Length ?? 0) > 0)
            {
                var eodPrices = eodPriceActions!.Select(p => new EodPrice(item.Source,
                        item.SubCategory,
                        p.Date, p.Open, p.High, p.Low, p.Close, p.Volume,
                        _processId)).ToArray();

                using var conn = _targetDbDef.GetConnection();
                if (_configuration.PriceMigrationMode.HasFlag(PriceMigrationMode.Raw))
                {
                    await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEodPrice), eodPrices).ConfigureAwait(false);
                }

                var adjPrices = Array.Empty<EodAdjustedPrice>();
                if (_configuration.PriceMigrationMode.HasFlag(PriceMigrationMode.Adjusted))
                {
                    if (_targetDbDef.Sql.TryGetSqlWithWhereClause(SqlKeys.FetchSplits, out var splitSql,
                        LogicalOperator.And, "source = @Source", "code = @Code"))
                    {
                        var splits = conn.Query<Database.DataAccessObjects.Split>(splitSql, new
                        {
                            item.Source,
                            Code = item.SubCategory
                        }).ToArray();

                        adjPrices = [.. SplitAdjustedPriceCalculator.Calculate(eodPrices, splits)];

                        if (adjPrices.Length > 0)
                        {
                            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertAdjustedEodPrice), adjPrices).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }

    private Task MigrateSplitsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var splits = JsonSerializer.Deserialize<DataProviders.EodHistoricalData.Models.Split[]>(
            responseBody, JsonSerializerOptionsRepository.Custom);

        if ((splits?.Length ?? 0) > 0)
        {
            using var conn = _targetDbDef.GetConnection();
            return conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertSplit),
                splits!.Select(s => new Database.DataAccessObjects.Split(item.Source, item.SubCategory,
                s.Date, s.SplitText, _processId)));
        }

        return Task.CompletedTask;
    }

    private Task MigrateDividendsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var dividends = JsonSerializer.Deserialize<DataProviders.EodHistoricalData.Models.Dividend[]>(
            responseBody, JsonSerializerOptionsRepository.Custom);

        if ((dividends?.Length ?? 0) > 0)
        {
            using var conn = _targetDbDef.GetConnection();
            return conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertDividend),
                dividends!.Select(s => new Database.DataAccessObjects.Dividend(item.Source, item.SubCategory,
                "CD", _processId)
                {
                    DeclarationDate = s.DeclarationDate,
                    Amount = s.Value,
                    ExDividendDate = null,
                    PayDate = s.PaymentDate,
                    RecordDate = s.RecordDate,
                    Type = s.Period?.ToLower() == "special" ? "SD" : "CD",
                    Frequency = ConvertFrequencyTextToNumber(s.Period)
                }));
        }

        return Task.CompletedTask;
    }

    private static int ConvertFrequencyTextToNumber(string? frequency)
    {
        var f = frequency?.ToLower();
        return f switch
        {
            null => 0,
            "quarterly" => 4,
            "other" or "unknown" or "special" => 0,
            "annual" or "interim" or "final" => 1,
            "semiannual" => 2,
            "monthly" => 12,
            "weekly" => 52,
            _ => throw new Exception($"Could not convert frequency '{frequency}' to a number.")
        };
    }

    private async Task<bool> TryMigrateFundamentalsForCommonStock(ApiTransactionForMigration item, string responseBody)
    {
        try
        {
            var fundamentals = JsonSerializer
                .Deserialize<DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock.FundamentalsCollection>(
                responseBody, JsonSerializerOptionsRepository.Custom);

            if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
            {
                using var conn = _targetDbDef.GetConnection();
                await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEntity),
                    new Entity(item.Source, item.SubCategory)
                    {
                        Country = fundamentals.General.CountryName ?? "USA",
                        Currency = fundamentals.General.CurrencyCode ?? "USD",
                        Delisted = fundamentals.General.IsDelisted.GetValueOrDefault(),
                        Exchange = fundamentals.General.Exchange ?? "Unknown",
                        GicGroup = fundamentals.General.GicGroup,
                        GicSector = fundamentals.General.GicSector,
                        GicIndustry = fundamentals.General.GicIndustry,
                        GicSubIndustry = fundamentals.General.GicSubIndustry,
                        Industry = fundamentals.General.Industry,
                        Name = fundamentals.General.Name ?? item.SubCategory,
                        Phone = fundamentals.General.Phone,
                        WebUrl = fundamentals.General.WebUrl,
                        Type = fundamentals.General.Type ?? "Common Stock",
                        Sector = fundamentals.General.Sector,
                        ProcessId = _processId
                    }).ConfigureAwait(false);
                conn.Close();
                return true;
            }

            return false;
        }
        catch
        {
            // TODO: this doesn't seem quite right - we aren't even logging this?
            return false;
        }
    }

    private async Task<bool> TryMigrateFundamentalsForEtf(ApiTransactionForMigration item, string responseBody)
    {
        try
        {
            var fundamentals = JsonSerializer
                .Deserialize<DataProviders.EodHistoricalData.Models.Fundamentals.Etf.FundamentalsCollection>(
                responseBody, JsonSerializerOptionsRepository.Custom);

            if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
            {
                using var conn = _targetDbDef.GetConnection();
                await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEntity),
                    new Entity(item.Source, item.SubCategory)
                    {
                        Country = fundamentals.General.CountryName ?? "USA",
                        Currency = fundamentals.General.CurrencyCode ?? "USD",
                        Delisted = false,
                        Exchange = fundamentals.General.Exchange ?? "Unknown",
                        Name = fundamentals.General.Name ?? item.SubCategory,
                        Type = fundamentals.General.Type ?? "ETF",
                        ProcessId = _processId
                    }).ConfigureAwait(false);
                conn.Close();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public class MigrationConfiguration(MigrationSourceMode mode)
    {
        public string Source { get; init; } = SourceName;
        public string[] Categories { get; init; } = [];
        public MigrationSourceMode Mode { get; init; } = mode;

        [JsonPropertyName("Source Deletion Mode")]
        public SourceDeletionMode SourceDeletionMode { get; init; } = SourceDeletionMode.None;

        [JsonPropertyName("Price Migration Mode")]
        public PriceMigrationMode PriceMigrationMode { get; init; } = PriceMigrationMode.None;

        [JsonPropertyName("Max Parallelization")]
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
