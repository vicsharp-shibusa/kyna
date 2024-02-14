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

ILogger<Program>? logger = null;
IConfiguration? configuration;

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
        var text = File.ReadAllLines("C:\\Users\\Vic\\migrated_files.txt");

        await apiTransactionService!.DeleteTransactionsAsync(EodHdImporter.SourceName, EodHdImporter.Constants.Actions.EndOfDayPrices,
            text.Select(t => t.Trim()));
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
    CliArg[] args = CliHelper.GetDefaultArgDescriptions().Union(Array.Empty<CliArg>()).ToArray();

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

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);

    dbLogService = new(logDef);
    apiTransactionService = new(importDef);
}

class Config(string appName, string appVersion, string? description = null) : CliConfigBase(appName, appVersion, description)
{
}

