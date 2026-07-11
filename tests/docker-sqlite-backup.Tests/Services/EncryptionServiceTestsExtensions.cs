// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Extension methods for testing <see cref="EncryptionService"/> functionality.
/// </summary>
public static class EncryptionServiceTestsExtensions
{
    /// <summary>
    /// Creates a new <see cref="EncryptionService"/> instance with encryption enabled and a valid key.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <returns>A new <see cref="EncryptionService"/> instance with encryption enabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="_"/> is <see langword="null"/>.</exception>
    public static EncryptionService CreateEnabledService(this EncryptionServiceTests _)
    {
        ArgumentNullException.ThrowIfNull(_);

        var settings = new AppSettings
        {
            EnableEncryption = true,
            EncryptionKey = EncryptionUtility.GenerateBase64Key()
        };
        var loggerMock = new Mock<ILogger<EncryptionService>>();
        return new EncryptionService(settings, loggerMock.Object);
    }

    /// <summary>
    /// Creates a new <see cref="EncryptionService"/> instance with encryption disabled.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <returns>A new <see cref="EncryptionService"/> instance with encryption disabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="_"/> is <see langword="null"/>.</exception>
    public static EncryptionService CreateDisabledService(this EncryptionServiceTests _)
    {
        ArgumentNullException.ThrowIfNull(_);

        var settings = new AppSettings { EnableEncryption = false };
        var loggerMock = new Mock<ILogger<EncryptionService>>();
        return new EncryptionService(settings, loggerMock.Object);
    }

    /// <summary>
    /// Asserts that encryption is enabled on the <see cref="EncryptionService"/>.
    /// </summary>
    /// <param name="service">The <see cref="EncryptionService"/> to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static void ShouldBeEncryptionEnabled(this EncryptionService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        service.IsEncryptionEnabled().Should().BeTrue("encryption should be enabled");
    }

    /// <summary>
    /// Asserts that encryption is disabled on the <see cref="EncryptionService"/>.
    /// </summary>
    /// <param name="service">The <see cref="EncryptionService"/> to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    public static void ShouldBeEncryptionDisabled(this EncryptionService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        service.IsEncryptionEnabled().Should().BeFalse("encryption should be disabled");
    }

    /// <summary>
    /// Creates a temporary file with the specified content and returns its path.
    /// </summary>
    /// <param name="_">Test context parameter (unused).</param>
    /// <param name="content">The content to write to the temporary file.</param>
    /// <param name="tempDir">Optional temporary directory path. If <see langword="null"/> or empty, a new directory is created in the system temp path.</param>
    /// <returns>The full path to the created temporary file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="_"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">Thrown when file or directory operations fail.</exception>
    public static string CreateTempFile(this EncryptionServiceTests _, string content = "test-data", string? tempDir = null)
    {
        ArgumentNullException.ThrowIfNull(_);
        ArgumentNullException.ThrowIfNull(content);

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
    /// <param name="filePath">The path to the file to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath"/> is empty or whitespace.</exception>
    public static void ShouldExistAndNotBeEmpty(this string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentException.ThrowIfNullOrEmpty(filePath.Trim(), nameof(filePath));

        File.Exists(filePath).Should().BeTrue($"file should exist: {filePath}");
        new FileInfo(filePath).Length.Should().BeGreaterThan(0, "file should not be empty");
    }

    /// <summary>
    /// Encrypts a file with the specified content and returns the encrypted file path.
    /// </summary>
    /// <param name="service">The <see cref="EncryptionService"/> instance to use for encryption.</param>
    /// <param name="content">The content to encrypt. Defaults to "test-backup-data".</param>
    /// <returns>The full path to the encrypted file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is <see langword="null"/>.</exception>
    public static async Task<string> EncryptToTempFileAsync(this EncryptionService service, string content = "test-backup-data")
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(content);

        var sourcePath = ((EncryptionServiceTests)null!).CreateTempFile(content);
        var encryptedPath = Path.Combine(Path.GetTempPath(), $"enc-{Guid.NewGuid()}.enc");
        await service.EncryptFileAsync(sourcePath, encryptedPath);
        return encryptedPath;
    }

    /// <summary>
    /// Asserts that two files have different content.
    /// </summary>
    /// <param name="filePath1">The first file path to compare.</param>
    /// <param name="filePath2">The second file path to compare.</param>
    /// <exception cref="ArgumentNullException"><paramref name="filePath1"/> or <paramref name="filePath2"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="filePath1"/> or <paramref name="filePath2"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when either file does not exist.</exception>
    public static async Task ShouldHaveDifferentContentAsync(this string filePath1, string filePath2)
    {
        ArgumentNullException.ThrowIfNull(filePath1);
        ArgumentNullException.ThrowIfNull(filePath2);
        ArgumentException.ThrowIfNullOrEmpty(filePath1.Trim(), nameof(filePath1));
        ArgumentException.ThrowIfNullOrEmpty(filePath2.Trim(), nameof(filePath2));

        var bytes1 = await File.ReadAllBytesAsync(filePath1);
        var bytes2 = await File.ReadAllBytesAsync(filePath2);
        bytes1.Should().NotEqual(bytes2, "files should have different content");
    }

    /// <summary>
    /// Asserts that decryption of an encrypted file produces the original content.
    /// </summary>
    /// <param name="service">The <see cref="EncryptionService"/> instance to use for decryption.</param>
    /// <param name="encryptedPath">The path to the encrypted file.</param>
    /// <param name="originalContent">The expected original content after decryption.</param>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="encryptedPath"/> or <paramref name="originalContent"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="encryptedPath"/> or <paramref name="originalContent"/> is empty or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the encrypted file does not exist.</exception>
    public static async Task ShouldRoundTripSuccessfullyAsync(this EncryptionService service, string encryptedPath, string originalContent)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(encryptedPath);
        ArgumentNullException.ThrowIfNull(originalContent);
        ArgumentException.ThrowIfNullOrEmpty(encryptedPath.Trim(), nameof(encryptedPath));
        ArgumentException.ThrowIfNullOrEmpty(originalContent.Trim(), nameof(originalContent));

        var decryptedPath = Path.Combine(Path.GetTempPath(), $"dec-{Guid.NewGuid()}.db");
        await service.DecryptFileAsync(encryptedPath, decryptedPath);

        var restored = await File.ReadAllTextAsync(decryptedPath);
        restored.Should().Be(originalContent, "decrypted content should match original");
    }
}