// Author: Vladyslav Zaiets
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EncryptionService"/> that verify encryption functionality including key generation,
/// validation, status reporting, and file encryption/decryption operations.
/// </summary>
public class EncryptionServiceTests : IAsyncLifetime
{
	/// <summary>
	/// Gets the application settings used for encryption configuration.
	/// </summary>
	private readonly AppSettings _settings;

	/// <summary>
	/// Mock logger for <see cref="EncryptionService"/> to verify logging behavior.
	/// </summary>
	private readonly Mock<ILogger<EncryptionService>> _loggerMock;

	/// <summary>
	/// Instance of the service under test.
	/// </summary>
	private EncryptionService _sut = null!;

	/// <summary>
	/// Temporary directory used for test file operations.
	/// </summary>
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

	/// <summary>
	/// Creates a temporary file with the specified content for testing purposes.
	/// </summary>
	/// <param name="content">The content to write to the temporary file. Defaults to "sqlite-backup-test-data".</param>
	/// <returns>The full path to the created temporary file.</returns>
	private string CreateTempFile(string content = "sqlite-backup-test-data")
	{
		var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.db");
		File.WriteAllText(path, content);
		return path;
	}

	// ── GenerateKey ───────────────────────────────────────────────────────────

	[Fact]
	/// <summary>
	/// Tests that GenerateKey returns a valid base64-encoded AES-256 encryption key.
	/// </summary>
	public void GenerateKey_ReturnsValidBase64Key()
	{
		var key = _sut.GenerateKey();

		key.Should().NotBeNullOrWhiteSpace();
		var bytes = Convert.FromBase64String(key);
		bytes.Should().HaveCount(32, "AES-256 key must be 32 bytes");
	}

	[Fact]
	/// <summary>
	/// Tests that each call to GenerateKey returns a unique encryption key.
	/// </summary>
	public void GenerateKey_EachCallReturnsUniqueKey()
	{
		var key1 = _sut.GenerateKey();
		var key2 = _sut.GenerateKey();

		key1.Should().NotBe(key2);
	}

	// ── ValidateKey ───────────────────────────────────────────────────────────

	[Fact]
	/// <summary>
	/// Tests that ValidateKey returns true for a valid encryption key.
	/// </summary>
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
	/// <summary>
	/// Tests that ValidateKey returns false for invalid key inputs including null, empty,
	/// malformed base64, and base64 that decodes to less than 32 bytes.
	/// </summary>
	/// <param name="key">The invalid key to test.</param>
	public void ValidateKey_InvalidInputs_ReturnsFalse(string? key)
	{
		_sut.ValidateKey(key).Should().BeFalse();
	}

	// ── IsEncryptionEnabled ───────────────────────────────────────────────────

	[Fact]
	/// <summary>
	/// Tests that IsEncryptionEnabled returns false when encryption is disabled in settings.
	/// </summary>
	public void IsEncryptionEnabled_WhenDisabled_ReturnsFalse()
	{
		_settings.EnableEncryption = false;
		_sut.IsEncryptionEnabled().Should().BeFalse();
	}

	[Fact]
	/// <summary>
	/// Tests that IsEncryptionEnabled returns true when encryption is enabled in settings.
	/// </summary>
	public void IsEncryptionEnabled_WhenEnabled_ReturnsTrue()
	{
		_settings.EnableEncryption = true;
		_sut.IsEncryptionEnabled().Should().BeTrue();
	}

	// ── GetStatus ─────────────────────────────────────────────────────────────

	[Fact]
	/// <summary>
	/// Tests that GetStatus returns a disabled status when encryption is disabled and no key is configured.
	/// </summary>
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
	/// <summary>
	/// Tests that GetStatus returns an active status when encryption is enabled with a valid key.
	/// </summary>
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
	/// <summary>
	/// Tests that EncryptFileAsync followed by DecryptFileAsync produces the original content,
	/// verifying the round-trip encryption/decryption process works correctly.
	/// </summary>
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
	/// <summary>
	/// Tests that the encrypted file is different from the plaintext file,
	/// confirming that encryption actually transforms the data.
	/// </summary>
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
	/// <summary>
	/// Tests that EncryptFileAsync throws InvalidOperationException when encryption is disabled,
	/// verifying that encryption operations are properly guarded by the enabled flag.
	/// </summary>
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
	/// <summary>
	/// Tests that EncryptFileAsync throws FileNotFoundException when the source file doesn't exist,
	/// verifying proper error handling for missing files.
	/// </summary>
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
	/// <summary>
	/// Tests that GetActiveKey returns the configured encryption key when encryption is enabled.
	/// </summary>
	public async Task GetActiveKey_WhenEnabledWithValidKey_ReturnsKey()
	{
		var key = EncryptionUtility.GenerateBase64Key();
		_settings.EnableEncryption = true;
		_settings.EncryptionKey = key;

		var activeKey = _sut.GetActiveKey();

		activeKey.Should().Be(key);
	}

	[Fact]
	/// <summary>
	/// Tests that GetActiveKey returns null when encryption is disabled,
	/// even if a key is configured in settings.
	/// </summary>
	public void GetActiveKey_WhenDisabled_ReturnsNull()
	{
		_settings.EnableEncryption = false;
		_settings.EncryptionKey = EncryptionUtility.GenerateBase64Key();

		_sut.GetActiveKey().Should().BeNull();
	}
}