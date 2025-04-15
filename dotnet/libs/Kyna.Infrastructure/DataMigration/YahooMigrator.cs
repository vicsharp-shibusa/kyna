using Kyna.Infrastructure.Database;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;

namespace Kyna.Infrastructure.DataMigration;

internal sealed class YahooMigrator(DbDef sourceDef, DbDef targetDef,
    YahooMigrator.MigrationConfiguration configuration, Guid? processId = null, bool dryRun = false)
    : ImportsMigratorBase(sourceDef, targetDef, processId, dryRun), IImportsMigrator, IDisposable
{
    static class Constants
    {
        public const string Prices = "Prices";
        public const string Splits = "Splits";
        public const string Dividends = "Dividends";
        public const string Financials = "Financials";
        public const string BalanceSheets = "Balance Sheets";
        public const string CashFlows = "Cash Flows";
        public const string QuarterlyBalanceSheets = "Quarterly Balance Sheets";
        public const string QuarterlyCashFlows = "Quarterly Cash Flows";
    }

    private readonly Dictionary<string, string> _inputFileSuffixes = new() {
        { Constants.Prices,"_prices.csv" },
        { Constants.Splits,"_splits.csv" },
        { Constants.Dividends,"_dividends.csv" },
        { Constants.Financials, "_Financials.csv" },
        { Constants.BalanceSheets,"_Balance_Sheet.csv" },
        { Constants.CashFlows,"_Cash_Flow.csv" },
        { Constants.QuarterlyBalanceSheets,"_Quarterly_Balance_Sheet.csv" },
        { Constants.QuarterlyCashFlows,"_Quarterly_Cash_Flow.csv" }
    };

    public override string Source => SourceName;
    public const string SourceName = "yahoo";

    private readonly MigrationConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

    public string GetInfo()
    {
        StringBuilder result = new();

        DirectoryInfo dirInfo = new(_configuration.InputPath);

        if (dirInfo.Exists)
        {
            result.AppendLine(dirInfo.FullName);

            var titles = _inputFileSuffixes.Keys.ToArray();
            var maxLen = titles.MaxBy(i => i.Length)!.Length + 1;
            foreach (var kvp in _inputFileSuffixes)
            {
                var pattern = $"*{kvp.Value}";
                var files = dirInfo.GetFiles(pattern);
                result.AppendLine($"{kvp.Key.PadRight(maxLen, ' ')}: {files.Length}");
            }
        }
        else
        {
            result.AppendLine($"The input path, '{dirInfo.FullName}', does not exist.");
        }

        return result.ToString();
    }

    public Task<TimeSpan> MigrateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dirInfo = new DirectoryInfo(_configuration.InputPath);

        if (!dirInfo.Exists)
        {
            throw new Exception($"Input directory, '{dirInfo.FullName}', does not exist.");
        }

        var timer = Stopwatch.StartNew();

        List<Task> tasks = new(8);

        if (configuration.Categories.Contains(Constants.Prices))
        {
            tasks.Add(MigrateFilesAsync(dirInfo.GetFiles($"*{_inputFileSuffixes[Constants.Prices]}",
                SearchOption.AllDirectories), ProcessPriceFileAsync));
        }

        if (configuration.Categories.Contains(Constants.Splits))
        {
            tasks.Add(MigrateFilesAsync(dirInfo.GetFiles($"*{_inputFileSuffixes[Constants.Splits]}",
                SearchOption.AllDirectories), ProcessSplitFileAsync));
        }

        if (configuration.Categories.Contains(Constants.Dividends))
        {
            tasks.Add(MigrateFilesAsync(dirInfo.GetFiles($"*{_inputFileSuffixes[Constants.Dividends]}",
                SearchOption.AllDirectories), ProcessDividendFileAsync));
        }

        Task.WaitAll([.. tasks], cancellationToken);

        timer.Stop();
        return Task.FromResult(timer.Elapsed);
    }

    private async Task MigrateFilesAsync(FileInfo[] files, Func<FileInfo, Task> func)
    {
        if (_configuration.MaxParallelization < 2)
        {
            foreach (var file in files)
            {
                if (file.Exists)
                {
                    await func(file).ConfigureAwait(false);
                }
            }
        }
        else
        {
            await Parallel.ForEachAsync(files, new ParallelOptions()
            {
                MaxDegreeOfParallelism = _configuration.MaxParallelization
            }, async (file, ct) =>
            {
                if (file.Exists)
                {
                    await func(file).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
    }

    private static string GetTickerFromFileName(string fileName) => fileName.Split('_')[0];

    private Task ProcessPriceFileAsync(FileInfo file)
    {
        Printf($"Processing {file.FullName}");
        var ticker = GetTickerFromFileName(file.Name);
        var lines = File.ReadAllLines(file.FullName);
        var priceDaos = new Database.DataAccessObjects.EodAdjustedPrice[lines.Length - 1];
        for (int i = 1; i < lines.Length; i++)
        {
            var split = lines[i].Split(',');
            if (!DateOnly.TryParse(split[0], out DateOnly date))
                throw new Exception($"Could not parse date from line {i} in file {file.Name}");

            if (!decimal.TryParse(split[1], System.Globalization.NumberStyles.Float, null, out decimal open))
                throw new Exception($"Could not parse open from line {i} in file {file.Name}");

            if (!decimal.TryParse(split[2], System.Globalization.NumberStyles.Float, null, out decimal high))
                throw new Exception($"Could not parse high from line {i} in file {file.Name}");

            if (!decimal.TryParse(split[3], System.Globalization.NumberStyles.Float, null, out decimal low))
                throw new Exception($"Could not parse low from line {i} in file {file.Name}");

            if (!decimal.TryParse(split[4], System.Globalization.NumberStyles.Float, null, out decimal close))
                throw new Exception($"Could not parse close from line {i} in file {file.Name}");

            if (!decimal.TryParse(split[5], System.Globalization.NumberStyles.Float, null, out _))
                throw new Exception($"Could not parse adjusted close from line {i} in file {file.Name}");

            if (!long.TryParse(split[6], out long volume))
                throw new Exception($"Could not parse volume from line {i} in file {file.Name}");

            priceDaos[i - 1] = new Database.DataAccessObjects.EodAdjustedPrice(SourceName, ticker, _processId)
            {
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume,
                DateEod = date
            };
        }

        int sizeOfChunk = 500;
        Task[] tasks = new Task[Convert.ToInt32(Math.Ceiling(priceDaos.Length / (decimal)sizeOfChunk))];
        int index = 0;
        foreach (var chunk in priceDaos.Chunk(sizeOfChunk))
        {
            using var conn = _targetDbDef.GetConnection();
            tasks[index++] = conn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertEodAdjustedPrice), chunk);
        }
        Task.WaitAll(tasks);

        if (_configuration.SourceDeletionMode == SourceDeletionMode.All)
        {
            file.Delete();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessSplitFileAsync(FileInfo file)
    {
        Printf($"Processing {file.FullName}");

        var ticker = GetTickerFromFileName(file.Name);
        var lines = File.ReadAllLines(file.FullName);
        var splitDaos = new List<Database.DataAccessObjects.Split>();

        for (int i = 1; i < lines.Length; i++)
        {
            var split = lines[i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (split[1].Equals("inf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!DateTime.TryParse(split[0], out DateTime dateTime))
            {
                throw new Exception($"Could not parse date/time from line {i} in file {file.Name}");
            }

            if (!double.TryParse(split[1], out double ratio))
            {
                throw new Exception($"Could not parse split ratio from line {i} in file {file.Name}");
            }

            var date = DateOnly.FromDateTime(dateTime);

            splitDaos.Add(new Database.DataAccessObjects.Split(SourceName, ticker, _processId)
            {
                Before = 1D,
                After = ratio,
                SplitDate = date
            });
        }

        using var tgtConn = _targetDbDef.GetConnection();
        await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertSplit), splitDaos);
        tgtConn.Close();

        if (_configuration.SourceDeletionMode == SourceDeletionMode.All)
        {
            file.Delete();
        }
    }

    private async Task ProcessDividendFileAsync(FileInfo file)
    {
        Printf($"Processing {file.FullName}");

        var ticker = GetTickerFromFileName(file.Name);
        var lines = File.ReadAllLines(file.FullName);
        var dividendDaos = new Database.DataAccessObjects.Dividend[lines.Length - 1];

        for (int i = 1; i < lines.Length; i++)
        {
            var split = lines[i].Split(',');
            if (!DateTime.TryParse(split[0], out DateTime dateTime))
            {
                throw new Exception($"Could not parse date/time from line {i} in file {file.Name}");
            }
            if (!decimal.TryParse(split[1], System.Globalization.NumberStyles.Float, null, out decimal dollarAmount))
            {
                throw new Exception($"Could not parse dividend amount from line {i} in file {file.Name}");
            }

            var date = DateOnly.FromDateTime(dateTime);

            dividendDaos[i - 1] = new Database.DataAccessObjects.Dividend(SourceName, ticker, "CD", _processId)
            {
                Amount = dollarAmount,
                DeclarationDate = date,
            };
        }
        using var tgtConn = _targetDbDef.GetConnection();
        await tgtConn.ExecuteAsync(_targetDbDef.Sql.GetSql(SqlKeys.UpsertDividend), dividendDaos);
        tgtConn.Close();

        if (_configuration.SourceDeletionMode == SourceDeletionMode.All)
        {
            file.Delete();
        }
    }

    public void Dispose()
    {
    }

    public class MigrationConfiguration(string inputPath)
    {
        public string Source { get; init; } = SourceName;
        public string[] Categories { get; init; } = [];

        [JsonPropertyName("Source Deletion Mode")]
        public SourceDeletionMode SourceDeletionMode { get; init; } = SourceDeletionMode.None;

        [JsonPropertyName("Max Parallelization")]
        public int MaxParallelization { get; init; }

        [JsonPropertyName("Import File Location")]
        public string InputPath { get; init; } = inputPath;
    }

    /// <summary>
    /// Represents the rules for deletion of import files.
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
        All
    }
}
