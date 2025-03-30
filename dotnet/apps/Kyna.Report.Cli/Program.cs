using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.Reports;
using Kyna.Common;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

ILogger<Program>? logger = null;
IConfiguration? configuration;
ReportService? reportService = null;

int exitCode = -1;

DirectoryInfo? outputDir = null;
ReportOptions? reportOptions = null;
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
    Debug.Assert(reportService != null);

    if (config.ShowHelp)
    {
        ShowHelp();
    }
    else if (config.ListProcessIds)
    {
        var processInfo = (await reportService.GetBacktestProcessesAsync()).ToArray();

        if (processInfo.Length == 0)
        {
            Communicate("No processes found");
        }
        else
        {
            foreach (var p in processInfo)
            {
                Communicate(p.ToString());
            }
        }
    }
    else
    {
        if (config.ProcessIdsToDelete.Any())
        {
            Communicate("Deleting backtests ...");
            await reportService.DeleteProcessesAsync([.. config.ProcessIdsToDelete]);
        }

        if (config.StatsReport)
        {
            Communicate($"Generating stats report for {processId}");

            foreach (var filename in reportService.CreateBacktestingCsvReports(processId, outputDir!.FullName))
            {
                Communicate(filename);
            }
        }

        if (config.SplitsCompare)
        {
            Communicate($"Generating splits report");

            foreach (var filename in reportService.CreateSplitsComparisonCsvReport(outputDir!.FullName))
            {
                Communicate(filename);
            }
        }

        if (config.ChartsCompare)
        {
            Communicate($"Generating chart comparison report");

            foreach (var filename in await reportService.CreateChartComparisonCsvReportAsync(outputDir!.FullName))
            {
                Communicate(filename);
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
    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

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
        new CliArg(["--stats"], [], false, "Generate the backtesting stats report."),
        new CliArg(["--splits"], [], false, "Generate a report comparing splits between data providers."),
        new CliArg(["--compare-charts"], [], false, "Generate a report that compares adjusted charts between data sources."),
        new CliArg(["-o", "--output", "--output-dir"], ["output directory"], true, "Set (or create) output directory."),
        new CliArg(["-p", "--process", "--process-id"], ["process id"], true, "Filter report by specified process id."),
        new CliArg(["-l", "--list"], [], false, "List process identifiers."),
        new CliArg(["-d", "--delete"], ["process id"], false, "Delete backtest, results, and stats for specified process id.")
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
        "CLI for generating reports from Kyna data sources.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    for (int a = 0; a < args.Length; a++)
    {
        string argument = args[a].ToLower();

        switch (argument)
        {
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
            case "--stats":
                config.StatsReport = true;
                break;
            case "--compare-charts":
                config.ChartsCompare = true;
                break;
            case "--splits":
                config.SplitsCompare = true;
                break;
            case "-p":
            case "--process":
            case "--process-id":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"Expecting a process id after {args[a]}");
                }
                if (!Guid.TryParse(args[++a], out processId))
                {
                    throw new ArgumentException($"{args[a]} is not a valid process id.");
                }
                break;
            case "-o":
            case "--output":
            case "--output-dir":
                if (a == args.Length - 1)
                {
                    throw new ArgumentException($"A path to the output directory is required after {args[a]}");
                }
                outputDir = new DirectoryInfo(args[++a]);
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

    if (!config.ShowHelp && !config.ListProcessIds && config.ProcessIdsToDelete.Count == 0)
    {
        if (!config.StatsReport && !config.SplitsCompare && !config.ChartsCompare)
        {
            throw new ArgumentException("There are no reports specified");
        }

        if (outputDir == null)
        {
            throw new ArgumentException("Missing argument: -o <output directory>.");
        }

        if (!outputDir.Exists)
        {
            outputDir.Create();
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

    reportOptions = new ReportOptions();
    configuration.GetSection("Report Options").Bind(reportOptions);

    var dbDefs = CliHelper.GetDbDefs(configuration);

    var logDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Logs);
    var finDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials);
    var bckDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Backtests);

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);

    reportService = new ReportService(bckDef, finDef, reportOptions);
    reportService.Communicate += ReportService_Communicate;
}

void ReportService_Communicate(object? sender, Kyna.Infrastructure.Events.CommunicationEventArgs e)
{
    Communicate(e.Message, scope: e.Scope);
}

class Config(string appName, string appVersion, string? description)
    : CliConfigBase(appName, appVersion, description)
{
    public bool SplitsCompare { get; set; }
    public bool StatsReport { get; set; }
    public bool ChartsCompare { get; set; }
    public bool ListProcessIds { get; set; }
    public IList<Guid> ProcessIdsToDelete { get; set; } = new List<Guid>(10);
}
