#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for DateTime operations. Handles formatting, parsing,
/// timezone conversions, and duration calculations.
/// </summary>
public static class DateTimeUtility
{
    /// <summary>
    /// Formats a DateTime as an ISO 8601 string (UTC).
    /// </summary>
    public static string ToIso8601(DateTime dateTime)
    {
        return dateTime.ToUniversalTime().ToString("O");
    }

    /// <summary>
    /// Parses an ISO 8601 formatted string to DateTime.
    /// </summary>
    public static bool TryParseIso8601(string dateString, out DateTime result)
    {
        return DateTime.TryParse(dateString, out result);
    }

    /// <summary>
    /// Formats a DateTime for display in a human-readable format.
    /// </summary>
    public static string FormatForDisplay(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        return dateTime.ToString(format);
    }

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "2 hours ago").
    /// </summary>
    public static string GetRelativeTime(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (span.TotalSeconds < 60)
            return $"{(int)span.TotalSeconds}s ago";

        if (span.TotalMinutes < 60)
            return $"{(int)span.TotalMinutes}m ago";

        if (span.TotalHours < 24)
            return $"{(int)span.TotalHours}h ago";

        if (span.TotalDays < 30)
            return $"{(int)span.TotalDays}d ago";

        if (span.TotalDays < 365)
            return $"{(int)(span.TotalDays / 30)}mo ago";

        return $"{(int)(span.TotalDays / 365)}y ago";
    }

    /// <summary>
    /// Formats a TimeSpan as a human-readable duration string.
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
            return $"{(int)duration.TotalSeconds}s";

        if (duration.TotalMinutes < 60)
            return $"{(int)duration.TotalMinutes}m {duration.Seconds}s";

        if (duration.TotalHours < 24)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        return $"{(int)duration.TotalDays}d {duration.Hours}h";
    }

    /// <summary>
    /// Gets the start of the day in UTC.
    /// </summary>
    public static DateTime GetDayStart(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return dt.Date;
    }

    /// <summary>
    /// Gets the end of the day in UTC.
    /// </summary>
    public static DateTime GetDayEnd(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return dt.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Gets the start of the month.
    /// </summary>
    public static DateTime GetMonthStart(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        return new DateTime(dt.Year, dt.Month, 1);
    }

    /// <summary>
    /// Gets the end of the month.
    /// </summary>
    public static DateTime GetMonthEnd(DateTime? dateTime = null)
    {
        var dt = dateTime ?? DateTime.UtcNow;
        var nextMonth = dt.AddMonths(1);
        return new DateTime(nextMonth.Year, nextMonth.Month, 1).AddTicks(-1);
    }

    /// <summary>
    /// Calculates the time until the next occurrence of a given time of day.
    /// </summary>
    public static TimeSpan GetTimeUntil(TimeOnly targetTime)
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var remaining = targetTime - now;

        if (remaining.TotalSeconds <= 0)
            remaining = remaining.Add(TimeSpan.FromHours(24));

        return remaining;
    }

    /// <summary>
    /// Rounds a DateTime down to the nearest interval.
    /// </summary>
    public static DateTime RoundDown(DateTime dateTime, TimeSpan interval)
    {
        return dateTime.AddTicks(-(dateTime.Ticks % interval.Ticks));
    }

    /// <summary>
    /// Rounds a DateTime up to the nearest interval.
    /// </summary>
    public static DateTime RoundUp(DateTime dateTime, TimeSpan interval)
    {
        var remainder = dateTime.Ticks % interval.Ticks;
        return remainder == 0 ? dateTime : dateTime.AddTicks(interval.Ticks - remainder);
    }
}
