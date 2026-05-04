// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Cli;

/// <summary>
/// Parses command-line arguments into strongly-typed options.
/// Handles short flags (-v), long flags (--verbose), and key-value pairs (--key value).
/// </summary>
public class CliCommandParser
{
    private static readonly HashSet<string> LongFlags = ["--help", "--version", "--verbose", "--json", "--csv", "--xml"];
    private static readonly HashSet<string> ShortFlags = ["-h", "-v", "-V"];

    /// <summary>
    /// Parses command-line arguments into CliOptions.
    /// </summary>
    public CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        if (args.Length == 0)
        {
            options.ShowHelp = true;
            return options;
        }

        var argList = new List<string>(args);
        var index = 0;

        // Process first positional arg as command
        if (index < argList.Count && !IsOption(argList[index]))
        {
            options.Command = argList[index];
            index++;

            // Check for subcommand
            if (index < argList.Count && !IsOption(argList[index]))
            {
                options.SubCommand = argList[index];
                index++;
            }
        }

        // Process remaining arguments
        while (index < argList.Count)
        {
            var arg = argList[index];

            if (arg == "--help" || arg == "-h")
            {
                options.ShowHelp = true;
                index++;
            }
            else if (arg == "--version" || arg == "-v")
            {
                options.ShowVersion = true;
                index++;
            }
            else if (arg == "--verbose" || arg == "-V")
            {
                options.Flags["verbose"] = true;
                index++;
            }
            else if (arg == "--json")
            {
                options.OutputFormat = "json";
                index++;
            }
            else if (arg == "--csv")
            {
                options.OutputFormat = "csv";
                index++;
            }
            else if (arg == "--xml")
            {
                options.OutputFormat = "xml";
                index++;
            }
            else if (arg.StartsWith("--"))
            {
                // Long option: --key value or --key=value
                var (key, value) = ParseLongOption(arg, argList, ref index);
                if (value != null)
                    options.Arguments[key.ToLowerInvariant()] = value;
                else
                    options.Flags[key.ToLowerInvariant()] = true;
            }
            else if (arg.StartsWith("-") && arg.Length > 1)
            {
                // Short option: -k value
                var (key, value) = ParseShortOption(arg, argList, ref index);
                if (value != null)
                    options.Arguments[key.ToLowerInvariant()] = value;
                else
                    options.Flags[key.ToLowerInvariant()] = true;
            }
            else
            {
                // Positional argument
                options.Positional.Add(arg);
                index++;
            }
        }

        return options;
    }

    private static (string key, string? value) ParseLongOption(string arg, List<string> args, ref int index)
    {
        var equalIndex = arg.IndexOf('=');
        if (equalIndex > 0)
        {
            var key = arg[2..equalIndex];
            var value = arg[(equalIndex + 1)..];
            index++;
            return (key, value);
        }

        var optionKey = arg[2..];
        index++;

        // Check if next arg is a value (not an option)
        if (index < args.Count && !IsOption(args[index]))
        {
            var value = args[index];
            index++;
            return (optionKey, value);
        }

        return (optionKey, null);
    }

    private static (string key, string? value) ParseShortOption(string arg, List<string> args, ref int index)
    {
        var optionKey = arg[1..];
        index++;

        if (index < args.Count && !IsOption(args[index]))
        {
            var value = args[index];
            index++;
            return (optionKey, value);
        }

        return (optionKey, null);
    }

    private static bool IsOption(string arg) => arg.StartsWith("-");
}
