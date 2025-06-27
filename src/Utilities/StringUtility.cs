// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.RegularExpressions;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for string operations. Provides formatting, validation,
/// and transformation utilities for common string tasks.
/// </summary>
public static class StringUtility
{
    /// <summary>
    /// Converts a byte size to a human-readable format (e.g., "1.5 MB").
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Converts a string to kebab-case.
    /// </summary>
    public static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([a-z])([A-Z])", "$1-$2", RegexOptions.Compiled);
        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to snake_case.
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = Regex.Replace(input, "([a-z])([A-Z])", "$1_$2", RegexOptions.Compiled);
        return result.ToLowerInvariant();
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : ""));
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        return pascal.Length > 0 ? char.ToLower(pascal[0]) + pascal[1..] : pascal;
    }

    /// <summary>
    /// Truncates a string to a maximum length, optionally adding ellipsis.
    /// </summary>
    public static string Truncate(string value, int maxLength, bool addEllipsis = true)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        var truncated = value[..maxLength];
        return addEllipsis ? truncated + "..." : truncated;
    }

    /// <summary>
    /// Masks sensitive information in a string (e.g., API keys, passwords).
    /// </summary>
    public static string MaskSensitive(string value, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars)
            return "***";

        var visible = value[..visibleChars];
        var masked = new string('*', value.Length - visibleChars);
        return visible + masked;
    }

    /// <summary>
    /// Checks if a string is a valid email address.
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string is a valid UUID.
    /// </summary>
    public static bool IsValidGuid(string value)
    {
        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Splits a string into lines, handling different line endings.
    /// </summary>
    public static string[] SplitLines(string value)
    {
        return value.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }

    /// <summary>
    /// Joins multiple strings with proper formatting.
    /// </summary>
    public static string JoinReadable(params string[] values)
    {
        var filtered = values.Where(v => !string.IsNullOrEmpty(v)).ToArray();
        if (filtered.Length == 0) return "";
        if (filtered.Length == 1) return filtered[0];
        if (filtered.Length == 2) return $"{filtered[0]} and {filtered[1]}";

        return string.Join(", ", filtered[..^1]) + $", and {filtered[^1]}";
    }

    /// <summary>
    /// Removes all whitespace from a string.
    /// </summary>
    public static string RemoveWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", "", RegexOptions.Compiled);
    }

    /// <summary>
    /// Repeats a string multiple times.
    /// </summary>
    public static string Repeat(string value, int count)
    {
        if (count <= 0) return "";
        var sb = new StringBuilder(value.Length * count);
        for (int i = 0; i < count; i++)
            sb.Append(value);
        return sb.ToString();
    }
}
