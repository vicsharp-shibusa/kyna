using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.Infrastructure.DataImport;
using Kyna.ApplicationServices.Logging;
using Kyna.Common;
using Kyna.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using Kyna.ApplicationServices.Analysis;
using System.Collections.ObjectModel;
using Kyna.Analysis.Technical;

ILogger<Program>? logger = null;
IConfiguration? configuration;

ChartFactory chartFactory;
SymbolRepository symbolRepository;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string appName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new Exception("Could not determine app name.");
Debug.Assert(appName != null);

string defaultScope = appName ?? nameof(Program);

DatabaseLogService? dbLogService = null;
ApiTransactionService? apiTransactionService = null;

Stopwatch timer = Stopwatch.StartNew();

Config? config = null;

try
{
    Configure();

    config = HandleArguments(args);

    if (config.ShowHelp)
    {
        ShowHelp();
    }
    else
    {
        if (!Directory.Exists("output"))
        {
            Directory.CreateDirectory("output");
        }
        var stream = File.Create(Path.Combine("output", "candle_patterns.csv"));
        string source = "eodhd.com";
        var symbols = await symbolRepository.GetAllAdjustedSymbolsForSourceAsync("eodhd.com");

        List<Pattern> patterns = [
            new("IsDoji", c => c.IsDoji),
            new("IsLongLeggedDoji", c => c.IsLongLeggedDoji),
            new("IsDragonflyDoji", c => c.IsDragonflyDoji),
            new("IsGravestoneDoji", c => c.IsGravestoneDoji),
            new("IsFourPriceDoji", c => c.IsFourPriceDoji),
            new("IsBullishBelthold", c => c.IsBullishBelthold),
            new("IsBearishBelthold", c => c.IsBearishBelthold),
            new("IsBullisht pMarubozu", c => c.IsBullishMarubozu),
            new("IsBearishMarubozu", c => c.IsBearishMarubozu),
            new("IsUmbrella", c => c.IsUmbrella),
            new("IsInvertedUmbrella", c => c.IsInvertedUmbrella),
            new("IsSpinningTop", c => c.IsSpinningTop),
        ];

        Dictionary<string, Collection<(string Symbol, DateOnly Date)>> dict = new(10_000);

        string[] desiredSymbols = [ "AAPL.US", "MSFT.US", "TSLA.US", "NFLX.US",
            "AMZN.US", "NVDA.US", "GOOGL.US", "META.US", "UNH.US", "JPM.US",
            "V.US", "COST.US", "ADBE.US", "PFE.US", "BAC.US", "ACN.US", "ORCL.US",
            "IBM.US", "HON.US", "DE.US", "T.US", "MO.US", "LULU.US", "MMM.US", "F.US",
            "EBAY.US"
        ];

        foreach (var symbol in symbols.Where(s => desiredSymbols.Contains(s)))
        {
            Console.WriteLine(symbol);
            var chart = chartFactory.CreateCandlestick(source, symbol, new DateOnly(2000, 1, 1), new DateOnly(2022, 12, 31));

            using var chartFile = File.Create(Path.Combine("output", $"{symbol}.txt"));
            foreach (var item in chart.Candlesticks)
            {
                chartFile.WriteLine(item.ToString());
            }
            chartFile.Flush();
            chartFile.Close();

            foreach (var pattern in patterns)
            {
                var matches = chart.Candlesticks.Where(pattern.Func).ToArray();

                foreach (var match in matches)
                {
                    if (!dict.TryGetValue(pattern.Key, out Collection<(string Symbol, DateOnly Date)>? value))
                    {
                        value = ([]);
                        dict.Add(pattern.Key, value);
                    }

                    value.Add((match.Symbol, match.Date));
                }
            }
        }

        foreach (var pattern in patterns)
        {
            Console.WriteLine(pattern.Key);
            if (!dict.TryGetValue(pattern.Key, out Collection<(string Symbol, DateOnly Date)>? value) || value.Count == 0)
            {
                stream.WriteLine(pattern.Key);
            }
            else
            {
                DateOnly firstDate = new(2020, 1, 1);
                foreach (var (Symbol, Date) in value) //.Where(d => d.Date >= firstDate))
                {
                    stream.WriteLine($"{pattern.Key},{Symbol},{Date}");
                }
            }
        }

        stream.Flush();
        stream.Close();
    }
    exitCode = 0;
}
catch (ArgumentException exc)
{
    exitCode = 1;
    Communicate(exc.ToString(), true);
    KLogger.LogCritical(exc, defaultScope, processId);
}
catch (Exception exc)
{
    exitCode = 2;
    Communicate(exc.ToString(), true);
    KLogger.LogCritical(exc, defaultScope, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false))
    {
        // test log finished event.
        KLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);
    }

    timer.Stop();

    Communicate($"Completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200);

    Environment.Exit(exitCode);
}

void Communicate(string message, bool force = false, LogLevel logLevel = LogLevel.None,
    string? scope = null)
{
    if (force || (config?.Verbose ?? false))
    {
        Console.WriteLine(message);
    }

    KLogger.Log(logLevel, message, scope ?? defaultScope, processId);
}

void ShowHelp()
{
    CliArg[] args = CliHelper.GetDefaultArgDescriptions().Union([]).ToArray();

    Communicate($"{config.AppName} {config.AppVersion}".Trim(), true);
    Communicate("", true);
    if (!string.IsNullOrWhiteSpace(config.Description))
    {
        Communicate(config.Description, true);
        Communicate("", true);
    }
    Communicate(CliHelper.FormatArguments(args), true);
}

Config HandleArguments(string[] args)
{
    var config = new Config(Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Program), "v1",
        "CLI for testing various things; a throw-away app.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    return config;
}

void Configure()
{
    IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

    configuration = builder.Build();

    var dbDefs = CliHelper.GetDbDefs(configuration);

    var logDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Logs);
    var importDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Imports);
    var finDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials);

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);

    dbLogService = new(logDef);
    apiTransactionService = new(importDef);
    chartFactory = new(finDef);
    symbolRepository = new(finDef);
}

class Config(string appName, string appVersion, string? description = null) : CliConfigBase(appName, appVersion, description)
{
}

class Pattern(string key, Func<Candlestick, bool> func)
{
    public string Key { get; } = key;
    public Collection<(string Symbol, DateOnly Date)> Candles { get; } = [];

    public Func<Candlestick, bool> Func { get; } = func;
}