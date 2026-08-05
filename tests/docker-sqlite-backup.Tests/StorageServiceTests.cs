// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Tests;

/// <summary>
/// Unit tests for <see cref="StorageService"/> covering constructor arguments,
/// null-argument validation and backend-agnostic behaviors not already
/// exercised by the local-filesystem integration tests.
/// </summary>
public class StorageServiceTests : IDisposable
{
    private readonly StorageService _sut;
    private readonly string _tempDir;

    public StorageServiceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<LocalStorageBackend>();
        services.AddSingleton<S3StorageBackend>();
        services.AddSingleton<AzureStorageBackend>();
        var serviceProvider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<StorageService>>().Object;
        _sut = new StorageService(logger, serviceProvider);

        _tempDir = Path.Combine(Path.GetTempPath(), $"storage-service-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private LocalStorageConfiguration MakeLocalConfig(string? subDir = null)
    {
        return new LocalStorageConfiguration
        {
            BaseDirectory = Path.Combine(_tempDir, subDir ?? "storage")
        };
    }

    [Fact]
    public async Task UploadBackupAsync_NullFilePath_ThrowsArgumentNullException()
    {
        var config = MakeLocalConfig();

        var act = async () => await _sut.UploadBackupAsync(null!, config);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UploadBackupAsync_NullConfig_ThrowsArgumentNullException()
    {
        var sourceFile = Path.Combine(_tempDir, "backup.sqlite");
        File.WriteAllText(sourceFile, "data");

        var act = async () => await _sut.UploadBackupAsync(sourceFile, null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DownloadBackupAsync_EmptyStoragePath_ThrowsArgumentException()
    {
        var config = MakeLocalConfig();

        var act = async () => await _sut.DownloadBackupAsync(string.Empty, config);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DownloadBackupAsync_NullConfig_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.DownloadBackupAsync("some/path.sqlite", null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TestConnectionAsync_ValidLocalDirectory_ReturnsTrue()
    {
        var config = MakeLocalConfig();

        var result = await _sut.TestConnectionAsync(config);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_UnknownConfigurationType_ReturnsFalseInsteadOfThrowing()
    {
        var config = new UnknownStorageConfiguration();

        var result = await _sut.TestConnectionAsync(config);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_UnknownConfigurationType_ReturnsZeroInsteadOfThrowing()
    {
        var config = new UnknownStorageConfiguration();

        var result = await _sut.GetAvailableSpaceAsync(config);

        result.Should().Be(0);
    }

    [Fact]
    public async Task ListBackupsAsync_UnknownConfigurationType_ThrowsStorageException()
    {
        var config = new UnknownStorageConfiguration();

        var act = async () => await _sut.ListBackupsAsync(config);

        await act.Should().ThrowAsync<StorageException>();
    }

    /// <summary>
    /// A minimal storage configuration subtype not registered with any backend,
    /// used to exercise the "unknown storage configuration type" branch.
    /// </summary>
    private sealed class UnknownStorageConfiguration : StorageConfiguration
    {
        public override int StorageType => -1;

        public override bool IsValid() => true;

        public override Task<bool> TestConnectionAsync() => Task.FromResult(true);
    }
}
