using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.DataManagement;
using Kyna.Common;
using Kyna.Common.Logging;
using Kyna.Infrastructure.DataImport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

ILogger<Program>? logger = null;
IConfiguration? configuration;

int exitCode = -1;

bool acceptDanger = false;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

IExternalDataImporter? importer = null;

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
    else if (config.ShowInfo)
    {
        ValidateArgsAndSetDefaults();

        Configure();

        Debug.Assert(importer != null);

        Communicate((await importer.GetInfoAsync()), true);
    }
    else
    {
        ValidateArgsAndSetDefaults();

        Configure();

        Debug.Assert(importer != null);

        KLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);

        CancellationTokenSource cts = new();

        TimeSpan duration = TimeSpan.Zero;

        var (IsDangerous, DangerMessages) = importer.ContainsDanger();

        acceptDanger = acceptDanger || !IsDangerous;

        if (!acceptDanger)
        {
            if (IsDangerous)
            {
                bool? allowDanger = null;
                foreach (var message in DangerMessages)
                {
                    allowDanger = allowDanger.HasValue ? allowDanger.Value && CliHelper.ConfirmActionWithUser(message)
                        : CliHelper.ConfirmActionWithUser(message);
                }

                acceptDanger = allowDanger.GetValueOrDefault();
            }
        }
        else
        {
            acceptDanger = true;
        }

        if (!acceptDanger)
        {
            Communicate($"{Environment.NewLine}Process halted at user's request.", true);
        }
        else
        {
            try
            {
                duration = await importer.ImportAsync(cts.Token);
            }
            catch (AggregateException ex)
            {
                cts.Cancel(true);

                foreach (var e in ex.InnerExceptions)
                {
                    Communicate(e.ToString(), true, LogLevel.Error);
                }
            }
            catch (ApiLimitReachedException)
            {
                Communicate("API credit limit reached; halting processing", false, LogLevel.Warning);
            }
            finally
            {
                Communicate($"{Environment.NewLine}Import for '{importer.Source}' using file '{config.ConfigFile?.Name}' completed in {duration.ConvertToText()}");

                importer.Dispose();
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

    if (importer != null)
    {
        importer.Communicate -= Importer_Communicate;
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
        new CliArg(["-s", "--source"], ["source name"], false, $"Source for import. When excluded, defaults to {ImporterFactory.DefaultSource}"),
        new CliArg(["--dry-run"], [], false, "Executes a 'dry run' - reports only what the app would do with the specified configuration."),
        new CliArg(["--info", "--show-info"], [], false, "Show source-specific information."),
        new CliArg(["-y"], [], false, "Accept danger automatically.")
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
    {
        throw new Exception("Logic error; configuration was not created.");
    }

    if (config.ConfigFile == null && !config.ShowInfo)
    {
        throw new ArgumentException("A configuration file is required. See --help.");
    }

    if (string.IsNullOrWhiteSpace(config.Source))
    {
        config.Source = ImporterFactory.DefaultSource;
    }

    if (config.ShowInfo)
    {
        config.DryRun = config.ShowHelp = false;
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

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);

    config.ApiKey = configuration.GetSection($"ApiKeys:{config.Source}").Value;

    importer = ImporterFactory.Create(config!.Source?.ToLower() ?? "", importDef,
        config.ConfigFile, config.ApiKey, processId, config.DryRun);

    if (importer == null)
    {
        throw new Exception($"Unable to instantiate {config.Source} importer.");
    }

    importer!.Communicate += Importer_Communicate;
}

void Importer_Communicate(object? sender, Kyna.Common.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description, bool showInfo = false, bool dryRun = false)
    : CliConfigBase(appName, appVersion, description)
{
    public bool ShowInfo { get; set; } = showInfo;

    public bool DryRun { get; set; } = dryRun;

    public FileInfo? ConfigFile { get; set; }

    public string? Source { get; set; }

    public string? ApiKey { get; set; }
}
