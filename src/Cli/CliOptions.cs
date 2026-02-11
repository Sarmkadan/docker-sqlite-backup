#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Cli;

/// <summary>
/// Represents parsed command-line arguments.
/// </summary>
public class CliOptions
{
    public string? Command { get; set; }
    public string? SubCommand { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = [];
    public Dictionary<string, bool> Flags { get; set; } = [];
    public List<string> Positional { get; set; } = [];
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public string? OutputFormat { get; set; } = "console";

    /// <summary>
    /// Tries to get an argument value.
    /// </summary>
    public bool TryGetArgument(string key, out string? value)
    {
        return Arguments.TryGetValue(key.ToLowerInvariant(), out value);
    }

    /// <summary>
    /// Tries to get a flag value.
    /// </summary>
    public bool TryGetFlag(string key, out bool value)
    {
        return Flags.TryGetValue(key.ToLowerInvariant(), out value);
    }

    /// <summary>
    /// Gets an argument with a default value.
    /// </summary>
    public string GetArgument(string key, string defaultValue = "")
    {
        return TryGetArgument(key, out var value) ? value ?? defaultValue : defaultValue;
    }

    /// <summary>
    /// Checks if a flag is set.
    /// </summary>
    public bool HasFlag(string key) => Flags.GetValueOrDefault(key.ToLowerInvariant(), false);

    /// <summary>
    /// Validates the options are consistent.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Command) || ShowHelp || ShowVersion;
    }
}
