using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;

namespace DockerSqliteBackup.Tests;

public class StorageServiceJsonExtensionsTests
{
    private readonly Mock<ILogger<StorageService>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly StorageService _service;

    public StorageServiceJsonExtensionsTests()
    {
        _mockLogger = new Mock<ILogger<StorageService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _service = new StorageService(_mockLogger.Object, _mockServiceProvider.Object);
    }

    [Fact]
    public void ToJson_SerializesToEmptyObject()
    {
        var json = _service.ToJson();
        Assert.Equal("{}", json);
    }

    [Fact]
    public void ToJson_WithIndentation_ReturnsIndentedJson()
    {
        var json = _service.ToJson(indented: true);
        Assert.Contains("{}", json);
    }

    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageService)null!).ToJson());
    }

    [Fact]
    public void FromJson_ReturnsNull_ForEmptyString()
    {
        var result = StorageServiceJsonExtensions.FromJson("   ");
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ThrowsJsonException_ForInvalidJson()
    {
        Assert.Throws<JsonException>(() => StorageServiceJsonExtensions.FromJson("invalid"));
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_ForInvalidJson()
    {
        var result = StorageServiceJsonExtensions.TryFromJson("invalid", out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_ForEmptyString()
    {
        var result = StorageServiceJsonExtensions.TryFromJson("", out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_Throws_WhenConstructorParametersCannotBeBound()
    {
        // StorageService's only constructor takes ILogger/IServiceProvider, neither of which
        // maps to a JSON property, so System.Text.Json cannot materialize it even from "{}".
        Assert.Throws<InvalidOperationException>(() => StorageServiceJsonExtensions.TryFromJson("{}", out _));
    }
}
