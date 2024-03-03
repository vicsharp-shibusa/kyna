using Kyna.Common.Events;
using Kyna.Common.Logging;
using Kyna.Infrastructure.Database;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace Kyna.Infrastructure.DataImport;

internal sealed class YahooImporter : IExternalDataImporter
{
    private readonly IDbContext _dbContext;
    private readonly DbDef _dbDef;
    private readonly DataImportConfiguration _importConfig;
    private readonly Guid? _processId;
    private readonly bool _dryRun;
    private int? _maxParallelization = null;

    private readonly ReadOnlyDictionary<string, string[]> _options;

    public event EventHandler<CommunicationEventArgs>? Communicate;

    public YahooImporter(DbDef dbDef, DataImportConfiguration importConfig,
        Guid? processId = null, bool dryRun = false)
    {
        _dbDef = dbDef;
        _importConfig = importConfig;
        _processId = processId;
        _dryRun = dryRun;
        _dbContext = DbContextFactory.Create(dbDef);
        _options = DataImportConfiguration.CreateDictionary(importConfig.Options);

        ConfigureOptions(_options);
    }

    public const string SourceName = "yahoo";
    public string Source => SourceName;

    public (bool IsDangerous, string[] DangerMessages) ContainsDanger()
    {
        return (false, Array.Empty<string>());
    }

    public void Dispose()
    {
        // nothing to dispose
    }

    public Task<string> GetInfoAsync()
    {
        return Task.FromResult("Not implemented.");
    }

    public async Task<TimeSpan> ImportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch timer = Stopwatch.StartNew();

        FileInfo symbolsFileInfo = new FileInfo(Path.Combine(
            new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName ?? "", "data", "us_symbols.txt"));

        if (!symbolsFileInfo.Exists)
        {
            throw new Exception("Could not find us_symbols.txt");
        }

        var symbols = File.ReadAllLines(symbolsFileInfo.FullName);

        if (_maxParallelization.GetValueOrDefault() > 1)
        {
            await Parallel.ForEachAsync(symbols, new ParallelOptions()
            {
                MaxDegreeOfParallelism = _maxParallelization.GetValueOrDefault()
            }, async (symbol, ct) =>
            {
                if (_dryRun)
                {
                    Communicate?.Invoke(this, new CommunicationEventArgs(symbol, nameof(YahooImporter)));
                }
                else
                {
                    await ImportSymbolAsync(symbol, cancellationToken);
                }
            });
        }
        else
        {
            foreach (var symbol in symbols)
            {
                if (_dryRun)
                {
                    Communicate?.Invoke(this, new CommunicationEventArgs(symbol, nameof(YahooImporter)));
                }
                else
                {
                    await ImportSymbolAsync(symbol, cancellationToken);
                }
            }
        }

        timer.Stop();
        return timer.Elapsed;
    }

    private async Task ImportSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var chart = await YahooFinanceApi.Yahoo.GetHistoricalAsync(symbol.Trim().ToUpper());

            Communicate?.Invoke(this, new CommunicationEventArgs(symbol, null));

            var daos = chart.Select(c => new Database.DataAccessObjects.AdjustedEodPrice(SourceName, symbol, _processId)
            {
                Source = SourceName,
                Code = symbol,
                DateEod = DateOnly.FromDateTime(c.DateTime),
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume,
                Factor = 1D,
                ProcessId = _processId
            });

            await _dbContext.ExecuteAsync(_dbContext.Sql.AdjustedEodPrices.Upsert, daos, cancellationToken: cancellationToken);
        }
        catch (Exception exc)
        {
            KLogger.LogError(exc, nameof(YahooImporter), _processId);
        }
    }

    private void CommunicateAction(string message)
    {
        message = _dryRun ? $"{message} (dry run)" : message;

        Communicate?.Invoke(this, new CommunicationEventArgs(message, nameof(EodHdImporter)));
    }

    private void ConfigureOptions(ReadOnlyDictionary<string, string[]> options)
    {
        if (options.TryGetValue(Constants.OptionKeys.MaxParallelization, out string[]? value) && value.Length != 0)
        {
            if (int.TryParse(value[0], out int maxP))
            {
                _maxParallelization = maxP;
            }
        }
    }

    public static class Constants
    {
        public static class OptionKeys
        {
            public const string MaxParallelization = "Max Parallelization";
        }
    }

    public class ImportConfigfile(IDictionary<string, string>? options)
    {
        public IDictionary<string, string>? Options { get; set; } = options;
    }

    public class DataImportConfiguration(IDictionary<string, string>? options)
    {
        public IDictionary<string, string>? Options { get; } = options;

        internal static ReadOnlyDictionary<string, string[]> CreateDictionary(IDictionary<string, string>? dict)
        {
            var result = new Dictionary<string, string[]>(dict?.Keys.Count ?? 0);

            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    string val = kvp.Value;
                    string[] vals = string.IsNullOrWhiteSpace(val)
                        ? []
                        : val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                    result.Add(kvp.Key.Trim(), vals);
                }
            }

            return new ReadOnlyDictionary<string, string[]>(result);
        }
    }
}
