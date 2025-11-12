using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Tests.Utilities;

/// <summary>
/// Extension methods for testing string utility functionality.
/// Provides test-friendly wrappers around <see cref="StringUtility"/> methods
/// with proper guard clauses and exception documentation.
/// </summary>
public static class StringUtilityTestsExtensions
{
    /// <summary>
    /// Truncates a string to the specified maximum length with ellipsis.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <returns>The truncated string with ellipsis, or the original string if it's shorter than maxLength.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxLength"/> is negative.</exception>
    public static string TruncateForTest(this string value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);
        return StringUtility.Truncate(value, maxLength, addEllipsis: true);
    }

    /// <summary>
    /// Masks sensitive information in a string, showing only the specified number of visible characters.
    /// </summary>
    /// <param name="value">The string to mask.</param>
    /// <param name="visibleChars">The number of characters to leave visible at the start.</param>
    /// <returns>A masked string with visible characters followed by asterisks.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="visibleChars"/> is negative.</exception>
    public static string MaskForTest(this string value, int visibleChars = 4)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(visibleChars);
        return StringUtility.MaskSensitive(value, visibleChars);
    }

    /// <summary>
    /// Converts a string to kebab-case format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The kebab-case formatted string, or the original value if null or empty.</returns>
    public static string ToKebabCaseForTest(this string value)
    {
        return StringUtility.ToKebabCase(value);
    }

    /// <summary>
    /// Converts a string to snake_case format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The snake_case formatted string, or the original value if null or empty.</returns>
    public static string ToSnakeCaseForTest(this string value)
    {
        return StringUtility.ToSnakeCase(value);
    }

    /// <summary>
    /// Converts a string to PascalCase format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The PascalCase formatted string, or the original value if null or empty.</returns>
    public static string ToPascalCaseForTest(this string value)
    {
        return StringUtility.ToPascalCase(value);
    }

    /// <summary>
    /// Converts a string to camelCase format.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase formatted string, or the original value if null or empty.</returns>
    public static string ToCamelCaseForTest(this string value)
    {
        return StringUtility.ToCamelCase(value);
    }

    /// <summary>
    /// Removes all whitespace from a string.
    /// </summary>
    /// <param name="value">The string to process.</param>
    /// <returns>A new string with all whitespace removed.</returns>
    public static string RemoveWhitespaceForTest(this string value)
    {
        return StringUtility.RemoveWhitespace(value);
    }

    /// <summary>
    /// Checks if a string is a valid email address.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns><see langword="true"/> if the string is a valid email; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidEmailForTest(this string value)
    {
        return StringUtility.IsValidEmail(value);
    }

    /// <summary>
    /// Checks if a string is a valid GUID.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns><see langword="true"/> if the string is a valid GUID; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidGuidForTest(this string value)
    {
        return StringUtility.IsValidGuid(value);
    }

    /// <summary>
    /// Repeats a string multiple times.
    /// </summary>
    /// <param name="value">The string to repeat.</param>
    /// <param name="count">The number of times to repeat the string.</param>
    /// <returns>A new string containing the original string repeated <paramref name="count"/> times.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static string RepeatForTest(this string value, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return StringUtility.Repeat(value, count);
    }

    /// <summary>
    /// Splits a string into lines, handling different line endings (\r\n, \r, \n).
    /// </summary>
    /// <param name="value">The string to split.</param>
    /// <returns>An array of strings, one for each line.</returns>
    public static string[] SplitLinesForTest(this string value)
    {
        return StringUtility.SplitLines(value);
    }

    /// <summary>
    /// Joins multiple strings with proper formatting for readability.
    /// </summary>
    /// <param name="first">The first string in the sequence.</param>
    /// <param name="values">Additional strings to join.</param>
    /// <returns>A formatted string joining all non-empty values with proper conjunctions.</returns>
    public static string JoinReadableForTest(this string first, params string[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var allValues = new string[values.Length + 1];
        allValues[0] = first;
        Array.Copy(values, 0, allValues, 1, values.Length);
        return StringUtility.JoinReadable(allValues);
    }
}