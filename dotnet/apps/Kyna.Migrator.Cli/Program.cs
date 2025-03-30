using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.DataManagement;
using Kyna.Common;
using Kyna.Infrastructure.Logging;
using Kyna.Infrastructure.DataMigration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

ILogger<Program>? logger = null;
IConfiguration? configuration;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

IImportsMigrator? migrator = null;

Stopwatch timer = Stopwatch.StartNew();

Config? config = null;

try
{
    HandleArguments(args);

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
        {
            Console.WriteLine(await migrator.GetInfoAsync());
        }
        else
        {
            KLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);

            CancellationTokenSource cts = new();

            TimeSpan duration = TimeSpan.Zero;

            try
            {
                duration = await migrator.MigrateAsync(cts.Token);
            }
            catch (AggregateException ex)
            {
                cts.Cancel(true);

                foreach (var e in ex.InnerExceptions)
                {
#if DEBUG
                    Communicate(e.ToString(), true, LogLevel.Error);
#else
                    Communicate(e.Message, true, LogLevel.Error);
#endif
                }
            }
            finally
            {
                Communicate($"{Environment.NewLine}Migration using file '{config.ConfigFile?.Name}' completed in {duration.ConvertToText()}");

                cts.Dispose();
            }
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

    KLogger.LogCritical(exc, appName, processId);
}
catch (Exception exc)
{
    exitCode = 2;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif

    KLogger.LogCritical(exc, appName, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false))
    {
        KLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);
    }

    if (migrator != null)
    {
        migrator.Communicate -= Migrator_Communicate;
    }

    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200); // give the logger a chance to catch up

    Environment.Exit(exitCode);
}

void Communicate(string? message, bool force = false, LogLevel logLevel = LogLevel.None,
    string? scope = null)
{
    if (force || (config?.Verbose ?? false))
    {
        Console.WriteLine(message);
    }

    if (!string.IsNullOrEmpty(message))
    {
        KLogger.Log(logLevel, message, scope ?? appName, processId);
    }
}

void ShowHelp()
{
    CliArg[] localArgs = [
        new CliArg(["-f", "--file"], ["configuration file"], true, "JSON import configuration file to process."),
        new CliArg(["--dry-run"], [], false, "Executes a 'dry run' - reports only what the app would do with the specified configuration."),
        new CliArg(["--info","--show-info"], [], false, "Displays summary info from the provided configuration file.")
    ];

    CliArg[] args = localArgs.Union(CliHelper.GetDefaultArgDescriptions()).ToArray();

    Communicate($"{config.AppName} {config.AppVersion}".Trim(), true);
    Communicate(null, true);
    if (!string.IsNullOrWhiteSpace(config.Description))
    {
        Communicate(config.Description, true);
        Communicate(null, true);
    }
    Communicate(CliHelper.FormatArguments(args), true);
}

void HandleArguments(string[] args)
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
    {
        throw new Exception("Logic error; configuration was not created.");
    }

    if (config.ConfigFile == null)
    {
        throw new ArgumentException($"A configuration file is required; use -f <file name>.");
    }

    if (config.DryRun)
    {
        config.Verbose = true;
    }
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

    migrator = MigratorFactory.Create(importDef, finDef, config.ConfigFile!, processId, config.DryRun);

    if (migrator == null)
    {
        throw new Exception($"Unable to instantiate importer.");
    }

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
