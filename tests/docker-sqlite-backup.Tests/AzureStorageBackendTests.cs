using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;

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

            // Act
            var result = await azureStorageBackend.UploadBackupAsync(filePath, storageConfiguration);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TestDownloadBackupAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storagePath = "test.txt";
            var storageConfiguration = new AzureConfiguration();

            // Act
            var result = await azureStorageBackend.DownloadBackupAsync(storagePath, storageConfiguration);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TestDeleteBackupAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storagePath = "test.txt";
            var storageConfiguration = new AzureConfiguration();

            // Act
            await azureStorageBackend.DeleteBackupAsync(storagePath, storageConfiguration);

            // Assert
            // No exception thrown
        }

        [Fact]
        public async Task TestListBackupsAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storageConfiguration = new AzureConfiguration();

            // Act
            var result = await azureStorageBackend.ListBackupsAsync(storageConfiguration);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task TestTestConnectionAsync_HappyPath()
        {
            // Arrange
            var logger = new Mock<ILogger<AzureStorageBackend>>();
            var azureStorageBackend = new AzureStorageBackend(logger.Object);
            var storageConfiguration = new AzureConfiguration();

            // Act
            var result = await azureStorageBackend.TestConnectionAsync(storageConfiguration);

            // Assert
            Assert.True(result);
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
            await Assert.ThrowsAsync<ArgumentNullException>(() => azureStorageBackend.UploadBackupAsync(filePath, storageConfiguration));
        }
    }
}
