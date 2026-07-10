using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Tests.Utilities;

public static class StringUtilityTestsExtensions
{
    /// <summary>
    /// Creates a test string that is truncated to the specified length with ellipsis.
    /// </summary>
    public static string TruncateForTest(this string value, int maxLength)
    {
        return StringUtility.Truncate(value, maxLength, addEllipsis: true);
    }

    /// <summary>
    /// Creates a test string with sensitive information masked.
    /// </summary>
    public static string MaskForTest(this string value, int visibleChars = 4)
    {
        return StringUtility.MaskSensitive(value, visibleChars);
    }

    /// <summary>
    /// Converts the string to kebab-case format for testing.
    /// </summary>
    public static string ToKebabCaseForTest(this string value)
    {
        return StringUtility.ToKebabCase(value);
    }

    /// <summary>
    /// Converts the string to snake_case format for testing.
    /// </summary>
    public static string ToSnakeCaseForTest(this string value)
    {
        return StringUtility.ToSnakeCase(value);
    }

    /// <summary>
    /// Converts the string to PascalCase format for testing.
    /// </summary>
    public static string ToPascalCaseForTest(this string value)
    {
        return StringUtility.ToPascalCase(value);
    }

    /// <summary>
    /// Converts the string to camelCase format for testing.
    /// </summary>
    public static string ToCamelCaseForTest(this string value)
    {
        return StringUtility.ToCamelCase(value);
    }

    /// <summary>
    /// Removes all whitespace from the string for testing.
    /// </summary>
    public static string RemoveWhitespaceForTest(this string value)
    {
        return StringUtility.RemoveWhitespace(value);
    }

    /// <summary>
    /// Checks if the string is a valid email address.
    /// </summary>
    public static bool IsValidEmailForTest(this string value)
    {
        return StringUtility.IsValidEmail(value);
    }

    /// <summary>
    /// Checks if the string is a valid GUID.
    /// </summary>
    public static bool IsValidGuidForTest(this string value)
    {
        return StringUtility.IsValidGuid(value);
    }

    /// <summary>
    /// Repeats the string the specified number of times.
    /// </summary>
    public static string RepeatForTest(this string value, int count)
    {
        return StringUtility.Repeat(value, count);
    }

    /// <summary>
    /// Splits the string into lines, handling different line endings.
    /// </summary>
    public static string[] SplitLinesForTest(this string value)
    {
        return StringUtility.SplitLines(value);
    }

    /// <summary>
    /// Joins multiple strings with proper formatting for testing.
    /// </summary>
    public static string JoinReadableForTest(this string first, params string[] values)
    {
        var allValues = new string[values.Length + 1];
        allValues[0] = first;
        Array.Copy(values, 0, allValues, 1, values.Length);
        return StringUtility.JoinReadable(allValues);
    }
}