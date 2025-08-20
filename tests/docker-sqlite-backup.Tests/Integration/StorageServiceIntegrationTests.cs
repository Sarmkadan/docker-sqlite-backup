// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Integration;

/// <summary>
/// Integration tests for StorageService using the local filesystem backend.
/// Tests the real upload, download, delete, and list operations end-to-end.
/// </summary>
public class StorageServiceIntegrationTests : IDisposable
{
    private readonly StorageService _sut;
    private readonly string _tempDir;

    public StorageServiceIntegrationTests()
    {
        var logger = new Mock<ILogger<StorageService>>().Object;
        _sut = new StorageService(logger);
        _tempDir = Path.Combine(Path.GetTempPath(), $"storage-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string CreateTempFile(string name, string content = "sqlite backup data")
    {
        var path = Path.Combine(_tempDir, "source", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    private LocalStorageConfiguration MakeLocalConfig(string? subDir = null)
    {
        return new LocalStorageConfiguration
        {
            BaseDirectory = Path.Combine(_tempDir, subDir ?? "storage")
        };
    }

    [Fact]
    public async Task UploadBackupAsync_LocalStorage_CopiesFileToDestination()
    {
        var sourceFile = CreateTempFile("backup.sqlite");
        var config = MakeLocalConfig("uploads");

        var resultPath = await _sut.UploadBackupAsync(sourceFile, config);

        resultPath.Should().EndWith("backup.sqlite");
        File.Exists(resultPath).Should().BeTrue();
        File.ReadAllText(resultPath).Should().Be("sqlite backup data");
    }

    [Fact]
    public async Task UploadBackupAsync_FileDoesNotExist_ThrowsLocalStorageException()
    {
        var missingFile = Path.Combine(_tempDir, "nonexistent.sqlite");
        var config = MakeLocalConfig();

        var act = async () => await _sut.UploadBackupAsync(missingFile, config);

        await act.Should().ThrowAsync<LocalStorageException>();
    }

    [Fact]
    public async Task UploadBackupAsync_CreatesDestinationDirectoryIfMissing()
    {
        var sourceFile = CreateTempFile("backup-new.sqlite");
        var config = MakeLocalConfig("new-storage-dir");

        Directory.Exists(config.BaseDirectory).Should().BeFalse();

        await _sut.UploadBackupAsync(sourceFile, config);

        Directory.Exists(config.BaseDirectory).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadBackupAsync_ExistingLocalFile_CopiesFileToTemp()
    {
        var sourcePath = Path.Combine(_tempDir, "download-source.sqlite");
        File.WriteAllText(sourcePath, "backup content");

        var config = MakeLocalConfig();
        var tempPath = await _sut.DownloadBackupAsync(sourcePath, config);

        try
        {
            File.Exists(tempPath).Should().BeTrue();
            File.ReadAllText(tempPath).Should().Be("backup content");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task DeleteBackupAsync_ExistingLocalFile_DeletesIt()
    {
        var filePath = Path.Combine(_tempDir, "to-delete.sqlite");
        File.WriteAllText(filePath, "delete me");

        await _sut.DeleteBackupAsync(filePath, MakeLocalConfig());

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteBackupAsync_NonExistentLocalFile_DoesNotThrow()
    {
        var act = async () => await _sut.DeleteBackupAsync(
            Path.Combine(_tempDir, "ghost.sqlite"),
            MakeLocalConfig());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListBackupsAsync_DirectoryWithSqliteFiles_ReturnsCorrectEntries()
    {
        var storageDir = Path.Combine(_tempDir, "list-storage");
        Directory.CreateDirectory(storageDir);
        File.WriteAllText(Path.Combine(storageDir, "backup1.sqlite"), new string('A', 100));
        File.WriteAllText(Path.Combine(storageDir, "backup2.sqlite"), new string('B', 200));
        File.WriteAllText(Path.Combine(storageDir, "not-a-backup.txt"), "ignore me");

        var config = new LocalStorageConfiguration { BaseDirectory = storageDir };
        var backups = (await _sut.ListBackupsAsync(config)).ToList();

        backups.Should().HaveCount(2);
        backups.Should().AllSatisfy(b => b.Path.Should().EndWith(".sqlite"));
        backups.Should().AllSatisfy(b => b.Size.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task ListBackupsAsync_EmptyDirectory_ReturnsEmptyList()
    {
        var emptyDir = Path.Combine(_tempDir, "empty-storage");
        Directory.CreateDirectory(emptyDir);
        var config = new LocalStorageConfiguration { BaseDirectory = emptyDir };

        var backups = await _sut.ListBackupsAsync(config);

        backups.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBackupsAsync_NonExistentDirectory_ReturnsEmptyList()
    {
        var config = new LocalStorageConfiguration { BaseDirectory = Path.Combine(_tempDir, "ghost-dir") };

        var backups = await _sut.ListBackupsAsync(config);

        backups.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_LocalStorage_ReturnsPositiveValue()
    {
        var config = MakeLocalConfig();

        var space = await _sut.GetAvailableSpaceAsync(config);

        space.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_S3Storage_ReturnsMaxValue()
    {
        var config = new S3Configuration
        {
            BucketName = "my-bucket",
            RegionName = "us-east-1"
        };

        var space = await _sut.GetAvailableSpaceAsync(config);

        space.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task UploadBackupAsync_UnknownStorageType_ThrowsStorageException()
    {
        var unknownConfig = new Mock<StorageConfiguration>().Object;
        var source = CreateTempFile("backup-unknown.sqlite");

        var act = async () => await _sut.UploadBackupAsync(source, unknownConfig);

        await act.Should().ThrowAsync<StorageException>();
    }

    [Fact]
    public async Task FullWorkflow_UploadThenListThenDelete_WorksEndToEnd()
    {
        var storageDir = Path.Combine(_tempDir, "workflow-storage");
        var config = new LocalStorageConfiguration { BaseDirectory = storageDir };
        var sourceFile = CreateTempFile("workflow-backup.sqlite");

        // Upload
        var uploadedPath = await _sut.UploadBackupAsync(sourceFile, config);
        File.Exists(uploadedPath).Should().BeTrue();

        // List
        var backups = (await _sut.ListBackupsAsync(config)).ToList();
        backups.Should().HaveCount(1);
        backups[0].Path.Should().Be(uploadedPath);

        // Delete
        await _sut.DeleteBackupAsync(uploadedPath, config);
        File.Exists(uploadedPath).Should().BeFalse();

        // List again - should be empty
        var backupsAfterDelete = await _sut.ListBackupsAsync(config);
        backupsAfterDelete.Should().BeEmpty();
    }
}
