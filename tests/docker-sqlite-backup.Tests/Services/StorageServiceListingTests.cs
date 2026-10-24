// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Verifies <see cref="StorageService.ListBackupInfosAsync"/> against the local filesystem backend.
/// </summary>
public class StorageServiceListingTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"storage-listing-tests-{Guid.NewGuid()}");
    private readonly StorageService _sut;

    public StorageServiceListingTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<LocalStorageBackend>();
        services.AddSingleton<S3StorageBackend>();
        services.AddSingleton<AzureStorageBackend>();
        var serviceProvider = services.BuildServiceProvider();
        _sut = new StorageService(new Mock<ILogger<StorageService>>().Object, serviceProvider);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task ListBackupInfosAsync_ExistingBackups_ReturnsNameSizeAndCreatedNewestFirst()
    {
        var scheduleDir = Path.Combine(_tempDir, "nightly");
        Directory.CreateDirectory(scheduleDir);

        var older = Path.Combine(scheduleDir, "backup_2026-10-01_02-00-00.sqlite");
        var newer = Path.Combine(scheduleDir, "backup_2026-10-02_02-00-00.sqlite");
        File.WriteAllText(older, "old-data");
        File.WriteAllText(newer, "new-backup-data");
        File.SetLastWriteTimeUtc(older, new DateTime(2026, 10, 1, 2, 0, 0, DateTimeKind.Utc));
        File.SetLastWriteTimeUtc(newer, new DateTime(2026, 10, 2, 2, 0, 0, DateTimeKind.Utc));

        var result = await _sut.ListBackupInfosAsync(CreateConfig());

        result.Should().HaveCount(2);
        result[0].Should().Be(new BackupFileInfo(
            "backup_2026-10-02_02-00-00.sqlite",
            "new-backup-data".Length,
            new DateTime(2026, 10, 2, 2, 0, 0, DateTimeKind.Utc)));
        result[1].Name.Should().Be("backup_2026-10-01_02-00-00.sqlite");
        result[1].Size.Should().Be("old-data".Length);
    }

    [Fact]
    public async Task ListBackupInfosAsync_MissingDirectory_ReturnsEmpty()
    {
        var result = await _sut.ListBackupInfosAsync(CreateConfig());

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBackupInfosAsync_NonBackupFiles_AreExcluded()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "notes.txt"), "not a backup");
        File.WriteAllText(Path.Combine(_tempDir, "backup_1.sqlite"), "data");

        var result = await _sut.ListBackupInfosAsync(CreateConfig());

        result.Select(backup => backup.Name).Should().Equal("backup_1.sqlite");
    }

    private LocalStorageConfiguration CreateConfig() => new()
    {
        Name = "Test",
        BaseDirectory = _tempDir,
    };
}
