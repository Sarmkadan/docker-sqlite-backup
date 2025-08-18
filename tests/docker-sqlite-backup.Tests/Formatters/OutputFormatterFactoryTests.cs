// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Formatters;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Formatters;

public class OutputFormatterFactoryTests
{
    private readonly OutputFormatterFactory _sut;

    public OutputFormatterFactoryTests()
    {
        _sut = new OutputFormatterFactory();
    }

    [Fact]
    public void GetFormatter_JsonFormat_ReturnsJsonFormatter()
    {
        var formatter = _sut.GetFormatter("json");

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("JSON");
    }

    [Fact]
    public void GetFormatter_CsvFormat_ReturnsCsvFormatter()
    {
        var formatter = _sut.GetFormatter("csv");

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("CSV");
    }

    [Fact]
    public void GetFormatter_XmlFormat_ReturnsXmlFormatter()
    {
        var formatter = _sut.GetFormatter("xml");

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("XML");
    }

    [Fact]
    public void GetFormatter_CaseInsensitive_ReturnsFormatter()
    {
        var upperFormatter = _sut.GetFormatter("JSON");
        var lowerFormatter = _sut.GetFormatter("json");
        var mixedFormatter = _sut.GetFormatter("Json");

        upperFormatter.Should().NotBeNull();
        lowerFormatter.Should().NotBeNull();
        mixedFormatter.Should().NotBeNull();
    }

    [Fact]
    public void GetFormatter_UnknownFormat_ThrowsArgumentException()
    {
        var act = () => _sut.GetFormatter("yaml");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*yaml*");
    }

    [Fact]
    public void GetFormatterOrDefault_NullFormat_ReturnsJsonFormatter()
    {
        var formatter = _sut.GetFormatterOrDefault(null);

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("JSON");
    }

    [Fact]
    public void GetFormatterOrDefault_EmptyFormat_ReturnsJsonFormatter()
    {
        var formatter = _sut.GetFormatterOrDefault(string.Empty);

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("JSON");
    }

    [Fact]
    public void GetFormatterOrDefault_UnknownFormat_ReturnsJsonFormatter()
    {
        var formatter = _sut.GetFormatterOrDefault("protobuf");

        formatter.Should().NotBeNull();
        formatter.Name.Should().Be("JSON");
    }

    [Fact]
    public void GetFormatterOrDefault_UnknownFormatWithCustomDefault_ReturnsCustomDefault()
    {
        var csvFormatter = new CsvOutputFormatter();

        var result = _sut.GetFormatterOrDefault("unknown", csvFormatter);

        result.Should().BeSameAs(csvFormatter);
    }

    [Fact]
    public void RegisterFormatter_CustomFormatter_CanBeRetrievedByName()
    {
        var customFormatter = new JsonOutputFormatter(prettyPrint: false);
        _sut.RegisterFormatter("compact", customFormatter);

        var retrieved = _sut.GetFormatter("compact");

        retrieved.Should().BeSameAs(customFormatter);
    }

    [Fact]
    public void GetAvailableFormats_ReturnsAtLeastThreeFormats()
    {
        var formats = _sut.GetAvailableFormats().ToList();

        formats.Should().HaveCountGreaterThanOrEqualTo(3);
        formats.Should().Contain(f => f.Equals("json", StringComparison.OrdinalIgnoreCase));
        formats.Should().Contain(f => f.Equals("csv", StringComparison.OrdinalIgnoreCase));
        formats.Should().Contain(f => f.Equals("xml", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetCompactJsonFormatter_ReturnsJsonFormatter()
    {
        var formatter = _sut.GetCompactJsonFormatter();

        formatter.Should().NotBeNull();
    }

    [Fact]
    public void JsonFormatter_Format_ProducesValidJson()
    {
        var formatter = _sut.GetFormatter("json");
        var data = new { Name = "test", Value = 42 };

        var result = formatter.Format(data);

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("test");
        result.Should().Contain("42");
    }

    [Fact]
    public void JsonFormatter_FormatNull_ReturnsNullString()
    {
        var formatter = _sut.GetFormatter("json");

        var result = formatter.Format(null);

        result.Should().Be("null");
    }
}
