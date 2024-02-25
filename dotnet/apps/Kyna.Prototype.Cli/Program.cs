using Kyna.Analysis.Technical;
using Kyna.Analysis.Technical.Trends;
using Kyna.ApplicationServices.Analysis;
using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.Logging;
using Kyna.Common;
using Kyna.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

ILogger<Program>? logger = null;
IConfiguration? configuration;

FinancialsRepository financialsRepository;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string appName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new Exception("Could not determine app name.");
Debug.Assert(appName != null);

string defaultScope = appName ?? nameof(Program);

DatabaseLogService? dbLogService = null;

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
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "kyna-output");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var stream = File.Create(Path.Combine(outputPath, "candle_patterns.csv"));
        string source = "eodhd.com";
        //var symbols = await financialsRepository.GetAllAdjustedSymbolsForSourceAsync(source);

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

        Dictionary<string, Collection<(string Symbol, Candlestick Candle)>> dict = new(10_000);

        string[] desiredSymbols = [ "AAPL.US", "MSFT.US", "TSLA.US", "NFLX.US",
            "AMZN.US", "NVDA.US", "GOOGL.US", "META.US", "UNH.US", "JPM.US",
            "V.US", "COST.US", "ADBE.US", "PFE.US", "BAC.US", "ACN.US", "ORCL.US",
            "IBM.US", "HON.US", "DE.US", "T.US", "MO.US", "LULU.US", "MMM.US", "F.US",
            "EBAY.US"
        ];

        foreach (var symbol in desiredSymbols)
        {
            Console.WriteLine(symbol);

            var ohlc = (await financialsRepository.GetOhlcForSourceAndCodeAsync(source, symbol)).ToArray();

            var maTrend = new MovingAverageTrend(new MovingAverageKey(21), ohlc);
            var exTrend = new ExtremeTrend(ohlc);
            var wTrend1 = new WeightedTrend(maTrend, .6D);
            var wTrend2 = new WeightedTrend(exTrend, .4D);

            var blendedTrend = new CombinedTrend(wTrend1, wTrend2);

            var chart1 = new Chart()
                .WithPriceActions(ohlc)
                .WithCandles()
                .WithTrend(maTrend)
                .Build();

            var chart2 = new Chart()
                .WithPriceActions(ohlc)
                .WithCandles()
                .WithTrend(exTrend)
                .Build();

            var chart3 = new Chart()
                .WithPriceActions(ohlc)
                .WithCandles()
                .WithTrend(blendedTrend)
                .Build();

            using var trendFile = File.Create(Path.Combine(outputPath, $"{symbol}_trend.csv"));

            trendFile.WriteLine("Symbol,Date,Close,MA Trend,MA Value,Ex Trend,Ex Value,Blend Trend,Blend Value");
            for (int i = 0; i < chart1.PriceActions.Length; i++)
            {
                string[] trendItems = [
                    chart1.PriceActions[i].Symbol,
                    chart1.PriceActions[i].Date.ToString("yyyy-MM-dd"),
                    chart1.PriceActions[i].Close.ToString("#,##0.00"),
                    chart1.TrendValues[i].Sentiment.GetEnumDescription(),
                    chart1.TrendValues[i].Value.ToString(),
                    chart2.TrendValues[i].Sentiment.GetEnumDescription(),
                    chart2.TrendValues[i].Value.ToString(),
                    chart3.TrendValues[i].Sentiment.GetEnumDescription(),
                    chart3.TrendValues[i].Value.ToString(),
                ];
                trendFile.WriteLine(string.Join(',', trendItems));
            }

            trendFile.Flush();
            trendFile.Close();
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
    financialsRepository = new(finDef);
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