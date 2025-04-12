using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.DataManagement;
using Kyna.Common;
using Kyna.Infrastructure.DataMigration;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

IConfiguration? configuration;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

IImportsMigrator? migrator = null;

Config? config = null;

TimeSpan duration = TimeSpan.Zero;

Stopwatch timer = Stopwatch.StartNew();

CancellationTokenSource cts = new();


try
{
    ParseArguments(args);

    Debug.Assert(config != null);

    if (config.ShowHelp)
    {
        ShowHelp();
    }
    else
    {
        ValidateArgsAndSetDefaults();
        Configure();

        Debug.Assert(migrator != null);

        if (config.ShowInfo)
            Console.WriteLine(migrator.GetInfo());
        else
        {
            KyLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);

            duration = await migrator.MigrateAsync(cts.Token);
            Communicate($"{Environment.NewLine}Migration completed in {duration.ConvertToText()}.");
        }
    }

    exitCode = 0;
}
catch (ArgumentException exc)
{
    exitCode = 1;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif

    KyLogger.LogCritical(exc, appName, processId);
}
catch (Exception exc)
{
    exitCode = 2;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif

    KyLogger.LogCritical(exc, appName, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false))
        KyLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);

    if (migrator != null)
    {
        migrator.Communicate -= Migrator_Communicate;
        migrator.Dispose();
    }

    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

    cts?.Dispose();

    Environment.Exit(exitCode);
}

void Communicate(string? message, bool force = false, LogLevel logLevel = LogLevel.None,
    string? scope = null)
{
    if (force || (config?.Verbose ?? false))
        Console.WriteLine(message);

    if (!string.IsNullOrEmpty(message))
        KyLogger.Log(logLevel, message, scope ?? appName, processId);
}

void ShowHelp()
{
    CliArg[] localArgs = [
        new CliArg(["-f", "--file"], ["configuration file"], true, "JSON import configuration file to process."),
        new CliArg(["--dry-run"], [], false, "Executes a 'dry run' - reports only what the app would do with the specified configuration."),
        new CliArg(["--info","--show-info"], [], false, "Displays summary info from the provided configuration file.")
    ];

    CliArg[] args = [.. localArgs.Union(CliHelper.GetDefaultArgDescriptions())];

    Communicate($"{config.AppName} {config.AppVersion}".Trim(), true);
    Communicate(null, true);
    if (!string.IsNullOrWhiteSpace(config.Description))
    {
        Communicate(config.Description, true);
        Communicate(null, true);
    }
    Communicate(CliHelper.FormatArguments(args), true);
}

void ParseArguments(string[] args)
{
    config = new Config(Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Program), "v1",
        "CLI for migrating from the imports database to the financials database.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    for (int a = 0; a < args.Length; a++)
    {
        string argument = args[a].ToLower();

        switch (argument)
        {
            case "-f":
            case "--file":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"A path to a configuration file is required after {args[a]}");
                }
                config.ConfigFile = new FileInfo(args[++a]);

                if (!config.ConfigFile.Exists)
                {
                    throw new ArgumentException("The specified configuration file does not exist.");
                }
                break;
            case "--dry-run":
                config.DryRun = true;
                break;
            case "--info":
            case "--show-info":
                config.ShowInfo = true;
                break;
            default:
                throw new Exception($"Unknown argument: {args[a]}");
        }
    }
}

void ValidateArgsAndSetDefaults()
{
    if (config == null)
        throw new Exception("Logic error; configuration was not created.");

    if (config.ConfigFile == null)
        throw new ArgumentException($"A configuration file is required; use -f <file name>.");

    if (config.DryRun)
        config.Verbose = true;
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

    if (logDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Logs)} db connection; no '{ConfigKeys.DbKeys.Logs}' key found.");

    if (importDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Imports)} db connection; no '{ConfigKeys.DbKeys.Imports}' key found.");

    if (finDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Financials)} db connection; no '{ConfigKeys.DbKeys.Financials}' key found.");

    var logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KyLogger.SetLogger(logger);

    migrator = MigratorFactory.Create(importDef, finDef, config.ConfigFile!, processId, config.DryRun);

    if (migrator == null)
        throw new Exception($"Unable to instantiate importer.");

    migrator!.Communicate += Migrator_Communicate;
}

void Migrator_Communicate(object? sender, Kyna.Infrastructure.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description, bool dryRun = false)
    : CliConfigBase(appName, appVersion, description)
{
    public bool ShowInfo { get; set; }
    public bool DryRun { get; set; } = dryRun;
    public FileInfo? ConfigFile { get; set; }
}
