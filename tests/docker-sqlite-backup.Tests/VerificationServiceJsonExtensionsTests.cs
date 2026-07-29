using DockerSqliteBackup.Services;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;

namespace DockerSqliteBackup.Tests;

public class VerificationServiceJsonExtensionsTests
{
    private readonly Mock<IBackupRepository> _mockRepository;
    private readonly Mock<AppSettings> _mockAppSettings;
    private readonly Mock<ILogger<VerificationService>> _mockLogger;
    private readonly VerificationService _service;

    public VerificationServiceJsonExtensionsTests()
    {
        _mockRepository = new Mock<IBackupRepository>();
        _mockAppSettings = new Mock<AppSettings>();
        _mockLogger = new Mock<ILogger<VerificationService>>();
        _service = new VerificationService(_mockRepository.Object, _mockAppSettings.Object, _mockLogger.Object);
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
        Assert.Throws<ArgumentNullException>(() => ((VerificationService)null!).ToJson());
    }

    [Fact]
    public void FromJson_ReturnsNull_ForEmptyString()
    {
        var result = VerificationServiceJsonExtensions.FromJson("   ");
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_ThrowsJsonException_ForInvalidJson()
    {
        Assert.Throws<JsonException>(() => VerificationServiceJsonExtensions.FromJson("invalid"));
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_ForInvalidJson()
    {
        var result = VerificationServiceJsonExtensions.TryFromJson("invalid", out var value);
        Assert.False(result);
        Assert.Null(value);
    }
}
