using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;
using Kyna.Infrastructure.Events;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class PolygonMigrator : ImportsMigratorBase, IImportsMigrator
{
    private const int MaxDbConnections = 50;
    public override string Source => SourceName;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public const string SourceName = "polygon.io";

    private readonly MigrationConfiguration _configuration;

    private readonly SemaphoreSlim _connectionSemaphore;

    public PolygonMigrator(DbDef sourceDef, DbDef targetDef,
        MigrationConfiguration configuration, Guid? processId = null, bool dryRun = false) : base(sourceDef, targetDef, processId, dryRun)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _connectionSemaphore = new SemaphoreSlim(_configuration.MaxParallelization > 0
           ? Math.Min(_configuration.MaxParallelization, MaxDbConnections)
           : MaxDbConnections,
           MaxDbConnections);
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
                    (_configuration.SourceDeletionMode == SourceDeletionMode.AllExceptLatest &&
                        !item.Equals(last)))
                {
                    item.DeleteFromSource = true;
                }

                actions.Add(item);
            }
        }

        goto SkipStuff1;
        foreach (var actionType in new[] {
            PolygonImporter.Constants.Actions.TickerDetails,
            PolygonImporter.Constants.Actions.Splits,
            PolygonImporter.Constants.Actions.Dividends
        })
        {
            var itemsToProcess = actions.Where(a => a.Category.Equals(actionType) && a.DoMigrate).ToArray();
            if (_configuration.MaxParallelization > 1 && itemsToProcess.Length > 0)
            {
                await Parallel.ForEachAsync(itemsToProcess,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = _configuration.MaxParallelization,
                        CancellationToken = cancellationToken
                    },
                    async (item, ct) =>
                    {
                        string msg = $"Migrate {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}\tDelete from source: {item.DeleteFromSource}";
                        Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(PolygonMigrator)));

                        if (!_dryRun)
                        {
                            await MigrateItemAsync(item, ct).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
            }
            else
            {
                foreach (var item in itemsToProcess)
                {
                    string msg = $"Migrate {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}\tDelete from source: {item.DeleteFromSource}";
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
                    string msg = $"Delete {item.Id,12}\t{item.Category,15}\t{item.SubCategory,10}";
                    Communicate?.Invoke(this, new CommunicationEventArgs(msg, nameof(PolygonMigrator)));

                    if (!_dryRun)
                    {
                        string sql = $"{_sourceDbDef.Sql.GetSql(SqlKeys.DeleteApiTransactions, "id = @Id")}";
                        await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            using var srcConn = _sourceDbDef.GetConnection();
                            await srcConn.ExecuteAsync(sql, new { item.Id }, cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            _connectionSemaphore.Release();
                        }
                    }
                }
            }
        }

    SkipStuff1:
        IDbConnection? conn = null;

        goto SkipStuff2;
        
        // Remaining operations unchanged
        if (_configuration.Categories.Contains(PolygonImporter.Constants.Actions.FlatFiles.ToString(),
            StringComparer.OrdinalIgnoreCase))
        {
            if (_dryRun)
                Communicate?.Invoke(this, new CommunicationEventArgs("Migrate flat files.", nameof(PolygonMigrator)));
            else
                await MigrateFlatFilesAsync(cancellationToken);
        }

    SkipStuff2:

        if (_dryRun)
            Communicate?.Invoke(this, new CommunicationEventArgs("Hydrate missing entities.", nameof(PolygonMigrator)));
        else
        {
            Communicate?.Invoke(this, new CommunicationEventArgs("Hydrating missing entities", nameof(PolygonMigrator)));
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.HydrateMissingEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (_dryRun)
            Communicate?.Invoke(this, new CommunicationEventArgs("Set split indicator for entities", nameof(PolygonMigrator)));
        else
        {
            Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
            Communicate?.Invoke(this, new CommunicationEventArgs("Setting split indicator for entities", nameof(PolygonMigrator)));
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetSplitIndicatorForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (_dryRun)
            Communicate?.Invoke(this, new CommunicationEventArgs("Set price indicator for entities", nameof(PolygonMigrator)));
        else
        {
            Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
            Communicate?.Invoke(this, new CommunicationEventArgs("Setting price action indicator for entities", nameof(PolygonMigrator)));
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetPriceActionIndicatorForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (_dryRun)
            Communicate?.Invoke(this, new CommunicationEventArgs("Set last price action for entities", nameof(PolygonMigrator)));
        else
        {
            Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
            Communicate?.Invoke(this, new CommunicationEventArgs("Setting last price actions for entities", nameof(PolygonMigrator)));
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetLastPriceActionForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (_dryRun)
            Communicate?.Invoke(this, new CommunicationEventArgs("Clean up entities", nameof(PolygonMigrator)));
        else
        {
            Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
            Communicate?.Invoke(this, new CommunicationEventArgs("Cleaning up entities", nameof(PolygonMigrator)));
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.DeleteEntitiesWithoutTypesOrPriceActions),
                new { Source }, commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        timer.Stop();
        Communicate?.Invoke(this, new CommunicationEventArgs(timer.Elapsed.ConvertToText(), null));
        return timer.Elapsed;
    }

    private async Task MigrateItemAsync(ApiTransactionForMigration item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcConn = _sourceDbDef.GetConnection();
        var responseBody = await srcConn.QueryFirstOrDefaultAsync<string>(
            _sourceDbDef.Sql.GetSql(SqlKeys.FetchApiResponseBodyForId),
            new { item.Id });
        srcConn.Close();

        if (!string.IsNullOrWhiteSpace(responseBody) && responseBody != "[]" && responseBody != "{}")
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

    public string GetInfo()
    {
        StringBuilder result = new();

        result.AppendLine($"Mode                 : {_configuration.Mode.GetEnumDescription()}");
        result.AppendLine($"Source               : {_configuration.Source}");
        result.AppendLine($"Source Deletion Mode : {_configuration.SourceDeletionMode.GetEnumDescription()}");
        result.AppendLine($"Max Parallelization  : {_configuration.MaxParallelization}");

        return result.ToString();
    }

    private IEnumerable<ApiTransactionForMigration> GetTransactionsToMigrate()
    {
        var conn = _sourceDbDef.GetConnection();
        try
        {
            return conn.Query<ApiTransactionForMigration>(BuildFetchForMigrationSql(),
                new { _configuration.Source, _configuration.Categories });
        }
        finally
        {
            conn.Close();
        }
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
            whereClauses.Add($"category {SqlCollection.GetSqlSyntaxForInCollection("Categories")}");
        }

        return _sourceDbDef.Sql.GetSql(SqlKeys.FetchApiTransactionsForMigration, [.. whereClauses]);
    }

    private async Task MigrateFlatFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_configuration.ImportFileLocation))
            return;

        DirectoryInfo importDirectory = new(_configuration.ImportFileLocation);

        if (!importDirectory.Exists)
        {
            Communicate?.Invoke(this, new CommunicationEventArgs($"Directory not found: {importDirectory.FullName}.", nameof(PolygonMigrator)));
            return;
        }

        Communicate?.Invoke(this, new CommunicationEventArgs("Migrating flat files.", nameof(PolygonMigrator)));

        var zippedFiles = importDirectory.GetFiles("*.gz", SearchOption.TopDirectoryOnly);

        if (_configuration.MaxParallelization > 1 && zippedFiles.Length > 0)
        {
            Parallel.ForEach(zippedFiles,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
                }, (file) =>
                {
                    Communicate?.Invoke(this, new CommunicationEventArgs($"Unzipping {file.FullName}", nameof(PolygonMigrator)));
                    if (TryUnzipGzFile(file, out var newFilename, false))
                        Communicate?.Invoke(this, new CommunicationEventArgs($"{newFilename} created.", nameof(PolygonMigrator)));
                });
        }
        else
        {
            foreach (var file in zippedFiles)
            {
                Communicate?.Invoke(this, new CommunicationEventArgs($"Unzipping {file.FullName}", nameof(PolygonMigrator)));
                if (TryUnzipGzFile(file, out var newFilename, false))
                    Communicate?.Invoke(this, new CommunicationEventArgs($"{newFilename} created.", nameof(PolygonMigrator)));
            }
        }

        zippedFiles = null;

        var csvFiles = importDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);

        if (_configuration.MaxParallelization > 1 && csvFiles.Length > 0)
        {
            await Parallel.ForEachAsync(csvFiles,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
                    CancellationToken = cancellationToken
                },
                async (file, ct) =>
                {
                    await ImportCsvFileAsync(file, ct);
                });
        }
        else
        {
            foreach (var file in csvFiles)
            {
                await ImportCsvFileAsync(file);
            }
        }
        csvFiles = null;


        var timer = Stopwatch.StartNew();
        /*
         * For all the tickers that definitely do not have split data, just move them along.
         */
        Communicate?.Invoke(this, new CommunicationEventArgs($"Migrating prices for tickers without splits.", nameof(PolygonMigrator)));
        using var tgtConn = _targetDbDef.GetConnection();
        await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.CopyPricesWithoutSplitsToAdjustedPrices),
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        timer.Stop();
        Communicate?.Invoke(this, new CommunicationEventArgs($"Migrated prices for tickers without splits in {timer.Elapsed.ConvertToText()}", nameof(PolygonMigrator)));

        timer = Stopwatch.StartNew();
        /*
         * Some of the inbound data has big gaps in it. Not sure why; don't care. Need consecutive data points.
         * So, remove all the data before the last big price gap (currently set at '30 days') for any given ticker.
         */
        Communicate?.Invoke(this, new CommunicationEventArgs($"Deleting leading price gaps.", nameof(PolygonMigrator)));
        await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.DeleteLeadingPriceGaps),
            new { _configuration.Source, ProcessId = _processId },
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        timer.Stop();
        Communicate?.Invoke(this, new CommunicationEventArgs($"Deleted leading price gaps in {timer.Elapsed.ConvertToText()}.", nameof(PolygonMigrator)));

        timer = Stopwatch.StartNew();
        Communicate?.Invoke(this, new CommunicationEventArgs($"Fetching codes with splits.", nameof(PolygonMigrator)));
        var codesWithSplits = (await tgtConn.QueryAsync<string>(
            _targetDbDef.Sql.GetSql(SqlKeys.FetchCodesWithSplits), new { Source },
            cancellationToken: cancellationToken).ConfigureAwait(false)).ToArray();
        timer.Stop();
        Communicate?.Invoke(this, new CommunicationEventArgs($"Fetched codes with splits in {timer.Elapsed.ConvertToText()}", nameof(PolygonMigrator)));

        tgtConn.Close();

        if (_configuration.MaxParallelization > 1 && codesWithSplits.Length > 0)
        {
            await Parallel.ForEachAsync(codesWithSplits,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
                    CancellationToken = cancellationToken
                },
                async (code, ct) => await AdjustPriceForCodeAsync(code, ct));
        }
        else
        {
            foreach (var code in codesWithSplits)
            {
                await AdjustPriceForCodeAsync(code, cancellationToken);
            }
        }
    }

    private static readonly string[] _whereClauses = ["source = @Source", "code = @Code"];

    private async Task AdjustPriceForCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Communicate?.Invoke(this, new CommunicationEventArgs($"Adjusting prices for {code}.", nameof(PolygonMigrator)));
        var chartSql = _targetDbDef.Sql.GetSql(SqlKeys.FetchEodPrices, _whereClauses);
        var splitSql = _targetDbDef.Sql.GetSql(SqlKeys.FetchSplits, _whereClauses);
        using var tgtConn1 = _targetDbDef.GetConnection();
        using var tgtConn2 = _targetDbDef.GetConnection();
        var splits = tgtConn1.QueryAsync<Split>(splitSql,
            new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var chart = tgtConn2.QueryAsync<EodPrice>(chartSql,
            new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var adjustedChart = SplitAdjustedPriceCalculator.Calculate(await chart, await splits).ToArray();

        tgtConn2.Close();

        await tgtConn1.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertAdjustedEodPrice),
            adjustedChart, cancellationToken: cancellationToken).ConfigureAwait(false);
        tgtConn1.Close();
    }

    private async Task ImportCsvFileAsync(FileInfo file, CancellationToken cancellationToken = default)
    {
        Communicate?.Invoke(this, new CommunicationEventArgs($"Processing {file.FullName}", nameof(PolygonMigrator)));
        var lines = File.ReadAllLines(file.FullName);

        if (lines.Length > 1)
        {
            DataProviders.Polygon.Models.FlatFile[] flatFileLines = new DataProviders.Polygon.Models.FlatFile[lines.Length - 1];
            for (int i = 1; i < lines.Length; i++)
            {
                flatFileLines[i - 1] = new DataProviders.Polygon.Models.FlatFile(lines[i]);
            }
            using var priceConn = _targetDbDef.GetConnection();

            await priceConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEodPrice),
                flatFileLines.Select(f => new EodPrice(SourceName, f.Code, _processId)
                {
                    Open = f.Open,
                    High = f.High,
                    Low = f.Low,
                    Close = f.Close,
                    Volume = f.Volume,
                    DateEod = f.Date,
                }), cancellationToken: cancellationToken).ConfigureAwait(false);

            priceConn.Close();
        }
    }

    private bool TryUnzipGzFile(FileInfo file, out string? newFilename, bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (!file.Extension.Equals(".gz", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"File not in right format; should end with '.gz'");

        newFilename = file.FullName[0..^3]; // removes the ".gz"
        if (!File.Exists(newFilename) || overwrite)
        {
            Communicate?.Invoke(this, new CommunicationEventArgs($"Unzipping {file.FullName}", nameof(PolygonMigrator)));
            using var fs = file.OpenRead();
            using var newFile = File.Create(newFilename);
            using GZipStream zs = new(fs, CompressionMode.Decompress);
            zs.CopyTo(newFile);
            return true;
        }
        return false;
    }

    private async Task MigrateSplitsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var splitResponse = JsonSerializer.Deserialize<DataProviders.Polygon.Models.SplitResponse>(
            responseBody, JsonSerializerOptionsRepository.Custom);

        if (splitResponse.Results.Length > 0)
        {
            using var conn = _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertSplit),
                splitResponse.Results.Select(s => new Split(item.Source, item.SubCategory,
                s.ExecutionDate, s.SplitFrom, s.SplitTo, _processId))).ConfigureAwait(false);
        }
    }

    private async Task MigrateDividendsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var dividendResponse = JsonSerializer.Deserialize<DataProviders.Polygon.Models.DividendResponse>(
            responseBody, JsonSerializerOptionsRepository.Custom);

        if (dividendResponse.Results.Length > 0)
        {
            using var conn = _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertDividend),
                dividendResponse.Results.Select(d => new Dividend(item.Source, item.SubCategory,
                d.Type, d.DeclarationDate, d.ExDividendDate, d.PayDate, d.RecordDate, d.Frequency,
                d.CashAmount, item.ProcessId))).ConfigureAwait(false);
        }
    }

    private async Task MigrateTickerDetailsAsync(ApiTransactionForMigration item, string responseBody)
    {
        var detailResponse = JsonSerializer.Deserialize<DataProviders.Polygon.Models.TickerDetailResponse>(responseBody,
            JsonSerializerOptionsRepository.Custom);

        if ("OK".Equals(detailResponse.Status, StringComparison.OrdinalIgnoreCase))
        {
            using var conn = _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEntity),
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
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now
                }).ConfigureAwait(false);
        }
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
