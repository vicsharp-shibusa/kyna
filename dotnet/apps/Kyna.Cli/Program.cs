using Kyna.ApplicationServices.Cli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

IConfiguration? configuration;

int exitCode = -1;

string? appName = Assembly.GetExecutingAssembly().GetName().Name;

string[] backtestNames = ["backtest", "backtests", "backtesting"];
string[] importerNames = ["import", "importer", "imports"];
string[] migratorNames = ["migrate", "migrator", "migrates"];
string[] reportNames = ["report", "reports", "reporting"];

Dictionary<string, string[]> subcommandDict = new()
{
    {CommandKeys.Backtest, backtestNames },
    {CommandKeys.Importer, importerNames  },
    {CommandKeys.Migrator, migratorNames },
    {CommandKeys.Report, reportNames }
};

Debug.Assert(appName != null);

Config? config = null;

try
{
    HandleArguments(args, out string[] childArgs);

    Debug.Assert(config != null);

    Configure();
    ValidateArgsAndSetDefaults();

    if (args.Length == 0 || config.ShowHelp)
    {
        if (config.HelpArg == null)
        {
            ShowHelp();
            exitCode = 0;
        }
        else
        {
            ProcessStartInfo processStartInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = GetSubcommandFilename(config.HelpArg),
                Arguments = childArgs.Length != 0 ? GetChildArgsString(childArgs) : "--help"
            };
            var process = Process.Start(processStartInfo);
            process?.WaitForExit();
            exitCode = process?.ExitCode ?? 2;
        }
    }
    else
    {
        string filename = GetSubcommandFilename(config.Subcommand);

        if (string.IsNullOrWhiteSpace(filename))
        {
            ShowHelp();
        }
        else
        {
            ProcessStartInfo processStartInfo = new()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = filename,
                Arguments = GetChildArgsString(args[1..^0])
            };
            var process = Process.Start(processStartInfo);
            process?.WaitForExit();
            exitCode = process?.ExitCode ?? 3;
        }
    }
}
catch (ArgumentException exc)
{
    exitCode = 1;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif
}
catch (Exception exc)
{
    exitCode = 2;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif
}
finally
{
    Environment.Exit(exitCode);
}

void Communicate(string? message, bool force = false, LogLevel logLevel = LogLevel.None,
    string? scope = null)
{
    if (force || (config?.Verbose ?? false))
    {
        Console.WriteLine(message);
    }
}

void ShowHelp()
{
    CliArg[] localArgs = [];

    CliArg[] args =
    [
        new CliArg(["command"], ["args"], false, "Run sub-command."),
        new CliArg(["-?", "?", "-h", "--help", "help"], ["command name"], false, "Show this help.")
    ];

    Communicate($"{config.AppName} {config.AppVersion}".Trim(), true);
    Communicate(null, true);
    if (!string.IsNullOrWhiteSpace(config.Description))
    {
        Communicate(config.Description, true);
        Communicate(null, true);
    }
    Communicate(CliHelper.FormatArguments(args), true);

    Communicate(null, true);

    Communicate("Commands:", true);
    foreach (var kvp in subcommandDict)
    {
        Communicate($"\t{string.Join(" | ", kvp.Value.Select(v => v.PadRight(10)))}", true);
    }
    Communicate(null, true);
    Communicate("Use '--help <command>' or '<command> --help' to get help on a specific sub-command.");
}

void HandleArguments(string[] args, out string[] childArgs)
{
    config = new Config(Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Program), "v1",
        "Control CLI for Kyna applications.");

    childArgs = [];

    for (int a = 0; a < args.Length; a++)
    {
        string argument = args[a].ToLower();

        switch (argument)
        {
            case "help":
            case "--help":
            case "-h":
            case "-?":
            case "?":
                if (!config.ShowHelp && config.Subcommand == null)
                {
                    if (a < args.Length - 1)
                    {
                        config.HelpArg = args[++a];
                    }
                    config.ShowHelp = true;
                }
                break;
            default:
                var key = subcommandDict.Where(d => d.Value.Contains(argument)).Select(d => d.Key).FirstOrDefault();
                if (key != null)
                {
                    config.Subcommand ??= key;
                    childArgs = args[++a..];
                }
                break;
        }

        if (!string.IsNullOrWhiteSpace(config.Subcommand))
        {
            break;
        } // If we hit a subcommand, we're done; everything that follows belongs to the subcommand.
    }
}

void ValidateArgsAndSetDefaults()
{
    if (config == null)
    {
        throw new Exception("Logic error; configuration was not created.");
    }

    if (!config.ShowHelp && string.IsNullOrWhiteSpace(config.Subcommand))
    {
        throw new ArgumentException("No valid subcommand provided.");
    }
}

void Configure()
{
    IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

    configuration = builder.Build();
}

string GetSubcommandFilename(string? subcommand)
{
    if (string.IsNullOrWhiteSpace(subcommand))
    {
        throw new ArgumentException($"Unknown argument: {subcommand}");
    }

    const string SectionName = "CommandNames";

    var commandName = subcommand.ToLower() switch
    {
        string x when subcommandDict[CommandKeys.Backtest].Contains(x) =>
            configuration!.GetSection(SectionName)[CommandKeys.Backtest],
        string x when subcommandDict[CommandKeys.Importer].Contains(x) =>
            configuration!.GetSection(SectionName)[CommandKeys.Importer],
        string x when subcommandDict[CommandKeys.Migrator].Contains(x) =>
            configuration!.GetSection(SectionName)[CommandKeys.Migrator],
        string x when subcommandDict[CommandKeys.Report].Contains(x) =>
            configuration!.GetSection(SectionName)[CommandKeys.Report],
        _ => throw new ArgumentException($"Unknown argument: {subcommand}")
    };

    commandName = CliHelper.IsWindows() ? $"{commandName}.exe" : commandName;

    Debug.Assert(commandName != null);

    var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

#if DEBUG
    dir = new DirectoryInfo(Path.Combine("/", "repos", "kyna", "dotnet", "apps"));
#endif

    Debug.Assert(dir != null);

    var files = dir.GetFiles(commandName, SearchOption.AllDirectories);

    if (files.Length == 0)
    {
        throw new ArgumentException($"Could not find command file for {commandName}");
    }

    return files[0].FullName;
}

string GetChildArgsString(string[] args)
{
    List<string> childArgs = new(10);
    for (int a = 0; a < args.Length; a++)
    {
        if (args[a].Contains(' '))
        {
            childArgs.Add($"\"{args[a]}\"");
        }
        else
        {
            childArgs.Add(args[a]);
        }
    }
    return string.Join(' ', childArgs);
}

class Config(string appName, string appVersion, string? description)
    : CliConfigBase(appName, appVersion, description)
{
    public string? HelpArg { get; set; }
    public string? Subcommand { get; set; }
}

static class CommandKeys
{
    public const string Backtest = "backtest";
    public const string Importer = "importer";
    public const string Migrator = "migrator";
    public const string Report = "report";
}
