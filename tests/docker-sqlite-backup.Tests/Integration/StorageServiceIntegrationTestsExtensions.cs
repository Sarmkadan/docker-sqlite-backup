// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Integration;

/// <summary>
/// Extension methods for StorageServiceIntegrationTests to provide additional test utilities
/// and helper methods for common test scenarios.
/// </summary>
public static class StorageServiceIntegrationTestsExtensions
{
    /// <summary>
    /// Creates a temporary file in the test's temp directory with the specified content.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="name">The filename</param>
    /// <param name="content">The file content (default: "sqlite backup data")</param>
    /// <returns>The full path to the created file</returns>
    public static string CreateTempFile(this StorageServiceIntegrationTests test, string name, string content = "sqlite backup data")
    {
        var path = Path.Combine(test.GetTempDir(), "source", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a LocalStorageConfiguration pointing to a subdirectory within the test's temp directory.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="subDir">Optional subdirectory name (default: "storage")</param>
    /// <returns>A configured LocalStorageConfiguration</returns>
    public static LocalStorageConfiguration MakeLocalConfig(this StorageServiceIntegrationTests test, string? subDir = null)
    {
        return new LocalStorageConfiguration
        {
            BaseDirectory = Path.Combine(test.GetTempDir(), subDir ?? "storage")
        };
    }

    /// <summary>
    /// Gets the temporary directory path used by the test.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>The temp directory path</returns>
    public static string GetTempDir(this StorageServiceIntegrationTests test)
    {
        return test.GetFieldValue<string>("_tempDir");
    }

    /// <summary>
    /// Gets the StorageService instance under test.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>The StorageService instance</returns>
    public static StorageService GetStorageService(this StorageServiceIntegrationTests test)
    {
        return test.GetFieldValue<StorageService>("_sut");
    }

    /// <summary>
    /// Verifies that a backup file exists at the specified path and contains expected content.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="filePath">The path to verify</param>
    /// <param name="expectedContent">The expected content (default: "sqlite backup data")</param>
    public static void VerifyBackupFile(this StorageServiceIntegrationTests test, string filePath, string expectedContent = "sqlite backup data")
    {
        File.Exists(filePath).Should().BeTrue($"Backup file should exist at {filePath}");
        File.ReadAllText(filePath).Should().Be(expectedContent, "Backup file should contain expected content");
    }

    /// <summary>
    /// Creates a backup file with random content of specified size.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="name">The filename</param>
    /// <param name="size">The size in bytes (default: 1024)</param>
    /// <returns>The full path to the created file</returns>
    public static string CreateRandomBackupFile(this StorageServiceIntegrationTests test, string name, int size = 1024)
    {
        var path = Path.Combine(test.GetTempDir(), "random", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var randomContent = new byte[size];
        new Random().NextBytes(randomContent);
        File.WriteAllBytes(path, randomContent);

        return path;
    }

    /// <summary>
    /// Asserts that a backup tuple represents the expected file.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="actual">The actual backup tuple</param>
    /// <param name="expectedPath">The expected file path</param>
    public static void ShouldMatchBackupPath(this StorageServiceIntegrationTests test, (string Path, long Size, DateTime Modified) actual, string expectedPath)
    {
        actual.Path.Should().Be(expectedPath, "Backup tuple path should match");
        Path.GetFileName(actual.Path).Should().Be(Path.GetFileName(expectedPath), "Backup tuple filename should match");
    }

    /// <summary>
    /// Creates multiple backup files in a directory and returns their paths.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="count">Number of files to create</param>
    /// <param name="prefix">Filename prefix (default: "backup")</param>
    /// <returns>List of created file paths</returns>
    public static List<string> CreateMultipleBackupFiles(this StorageServiceIntegrationTests test, int count, string prefix = "backup")
    {
        var paths = new List<string>();
        var storageDir = Path.Combine(test.GetTempDir(), "multi");
        Directory.CreateDirectory(storageDir);

        for (int i = 0; i < count; i++)
        {
            var path = Path.Combine(storageDir, $"{prefix}{i}.sqlite");
            File.WriteAllText(path, $"backup content {i}");
            paths.Add(path);
        }

        return paths;
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="filePath">The file path</param>
    /// <returns>The file size in bytes</returns>
    public static long GetFileSize(this StorageServiceIntegrationTests test, string filePath)
    {
        return new FileInfo(filePath).Length;
    }

    /// <summary>
    /// Creates a backup tuple with the specified properties.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="name">The backup name</param>
    /// <param name="size">The backup size in bytes</param>
    /// <param name="timestamp">Optional timestamp (default: DateTime.UtcNow)</param>
    /// <returns>A backup tuple (Path, Size, Modified)</returns>
    public static (string Path, long Size, DateTime Modified) CreateBackupTuple(this StorageServiceIntegrationTests test, string name, long size, DateTime? timestamp = null)
    {
        return (Path.Combine(test.GetTempDir(), name), size, timestamp ?? DateTime.UtcNow);
    }

    private static T GetFieldValue<T>(this StorageServiceIntegrationTests test, string fieldName)
    {
        var field = typeof(StorageServiceIntegrationTests).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (T)field!.GetValue(test)!;
    }
}