using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Database.DataAccessObjects;
using Kyna.Infrastructure.DataImport;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class PolygonMigrator : ImportsMigratorBase, IImportsMigrator, IDisposable
{
    private const int MaxDbConnections = 50;
    internal const string SourceName = "polygon.io";
    private const string Provider = "AWS";
    public override string Source => SourceName;

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

        Printf("Finding transactions to migrate.");
        var itemsArray = GetTransactionsToMigrate().ToArray();
        Printf($"{itemsArray.Length:#,##0} discovered.");

        if (itemsArray.Length == 0)
            return timer.Elapsed;

        HashSet<ApiTransactionForMigration> actions = new(20_000);

        var itemsToMigrate = itemsArray.GroupBy(g => new { g.Category, g.SubCategory })
            .Select(g => new
            {
                g.Key,
                Items = g.OrderBy(i => i.Id).ToArray()
            }).Where(i => _configuration.Categories.Length == 0
                || _configuration.Categories.Contains(i.Key.Category)).ToArray();
        Printf($"{itemsToMigrate.Length:#,##0} items grouped by category and sub-category.");

        foreach (var itemGrp in itemsToMigrate)
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
            var itemsToProcess = actions.Where(a => a.Category.Equals(actionType) && a.DoMigrate).ToArray();

            Printf($"{actionType} - {itemsToProcess.Length:#,##0}");

            if (!_dryRun)
            {
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
                            await MigrateItemAsync(item, ct).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                }
                else
                {
                    foreach (var item in itemsToProcess)
                    {
                        await MigrateItemAsync(item, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (_configuration.SourceDeletionMode != SourceDeletionMode.None)
            {
                var items = actions.Where(a => a.Category.Equals(actionType) && a.DeleteFromSource).ToArray();
                Printf($"Delete from source: {items.Length:#,##0}");
                if (!_dryRun)
                {
                    foreach (var item in items)
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

        IDbConnection? conn = null;

        // Remaining operations unchanged
        if (_configuration.Categories.Contains(PolygonImporter.Constants.Actions.FlatFiles.ToString(),
            StringComparer.OrdinalIgnoreCase))
        {
            Printf("Migrate flat files.");

            if (!_dryRun)
                await MigrateFlatFilesAsync(cancellationToken);
        }

        Printf("Hydrate missing entities.");
        if (!_dryRun)
        {
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.HydrateMissingEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Printf("Set split indicator for entities");
        if (!_dryRun)
        {
            Printf(timer.Elapsed.ConvertToText());
            Printf("Setting split indicator for entities");
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetSplitIndicatorForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Printf("Set price indicator for entities");
        if (!_dryRun)
        {
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetPriceActionIndicatorForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Printf("Set last price action for entities");
        if (!_dryRun)
        {
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.SetLastPriceActionForEntities), commandTimeout: 0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Printf("Clean up entities");
        if (!_dryRun)
        {
            conn ??= _targetDbDef.GetConnection();
            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.DeleteEntitiesWithoutTypesOrPriceActions),
                new { Source }, commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        timer.Stop();
        Printf(timer.Elapsed.ConvertToText());
        return timer.Elapsed;
    }

    private async Task MigrateItemAsync(ApiTransactionForMigration item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var srcConn = _sourceDbDef.GetConnection();
        var responseBody = await srcConn.QueryFirstOrDefaultAsync<string>(_sourceDbDef.Sql.GetSql(SqlKeys.SelectApiResponseBodyForId), new { item.Id }, cancellationToken: cancellationToken);
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

        return _sourceDbDef.Sql.GetSql(SqlKeys.SelectApiTransactionsForMigration, [.. whereClauses]);
    }

    private async Task MigrateFlatFilesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_configuration.ImportFileLocation))
            return;

        DirectoryInfo importDirectory = new(_configuration.ImportFileLocation);

        if (!importDirectory.Exists)
        {
            Printf($"Directory not found: {importDirectory.FullName}.");
            return;
        }

        var zippedFiles = importDirectory.GetFiles("*.gz", SearchOption.TopDirectoryOnly);

        Printf($"Unzipping: {zippedFiles.Length}");
        if (_configuration.MaxParallelization > 1 && zippedFiles.Length > 0)
        {
            Parallel.ForEach(zippedFiles,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _configuration.MaxParallelization,
                }, (file) =>
                {
                    if (!TryUnzipGzFile(file, out _, false))
                        Printf($"Unable to unzip {file.Name}.");
                });
        }
        else
        {
            foreach (var file in zippedFiles)
            {
                if (!TryUnzipGzFile(file, out _, false))
                    Printf($"Unable to unzip {file.Name}.");
            }
        }

        zippedFiles = null;

        var csvFiles = importDirectory.GetFiles("*.csv", SearchOption.TopDirectoryOnly);

        Printf("Fetching 'remote file' records from db.");

        RemoteFile[] remoteFiles = [];

        var sql = _sourceDbDef.Sql.GetFormattedSqlWithWhereClause(SqlKeys.SelectRemoteFiles,
            LogicalOperator.And, "source = @Source", "provider = @Provider");

        using (var conn = _sourceDbDef.GetConnection())
        {
            remoteFiles = [.. (await conn.QueryAsync<RemoteFile>(sql,
                                new { Source, Provider },
                                cancellationToken: cancellationToken).ConfigureAwait(false))];
        }
        Printf($"Remote records found: {remoteFiles.Length:#,##0}");

        Printf($"Process csv files: {csvFiles.Length:#,##0}");
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
                    var match = remoteFiles.FirstOrDefault(f => f.IsNameMatch(file.Name));

                    await ImportCsvFileAsync(file, match, ct);
                });
        }
        else
        {
            foreach (var file in csvFiles)
            {
                var match = remoteFiles.FirstOrDefault(f =>
                    f.LocalName?.Equals(file.Name, StringComparison.OrdinalIgnoreCase) ?? false);

                await ImportCsvFileAsync(file, match, cancellationToken);
            }
        }
        csvFiles = null;

        var timer = Stopwatch.StartNew();
        /*
         * For all the tickers that definitely do not have split data, just move them along.
         */
        Printf($"Migrating prices for tickers without splits.");
        using var tgtConn = _targetDbDef.GetConnection();
        await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.CopyPricesWithoutSplitsToAdjustedPrices),
            commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        timer.Stop();

        Printf(timer.Elapsed.ConvertToText());

        timer = Stopwatch.StartNew();
        /*
         * Some of the inbound data has big gaps in it. Not sure why; don't care. Need consecutive data points.
         * So, remove all the data before the last big price gap (currently set at '30 days') for any given ticker.
         */
        // TODO: removing this code to see if this is causing a problem with DJIA history.
        //Printf($"Deleting leading price gaps.");
        //await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.DeleteLeadingPriceGaps),
        //    new { _configuration.Source, ProcessId = _processId },
        //    commandTimeout: 0, cancellationToken: cancellationToken).ConfigureAwait(false);
        //timer.Stop();

        //Printf(timer.Elapsed.ConvertToText());

        timer = Stopwatch.StartNew();
        Printf($"Fetching codes with splits.");
        var codesWithSplits = (await tgtConn.QueryAsync<string>(
            _targetDbDef.Sql.GetSql(SqlKeys.SelectCodesWithSplits), new { Source },
            cancellationToken: cancellationToken).ConfigureAwait(false)).ToArray();

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
        timer.Stop();
        Printf(timer.Elapsed.ConvertToText());
    }

    private static readonly string[] _whereClauses = ["source = @Source", "code = @Code"];

    private async Task AdjustPriceForCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Printf($"Adjusting prices for {code}.");
        var chartSql = _targetDbDef.Sql.GetSql(SqlKeys.SelectEodPrices, _whereClauses);
        var splitSql = _targetDbDef.Sql.GetSql(SqlKeys.SelectSplits, _whereClauses);
        using var tgtConn1 = _targetDbDef.GetConnection();
        using var tgtConn2 = _targetDbDef.GetConnection();
        var splits = tgtConn1.QueryAsync<Split>(splitSql,
            new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var chart = tgtConn2.QueryAsync<EodPrice>(chartSql,
            new { Source, code }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var adjustedChart = SplitAdjustedPriceCalculator.Calculate(await chart, await splits).ToArray();

        tgtConn2.Close();

        await tgtConn1.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEodAdjustedPrice),
            adjustedChart, cancellationToken: cancellationToken).ConfigureAwait(false);
        tgtConn1.Close();
    }

    private async Task ImportCsvFileAsync(FileInfo file, RemoteFile? remoteFile = null, CancellationToken cancellationToken = default)
    {
        if (remoteFile?.MigratedAt.HasValue ?? false)
            return;

        var lines = File.ReadAllLines(file.FullName);

        if (lines.Length > 1)
        {
            DataProviders.Polygon.Models.FlatFileLine[] flatFileLines = new DataProviders.Polygon.Models.FlatFileLine[lines.Length - 1];
            for (int i = 1; i < lines.Length; i++)
            {
                flatFileLines[i - 1] = new DataProviders.Polygon.Models.FlatFileLine(lines[i]);
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


            if (remoteFile != null)
            {
                using var rfConn = _sourceDbDef.GetConnection();
                DateTimeOffset timestamp = DateTimeOffset.UtcNow;
                await rfConn.ExecuteAsync(_sourceDbDef.Sql.GetSql(SqlKeys.MarkRemoteFileAsMigrated), new
                {
                    remoteFile.Id,
                    timestamp,
                    TimestampMs = timestamp.ToUnixTimeMilliseconds()
                }, cancellationToken: cancellationToken);
                rfConn.Close();
            }

            if (_configuration.RemoveMigratedFiles)
                file.Delete();
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
            using var fs = file.OpenRead();
            using var newFile = File.Create(newFilename);
            using GZipStream zs = new(fs, CompressionMode.Decompress);
            zs.CopyTo(newFile);

            fs.Dispose();
            if (_configuration.RemoveCompressedFiles)
                file.Delete();
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

    #region Archive - maybe soon
    //private async Task<bool> TryMigrateFundamentalsForCommonStock(ApiTransactionForMigration item, string responseBody)
    //{
    //    try
    //    {
    //        var fundamentals = JsonSerializer
    //            .Deserialize<DataProviders.EodHistoricalData.Models.Fundamentals.CommonStock.FundamentalsCollection>(
    //            responseBody, JsonSerializerOptionsRepository.Custom);

    //        if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
    //        {
    //            using var conn = _targetDbDef.GetConnection();
    //            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEntity),
    //                new Entity(item.Source, item.SubCategory)
    //                {
    //                    Country = fundamentals.General.CountryName ?? "USA",
    //                    Currency = fundamentals.General.CurrencyCode ?? "USD",
    //                    Delisted = fundamentals.General.IsDelisted.GetValueOrDefault(),
    //                    Exchange = fundamentals.General.Exchange ?? "Unknown",
    //                    GicGroup = fundamentals.General.GicGroup,
    //                    GicSector = fundamentals.General.GicSector,
    //                    GicIndustry = fundamentals.General.GicIndustry,
    //                    GicSubIndustry = fundamentals.General.GicSubIndustry,
    //                    Industry = fundamentals.General.Industry,
    //                    Name = fundamentals.General.Name ?? item.SubCategory,
    //                    Phone = fundamentals.General.Phone,
    //                    WebUrl = fundamentals.General.WebUrl,
    //                    Type = fundamentals.General.Type ?? "Common Stock",
    //                    Sector = fundamentals.General.Sector,
    //                    ProcessId = _processId
    //                }).ConfigureAwait(false);
    //            return true;
    //        }

    //        return false;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}

    //private async Task<bool> TryMigrateFundamentalsForEtf(ApiTransactionForMigration item, string responseBody)
    //{
    //    try
    //    {
    //        var fundamentals = JsonSerializer
    //            .Deserialize<DataProviders.EodHistoricalData.Models.Fundamentals.Etf.FundamentalsCollection>(
    //            responseBody, JsonSerializerOptionsRepository.Custom);

    //        if (!string.IsNullOrWhiteSpace(fundamentals.General.Code))
    //        {
    //            using var conn = _targetDbDef.GetConnection();
    //            await conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEntity),
    //                new Entity(item.Source, item.SubCategory)
    //                {
    //                    Country = fundamentals.General.CountryName ?? "USA",
    //                    Currency = fundamentals.General.CurrencyCode ?? "USD",
    //                    Delisted = false,
    //                    Exchange = fundamentals.General.Exchange ?? "Unknown",
    //                    Name = fundamentals.General.Name ?? item.SubCategory,
    //                    Type = fundamentals.General.Type ?? "ETF",
    //                    ProcessId = _processId
    //                }).ConfigureAwait(false);

    //            return true;
    //        }

    //        return false;
    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}
    #endregion
    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
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
        [JsonPropertyName("Remove Migrated Files")]
        public bool RemoveMigratedFiles { get; init; }
        [JsonPropertyName("Remove Compressed Files")]
        public bool RemoveCompressedFiles { get; init; }
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
