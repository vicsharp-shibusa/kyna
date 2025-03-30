using Kyna.ApplicationServices.Backtests.Runners;
using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.Common;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

ILogger<Program>? logger = null;
IConfiguration? configuration;
BacktestingService? backtestingService = null;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

Stopwatch timer = Stopwatch.StartNew();

Config? config = null;

try
{
    HandleArguments(args);
    ValidateArgsAndSetDefaults();
    Configure();

    Debug.Assert(config != null);

    if (config.ShowHelp)
    {
        ShowHelp();
    }
    else if (config.ListProcessIds)
    {
        Debug.Assert(backtestingService != null);
        var processInfo = (await backtestingService.GetBacktestProcessesAsync()).ToArray();

        if (processInfo.Length == 0)
        {
            Communicate("No processes found", true);
        }
        else
        {
            foreach (var p in processInfo)
            {
                Communicate(p.ToString(), true);
            }
        }
    }
    else
    {
        Debug.Assert(backtestingService != null);
        if (config.ProcessIdsToDelete.Any())
        {
            Communicate("Deleting backtests ...", true);
            await backtestingService.DeleteProcessesAsync([.. config.ProcessIdsToDelete]);
        }

        if (config.ConfigDir != null || config.ConfigFile != null)
        {
            KLogger.LogEvent(EventIdRepository.GetAppStartedEvent(config!), processId);
        }

        CancellationTokenSource cts = new();

        try
        {
            Debug.Assert(backtestingService != null);

            if (config.ConfigDir != null)
            {
                var files = config.ConfigDir.GetFiles("*.json", SearchOption.TopDirectoryOnly);
                if (files.Length == 0)
                {
                    throw new ArgumentException("No JSON configuration files found in directory.");
                }
                await backtestingService.ExecuteAsync(files);
            }
            if (config.ConfigFile != null)
            {
                await backtestingService.ExecuteAsync(config.ConfigFile);
            }
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
    if (config?.ConfigDir != null || config?.ConfigFile != null)
    {
        KLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);
    }

    if (backtestingService != null)
    {
        backtestingService.Communicate -= Backtests_Communicate;
    }

    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200); // give the logger a chance to catch up

    backtestingService?.Dispose();

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
        new CliArg(["-i","--input-dir"], ["directory"], false, "Directory of JSON import configuration files to process."),
        new CliArg(["-f", "--file"], ["configuration file"], false, "JSON import configuration file to process."),
        new CliArg(["-l", "--list"], [], false, "List process identifiers."),
        new CliArg(["-d", "--delete"], ["process id"], false, "Delete backtest, results, and stats for specified process id.")    ];

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
        "CLI for importing financial data.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    for (int a = 0; a < args.Length; a++)
    {
        string argument = args[a].ToLower();

        switch (argument)
        {
            case "-i":
            case "--input-dir":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"Expecting a directory name after {args[a]}");
                }
                config.ConfigDir = new DirectoryInfo(args[++a]);
                if (!config.ConfigDir.Exists)
                {
                    throw new ArgumentException("The specified directory does not exist.");
                }
                break;
            case "-d":
            case "--delete":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"Expecting a process id after {args[a]}");
                }
                if (Guid.TryParse(args[++a], out Guid pid))
                {
                    config.ProcessIdsToDelete.Add(pid);
                }
                else
                {
                    throw new ArgumentException($"'{args[a]}' is not a valid process id.");
                }
                break;
            case "-l":
            case "--list":
                config.ListProcessIds = true;
                break;
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

    if (!config.ShowHelp)
    {
        if (!config.ListProcessIds && config.ProcessIdsToDelete.Count == 0 &&
            config.ConfigDir == null && config.ConfigFile == null)
        {
            throw new ArgumentException("Either a configuration file or directory is required.");
        }
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
    var finDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials);
    var bckDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Backtests);

    backtestingService = new BacktestingService(finDef, bckDef);
    if (backtestingService == null)
    {
        throw new Exception($"Could not instantiate {nameof(BacktestingService)}");
    }
    backtestingService.Communicate += Backtests_Communicate;

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);
}

void Backtests_Communicate(object? sender, Kyna.Infrastructure.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description)
    : CliConfigBase(appName, appVersion, description)
{
    public DirectoryInfo? ConfigDir { get; set; }
    public FileInfo? ConfigFile { get; set; }
    public bool ListProcessIds { get; set; }
    public IList<Guid> ProcessIdsToDelete { get; set; } = new List<Guid>(10);
}
