using Kyna.ApplicationServices.Configuration;
using Kyna.Common.Abstractions;
using Kyna.Infrastructure.Database;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Kyna.ApplicationServices.Cli;

public static class CliHelper
{
    public static CliArg[] GetDefaultArgDescriptions()
    {
        return [
            new CliArg(["-?", "?", "-h", "--help"], [], false, "Show this help."),
            new CliArg(["-v", "--verbose"], [], false, "Turn on verbose communication.")
        ];
    }

    public static string[] HydrateDefaultAppConfig(string[] args, IAppConfig config)
    {
        List<string> remainingArgs = new(args.Length + 1);

        for (int i = 0; i < args.Length; i++)
        {
            string argument = args[i].ToLower();

            switch (argument)
            {
                case "?":
                case "-?":
                case "--help":
                    config.ShowHelp = true;
                    break;
                case "-v":
                case "--verbose":
                    config.Verbose = true;
                    break;
                default:
                    remainingArgs.Add(args[i]);
                    break;
            }
        }

        return [.. remainingArgs];
    }

    public static string FormatArguments(CliArg[] cliArgs)
    {
        List<KeyValuePair<string, string>> args = new(cliArgs.Length);

        var requiredArgs = cliArgs.Where(a => a.Required).ToArray();
        var optionalArgs = cliArgs.Except(requiredArgs).ToArray();

        foreach (var arg in requiredArgs)
        {
            args.Add(arg.AsKeyValuePair());
        }

        foreach (var arg in optionalArgs)
        {
            args.Add(arg.AsKeyValuePair());
        }

        var keyWidth = 1 + args.MaxBy(a => a.Key.Length).Key.Length;

        StringBuilder result = new();

        foreach (var arg in args)
        {
            result.AppendLine($"{arg.Key.PadRight(keyWidth)}\t{arg.Value}");
        }

        return result.ToString();
    }

    public static DbDef[] GetDbDefs(IConfiguration configuration)
    {
        List<DbDef> dbDefs = new(10);

        var connStringSection = configuration.GetSection(ConfigKeys.Sections.ConnectionStrings);
        var engineSection = configuration.GetSection(ConfigKeys.Sections.DbEngines);

        foreach (var kvp in connStringSection.GetChildren())
        {
            if (kvp.Value is null)
            {
                continue;
            }

            var engItem = engineSection.GetSection(kvp.Key);

            var engString = engItem is null
                ? DatabaseEngine.PostgreSql.ToString()
                : engItem.Value ?? DatabaseEngine.PostgreSql.ToString();

            if (Enum.TryParse<DatabaseEngine>(engString, out var engine))
            {
                dbDefs.Add(new(kvp.Key, engine, kvp.Value));
            }
        }

        return [.. dbDefs];
    }

    public static bool ConfirmActionWithUser(string message, string? defaultAnswer = "n")
    {
        string[] answerOptions = ["y", "n"];

        StringBuilder sb = new(message.Trim());

        if (answerOptions.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < answerOptions.Length; i++)
        {
            if (answerOptions[i].Equals(defaultAnswer, StringComparison.OrdinalIgnoreCase))
            {
                answerOptions[i] = answerOptions[i].ToUpper();
            }
            else
            {
                answerOptions[i] = answerOptions[i].ToLower();
            }
        }

        sb.Append($" ({string.Join('/', answerOptions)}) ");

        Console.Write(sb.ToString());

        var answer = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(answer))
        {
            answer = defaultAnswer;
        }

        return answer?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public static bool IsWindows() => System.Runtime.InteropServices.RuntimeInformation.
        IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

    public static bool IsLinux() => System.Runtime.InteropServices.RuntimeInformation.
        IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);

    public static bool IsMacOs() => System.Runtime.InteropServices.RuntimeInformation.
        IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD);

}

public struct CliArg(string[] args, string[] subArgs, bool required, string description)
{
    public string[] Args = [.. args.OrderBy(a => a.Length).ThenBy(a => a)];
    public string[] SubArgs = subArgs;
    public bool Required = required;
    public string Description = description;

    public readonly KeyValuePair<string, string> AsKeyValuePair()
    {
        string args = string.Join('|', Args);
        string subArgs = SubArgs.Length != 0 ? string.Join(' ', SubArgs.Select(a => $"<{a}>")) : string.Empty;

        string exp = $"{args} {subArgs}".Trim();
        exp = Required ? exp : $"[{exp}]";

        return new KeyValuePair<string, string>(exp, Description);
    }
}

