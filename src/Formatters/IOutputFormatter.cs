// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Formatters;

/// <summary>
/// Interface for output formatters. Defines a contract for converting
/// domain objects to different output formats (JSON, CSV, XML, etc.).
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the name of this formatter.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the supported file extension.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Formats a single object.
    /// </summary>
    string Format(object? value);

    /// <summary>
    /// Formats a collection of objects.
    /// </summary>
    string FormatCollection(IEnumerable<object?> values);

    /// <summary>
    /// Formats a key-value dictionary.
    /// </summary>
    string FormatDictionary(Dictionary<string, object?> data);
}
