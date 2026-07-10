using Xunit;
using FluentAssertions;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Tests.Utilities;

/// <summary>
/// Tests for the StringUtility class.
/// </summary>
public class StringUtilityTests
{
    [Theory]
    [InlineData(1024, "1 KB")]
    [InlineData(1024 * 1024, "1 MB")]
    [InlineData(1024 * 1024 * 5, "5 MB")]
    [InlineData(100, "100 B")]
    public void FormatBytes_ShouldReturnExpectedString(long bytes, string expected)
    {
        /// <summary>
        /// Verifies that FormatBytes returns the expected string representation of a given number of bytes.
        /// </summary>
        /// <param name="bytes">The number of bytes to format.</param>
        /// <param name="expected">The expected string representation of the bytes.</param>
        var result = StringUtility.FormatBytes(bytes);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("MyPropertyName", "my-property-name")]
    [InlineData("simple", "simple")]
    public void ToKebabCase_ShouldConvertCorrectly(string input, string expected)
    {
        /// <summary>
        /// Verifies that ToKebabCase converts a given string to kebab case.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="expected">The expected kebab case representation of the string.</param>
        var result = StringUtility.ToKebabCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("MyPropertyName", "my_property_name")]
    [InlineData("simple", "simple")]
    public void ToSnakeCase_ShouldConvertCorrectly(string input, string expected)
    {
        /// <summary>
        /// Verifies that ToSnakeCase converts a given string to snake case.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="expected">The expected snake case representation of the string.</param>
        var result = StringUtility.ToSnakeCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world", "HelloWorld")]
    [InlineData("my-property-name", "MyPropertyName")]
    [InlineData("simple", "Simple")]
    public void ToPascalCase_ShouldConvertCorrectly(string input, string expected)
    {
        /// <summary>
        /// Verifies that ToPascalCase converts a given string to pascal case.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="expected">The expected pascal case representation of the string.</param>
        var result = StringUtility.ToPascalCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("hello_world", "helloWorld")]
    [InlineData("my-property-name", "myPropertyName")]
    [InlineData("simple", "simple")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        /// <summary>
        /// Verifies that ToCamelCase converts a given string to camel case.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <param name="expected">The expected camel case representation of the string.</param>
        var result = StringUtility.ToCamelCase(input);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Hello World", 5, true, "Hello...")]
    [InlineData("Hello", 10, true, "Hello")]
    [InlineData("Hello World", 5, false, "Hello")]
    public void Truncate_ShouldTruncateCorrectly(string value, int maxLength, bool addEllipsis, string expected)
    {
        /// <summary>
        /// Verifies that Truncate truncates a given string to a specified length, optionally adding an ellipsis.
        /// </summary>
        /// <param name="value">The string to truncate.</param>
        /// <param name="maxLength">The maximum length of the truncated string.</param>
        /// <param name="addEllipsis">Whether to add an ellipsis to the truncated string.</param>
        /// <param name="expected">The expected truncated string.</param>
        var result = StringUtility.Truncate(value, maxLength, addEllipsis);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("password123", 4, "pass*******")]
    [InlineData("abc", 4, "***")]
    public void MaskSensitive_ShouldMaskCorrectly(string value, int visibleChars, string expected)
    {
        /// <summary>
        /// Verifies that MaskSensitive masks a given string, revealing only the specified number of visible characters.
        /// </summary>
        /// <param name="value">The string to mask.</param>
        /// <param name="visibleChars">The number of visible characters to reveal.</param>
        /// <param name="expected">The expected masked string.</param>
        var result = StringUtility.MaskSensitive(value, visibleChars);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    [InlineData("", false)]
    public void IsValidEmail_ShouldReturnExpectedResult(string email, bool expected)
    {
        /// <summary>
        /// Verifies that IsValidEmail returns the expected result for a given email address.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <param name="expected">The expected result of the validation.</param>
        StringUtility.IsValidEmail(email).Should().Be(expected);
    }

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)]
    [InlineData("invalid-guid", false)]
    public void IsValidGuid_ShouldReturnExpectedResult(string value, bool expected)
    {
        /// <summary>
        /// Verifies that IsValidGuid returns the expected result for a given GUID.
        /// </summary>
        /// <param name="value">The GUID to validate.</param>
        /// <param name="expected">The expected result of the validation.</param>
        StringUtility.IsValidGuid(value).Should().Be(expected);
    }

    [Fact]
    public void SplitLines_ShouldHandleDifferentLineEndings()
    {
        /// <summary>
        /// Verifies that SplitLines correctly splits a string into lines, handling different line endings.
        /// </summary>
        var input = "line1\r\nline2\rline3\nline4";
        var expected = new[] { "line1", "line2", "line3", "line4" };
        StringUtility.SplitLines(input).Should().Equal(expected);
    }

    [Fact]
    public void JoinReadable_ShouldFormatCorrectly()
    {
        /// <summary>
        /// Verifies that JoinReadable correctly formats a list of strings into a readable string.
        /// </summary>
        StringUtility.JoinReadable("a").Should().Be("a");
        StringUtility.JoinReadable("a", "b").Should().Be("a and b");
        StringUtility.JoinReadable("a", "b", "c").Should().Be("a, b, and c");
    }

    [Fact]
    public void RemoveWhitespace_ShouldRemoveAllWhitespace()
    {
        /// <summary>
        /// Verifies that RemoveWhitespace correctly removes all whitespace from a string.
        /// </summary>
        StringUtility.RemoveWhitespace("a b c \t d\n e").Should().Be("abcde");
    }

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("with space", "\"with space\"")]
    [InlineData("with=equal", "\"with=equal\"")]
    public void QuoteIfNeeded_ShouldQuoteWhenNecessary(string input, string expected)
    {
        /// <summary>
        /// Verifies that QuoteIfNeeded correctly quotes a string when necessary.
        /// </summary>
        /// <param name="input">The string to quote.</param>
        /// <param name="expected">The expected quoted string.</param>
        StringUtility.QuoteIfNeeded(input).Should().Be(expected);
    }

    [Fact]
    public void Repeat_ShouldRepeatCorrectly()
    {
        /// <summary>
        /// Verifies that Repeat correctly repeats a string the specified number of times.
        /// </summary>
        StringUtility.Repeat("abc", 3).Should().Be("abcabcabc");
        StringUtility.Repeat("a", 0).Should().Be("");
    }
}
