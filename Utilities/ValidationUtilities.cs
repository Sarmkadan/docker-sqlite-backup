// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using DockerSqliteBackup.Constants;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for data validation.
/// </summary>
public static class ValidationUtilities
{
    /// <summary>
    /// Validates if a string is a valid UUID.
    /// </summary>
    public static bool IsValidUuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Guid.TryParse(value, out _);
    }

    /// <summary>
    /// Validates if a string is a valid SHA256 hash.
    /// </summary>
    public static bool IsValidSha256(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(value, BackupConstants.FileHashPattern);
    }

    /// <summary>
    /// Validates if a string is a valid CRON expression.
    /// </summary>
    public static bool IsValidCronExpression(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // CRON should have 5 or 6 parts (minute, hour, day, month, weekday, [second])
            return parts.Length is 5 or 6;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is a valid email address.
    /// </summary>
    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is a valid URL.
    /// </summary>
    public static bool IsValidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Uri.TryCreate(value, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates if a string is a valid file path.
    /// </summary>
    public static bool IsValidFilePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var path = Path.GetFullPath(value);
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string is a valid directory path.
    /// </summary>
    public static bool IsValidDirectoryPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var path = Path.GetFullPath(value);
            var invalidChars = Path.GetInvalidPathChars();
            return !path.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if a string represents a valid integer within a range.
    /// </summary>
    public static bool IsValidIntInRange(string? value, int minValue, int maxValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return int.TryParse(value, out var result) &&
               result >= minValue &&
               result <= maxValue;
    }

    /// <summary>
    /// Validates if a string represents a valid long integer within a range.
    /// </summary>
    public static bool IsValidLongInRange(string? value, long minValue, long maxValue)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return long.TryParse(value, out var result) &&
               result >= minValue &&
               result <= maxValue;
    }

    /// <summary>
    /// Validates if a value is not null or empty.
    /// </summary>
    public static bool IsNotEmpty(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Validates if a string length is within acceptable bounds.
    /// </summary>
    public static bool IsLengthInRange(string? value, int minLength, int maxLength)
    {
        if (value == null)
            return minLength == 0;

        return value.Length >= minLength && value.Length <= maxLength;
    }

    /// <summary>
    /// Validates a collection is not empty.
    /// </summary>
    public static bool IsCollectionNotEmpty<T>(IEnumerable<T>? collection)
    {
        return collection?.Any() == true;
    }

    /// <summary>
    /// Validates a collection has minimum items.
    /// </summary>
    public static bool HasMinimumItems<T>(IEnumerable<T>? collection, int minimumCount)
    {
        return collection?.Count() >= minimumCount;
    }

    /// <summary>
    /// Validates a value is within a numeric range.
    /// </summary>
    public static bool IsInRange<T>(T value, T minValue, T maxValue) where T : IComparable
    {
        return value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0;
    }

    /// <summary>
    /// Gets the first validation error from a collection of errors.
    /// </summary>
    public static string? GetFirstError(List<string> errors)
    {
        return errors?.FirstOrDefault();
    }

    /// <summary>
    /// Gets all validation errors as a single formatted string.
    /// </summary>
    public static string GetErrorsSummary(List<string> errors)
    {
        if (errors == null || errors.Count == 0)
            return "No errors";

        return string.Join(Environment.NewLine + "  - ", new[] { "" }.Concat(errors)).TrimStart();
    }
}
