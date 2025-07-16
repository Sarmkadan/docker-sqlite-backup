#nullable enable
using FluentAssertions;
using DockerSqliteBackup.Extensions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public sealed class StringExtensionsEdgeCaseTests
{
    [Fact]
    public void IsEmpty_Null_ReturnsTrue() =>
        ((string?)null).IsEmpty().Should().BeTrue();

    [Fact]
    public void IsEmpty_Empty_ReturnsTrue() =>
        "".IsEmpty().Should().BeTrue();

    [Fact]
    public void IsEmpty_Whitespace_ReturnsTrue() =>
        "   ".IsEmpty().Should().BeTrue();

    [Fact]
    public void IsEmpty_ValidString_ReturnsFalse() =>
        "backup.db".IsEmpty().Should().BeFalse();

    [Fact]
    public void HasContent_Null_ReturnsFalse() =>
        ((string?)null).HasContent().Should().BeFalse();

    [Fact]
    public void HasContent_ValidString_ReturnsTrue() =>
        "data.sqlite".HasContent().Should().BeTrue();

    [Fact]
    public void OrDefault_Null_ReturnsDefault() =>
        ((string?)null).OrDefault("fallback").Should().Be("fallback");

    [Fact]
    public void OrDefault_Empty_ReturnsDefault() =>
        "".OrDefault("fallback").Should().Be("fallback");

    [Fact]
    public void OrDefault_ValidString_ReturnsOriginal() =>
        "value".OrDefault("fallback").Should().Be("value");

    [Fact]
    public void OrDefault_NullNoDefault_ReturnsEmpty() =>
        ((string?)null).OrDefault().Should().BeEmpty();

    [Fact]
    public void TruncateAt_ShortString_ReturnsUnchanged() =>
        "hello".TruncateAt(10).Should().Be("hello");

    [Fact]
    public void TruncateAt_LongString_TruncatesWithSuffix()
    {
        var result = "This is a very long backup name".TruncateAt(15);
        result.Should().StartWith("This is a very ");
        result.Should().EndWith("...");
    }

    [Fact]
    public void Repeat_ZeroCount_ReturnsEmpty() =>
        "ab".Repeat(0).Should().BeEmpty();

    [Fact]
    public void Repeat_NegativeCount_ReturnsEmpty() =>
        "ab".Repeat(-1).Should().BeEmpty();

    [Fact]
    public void Repeat_ValidCount_RepeatsCorrectly() =>
        "ab".Repeat(3).Should().Be("ababab");
}
