// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public static class EncryptionServiceTestsExtensions
{
    /// <summary>
    /// Creates a new EncryptionService instance with encryption enabled and a valid key.
    /// </summary>
    public static EncryptionService CreateEnabledService(this EncryptionServiceTests _)
    {
        var settings = new AppSettings
        {
            EnableEncryption = true,
            EncryptionKey = EncryptionUtility.GenerateBase64Key()
        };
        var loggerMock = new Mock<ILogger<EncryptionService>>();
        return new EncryptionService(settings, loggerMock.Object);
    }

    /// <summary>
    /// Creates a new EncryptionService instance with encryption disabled.
    /// </summary>
    public static EncryptionService CreateDisabledService(this EncryptionServiceTests _)
    {
        var settings = new AppSettings { EnableEncryption = false };
        var loggerMock = new Mock<ILogger<EncryptionService>>();
        return new EncryptionService(settings, loggerMock.Object);
    }

    /// <summary>
    /// Asserts that encryption is enabled on the service.
    /// </summary>
    public static void ShouldBeEncryptionEnabled(this EncryptionService service)
    {
        service.IsEncryptionEnabled().Should().BeTrue("encryption should be enabled");
    }

    /// <summary>
    /// Asserts that encryption is disabled on the service.
    /// </summary>
    public static void ShouldBeEncryptionDisabled(this EncryptionService service)
    {
        service.IsEncryptionEnabled().Should().BeFalse("encryption should be disabled");
    }

    /// <summary>
    /// Creates a temporary file with the specified content and returns its path.
    /// </summary>
    public static string CreateTempFile(this EncryptionServiceTests _, string content = "test-data", string? tempDir = null)
    {
        var directory = string.IsNullOrEmpty(tempDir)
            ? Path.Combine(Path.GetTempPath(), $"enc-svc-ext-tests-{Guid.NewGuid()}")
            : tempDir;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid()}.db");
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Asserts that a file exists and is not empty.
    /// </summary>
    public static void ShouldExistAndNotBeEmpty(this string filePath)
    {
        File.Exists(filePath).Should().BeTrue($"file should exist: {filePath}");
        new FileInfo(filePath).Length.Should().BeGreaterThan(0, "file should not be empty");
    }

    /// <summary>
    /// Encrypts a file and returns the encrypted file path.
    /// </summary>
    public static async Task<string> EncryptToTempFileAsync(this EncryptionService service, string content = "test-backup-data")
    {
        var sourcePath = ((EncryptionServiceTests)null!).CreateTempFile(content);
        var encryptedPath = Path.Combine(Path.GetTempPath(), $"enc-{Guid.NewGuid()}.enc");
        await service.EncryptFileAsync(sourcePath, encryptedPath);
        return encryptedPath;
    }

    /// <summary>
    /// Asserts that two files have different content.
    /// </summary>
    public static async Task ShouldHaveDifferentContentAsync(this string filePath1, string filePath2)
    {
        var bytes1 = await File.ReadAllBytesAsync(filePath1);
        var bytes2 = await File.ReadAllBytesAsync(filePath2);
        bytes1.Should().NotEqual(bytes2, "files should have different content");
    }

    /// <summary>
    /// Asserts that decryption of an encrypted file produces the original content.
    /// </summary>
    public static async Task ShouldRoundTripSuccessfullyAsync(this EncryptionService service, string encryptedPath, string originalContent)
    {
        var decryptedPath = Path.Combine(Path.GetTempPath(), $"dec-{Guid.NewGuid()}.db");
        await service.DecryptFileAsync(encryptedPath, decryptedPath);

        var restored = await File.ReadAllTextAsync(decryptedPath);
        restored.Should().Be(originalContent, "decrypted content should match original");
    }
}