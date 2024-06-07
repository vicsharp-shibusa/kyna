using Kyna.Common;
using Kyna.Common.Events;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class PolygonMigrator(DbDef sourceDef, DbDef targetDef,
    PolygonMigrator.MigrationConfiguration configuration, Guid? processId = null, bool dryRun = false)
    : ImportsMigratorBase(sourceDef, targetDef, processId, dryRun), IImportsMigrator
{
    public override string Source => SourceName;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public const string SourceName = "polygon.io";

    private readonly MigrationConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

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
                    (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest &&
                        !item.Equals(last)))
                {
                    item.DeleteFromSource = true;
                }

                actions.Add(item);
            }
        }

        foreach (var actionType in new[] {
            PolygonImporter.Constants.Actions.TickerDetails,
            PolygonImporter.Constants.Actions.Splits,
            PolygonImporter.Constants.Actions.Dividends
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

                        Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(PolygonMigrator)));

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

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(PolygonMigrator)));

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

                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(PolygonMigrator)));

                    if (!_dryRun)
                    {
                        string sql = $"{_sourceContext.Sql.ApiTransactions.Delete} WHERE id = @Id";
                        await _sourceContext.ExecuteAsync(sql, new { item.Id },
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        if (_configuration.Categories.Contains(PolygonImporter.Constants.Actions.FlatFiles.ToString(),
            StringComparer.OrdinalIgnoreCase))
        {
            await MigrateFlatFilesAsync(cancellationToken);
        }

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Hydrating missing entities", nameof(PolygonMigrator)));
        await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.HydrateMissingEntities, commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Setting split indicator for entities", nameof(PolygonMigrator)));
        await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.SetSplitIndicatorForEntities, commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Setting price action indicator for entities", nameof(PolygonMigrator)));
        await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.SetPriceActionIndicatorForEntities, commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Setting last price actions for entities", nameof(PolygonMigrator)));
        await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.SetLastPriceActionForEntities, commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        Communicate?.Invoke(this, new CommunicationEventArgs("Cleaning up entities", nameof(PolygonMigrator)));
        await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.DeleteEntitiesWithoutTypesOrPriceActions,
            new { Source },
            commandTimeout: 0,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        timer.Stop();
        return timer.Elapsed;
    }

    public Task<string> GetInfoAsync()
    {
        StringBuilder result = new();

        result.AppendLine($"Mode                 : {_configuration.Mode.GetEnumDescription()}");
        result.AppendLine($"Source               : {_configuration.Source}");
        result.AppendLine($"Source Deletion Mode : {_configuration.SourceDeletionMode.GetEnumDescription()}");
        result.AppendLine($"Max Parallelization  : {_configuration.MaxParallelization}");

        return Task.FromResult(result.ToString());
    }

    private async Task MigrateItemAsync(ApiTransactionForMigration item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var responseBody = _sourceContext.QueryFirstOrDefault<string>(
            _sourceContext.Sql.ApiTransactions.FetchResponseBodyForId,
            new { item.Id });

        if (!string.IsNullOrWhiteSpace(responseBody) &&
            responseBody != "[]" && responseBody != "{}")
        {
            if (item.Category.Equals(PolygonImporter.Constants.Actions.Splits))
            {
                await MigrateSplitsAsync(item, responseBody).ConfigureAwait(false);
            }

            if (item.Category.Equals(PolygonImporter.Constants.Actions.Dividends))
            {
                await MigrateDividendsAsync(item, responseBody).ConfigureAwait(false);
            }

            if (item.Category.Equals(PolygonImporter.Constants.Actions.TickerDetails))
            {
                await MigrateTickerDetailsAsync(item, responseBody).ConfigureAwait(false);
            }
        }
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

    private async Task MigrateFlatFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(_configuration.ImportFileLocation))
        {
            DirectoryInfo importDirectory = new(_configuration.ImportFileLocation);

            if (!importDirectory.Exists)
            {
                Communicate?.Invoke(this, new CommunicationEventArgs("No files found to import.", nameof(PolygonMigrator)));
                return;
            }

            var zippedFiles = importDirectory.GetFiles("*.gz", SearchOption.AllDirectories);

            if (zippedFiles.Length > 0)
            {
                foreach (var file in zippedFiles)
                {
                    Communicate?.Invoke(this, new CommunicationEventArgs(file.FullName, nameof(PolygonMigrator)));
                    var newFileName = file.FullName[0..^3];
                    using var fs = file.OpenRead();
                    using var newFile = File.Create(newFileName);
                    using GZipStream zs = new(fs, CompressionMode.Decompress);
                    Communicate?.Invoke(this, new CommunicationEventArgs(newFileName, nameof(PolygonMigrator)));
                    zs.CopyTo(newFile);
                }
            }

            var csvFiles = importDirectory.GetFiles("*.csv", SearchOption.AllDirectories);

            if (csvFiles.Length > 0)
            {
                foreach (var file in csvFiles)
                {
                    Communicate?.Invoke(this, new CommunicationEventArgs(file.FullName, nameof(PolygonMigrator)));
                    var lines = File.ReadAllLines(file.FullName);

                    if (lines.Length > 1)
                    {
                        Polygon.Models.FlatFile[] flatFileLines = new Polygon.Models.FlatFile[lines.Length - 1];
                        for (int i = 1; i < lines.Length; i++)
                        {
                            flatFileLines[i - 1] = new Polygon.Models.FlatFile(lines[i]);
                        }
                        await _targetContext.ExecuteAsync(_targetContext.Sql.EodPrices.Upsert, flatFileLines.Select(f => new EodPrice(SourceName, f.Code, _processId)
                        {
                            Open = f.Open,
                            High = f.High,
                            Low = f.Low,
                            Close = f.Close,
                            Volume = f.Volume,
                            DateEod = f.Date,
                            CreatedTicksUtc = DateTime.UtcNow.Ticks,
                            UpdatedTicksUtc = DateTime.UtcNow.Ticks
                        }), cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            await _targetContext.ExecuteAsync(_targetContext.Sql.EodPrices.CopyPricesWithoutSplitsToAdjustedPrices,
                commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);

            Communicate?.Invoke(this, new CommunicationEventArgs($"Adjusting prices for tickers without splits.", nameof(PolygonMigrator)));
            var codesWithSplits = (await _targetContext.QueryAsync<string>(
                _targetContext.Sql.EodPrices.FetchCodesWithSplits, new { Source },
                cancellationToken: cancellationToken).ConfigureAwait(false)).ToArray();

            foreach (var code in codesWithSplits)
            {
                Communicate?.Invoke(this, new CommunicationEventArgs($"Adjusting prices for {code}.", nameof(PolygonMigrator)));
                var chartSql = @$"{_targetContext.Sql.EodPrices.Fetch}
WHERE source = @Source AND code = @Code";
                var splitSql = @$"{_targetContext.Sql.Splits.Fetch}
WHERE source = @Source and code = @Code";
                var splits = await _targetContext.QueryAsync<Split>(splitSql,
                    new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

                var chart = await _targetContext.QueryAsync<EodPrice>(chartSql,
                    new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

                var adjustedChart = SplitAdjustedPriceCalculator.Calculate(chart, splits).ToArray();

                await _targetContext.ExecuteAsync(_targetContext.Sql.AdjustedEodPrices.Upsert,
                    adjustedChart, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private Task MigrateSplitsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var splitResponse = JsonSerializer.Deserialize<Polygon.Models.SplitResponse>(
            responseBody, JsonOptionsRepository.DefaultSerializerOptions);

        if (splitResponse.Results.Length > 0)
        {
            return _targetContext.ExecuteAsync(_targetContext.Sql.Splits.Upsert,
                splitResponse.Results.Select(s => new Split(item.Source, item.SubCategory,
                s.ExecutionDate, s.SplitFrom, s.SplitTo, DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, _processId)));
        }

        return Task.CompletedTask;
    }

    private Task MigrateDividendsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var dividendResponse = JsonSerializer.Deserialize<Polygon.Models.DividendResponse>(
            responseBody, JsonOptionsRepository.DefaultSerializerOptions);

        if (dividendResponse.Results.Length > 0)
        {
            return _targetContext.ExecuteAsync(_targetContext.Sql.Dividends.Upsert,
                dividendResponse.Results.Select(d => new Dividend(item.Source, item.SubCategory,
                d.Type, d.DeclarationDate, d.ExDividendDate, d.PayDate, d.RecordDate, d.Frequency,
                d.CashAmount, DateTime.UtcNow.Ticks, DateTime.UtcNow.Ticks, item.ProcessId)));
        }

        return Task.CompletedTask;
    }

    private Task MigrateTickerDetailsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var detailResponse = JsonSerializer.Deserialize<Polygon.Models.TickerDetailResponse>(responseBody,
            JsonOptionsRepository.DefaultSerializerOptions);

        if ("OK".Equals(detailResponse.Status, StringComparison.OrdinalIgnoreCase))
        {
            return _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.UpsertEntity,
                new
                {
                    item.Source,
                    Code = item.SubCategory,
                    Country = detailResponse.Results.Locale,
                    Currency = detailResponse.Results.CurrencyName,
                    Phone = detailResponse.Results.PhoneNumber,
                    Delisted = !detailResponse.Results.Active,
                    Exchange = detailResponse.Results.PrimaryExchange,
                    Industry = detailResponse.Results.SicDescription,
                    detailResponse.Results.Name,
                    ProcessId = _processId,
                    WebUrl = detailResponse.Results.HomepageUrl,
                    detailResponse.Results.Type,
                    Sector = (string?)null,
                    GicSector = (string?)null,
                    GicGroup = (string?)null,
                    GicIndustry = (string?)null,
                    GicSubIndustry = (string?)null,
                    CreatedTicksUtc = DateTime.UtcNow.Ticks,
                    UpdatedTicksUtc = DateTime.UtcNow.Ticks
                });
        }
        return Task.CompletedTask;
    }

    private async Task<bool> TryMigrateFundamentalsForCommonStock(ApiTransactionForMigration item, string responseBody)
    {
        try
        {
            var fundamentals = JsonSerializer
                .Deserialize<EodHistoricalData.Models.Fundamentals.CommonStock.FundamentalsCollection>(
                responseBody, JsonOptionsRepository.DefaultSerializerOptions);

            if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
            {
                await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.UpsertEntity,
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

                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryMigrateFundamentalsForEtf(ApiTransactionForMigration item, string responseBody)
    {
        try
        {
            var fundamentals = JsonSerializer
                .Deserialize<EodHistoricalData.Models.Fundamentals.Etf.FundamentalsCollection>(
                responseBody, JsonOptionsRepository.DefaultSerializerOptions);

            if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
            {
                await _targetContext.ExecuteAsync(_targetContext.Sql.Fundamentals.UpsertEntity,
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

        [JsonPropertyName("Max Parallelization")]
        public int MaxParallelization { get; init; }
        [JsonPropertyName("Import File Location")]
        public string? ImportFileLocation { get; init; } = null;
        [JsonPropertyName("Import File Prefixes")]
        public string[] ImportFilePrefixes { get; init; } = [];
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
}
