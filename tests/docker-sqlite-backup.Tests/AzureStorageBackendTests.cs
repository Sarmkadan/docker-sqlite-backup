using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Tests
{
    public class AzureStorageBackendTests
    {
        [Fact]
        public async Task TestUploadBackupAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var filePath = "test.txt";
            var storageConfiguration = new AzureConfiguration();

            // Act & Assert - without credentials, upload should throw
            await Assert.ThrowsAsync<LocalStorageException>(
                () => azureStorageBackend.UploadBackupAsync(filePath, storageConfiguration));
        }

        [Fact]
        public async Task TestDownloadBackupAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storagePath = "test.txt";
            var storageConfiguration = new AzureConfiguration();

            // Act & Assert - without credentials, download should throw
            await Assert.ThrowsAsync<AzureStorageException>(
                () => azureStorageBackend.DownloadBackupAsync(storagePath, storageConfiguration));
        }

        [Fact]
        public async Task TestDeleteBackupAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storagePath = "test.txt";
            var storageConfiguration = new AzureConfiguration();

            // Act & Assert - without credentials, delete should throw
            await Assert.ThrowsAsync<AzureStorageException>(
                () => azureStorageBackend.DeleteBackupAsync(storagePath, storageConfiguration));
        }

        [Fact]
        public async Task TestListBackupsAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storageConfiguration = new AzureConfiguration();

            // Act & Assert - without credentials, list should throw
            await Assert.ThrowsAsync<AzureStorageException>(
                () => azureStorageBackend.ListBackupsAsync(storageConfiguration));
        }

        [Fact]
        public async Task TestTestConnectionAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storageConfiguration = new AzureConfiguration();

            // Act - without credentials, connection test returns false
            var result = await azureStorageBackend.TestConnectionAsync(storageConfiguration);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TestGetAvailableSpaceAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storageConfiguration = new AzureConfiguration();

            // Act
            var result = await azureStorageBackend.GetAvailableSpaceAsync(storageConfiguration);

            // Assert
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public async Task TestUploadBackupAsync_NullFilePath_ThrowsArgumentNullException()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            string? filePath = null;
            var storageConfiguration = new AzureConfiguration();

            // Act and Assert
            await Assert.ThrowsAsync<System.ArgumentNullException>(() => azureStorageBackend.UploadBackupAsync(filePath, storageConfiguration));
        }
    }
}
