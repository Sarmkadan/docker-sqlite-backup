// Author: Vladyslav Zaiets

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

/// <summary>
/// Provides unit tests for the <see cref="DateTimeUtility"/> class.
/// Tests various date and time utility methods including ISO 8601 formatting,
/// parsing, display formatting, relative time calculations, duration formatting,
/// and date manipulation functions.
/// </summary>
public class DateTimeUtilityTests
{
    /// <summary>
    /// Tests that <see cref="DateTimeUtility.ToIso8601"/> correctly formats a UTC DateTime
    /// in ISO 8601 round-trip format with 'Z' timezone indicator.
    /// </summary>
    [Fact]
    public void ToIso8601_UtcDateTime_ReturnsRoundTripFormat()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var result = DateTimeUtility.ToIso8601(dt);

        result.Should().StartWith("2024-06-15T10:30:00");
        result.Should().EndWith("Z");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.TryParseIso8601"/> successfully parses a valid ISO 8601 string
    /// and returns true with the correctly parsed DateTime.
    /// </summary>
    [Fact]
    public void TryParseIso8601_ValidIso8601String_ReturnsTrueAndParsedDate()
    {
        var input = "2024-06-15T10:30:00Z";

        var success = DateTimeUtility.TryParseIso8601(input, out var result);

        success.Should().BeTrue();
        result.Year.Should().Be(2024);
        result.Month.Should().Be(6);
        result.Day.Should().Be(15);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.TryParseIso8601"/> returns false when given an invalid date string.
    /// </summary>
    [Fact]
    public void TryParseIso8601_InvalidString_ReturnsFalse()
    {
        var success = DateTimeUtility.TryParseIso8601("not-a-date", out _);

        success.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatForDisplay"/> formats a DateTime using the default pattern
    /// in the format "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    [Fact]
    public void FormatForDisplay_DefaultFormat_ReturnsExpectedPattern()
    {
        var dt = new DateTime(2024, 3, 7, 14, 5, 9);

        var result = DateTimeUtility.FormatForDisplay(dt);

        result.Should().Be("2024-03-07 14:05:09");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatForDisplay"/> formats a DateTime using a custom format string
    /// provided as a parameter.
    /// </summary>
    [Fact]
    public void FormatForDisplay_CustomFormat_ReturnsFormattedString()
    {
        var dt = new DateTime(2024, 1, 1);

        var result = DateTimeUtility.FormatForDisplay(dt, "yyyy/MM/dd");

        result.Should().Be("2024/01/01");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetRelativeTime"/> returns the correct time suffix for various time intervals.
    /// </summary>
    /// <param name="secondsAgo">The number of seconds in the past to test.</param>
    /// <param name="expectedSuffix">The expected time suffix string.</param>
    [Theory]
    [InlineData(30, "s ago")]
    [InlineData(90, "m ago")]
    [InlineData(7200, "h ago")]
    [InlineData(172800, "d ago")]
    public void GetRelativeTime_VariousIntervals_ReturnsCorrectSuffix(int secondsAgo, string expectedSuffix)
    {
        var dt = DateTime.UtcNow.AddSeconds(-secondsAgo);

        var result = DateTimeUtility.GetRelativeTime(dt);

        result.Should().EndWith(expectedSuffix);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetRelativeTime"/> returns the correct "mo ago" suffix for dates approximately one month in the past.
    /// </summary>
    [Fact]
    public void GetRelativeTime_OneMonthAgo_ReturnsMoAgoSuffix()
    {
        var dt = DateTime.UtcNow.AddDays(-40);

        var result = DateTimeUtility.GetRelativeTime(dt);

        result.Should().EndWith("mo ago");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetRelativeTime"/> returns the correct "y ago" suffix for dates approximately one year in the past.
    /// </summary>
    [Fact]
    public void GetRelativeTime_OneYearAgo_ReturnsYAgoSuffix()
    {
        var dt = DateTime.UtcNow.AddDays(-400);

        var result = DateTimeUtility.GetRelativeTime(dt);

        result.Should().EndWith("y ago");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatDuration"/> formats a TimeSpan of less than one minute
    /// using the seconds format (e.g., "45s").
    /// </summary>
    [Fact]
    public void FormatDuration_LessThanOneMinute_ReturnsSFormat()
    {
        var duration = TimeSpan.FromSeconds(45);

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("45s");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatDuration"/> formats a TimeSpan between one and sixty minutes
    /// using minutes and seconds format (e.g., "5m 30s").
    /// </summary>
    [Fact]
    public void FormatDuration_BetweenOneAndSixtyMinutes_ReturnsMSFormat()
    {
        var duration = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("5m 30s");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatDuration"/> formats a TimeSpan between one and twenty-four hours
    /// using hours and minutes format (e.g., "3h 15m").
    /// </summary>
    [Fact]
    public void FormatDuration_BetweenOneAndTwentyFourHours_ReturnsHMFormat()
    {
        var duration = TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(15));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("3h 15m");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.FormatDuration"/> formats a TimeSpan of more than one day
    /// using days and hours format (e.g., "2d 6h").
    /// </summary>
    [Fact]
    public void FormatDuration_MoreThanOneDay_ReturnsDHFormat()
    {
        var duration = TimeSpan.FromDays(2).Add(TimeSpan.FromHours(6));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("2d 6h");
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetDayStart"/> returns a DateTime with the time portion set to midnight (00:00:00)
    /// for the given input DateTime.
    /// </summary>
    [Fact]
    public void GetDayStart_SpecificDate_ReturnsDateWithZeroTime()
    {
        var dt = new DateTime(2024, 5, 20, 15, 30, 0);

        var result = DateTimeUtility.GetDayStart(dt);

        result.Hour.Should().Be(0);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
        result.Day.Should().Be(20);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetDayEnd"/> returns a DateTime with the time portion set to the last moment of the day (23:59:59)
    /// for the given input DateTime.
    /// </summary>
    [Fact]
    public void GetDayEnd_SpecificDate_ReturnsLastTickOfDay()
    {
        var dt = new DateTime(2024, 5, 20, 15, 30, 0);

        var result = DateTimeUtility.GetDayEnd(dt);

        result.Hour.Should().Be(23);
        result.Minute.Should().Be(59);
        result.Second.Should().Be(59);
        result.Day.Should().Be(20);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetMonthStart"/> returns a DateTime representing the first day of the month
    /// with the time portion set to midnight (00:00:00).
    /// </summary>
    [Fact]
    public void GetMonthStart_SpecificDate_ReturnsFirstDayOfMonth()
    {
        var dt = new DateTime(2024, 8, 15);

        var result = DateTimeUtility.GetMonthStart(dt);

        result.Day.Should().Be(1);
        result.Month.Should().Be(8);
        result.Year.Should().Be(2024);
        result.Hour.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.GetMonthEnd"/> returns a DateTime representing the last moment of the month
    /// with the time portion set to 23:59:59.
    /// </summary>
    [Fact]
    public void GetMonthEnd_SpecificDate_ReturnsLastMomentOfMonth()
    {
        var dt = new DateTime(2024, 2, 10);

        var result = DateTimeUtility.GetMonthEnd(dt);

        result.Month.Should().Be(2);
        result.Year.Should().Be(2024);
        result.Hour.Should().Be(23);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.RoundDown"/> rounds a DateTime down to the nearest hour interval,
    /// setting minutes and seconds to zero.
    /// </summary>
    [Fact]
    public void RoundDown_ToHourInterval_ReturnsFlooredHour()
    {
        var dt = new DateTime(2024, 1, 1, 14, 37, 45);

        var result = DateTimeUtility.RoundDown(dt, TimeSpan.FromHours(1));

        result.Hour.Should().Be(14);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.RoundUp"/> rounds a DateTime up to the nearest hour interval,
    /// incrementing the hour if minutes or seconds are non-zero.
    /// </summary>
    [Fact]
    public void RoundUp_ToHourInterval_ReturnsCeiledHour()
    {
        var dt = new DateTime(2024, 1, 1, 14, 37, 45);

        var result = DateTimeUtility.RoundUp(dt, TimeSpan.FromHours(1));

        result.Hour.Should().Be(15);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    /// <summary>
    /// Tests that <see cref="DateTimeUtility.RoundUp"/> returns the same DateTime unchanged when it's already aligned
    /// to the specified time interval.
    /// </summary>
    [Fact]
    public void RoundUp_AlreadyAligned_ReturnsUnchanged()
    {
        var dt = new DateTime(2024, 1, 1, 14, 0, 0);

        var result = DateTimeUtility.RoundUp(dt, TimeSpan.FromHours(1));

        result.Should().Be(dt);
    }
}
