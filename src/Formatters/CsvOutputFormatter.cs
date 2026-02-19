// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections;
using System.Reflection;
using System.Text;

namespace DockerSqliteBackup.Formatters;

/// <summary>
/// Formats output as CSV (Comma-Separated Values). Handles property reflection
/// and proper escaping of special characters. Suitable for Excel/spreadsheet import.
/// </summary>
public class CsvOutputFormatter : IOutputFormatter
{
    public string Name => "CSV";
    public string FileExtension => ".csv";

    /// <summary>
    /// Formats a single object as a CSV row.
    /// </summary>
    public string Format(object? value)
    {
        if (value == null)
            return "";

        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var values = properties.Select(p => EscapeCsvValue(p.GetValue(value)?.ToString() ?? ""));
        return string.Join(",", values);
    }

    /// <summary>
    /// Formats a collection as CSV with headers and rows.
    /// </summary>
    public string FormatCollection(IEnumerable<object?> values)
    {
        var list = values.Where(v => v != null).ToList();
        if (list.Count == 0)
            return "";

        var firstItem = list.First();
        var properties = firstItem?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];

        var sb = new StringBuilder();

        // Write headers
        var headers = properties.Select(p => EscapeCsvValue(p.Name));
        sb.AppendLine(string.Join(",", headers));

        // Write rows
        foreach (var item in list)
        {
            var rowValues = properties.Select(p =>
            {
                var val = p.GetValue(item)?.ToString() ?? "";
                return EscapeCsvValue(val);
            });
            sb.AppendLine(string.Join(",", rowValues));
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Formats a dictionary as a two-column CSV.
    /// </summary>
    public string FormatDictionary(Dictionary<string, object?> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Key,Value");

        foreach (var kvp in data)
        {
            var key = EscapeCsvValue(kvp.Key);
            var value = EscapeCsvValue(kvp.Value?.ToString() ?? "");
            sb.AppendLine($"{key},{value}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Escapes CSV values by quoting if necessary and escaping internal quotes.
    /// </summary>
    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // If the value contains comma, quote, or newline, it needs to be quoted
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            // Escape quotes by doubling them
            var escaped = value.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        return value;
    }
}
