// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class DateTimeUtilityTests
{
    [Fact]
    public void ToIso8601_UtcDateTime_ReturnsRoundTripFormat()
    {
        var dt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var result = DateTimeUtility.ToIso8601(dt);

        result.Should().StartWith("2024-06-15T10:30:00");
        result.Should().EndWith("Z");
    }

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

    [Fact]
    public void TryParseIso8601_InvalidString_ReturnsFalse()
    {
        var success = DateTimeUtility.TryParseIso8601("not-a-date", out _);

        success.Should().BeFalse();
    }

    [Fact]
    public void FormatForDisplay_DefaultFormat_ReturnsExpectedPattern()
    {
        var dt = new DateTime(2024, 3, 7, 14, 5, 9);

        var result = DateTimeUtility.FormatForDisplay(dt);

        result.Should().Be("2024-03-07 14:05:09");
    }

    [Fact]
    public void FormatForDisplay_CustomFormat_ReturnsFormattedString()
    {
        var dt = new DateTime(2024, 1, 1);

        var result = DateTimeUtility.FormatForDisplay(dt, "yyyy/MM/dd");

        result.Should().Be("2024/01/01");
    }

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

    [Fact]
    public void GetRelativeTime_OneMonthAgo_ReturnsMoAgoSuffix()
    {
        var dt = DateTime.UtcNow.AddDays(-40);

        var result = DateTimeUtility.GetRelativeTime(dt);

        result.Should().EndWith("mo ago");
    }

    [Fact]
    public void GetRelativeTime_OneYearAgo_ReturnsYAgoSuffix()
    {
        var dt = DateTime.UtcNow.AddDays(-400);

        var result = DateTimeUtility.GetRelativeTime(dt);

        result.Should().EndWith("y ago");
    }

    [Fact]
    public void FormatDuration_LessThanOneMinute_ReturnsSFormat()
    {
        var duration = TimeSpan.FromSeconds(45);

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("45s");
    }

    [Fact]
    public void FormatDuration_BetweenOneAndSixtyMinutes_ReturnsMSFormat()
    {
        var duration = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(30));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("5m 30s");
    }

    [Fact]
    public void FormatDuration_BetweenOneAndTwentyFourHours_ReturnsHMFormat()
    {
        var duration = TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(15));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("3h 15m");
    }

    [Fact]
    public void FormatDuration_MoreThanOneDay_ReturnsDHFormat()
    {
        var duration = TimeSpan.FromDays(2).Add(TimeSpan.FromHours(6));

        var result = DateTimeUtility.FormatDuration(duration);

        result.Should().Be("2d 6h");
    }

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

    [Fact]
    public void GetMonthEnd_SpecificDate_ReturnsLastMomentOfMonth()
    {
        var dt = new DateTime(2024, 2, 10);

        var result = DateTimeUtility.GetMonthEnd(dt);

        result.Month.Should().Be(2);
        result.Year.Should().Be(2024);
        result.Hour.Should().Be(23);
    }

    [Fact]
    public void RoundDown_ToHourInterval_ReturnsFlooredHour()
    {
        var dt = new DateTime(2024, 1, 1, 14, 37, 45);

        var result = DateTimeUtility.RoundDown(dt, TimeSpan.FromHours(1));

        result.Hour.Should().Be(14);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void RoundUp_ToHourInterval_ReturnsCeiledHour()
    {
        var dt = new DateTime(2024, 1, 1, 14, 37, 45);

        var result = DateTimeUtility.RoundUp(dt, TimeSpan.FromHours(1));

        result.Hour.Should().Be(15);
        result.Minute.Should().Be(0);
        result.Second.Should().Be(0);
    }

    [Fact]
    public void RoundUp_AlreadyAligned_ReturnsUnchanged()
    {
        var dt = new DateTime(2024, 1, 1, 14, 0, 0);

        var result = DateTimeUtility.RoundUp(dt, TimeSpan.FromHours(1));

        result.Should().Be(dt);
    }
}
