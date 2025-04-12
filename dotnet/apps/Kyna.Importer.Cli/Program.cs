using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.DataManagement;
using Kyna.Common;
using Kyna.Infrastructure.DataImport;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

IConfiguration? configuration;

int exitCode = -1;

bool acceptDanger = false;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

IExternalDataImporter? importer = null;

Config? config = null;
CancellationTokenSource cts = new();

Stopwatch timer = Stopwatch.StartNew();

try
{
    ParseArguments(args);

    Debug.Assert(config != null);

    if (config.ShowHelp)
    {
        ShowHelp();
        exitCode = 0;
        Environment.Exit(exitCode);
    }

    ValidateArgsAndSetDefaults();

    Configure();

    Debug.Assert(importer != null);

    if (config.ShowInfo)
    {
        Communicate((await importer.GetInfoAsync()), true);
        exitCode = 0;
        Environment.Exit(exitCode);
    }
    else
    {

        KyLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);

        TimeSpan duration = TimeSpan.Zero;

        var (IsDangerous, DangerMessages) = importer.ContainsDanger();

        acceptDanger = acceptDanger || !IsDangerous;

        if (!acceptDanger && IsDangerous)
        {
            bool? allowDanger = null;
            foreach (var message in DangerMessages)
            {
                allowDanger = allowDanger.HasValue
                    ? allowDanger.Value && CliHelper.ConfirmActionWithUser(message)
                    : CliHelper.ConfirmActionWithUser(message);
            }

            acceptDanger = allowDanger.GetValueOrDefault();
        }
        else
            acceptDanger = true;

        if (!acceptDanger)
            Communicate($"{Environment.NewLine}Process halted at user's request.", true);
        else
            duration = await importer.ImportAsync(cts.Token);
    }
    exitCode = 0;
}
catch (ArgumentException exc)
{
#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif

    KyLogger.LogCritical(exc, appName, processId);
    exitCode = 1;
}
catch (AggregateException ex)
{
    cts.Cancel(true);

    foreach (var e in ex.InnerExceptions)
    {
        Communicate(e.ToString(), true, LogLevel.Error);
    }
    exitCode = 2;
}
catch (ApiLimitReachedException)
{
    Communicate("API credit limit reached; halting processing", false, LogLevel.Warning);
}
catch (Exception exc)
{
    exitCode = 3;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif

    KyLogger.LogCritical(exc, appName, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false) && !(config?.ShowInfo ?? false))
        KyLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);

    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200); // give the logger a chance to catch up

    if (importer != null)
    {
        importer.Communicate -= Importer_Communicate;
        importer.Dispose();
    }

    cts.Dispose();

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
        new CliArg(["--info", "--show-info"], [], false, "Show source-specific information."),
        new CliArg(["-y"], [], false, "Accept danger automatically.")
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
            case "--dry-run":
                config.DryRun = true;
                break;
            case "--info":
            case "--show-info":
                config.ShowInfo = true;
                break;
            case "-y":
                acceptDanger = true;
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

    if (config.ConfigFile == null && !config.ShowInfo)
        throw new ArgumentException("A configuration file is required. See --help.");

    if (config.ShowInfo)
        config.DryRun = config.ShowHelp = false;

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

    if (logDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Logs)} db connection; no '{ConfigKeys.DbKeys.Logs}' key found.");

    var source = SourceUtility.GetSource(config.ConfigFile);
    if (source == "yahoo")
        importDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials);

    if (importDef == null)
        throw new Exception($"Unable to create {nameof(ConfigKeys.DbKeys.Imports)} db connection; no '{ConfigKeys.DbKeys.Imports}' key found.");

    var logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KyLogger.SetLogger(logger);

    config.ApiKey = configuration.GetSection($"ApiKeys:{source}").Value;
    config.AccessKey = configuration.GetSection($"AccessKeys:{source}").Value;

    importer = ImporterFactory.Create(importDef,
        config.ConfigFile, config.ApiKey, config.AccessKey, processId, config.DryRun);

    if (importer == null)
        throw new Exception($"Unable to instantiate {source} importer.");

    importer.Communicate += Importer_Communicate;
}

void Importer_Communicate(object? sender, Kyna.Infrastructure.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description, bool showInfo = false, bool dryRun = false)
    : CliConfigBase(appName, appVersion, description)
{
    public bool ShowInfo { get; set; } = showInfo;

    public bool DryRun { get; set; } = dryRun;

    public FileInfo? ConfigFile { get; set; }

    public string? ApiKey { get; set; }

    public string? AccessKey { get; set; }

    public DirectoryInfo? DownloadDirectory { get; set; }
}
