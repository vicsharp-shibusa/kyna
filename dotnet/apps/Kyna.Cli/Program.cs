using Kyna.ApplicationServices.Cli;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text;

IConfiguration? configuration;

int exitCode = -1;

var appName = Assembly.GetExecutingAssembly().GetName().Name;
Debug.Assert(appName != null);

Dictionary<string, SubCommand> commandDict;

Config? config = null;

try
{
    Configure();
    ParseArguments(args, out string[] childArgs);

    Debug.Assert(config != null);

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
                FileName = commandDict[config.HelpArg].FullPath,
                Arguments = childArgs.Length != 0 ? GetChildArgsString(childArgs) : "--help"
            };
            var process = Process.Start(processStartInfo);
            process?.WaitForExit();
            exitCode = process?.ExitCode ?? 2;
        }
    }
    else
    {
        var filename = commandDict.FirstOrDefault(k => k.Value.Aliases.Contains(config.Subcommand, StringComparer.OrdinalIgnoreCase)).Value.FullPath;

        if (string.IsNullOrWhiteSpace(filename))
        {
            ShowHelp();
            exitCode = 0;
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
    exitCode = 4;

#if DEBUG
    Communicate(exc.ToString(), true);
#else
    Communicate(exc.Message, true);
#endif
}
catch (Exception exc)
{
    exitCode = 5;

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

void Communicate(string? message, bool force = false)
{
    if (force || (config?.Verbose ?? false))
    {
        Console.WriteLine(message);
    }
}

void ShowHelp()
{
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
    const int PadRightVal = 11;
    var headers = new string[] { "Alias 1", "Alias 2", "Alias 3", "Command Name" };

    var cmdSb = new StringBuilder();
    cmdSb.AppendLine($"\t{string.Join(" | ", headers.Select(h => h.PadRight(PadRightVal)))}");
    cmdSb.AppendLine(new string('-', 70));
    foreach (var kvp in commandDict)
    {
        cmdSb.Append($"\t{string.Join(" | ", kvp.Value.Aliases.Select(v => v.PadRight(PadRightVal)))}");
        cmdSb.AppendLine($" | {Path.GetFileName(kvp.Value.FullPath)}");
    }
    Communicate(cmdSb.ToString(), true);
    Communicate(null, true);

    Communicate("You can use any of the aliases; these are all equivalent commands:", true);
    Communicate(@"
    kyna import -f ./file.json --info
    kyna importer -f ./file.json --info
    kyna imports -f ./file.json --info", true);

    Communicate(null, true);
    Communicate("Use 'help <command>' or '<command> --help' to get help on a specific sub-command.", true);

    Communicate(@"
    kyna help migrate
    kyna backtest --help", true);
    Communicate(null, true);
}

void ParseArguments(string[] args, out string[] childArgs)
{
    config = new Config(Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Program), "v1",
        "Control CLI for Kyna applications.");

    childArgs = [];

    List<string> unknownCommands = [];

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
                        var alias = args[++a];
                        config.HelpArg = commandDict.FirstOrDefault(k => k.Value.Aliases.Contains(alias)).Key;
                    }
                    config.ShowHelp = true;
                }
                break;
            default:
                var key = commandDict.Where(d => d.Value.Aliases.Contains(argument)).Select(d => d.Key).FirstOrDefault();
                if (key != null)
                {
                    config.Subcommand ??= key;
                    childArgs = args[++a..];
                }
                else
                {
                    unknownCommands.Add(args[a]);
                }
                break;
        }

        if (!string.IsNullOrWhiteSpace(config.Subcommand))
        {
            break; // If we hit a subcommand, we're done; everything that follows belongs to the subcommand.
        }
    }

    if (string.IsNullOrEmpty(config.Subcommand))
    {
        if (unknownCommands.Count > 0)
        {
            var candidate = unknownCommands.First();
            foreach (var possible in commandDict.Values.SelectMany(k => k.Aliases))
            {
                if (possible.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Did you mean '{possible}'?");
                    ShowHelp();
                    Environment.Exit(-2);
                }
            }
            throw new ArgumentException(unknownCommands.First());
        }
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

    const string SectionName = "CommandNames";
    var section = (configuration?.GetSection(SectionName)) ?? throw new Exception($"Configuration error: could not find section '{SectionName}'");

    commandDict = new Dictionary<string, SubCommand>()
    {
        { CommandKeys.Backtest, new SubCommand(GetSubcommandFilename(section[CommandKeys.Backtest]),
            ["backtest", "backtests", "backtesting"])},
        { CommandKeys.Importer, new SubCommand(GetSubcommandFilename(section[CommandKeys.Importer]),
            ["import", "importer", "imports"])},
        { CommandKeys.Migrator, new SubCommand(GetSubcommandFilename(section[CommandKeys.Migrator]),
            ["migrate", "migrator", "migrates"])},
        { CommandKeys.Report, new SubCommand(GetSubcommandFilename(section[CommandKeys.Report]),
            ["report", "reports", "reporting"])}
    };
}

string GetSubcommandFilename(string? commandName)
{
    ArgumentNullException.ThrowIfNull(commandName);

    commandName = CliHelper.IsWindows() ? $"{commandName}.exe" : commandName;

    var dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

#if DEBUG
    // Map this to your local dev directory to make this work with an IDE.
    dir = new DirectoryInfo(Path.Combine(Path.DirectorySeparatorChar.ToString(), "repos", "kyna", "dotnet", "apps"));
#endif

    Debug.Assert(dir != null);

    var files = dir.GetFiles(commandName, SearchOption.AllDirectories);

    if (files.Length == 0)
        throw new ArgumentException($"Could not find command file for {commandName}");

    return files[0].FullName;
}

string GetChildArgsString(string[] args)
{
    List<string> childArgs = new(10);
    for (int a = 0; a < args.Length; a++)
    {
        if (args[a].Contains(' '))
            childArgs.Add($"\"{args[a]}\"");
        else
            childArgs.Add(args[a]);
    }
    return string.Join(' ', childArgs);
}

class Config(string appName, string appVersion, string? description)
    : CliConfigBase(appName, appVersion, description)
{
    public string? HelpArg { get; set; }
    public string? Subcommand { get; set; }
}

/// <summary>
/// These keys correspond to the keys in the 'CommandName section of appsettings.json.
/// </summary>
static class CommandKeys
{
    public const string Backtest = "backtest";
    public const string Importer = "importer";
    public const string Migrator = "migrator";
    public const string Report = "report";
}

/// <summary>
/// Represents the full path to the executable
/// </summary>
struct SubCommand
{
    public SubCommand()
    {
        Aliases = [];
    }

    public SubCommand(string? fullPath, string[] aliases)
    {
        FullPath = fullPath;
        Aliases = aliases;
    }

    public string? FullPath;
    public string[] Aliases;
}
