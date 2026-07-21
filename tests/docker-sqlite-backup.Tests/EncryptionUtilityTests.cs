using System.IO;
using System.Security.Cryptography;
using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class EncryptionUtilityTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _plaintextFile;
    private readonly string _encryptedFile;
    private readonly string _decryptedFile;
    private readonly string _validKey;

    public EncryptionUtilityTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        _plaintextFile = Path.Combine(_testDirectory, "plaintext.txt");
        _encryptedFile = Path.Combine(_testDirectory, "encrypted.bin");
        _decryptedFile = Path.Combine(_testDirectory, "decrypted.txt");

        File.WriteAllText(_plaintextFile, "Hello, World! This is a test file for encryption.");
        _validKey = EncryptionUtility.GenerateBase64Key();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDirectory, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public async Task EncryptFileAsync_EncryptsFileWithValidKey()
    {
        // Act
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);

        // Assert
        File.Exists(_encryptedFile).Should().BeTrue();
        var fileInfo = new FileInfo(_encryptedFile);
        fileInfo.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DecryptFileAsync_DecryptsFileWithValidKey()
    {
        // Arrange
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);

        // Act
        await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, _validKey);

        // Assert
        File.Exists(_decryptedFile).Should().BeTrue();
        var decryptedContent = await File.ReadAllTextAsync(_decryptedFile);
        decryptedContent.Should().Be("Hello, World! This is a test file for encryption.");
    }

    [Fact]
    public async Task EncryptThenDecrypt_RoundTrip_ReturnsOriginalContent()
    {
        // Arrange
        var originalContent = "Round-trip test content with special chars: !@#$%^&*()";
        await File.WriteAllTextAsync(_plaintextFile, originalContent);

        // Act
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);
        await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, _validKey);

        // Assert
        var decryptedContent = await File.ReadAllTextAsync(_decryptedFile);
        decryptedContent.Should().Be(originalContent);
    }

    [Fact]
    public async Task DecryptFileAsync_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);
        var wrongKey = EncryptionUtility.GenerateBase64Key();

        // Act & Assert
        var act = async () => await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, wrongKey);
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task DecryptFileAsync_WithInvalidKey_ThrowsArgumentException()
    {
        // Arrange
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);
        var invalidKey = "invalid-key";

        // Act & Assert
        var act = async () => await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, invalidKey);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DecryptFileAsync_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);
        var emptyKey = string.Empty;

        // Act & Assert
        var act = async () => await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, emptyKey);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DecryptFileAsync_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);

        // Act & Assert
        var act = async () => await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, null!);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EncryptFileAsync_WithEmptyFile_Works()
    {
        // Arrange
        var emptyFile = Path.Combine(_testDirectory, "empty.txt");
        File.WriteAllText(emptyFile, string.Empty);
        var emptyEncrypted = Path.Combine(_testDirectory, "empty_encrypted.bin");
        var emptyDecrypted = Path.Combine(_testDirectory, "empty_decrypted.txt");

        // Act
        await EncryptionUtility.EncryptFileAsync(emptyFile, emptyEncrypted, _validKey);
        await EncryptionUtility.DecryptFileAsync(emptyEncrypted, emptyDecrypted, _validKey);

        // Assert
        File.Exists(emptyDecrypted).Should().BeTrue();
        var decryptedContent = await File.ReadAllTextAsync(emptyDecrypted);
        decryptedContent.Should().BeEmpty();
    }

    [Fact]
    public async Task EncryptFileAsync_WithLargeFile_Works()
    {
        // Arrange
        var largeContent = new string('A', 1024 * 1024); // 1MB file
        await File.WriteAllTextAsync(_plaintextFile, largeContent);

        // Act
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, _encryptedFile, _validKey);
        await EncryptionUtility.DecryptFileAsync(_encryptedFile, _decryptedFile, _validKey);

        // Assert
        var decryptedContent = await File.ReadAllTextAsync(_decryptedFile);
        decryptedContent.Should().Be(largeContent);
    }

    [Fact]
    public async Task EncryptFileAsync_ProducesDifferentCiphertextEachTime()
    {
        // Arrange
        var originalContent = "Test content for ciphertext variation";
        await File.WriteAllTextAsync(_plaintextFile, originalContent);

        var encryptedFile1 = Path.Combine(_testDirectory, "encrypted1.bin");
        var encryptedFile2 = Path.Combine(_testDirectory, "encrypted2.bin");

        // Act
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, encryptedFile1, _validKey);
        await EncryptionUtility.EncryptFileAsync(_plaintextFile, encryptedFile2, _validKey);

        // Assert
        var ciphertext1 = await File.ReadAllBytesAsync(encryptedFile1);
        var ciphertext2 = await File.ReadAllBytesAsync(encryptedFile2);

        ciphertext1.Should().NotBeEquivalentTo(ciphertext2);
    }

    [Fact]
    public void IsValidKey_WithValidBase64Key_ReturnsTrue()
    {
        // Arrange
        var validKey = EncryptionUtility.GenerateBase64Key();

        // Act & Assert
        EncryptionUtility.IsValidKey(validKey).Should().BeTrue();
    }

    [Fact]
    public void IsValidKey_WithInvalidBase64_ReturnsFalse()
    {
        // Arrange
        var invalidKey = "not-valid-base64!!!";

        // Act & Assert
        EncryptionUtility.IsValidKey(invalidKey).Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithEmptyString_ReturnsFalse()
    {
        // Act & Assert
        EncryptionUtility.IsValidKey(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithNull_ReturnsFalse()
    {
        // Act & Assert
        EncryptionUtility.IsValidKey(null).Should().BeFalse();
    }

    [Fact]
    public void IsValidKey_WithWrongLengthKey_ReturnsFalse()
    {
        // Arrange
        var wrongLengthKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)); // 16 bytes instead of 32

        // Act & Assert
        EncryptionUtility.IsValidKey(wrongLengthKey).Should().BeFalse();
    }

    [Fact]
    public async Task DecryptFileAsync_WithShortFile_ThrowsInvalidDataException()
    {
        // Arrange
        var shortFile = Path.Combine(_testDirectory, "short.bin");
        await File.WriteAllBytesAsync(shortFile, new byte[10]); // Too short for IV

        // Act & Assert
        var act = async () => await EncryptionUtility.DecryptFileAsync(shortFile, _decryptedFile, _validKey);
        await act.Should().ThrowAsync<InvalidDataException>();
    }
}