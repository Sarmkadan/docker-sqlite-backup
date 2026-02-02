// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DockerSqliteBackup.Extensions;

/// <summary>
/// Extension methods for string manipulation and validation.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null, empty, or consists only of whitespace.
    /// </summary>
    public static bool IsEmpty(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Checks if a string has content.
    /// </summary>
    public static bool HasContent(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Gets a string or a default value if it's empty.
    /// </summary>
    public static string OrDefault(this string? value, string defaultValue = "")
        => string.IsNullOrWhiteSpace(value) ? defaultValue : value;

    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    public static string TruncateAt(this string value, int maxLength, string suffix = "...")
    {
        if (value.Length <= maxLength) return value;
        return value[..maxLength] + suffix;
    }

    /// <summary>
    /// Repeats a string N times.
    /// </summary>
    public static string Repeat(this string value, int count)
    {
        if (count <= 0) return "";
        var sb = new StringBuilder(value.Length * count);
        for (int i = 0; i < count; i++)
            sb.Append(value);
        return sb.ToString();
    }

    /// <summary>
    /// Converts a string to a nullable GUID.
    /// </summary>
    public static Guid? ToGuid(this string? value)
    {
        return Guid.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Converts a string to a nullable int.
    /// </summary>
    public static int? ToIntNullable(this string? value)
    {
        return int.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Converts a string to a nullable long.
    /// </summary>
    public static long? ToLongNullable(this string? value)
    {
        return long.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Converts a string to a nullable bool.
    /// </summary>
    public static bool? ToBoolNullable(this string? value)
    {
        return bool.TryParse(value, out var result) ? result : null;
    }

    /// <summary>
    /// Splits a string by a delimiter and trims each part.
    /// </summary>
    public static string[] SplitAndTrim(this string value, string delimiter)
    {
        return value.Split(delimiter).Select(s => s.Trim()).ToArray();
    }

    /// <summary>
    /// Surrounds a string with quotes if it contains spaces.
    /// </summary>
    public static string QuoteIfNeeded(this string value)
    {
        return value.Contains(" ") ? $"\"{value}\"" : value;
    }

    /// <summary>
    /// Removes duplicate consecutive characters.
    /// </summary>
    public static string RemoveDuplicateChars(this string value, char character)
    {
        while (value.Contains($"{character}{character}"))
            value = value.Replace($"{character}{character}", character.ToString());
        return value;
    }

    /// <summary>
    /// Gets the first N characters of a string.
    /// </summary>
    public static string First(this string value, int count)
    {
        if (string.IsNullOrEmpty(value) || count <= 0) return "";
        return value.Length <= count ? value : value[..count];
    }

    /// <summary>
    /// Gets the last N characters of a string.
    /// </summary>
    public static string Last(this string value, int count)
    {
        if (string.IsNullOrEmpty(value) || count <= 0) return "";
        return value.Length <= count ? value : value[^count..];
    }
}
