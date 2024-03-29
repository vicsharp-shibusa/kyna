﻿using Kyna.Analysis.Technical;
using Kyna.ApplicationServices.Analysis;
using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.Logging;
using Kyna.Backtests;
using Kyna.Common;
using Kyna.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

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
        int num = 1;
        bool onlySignalWithMarket = false;

        var options = JsonOptionsRepository.DefaultSerializerOptions;
        options.Converters.Add(new EnumDescriptionConverter<PricePoint>());
        options.Converters.Add(new EnumDescriptionConverter<BacktestType>());
        options.WriteIndented = true;

        MarketConfiguration marketConfiguration = new MarketConfiguration()
        {
            Codes = ["SPY.US", "QQQ.US", "DIA.US"],
            Trends = [
                new TrendConfiguration()
                {
                    Trend = "S50C"
                }
            ]
        };

        foreach (var move in new double[] { .1, .2 })
        {
            foreach (var len in new int[] { 15, 30 })
            {
                foreach (var vol in new double[] { 1D, 1.5D, 2D })
                {
                    foreach (var trendDesc in new string[] { "S21C", "S50C", "S200C", "E21C", "E50C", "E200C" })
                    {
                        foreach (var useMarket in new bool[] { true, false })
                        {
                            ChartConfiguration chartConfig = new()
                            {
                                Interval = "Daily",
                                Trends = [new TrendConfiguration() { Trend = trendDesc }]
                            };

                            string[] descItems = [
                                $"move: {move}",
                                $"prologue len: {len}",
                                $"trend desc: {trendDesc}",
                                $"vol factor: {vol}",
                                $"use market: {useMarket}"
                            ];

                            var backtestConfig = new BacktestingConfiguration(BacktestType.CandlestickPattern,
                                "eodhd.com",
                                $"Bullish Engulfing {num}",
                                string.Join(';', descItems),
                                PricePoint.Close,
                                new TestTargetPercentage(PricePoint.High, move),
                                new TestTargetPercentage(PricePoint.Low, move),
                                [SignalName.BullishEngulfing.GetEnumDescription()],
                                len,
                                vol,
                                10,
                                onlySignalWithMarket,
                                chartConfig,
                                useMarket ? marketConfiguration : null);

                            var btJson = JsonSerializer.Serialize(backtestConfig, options);

                            var fileName = Path.Combine("\\repos\\kyna-ins-and-outs\\configuration\\bullish-engulfing", 
                                $"bullish-engulfing-{num.ToString().PadLeft(4,'0')}.json");
                            File.WriteAllText(fileName, btJson);
                            Console.WriteLine(fileName);
                            num++;
                        }
                    }
                }
            }
        }
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
    CliArg[] args = CliHelper.GetDefaultArgDescriptions();

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