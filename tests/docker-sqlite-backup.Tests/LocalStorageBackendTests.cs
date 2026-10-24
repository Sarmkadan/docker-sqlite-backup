using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Tests;

public class LocalStorageBackendTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"local-storage-tests-{Guid.NewGuid()}");
    private readonly LocalStorageBackend _sut = new(new Mock<ILogger<LocalStorageBackend>>().Object);

    public LocalStorageBackendTests()
    {
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private LocalStorageConfiguration CreateConfig(string baseDirectory, long minimumFreeSpaceBytes = 0) => new()
    {
        Name = "local-test",
        BaseDirectory = baseDirectory,
        MinimumFreeSpaceBytes = minimumFreeSpaceBytes
    };

    [Fact]
    public async Task UploadBackupAsync_ExistingFile_CopiesIntoMissingBaseDirectory()
    {
        // Arrange
        var source = Path.Combine(_root, "backup_daily.sqlite");
        await File.WriteAllTextAsync(source, "sqlite-bytes");
        var baseDir = Path.Combine(_root, "dest", "nested");
        var config = CreateConfig(baseDir);

        // Act
        var destPath = await _sut.UploadBackupAsync(source, config);

        // Assert
        destPath.Should().Be(Path.Combine(baseDir, "backup_daily.sqlite"));
        File.Exists(destPath).Should().BeTrue();
        (await File.ReadAllTextAsync(destPath)).Should().Be("sqlite-bytes");
    }

    [Fact]
    public async Task UploadBackupAsync_NullOrBlankPath_ThrowsArgumentException()
    {
        var config = CreateConfig(_root);

        await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.UploadBackupAsync(null!, config));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadBackupAsync("   ", config));
    }

    [Fact]
    public async Task UploadBackupAsync_MissingFile_ThrowsLocalStorageException()
    {
        var missing = Path.Combine(_root, "does-not-exist.sqlite");

        var ex = await Assert.ThrowsAsync<LocalStorageException>(() => _sut.UploadBackupAsync(missing, CreateConfig(_root)));

        ex.Message.Should().Contain("File not found");
    }

    [Fact]
    public async Task DownloadBackupAsync_ExistingFile_CopiesToTempDirectory()
    {
        // Arrange
        var storagePath = Path.Combine(_root, $"download-{Guid.NewGuid()}.sqlite");
        await File.WriteAllTextAsync(storagePath, "downloaded-bytes");

        // Act
        var tempPath = await _sut.DownloadBackupAsync(storagePath, CreateConfig(_root));

        try
        {
            // Assert
            tempPath.Should().Be(Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath)));
            (await File.ReadAllTextAsync(tempPath)).Should().Be("downloaded-bytes");
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task DeleteBackupAsync_ExistingAndMissingFile_RemovesWithoutThrowing()
    {
        // Arrange
        var storagePath = Path.Combine(_root, "to-delete.sqlite");
        await File.WriteAllTextAsync(storagePath, "x");
        var config = CreateConfig(_root);

        // Act
        await _sut.DeleteBackupAsync(storagePath, config);
        var deleteMissing = () => _sut.DeleteBackupAsync(storagePath, config);

        // Assert
        File.Exists(storagePath).Should().BeFalse();
        await deleteMissing.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListBackupsAsync_NestedDirectory_ReturnsOnlySqliteFiles()
    {
        // Arrange
        var baseDir = Path.Combine(_root, "backups");
        var nested = Path.Combine(baseDir, "hourly");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(Path.Combine(baseDir, "backup_a.sqlite"), "12345");
        await File.WriteAllTextAsync(Path.Combine(nested, "backup_b.sqlite.gz"), "1234567");
        await File.WriteAllTextAsync(Path.Combine(baseDir, "notes.txt"), "ignored");

        // Act
        var backups = (await _sut.ListBackupsAsync(CreateConfig(baseDir))).ToList();
        var missingDirBackups = await _sut.ListBackupsAsync(CreateConfig(Path.Combine(_root, "absent")));

        // Assert
        backups.Select(b => (Name: Path.GetFileName(b.Path), b.Size))
            .Should().BeEquivalentTo(new[] { ("backup_a.sqlite", 5L), ("backup_b.sqlite.gz", 7L) });
        missingDirBackups.Should().BeEmpty();
    }

    [Fact]
    public async Task TestConnectionAsync_WritableDirectory_ReturnsTrueAndUnwritablePath_ReturnsFalse()
    {
        // Arrange: a regular file used as a parent makes directory creation fail
        var blocker = Path.Combine(_root, "blocker");
        await File.WriteAllTextAsync(blocker, "not a directory");

        // Act & Assert
        (await _sut.TestConnectionAsync(CreateConfig(Path.Combine(_root, "writable")))).Should().BeTrue();
        (await _sut.TestConnectionAsync(CreateConfig(Path.Combine(blocker, "child")))).Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_LocalConfig_ReturnsNonNegativeAndRejectsOtherConfigTypes()
    {
        var available = await _sut.GetAvailableSpaceAsync(CreateConfig(_root));

        available.Should().BeGreaterThanOrEqualTo(0);
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAvailableSpaceAsync(new S3Configuration()));
    }
}
