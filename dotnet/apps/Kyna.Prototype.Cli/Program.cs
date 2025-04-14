﻿using Kyna.Analysis.Technical.Charts;
using Kyna.Analysis.Technical.Trends;
using Kyna.ApplicationServices.Analysis;
using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.Common;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

IConfiguration? configuration;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string appName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new Exception("Could not determine app name.");
Debug.Assert(appName != null);

string defaultScope = appName ?? nameof(Program);

Stopwatch timer = Stopwatch.StartNew();
Config? config = null;

DbDef? logDef = null;
DbDef? importDef = null;
DbDef? finDef = null;

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
        Debug.Assert(logDef is not null && importDef is not null && finDef is not null);

        // get chart and calculate trend.
        var financialsRepo = new FinancialsRepository(finDef);

        var aapl = (await financialsRepo.GetOhlcForSourceAndCodeAsync("polygon.io", "AAPL")).ToArray();

        MovingAverageKey[] maKeys = [
        new MovingAverageKey(21), new MovingAverageKey(50), new MovingAverageKey(200)
        ];

        List<WeightedTrend> trends = new(5)
        {
            new WeightedTrend(new ExtremeTrend(aapl), 0.50D), // good indicator.
            new WeightedTrend(new MultipleMovingAverageTrend(aapl, maKeys), 0.05D),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[0], aapl), 0.20D),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[1], aapl), 0.15D),
            new WeightedTrend(new PriceToMovingAverageTrend(maKeys[2], aapl), 0.10D)
        };

        var trend = new CombinedWeightedTrend(trends.ToArray());

        //var trend = new ExtremeTrend(aapl);

        //var trend = new MovingAverageTrend(maKeys[0], aapl);

        //var trend = new MultipleMovingAverageTrend(aapl, maKeys);
        //var trend = new PriceToMovingAverageTrend(maKeys[0], aapl);
        trend.Calculate();

        Debug.Assert(aapl.Length == trend.TrendValues.Length);

        // write output
        var rootFolder = Path.GetPathRoot(Environment.CurrentDirectory);
        Debug.Assert(rootFolder != null);
        var fullPath = Path.Combine(rootFolder, "temp", "kyna-tests");
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);

        // fullPath will be like "C:\Your\Path\Here"
        var outputFileFullName = Path.Combine(fullPath, $"{trend.GetType().Name}_{DateTime.Now:HHmmss}.csv");

        using var outputFile = File.Create(outputFileFullName);

        var headers = new string[] {
            "Date","Symbol","Open","High","Low","Close","Volume","Trend"
        };
        outputFile.WriteLine(string.Join(',', headers));
        for (int i = 0; i < aapl.Length; i++)
        {
            decimal percentPriceChange = 0M;
            double percentTrendChange = 0D;

            if (i > 0)
            {
                if (aapl[i - 1].Close != 0M)
                    percentPriceChange = (aapl[i].Close - aapl[i - 1].Close) / aapl[i - 1].Close;
                if (trend.TrendValues[i - 1] != 0D)
                    percentTrendChange = (trend.TrendValues[i] - trend.TrendValues[i - 1]) / trend.TrendValues[i - 1];
            }

            var lineItems = new string[] {
                aapl[i].Date.ToString("yyyy-MM-dd"),
                aapl[i].Symbol,
                aapl[i].Open.ToString("0.00"),
                aapl[i].High.ToString("0.00"),
                aapl[i].Low.ToString("0.00"),
                aapl[i].Close.ToString("0.00"),
                aapl[i].Volume.ToString("0.00"),
                trend.TrendValues[i].ToString("0.000")
            };
            outputFile.WriteLine(string.Join(',', lineItems));
        }
        outputFile.Flush();
    }
    exitCode = 0;
}
catch (ArgumentException exc)
{
    exitCode = 1;
    Communicate(exc.ToString(), true);
    KyLogger.LogCritical(exc, defaultScope, processId);
}
catch (Exception exc)
{
    exitCode = 2;
    Communicate(exc.ToString(), true);
    KyLogger.LogCritical(exc, defaultScope, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false))
    {
        // test log finished event.
        KyLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);
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

    KyLogger.Log(logLevel, message, scope ?? defaultScope, processId);
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

    logDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Logs) ?? logDef;
    importDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Imports) ?? importDef;
    finDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials) ?? finDef;

    if (logDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Logs)} db connection; no '{ConfigKeys.DbKeys.Logs}' key found.");

    if (importDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Imports)} db connection; no '{ConfigKeys.DbKeys.Imports}' key found.");

    if (finDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Financials)} db connection; no '{ConfigKeys.DbKeys.Financials}' key found.");

    var logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KyLogger.SetLogger(logger);
}

class Config(string appName, string appVersion, string? description = null) : CliConfigBase(appName, appVersion, description)
{
}
