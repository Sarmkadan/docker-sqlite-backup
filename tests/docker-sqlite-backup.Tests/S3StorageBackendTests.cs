using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Tests;

public class S3StorageBackendTests
{
    private readonly Mock<ILogger<S3StorageBackend>> _loggerMock;
    private readonly Mock<IBackupEventPublisher> _eventPublisherMock;
    private readonly S3StorageBackend _s3StorageBackend;

    public S3StorageBackendTests()
    {
        _loggerMock = new Mock<ILogger<S3StorageBackend>>();
        _eventPublisherMock = new Mock<IBackupEventPublisher>();
        _s3StorageBackend = new S3StorageBackend(_loggerMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task GetAvailableSpaceAsync_ReturnsMaxValue()
    {
        var config = new S3Configuration();
        var result = await _s3StorageBackend.GetAvailableSpaceAsync(config);
        result.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task UploadBackupAsync_ThrowsArgumentNullException_WhenFilePathIsNull()
    {
        await Assert.ThrowsAsync<System.ArgumentNullException>(() => _s3StorageBackend.UploadBackupAsync(null!, new S3Configuration()));
    }

    [Fact]
    public async Task UploadBackupAsync_ThrowsArgumentException_WhenFilePathIsInvalid()
    {
        var config = new S3Configuration();
        await Assert.ThrowsAsync<System.ArgumentException>(() => _s3StorageBackend.UploadBackupAsync("", config));
    }

    [Fact]
    public async Task UploadBackupAsync_ThrowsLocalStorageException_WhenFileDoesNotExist()
    {
        var config = new S3Configuration();
        await Assert.ThrowsAsync<LocalStorageException>(() => _s3StorageBackend.UploadBackupAsync("nonexistent_file.db", config));
    }

    [Fact]
    public async Task DownloadBackupAsync_ThrowsArgumentNullException_WhenStoragePathIsNull()
    {
        await Assert.ThrowsAsync<System.ArgumentNullException>(() => _s3StorageBackend.DownloadBackupAsync(null!, new S3Configuration()));
    }

    [Fact]
    public async Task DeleteBackupAsync_ThrowsArgumentNullException_WhenStoragePathIsNull()
    {
        await Assert.ThrowsAsync<System.ArgumentNullException>(() => _s3StorageBackend.DeleteBackupAsync(null!, new S3Configuration()));
    }

    [Fact]
    public async Task ListBackupsAsync_ThrowsArgumentNullException_WhenConfigIsNull()
    {
        await Assert.ThrowsAsync<System.ArgumentNullException>(() => _s3StorageBackend.ListBackupsAsync(null!));
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowsArgumentNullException_WhenConfigIsNull()
    {
        await Assert.ThrowsAsync<System.ArgumentNullException>(() => _s3StorageBackend.TestConnectionAsync(null!));
    }
}
