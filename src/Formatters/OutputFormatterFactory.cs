#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Formatters;

/// <summary>
/// Factory for creating output formatters based on format name.
/// Supports JSON, CSV, and XML formats with extensibility for custom formatters.
/// </summary>
public class OutputFormatterFactory
{
    private readonly Dictionary<string, IOutputFormatter> _formatters;

    public OutputFormatterFactory()
    {
        _formatters = new Dictionary<string, IOutputFormatter>(StringComparer.OrdinalIgnoreCase)
        {
            { "json", new JsonOutputFormatter(prettyPrint: true) },
            { "csv", new CsvOutputFormatter() },
            { "xml", new XmlOutputFormatter() }
        };
    }

    /// <summary>
    /// Gets a formatter by name. Throws if format not found.
    /// </summary>
    public IOutputFormatter GetFormatter(string format)
    {
        if (_formatters.TryGetValue(format, out var formatter))
            return formatter;

        throw new ArgumentException(
            $"Unknown format: {format}. Supported formats: {string.Join(", ", _formatters.Keys)}",
            nameof(format));
    }

    /// <summary>
    /// Tries to get a formatter, returning a default (JSON) if not found.
    /// </summary>
    public IOutputFormatter GetFormatterOrDefault(string? format, IOutputFormatter? defaultFormatter = null)
    {
        if (string.IsNullOrEmpty(format))
            return defaultFormatter ?? _formatters["json"];

        return _formatters.TryGetValue(format, out var formatter)
            ? formatter
            : (defaultFormatter ?? _formatters["json"]);
    }

    /// <summary>
    /// Registers a custom formatter.
    /// </summary>
    public void RegisterFormatter(string format, IOutputFormatter formatter)
    {
        _formatters[format.ToLowerInvariant()] = formatter;
    }

    /// <summary>
    /// Gets all available formatter names.
    /// </summary>
    public IEnumerable<string> GetAvailableFormats() => _formatters.Keys;

    /// <summary>
    /// Gets a compact JSON formatter (no pretty-printing).
    /// </summary>
    public IOutputFormatter GetCompactJsonFormatter() => new JsonOutputFormatter(prettyPrint: false);
}
