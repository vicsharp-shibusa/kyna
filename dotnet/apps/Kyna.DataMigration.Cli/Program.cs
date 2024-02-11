using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.Common;
using Kyna.Common.Logging;
using Kyna.Infrastructure.Database;
using Kyna.Infrastructure.DataImport;
using Kyna.Infrastructure.DataMigration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

ILogger<Program>? logger = null;
IConfiguration? configuration;

MigrationConfiguration? migrationConfigfile;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

ImportsMigrator? migrator = null;

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

        ShowConfiguration();

        Debug.Assert(migrator != null);

        KLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);

        CancellationTokenSource cts = new();

        TimeSpan duration = TimeSpan.Zero;

        try
        {
            duration = migrator.Migrate();
        }
        catch (AggregateException ex)
        {
            cts.Cancel(true);

            foreach (var e in ex.InnerExceptions)
            {
                Communicate(e.ToString(), true, LogLevel.Error);
            }
        }
        finally
        {
            Communicate($"{Environment.NewLine}Migration using file '{config.ConfigFile?.Name}' completed in {duration.ConvertToText()}");

            cts.Dispose();
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

void ShowConfiguration()
{
    if (migrationConfigfile != null)
    {
        Communicate($"Source               : {migrationConfigfile.Source}");
        Communicate($"Categories           : {string.Join(", ", migrationConfigfile.Categories)}");
        Communicate($"Mode                 : {migrationConfigfile.Mode.GetEnumDescription()}");
        Communicate($"Source Deletion Mode : {migrationConfigfile.SourceDeletionMode.GetEnumDescription()}");
        Communicate($"Max Parallelization  : {migrationConfigfile.MaxParallelization}");
        Communicate($"Adjusted Price Mode  : {migrationConfigfile.AdjustedPriceModes.GetEnumDescription()}");

    }
}

void ShowHelp()
{
    CliArg[] localArgs = [
        new CliArg(["-f", "--file"], ["configuration file"], true, "JSON import configuration file to process."),
        new CliArg(["-s", "--source"], ["source name"], false, $"Source for import. When excluded, defaults to {EodHdImporter.SourceName}"),
        new CliArg(["--dry-run"], [], false, "Executes a 'dry run' - reports only what the app would do with the specified configuration.")
    ];

    CliArg[] args = CliHelper.GetDefaultArgDescriptions().Union(localArgs).ToArray();

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
        "CLI for importing financial data.");

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
            case "-s":
            case "--source":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"A source name is required after {args[a]}");
                }
                config.Source = args[++a];
                break;

            case "--dry-run":
                config.DryRun = true;
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

    if (string.IsNullOrWhiteSpace(config.Source))
    {
        config.Source = EodHdImporter.SourceName;
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

    ConfigureMigrator(importDef, finDef);

    if (migrator == null)
    {
        throw new Exception($"Unable to instantiate {config.Source} importer.");
    }

    migrator!.Communicate += Migrator_Communicate;
}

void ConfigureMigrator(DbDef importDef, DbDef finDef)
{
    var options = JsonOptionsRepository.DefaultSerializerOptions;
    options.Converters.Add(new EnumDescriptionConverter<MigrationMode>());
    options.Converters.Add(new EnumDescriptionConverter<SourceDeletionMode>());
    options.Converters.Add(new EnumDescriptionConverter<AdjustedPriceModes>());

    migrationConfigfile = JsonSerializer.Deserialize<MigrationConfiguration>(
        File.ReadAllText(config.ConfigFile!.FullName), options);

    Debug.Assert(migrationConfigfile != null);

    migrator = new ImportsMigrator(importDef, finDef, migrationConfigfile, processId, config.DryRun);
}

void Migrator_Communicate(object? sender, Kyna.Common.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description, bool dryRun = false)
    : CliConfigBase(appName, appVersion, description)
{
    public bool DryRun { get; set; } = dryRun;

    public FileInfo? ConfigFile { get; set; }

    public string? Source { get; set; }
}
