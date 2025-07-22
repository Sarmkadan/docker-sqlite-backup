// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public class EncryptionServiceTests : IAsyncLifetime
{
    private readonly AppSettings _settings;
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;
    private EncryptionService _sut = null!;
    private string _tempDir = string.Empty;

    public EncryptionServiceTests()
    {
        _settings = new AppSettings { EnableEncryption = false };
        _loggerMock = new Mock<ILogger<EncryptionService>>();
    }

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"enc-svc-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sut = new EncryptionService(_settings, _loggerMock.Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    private string CreateTempFile(string content = "sqlite-backup-test-data")
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.db");
        File.WriteAllText(path, content);
        return path;
    }

    // ── GenerateKey ───────────────────────────────────────────────────────────

    [Fact]
    public void GenerateKey_ReturnsValidBase64Key()
    {
        var key = _sut.GenerateKey();

        key.Should().NotBeNullOrWhiteSpace();
        var bytes = Convert.FromBase64String(key);
        bytes.Should().HaveCount(32, "AES-256 key must be 32 bytes");
    }

    [Fact]
    public void GenerateKey_EachCallReturnsUniqueKey()
    {
        var key1 = _sut.GenerateKey();
        var key2 = _sut.GenerateKey();

        key1.Should().NotBe(key2);
    }

    // ── ValidateKey ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateKey_ValidKey_ReturnsTrue()
    {
        var key = _sut.GenerateKey();
        _sut.ValidateKey(key).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-base64!!!")]
    [InlineData("dGVzdA==")] // valid base64 but only 4 bytes
    public void ValidateKey_InvalidInputs_ReturnsFalse(string? key)
    {
        _sut.ValidateKey(key).Should().BeFalse();
    }

    // ── IsEncryptionEnabled ───────────────────────────────────────────────────

    [Fact]
    public void IsEncryptionEnabled_WhenDisabled_ReturnsFalse()
    {
        _settings.EnableEncryption = false;
        _sut.IsEncryptionEnabled().Should().BeFalse();
    }

    [Fact]
    public void IsEncryptionEnabled_WhenEnabled_ReturnsTrue()
    {
        _settings.EnableEncryption = true;
        _sut.IsEncryptionEnabled().Should().BeTrue();
    }

    // ── GetStatus ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetStatus_WhenDisabledAndNoKey_ReturnsDisabledStatus()
    {
        _settings.EnableEncryption = false;
        _settings.EncryptionKey = null;

        var status = _sut.GetStatus();

        status.IsEnabled.Should().BeFalse();
        status.HasValidKey.Should().BeFalse();
        status.KeyFingerprint.Should().BeNull();
        status.KeySource.Should().Be("None");
        status.Summary.Should().Contain("disabled");
    }

    [Fact]
    public void GetStatus_WhenEnabledWithValidKey_ReturnsActiveStatus()
    {
        var key = EncryptionUtility.GenerateBase64Key();
        _settings.EnableEncryption = true;
        _settings.EncryptionKey = key;

        var status = _sut.GetStatus();

        status.IsEnabled.Should().BeTrue();
        status.HasValidKey.Should().BeTrue();
        status.KeyFingerprint.Should().NotBeNullOrWhiteSpace();
        status.KeyFingerprint!.Length.Should().Be(8);
        status.KeySource.Should().Be("Configuration");
        status.Summary.Should().Contain("active");
    }

    // ── EncryptFileAsync / DecryptFileAsync ───────────────────────────────────

    [Fact]
    public async Task EncryptFileAsync_ThenDecryptFileAsync_RoundTripProducesOriginalContent()
    {
        var key = EncryptionUtility.GenerateBase64Key();
        _settings.EnableEncryption = true;
        _settings.EncryptionKey = key;

        var original = "This is test backup content for round-trip verification.";
        var sourcePath = CreateTempFile(original);
        var encryptedPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.enc");
        var decryptedPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.db");

        await _sut.EncryptFileAsync(sourcePath, encryptedPath);
        await _sut.DecryptFileAsync(encryptedPath, decryptedPath);

        var restored = await File.ReadAllTextAsync(decryptedPath);
        restored.Should().Be(original);
    }

    [Fact]
    public async Task EncryptFileAsync_EncryptedFileIsDifferentFromPlaintext()
    {
        var key = EncryptionUtility.GenerateBase64Key();
        _settings.EnableEncryption = true;
        _settings.EncryptionKey = key;

        var sourcePath = CreateTempFile("plaintext backup data");
        var encryptedPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.enc");

        await _sut.EncryptFileAsync(sourcePath, encryptedPath);

        var originalBytes = await File.ReadAllBytesAsync(sourcePath);
        var encryptedBytes = await File.ReadAllBytesAsync(encryptedPath);
        encryptedBytes.Should().NotEqual(originalBytes);
    }

    [Fact]
    public async Task EncryptFileAsync_WhenEncryptionDisabled_ThrowsInvalidOperationException()
    {
        _settings.EnableEncryption = false;
        var sourcePath = CreateTempFile();
        var destPath = Path.Combine(_tempDir, "output.enc");

        var act = async () => await _sut.EncryptFileAsync(sourcePath, destPath);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Encryption key is not configured*");
    }

    [Fact]
    public async Task EncryptFileAsync_NonExistentSource_ThrowsFileNotFoundException()
    {
        var key = EncryptionUtility.GenerateBase64Key();
        _settings.EnableEncryption = true;
        _settings.EncryptionKey = key;

        var act = async () => await _sut.EncryptFileAsync(
            Path.Combine(_tempDir, "missing.db"),
            Path.Combine(_tempDir, "output.enc"));

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetActiveKey_WhenEnabledWithValidKey_ReturnsKey()
    {
        var key = EncryptionUtility.GenerateBase64Key();
        _settings.EnableEncryption = true;
        _settings.EncryptionKey = key;

        var activeKey = _sut.GetActiveKey();

        activeKey.Should().Be(key);
    }

    [Fact]
    public void GetActiveKey_WhenDisabled_ReturnsNull()
    {
        _settings.EnableEncryption = false;
        _settings.EncryptionKey = EncryptionUtility.GenerateBase64Key();

        _sut.GetActiveKey().Should().BeNull();
    }
}
