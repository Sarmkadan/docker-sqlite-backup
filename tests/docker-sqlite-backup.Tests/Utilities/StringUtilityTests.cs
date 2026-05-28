// Author: Vladyslav Zaiets

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class StringUtilityTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatBytes_VariousSizes_ReturnsHumanReadableString(long bytes, string expected)
    {
        var result = StringUtility.FormatBytes(bytes);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("BackupService", "backup-service")]
    [InlineData("backupService", "backup-service")]
    [InlineData("DatabasePath", "database-path")]
    [InlineData("simpleword", "simpleword")]
    public void ToKebabCase_CamelOrPascalInput_ReturnsKebabCase(string input, string expected)
    {
        var result = StringUtility.ToKebabCase(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("BackupService", "backup_service")]
    [InlineData("rotationPolicy", "rotation_policy")]
    [InlineData("single", "single")]
    public void ToSnakeCase_CamelOrPascalInput_ReturnsSnakeCase(string input, string expected)
    {
        var result = StringUtility.ToSnakeCase(input);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello-world", "HelloWorld")]
    [InlineData("backup_service", "BackupService")]
    [InlineData("hello world", "HelloWorld")]
    [InlineData("already", "Already")]
    public void ToPascalCase_DelimitedInput_ReturnsPascalCase(string input, string expected)
    {
        var result = StringUtility.ToPascalCase(input);

        result.Should().Be(expected);
    }

    [Fact]
    public void Truncate_StringExceedsMaxLength_AddsEllipsis()
    {
        var result = StringUtility.Truncate("HelloWorldLongString", 5);

        result.Should().Be("Hello...");
    }

    [Fact]
    public void Truncate_StringWithinMaxLength_ReturnsOriginal()
    {
        var result = StringUtility.Truncate("Hello", 10);

        result.Should().Be("Hello");
    }

    [Fact]
    public void Truncate_StringExceedsMaxLength_NoEllipsisWhenDisabled()
    {
        var result = StringUtility.Truncate("HelloWorld", 5, addEllipsis: false);

        result.Should().Be("Hello");
    }

    [Fact]
    public void MaskSensitive_LongString_ShowsOnlyFirstFourChars()
    {
        var result = StringUtility.MaskSensitive("supersecretkey");

        result.Should().StartWith("supe");
        result.Should().Contain("*");
        result.Should().HaveLength("supersecretkey".Length);
    }

    [Fact]
    public void MaskSensitive_StringShorterThanVisibleChars_ReturnsAsterisks()
    {
        var result = StringUtility.MaskSensitive("abc");

        result.Should().Be("***");
    }

    [Theory]
    [InlineData("user@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("@nodomain.com", false)]
    [InlineData("", false)]
    public void IsValidEmail_VariousInputs_ReturnsExpectedValidity(string email, bool expected)
    {
        var result = StringUtility.IsValidEmail(email);

        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidGuid_ValidGuidString_ReturnsTrue()
    {
        var guid = Guid.NewGuid().ToString();

        StringUtility.IsValidGuid(guid).Should().BeTrue();
    }

    [Fact]
    public void IsValidGuid_InvalidString_ReturnsFalse()
    {
        StringUtility.IsValidGuid("not-a-guid").Should().BeFalse();
    }

    [Fact]
    public void JoinReadable_TwoValues_UsesAndConjunction()
    {
        var result = StringUtility.JoinReadable("alpha", "beta");

        result.Should().Be("alpha and beta");
    }

    [Fact]
    public void JoinReadable_ThreeValues_UsesOxfordComma()
    {
        var result = StringUtility.JoinReadable("alpha", "beta", "gamma");

        result.Should().Be("alpha, beta, and gamma");
    }

    [Fact]
    public void JoinReadable_SingleValue_ReturnsValueDirectly()
    {
        var result = StringUtility.JoinReadable("only");

        result.Should().Be("only");
    }

    [Fact]
    public void Repeat_PositiveCount_ReturnsRepeatedString()
    {
        var result = StringUtility.Repeat("ab", 3);

        result.Should().Be("ababab");
    }

    [Fact]
    public void Repeat_ZeroCount_ReturnsEmptyString()
    {
        var result = StringUtility.Repeat("ab", 0);

        result.Should().BeEmpty();
    }
}
