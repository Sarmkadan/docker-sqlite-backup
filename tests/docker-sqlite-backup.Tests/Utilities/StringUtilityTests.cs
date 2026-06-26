using Xunit;
using FluentAssertions;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Tests.Utilities;

public class StringUtilityTests
{
    [Theory]
    [InlineData(1024, "1 KB")]
    [InlineData(1024 * 1024, "1 MB")]
    [InlineData(1024 * 1024 * 5, "5 MB")]
    [InlineData(100, "100 B")]
    public void FormatBytes_ShouldReturnExpectedString(long bytes, string expected)
    {
        var result = StringUtility.FormatBytes(bytes);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("MyPropertyName", "my-property-name")]
    [InlineData("simple", "simple")]
    public void ToKebabCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = StringUtility.ToKebabCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("MyPropertyName", "my_property_name")]
    [InlineData("simple", "simple")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = StringUtility.ToSnakeCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("my-property-name", "MyPropertyName")]
    [InlineData("simple", "Simple")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = StringUtility.ToPascalCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("my-property-name", "myPropertyName")]
    [InlineData("simple", "simple")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        var result = StringUtility.ToCamelCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 5, true, "Hello...")]
    [InlineData("Hello", 10, true, "Hello")]
    [InlineData("Hello World", 5, false, "Hello")]
    public void Truncate_ShouldTruncateCorrectly(string value, int maxLength, bool addEllipsis, string expected)
    {
        var result = StringUtility.Truncate(value, maxLength, addEllipsis);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("password123", 4, "pass*******")]
    [InlineData("abc", 4, "***")]
    public void MaskSensitive_ShouldMaskCorrectly(string value, int visibleChars, string expected)
    {
        var result = StringUtility.MaskSensitive(value, visibleChars);
        result.Should().Be(expected);
    }
}
