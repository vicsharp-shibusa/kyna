using Kyna.Analysis.Technical;
using Kyna.ApplicationServices.Analysis;
using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.Research;
using Kyna.Common;
using Kyna.Infrastructure.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

IConfiguration? configuration;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

Debug.Assert(appName != null);

Config? config = null;

bool patterns = false;

FinancialsRepository? financialsRepository = null;
ResearchStatsService? researchStatsService = null;
ResearchConfiguration? researchConfiguration = null;

Stopwatch timer = Stopwatch.StartNew();

try
{
    ParseArguments(args);
    ValidateArgsAndSetDefaults();
    Configure();

    Debug.Assert(config != null);

    if (config.ShowHelp)
    {
        ShowHelp();
        Environment.Exit(0);
    }

    if (patterns)
    {
        Debug.Assert(financialsRepository != null);
        Debug.Assert(researchStatsService != null);
        Debug.Assert(researchConfiguration != null);

        var src = researchConfiguration.Source;
        Debug.Assert(src != null);

        var mostRecentDate = await financialsRepository.GetMostRecentPriceActionDateAsync();
        Debug.Assert(mostRecentDate != DateOnly.MinValue);

        var codes = (await financialsRepository.GetCodesForDateAndPriceRangeAsync(mostRecentDate,
            5M, 500M)).ToArray();

        var buildId = await researchStatsService.CreateStatsBuildAsync(researchConfiguration, processId);

        //codes = ["APRP"];
        List<Task> tasks = new(codes.Length);
        int i = 0;
        foreach (var ticker in codes)
        {
            i++;
            Console.WriteLine($"{ticker}\t{i}\t/\t{codes.Length}");
            var chart = ChartFactory.Create(src, ticker, researchConfiguration.ChartConfiguration, null, null,
                (await financialsRepository.GetOhlcForSourceAndCodeAsync(src, ticker)).ToArray());

            var results = PatternService.FindRandom(chart).ToArray();

            tasks.Add(Parallel.ForEachAsync(results, new ParallelOptions()
            {
                MaxDegreeOfParallelism = researchConfiguration.MaxParallelization
            }, async (r, ct) =>
            {
                var deviation = (double)r.EpiloguePriceDeviation;
                await researchStatsService.SaveResearchStatAsync(processId, buildId,
                    ticker,
                    r.Date,
                    r.Type.GetEnumDescription(),
                    "Random",
                    deviation,
                    r.Meta.ToString(),
                    ct).ConfigureAwait(false);
            }));
        }
        Task.WaitAll(tasks);
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
    timer.Stop();

    Communicate($"{Environment.NewLine}{appName} completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200); // give the logger a chance to catch up

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
        new CliArg(["-p","--patterns"], [], false, "Build stats for patterns"),
        new CliArg(["-f", "--file"], ["configuration file"], false, "JSON import configuration file to process.")
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
        "CLI for creating research statistics.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    for (int a = 0; a < args.Length; a++)
    {
        string argument = args[a].ToLower();

        switch (argument)
        {
            case "-p":
            case "--patterns":
                patterns = true;
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

                researchConfiguration = ResearchConfiguration.Create(config.ConfigFile);

                Debug.Assert(researchConfiguration != null);
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
}

void Configure()
{
    IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

    configuration = builder.Build();

    var dbDefs = CliHelper.GetDbDefs(configuration);

    var logDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Logs)
        ?? throw new Exception("Logging db could not be defined.");
    var finDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Financials)
        ?? throw new Exception("Financials db could not be defined.");
    var bckDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Backtests)
        ?? throw new Exception("Backtest db could not be defined.");

    financialsRepository = new FinancialsRepository(finDef);
    researchStatsService = new ResearchStatsService(bckDef);

    var logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KyLogger.SetLogger(logger);
}

class Config(string appName, string appVersion, string? description)
    : CliConfigBase(appName, appVersion, description)
{
    public FileInfo? ConfigFile { get; set; }
}
