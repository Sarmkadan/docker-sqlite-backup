// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;
using System;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Extension methods for <see cref="StorageAdapterTests"/> that provide test utilities for storage adapter testing.
/// </summary>
public static class StorageAdapterTestsExtensions
{
    /// <summary>
    /// Creates a temporary file with the specified content in the test's temp directory and returns its path.
    /// </summary>
    /// <param name="test">The StorageAdapterTests instance.</param>
    /// <param name="content">The content to write to the file. Defaults to "backup-data".</param>
    /// <returns>The full path to the created temporary file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static string CreateTempFile(this StorageAdapterTests test, string content = "backup-data")
    {
        System.ArgumentNullException.ThrowIfNull(test);

        var path = Path.Combine(Path.GetTempPath(), $"storage-test-{Guid.NewGuid()}.sqlite");
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Creates a local storage configuration with the specified base directory.
    /// </summary>
    /// <param name="test">The StorageAdapterTests instance.</param>
    /// <param name="baseDirectory">The base directory for local storage.</param>
    /// <param name="name">Optional name for the configuration.</param>
    /// <returns>A configured LocalStorageConfiguration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="baseDirectory"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="baseDirectory"/> is empty or whitespace.</exception>
    public static LocalStorageConfiguration WithLocalStorage(this StorageAdapterTests test, string baseDirectory, string? name = null)
    {
        System.ArgumentNullException.ThrowIfNull(test);
        System.ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        return new LocalStorageConfiguration
        {
            Name = name ?? "test-config",
            BaseDirectory = baseDirectory
        };
    }

    /// <summary>
    /// Creates an Azure storage configuration with the specified parameters.
    /// </summary>
    /// <param name="test">The StorageAdapterTests instance.</param>
    /// <param name="connectionString">Optional connection string.</param>
    /// <param name="containerName">Container name.</param>
    /// <param name="sasUri">Optional SAS URI.</param>
    /// <param name="name">Optional name for the configuration.</param>
    /// <returns>A configured AzureConfiguration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static AzureConfiguration WithAzureStorage(
        this StorageAdapterTests test,
        string? connectionString = null,
        string? containerName = null,
        string? sasUri = null,
        string? name = null)
    {
        System.ArgumentNullException.ThrowIfNull(test);

        return new AzureConfiguration
        {
            Name = name ?? "azure-config",
            ConnectionString = connectionString ?? "UseDevelopmentStorage=true",
            ContainerName = containerName ?? "test-container",
            SasUri = sasUri
        };
    }

    /// <summary>
    /// Asserts that a LocalStorageException is thrown with the expected message.
    /// </summary>
    /// <param name="task">The async task that should throw.</param>
    /// <param name="expectedMessage">Optional expected message substring.</param>
    /// <returns>An assertion task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is <see langword="null"/>.</exception>
    public static async Task ShouldThrowLocalStorageExceptionAsync(this Task task, string? expectedMessage = null)
    {
        System.ArgumentNullException.ThrowIfNull(task);

        var act = async () => await task;
        if (expectedMessage == null)
        {
            await act.Should().ThrowAsync<LocalStorageException>();
        }
        else
        {
            await act.Should().ThrowAsync<LocalStorageException>()
                .Where(e => e.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Asserts that the backup file exists and has the expected content.
    /// </summary>
    /// <param name="filePath">The path to the backup file.</param>
    /// <param name="expectedContent">Optional expected content.</param>
    /// <returns>An async task.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <see langword="null"/>.</exception>
    public static async Task ShouldExistWithContentAsync(this string filePath, string? expectedContent = null)
    {
        System.ArgumentNullException.ThrowIfNull(filePath);

        File.Exists(filePath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(filePath);

        if (expectedContent != null)
        {
            content.Should().Be(expectedContent);
        }
        else
        {
            content.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// Asserts that the backup file does not exist.
    /// </summary>
    /// <param name="filePath">The path to the backup file.</param>
    /// <exception cref="ArgumentNullException"><paramref name="filePath"/> is <see langword="null"/>.</exception>
    public static void ShouldNotExist(this string filePath)
    {
        System.ArgumentNullException.ThrowIfNull(filePath);

        File.Exists(filePath).Should().BeFalse();
    }

    /// <summary>
    /// Creates a temporary directory and returns its path.
    /// </summary>
    /// <param name="test">The StorageAdapterTests instance.</param>
    /// <param name="path">Optional path (defaults to random guid).</param>
    /// <returns>The full path to the created directory.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    public static string CreateTempDirectory(this StorageAdapterTests test, string? path = null)
    {
        System.ArgumentNullException.ThrowIfNull(test);

        path ??= Path.Combine(Path.GetTempPath(), $"storage-test-dir-{Guid.NewGuid()}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Asserts that a directory exists.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static void DirectoryShouldExist(this string path)
    {
        System.ArgumentNullException.ThrowIfNull(path);

        Directory.Exists(path).Should().BeTrue($"Directory should exist at {path}");
    }

    /// <summary>
    /// Asserts that a directory does not exist.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static void DirectoryShouldNotExist(this string path)
    {
        System.ArgumentNullException.ThrowIfNull(path);

        Directory.Exists(path).Should().BeFalse($"Directory should not exist at {path}");
    }
}