// tests/docker-sqlite-backup.Tests/AzureConfigurationExtensionsTests.cs
namespace DockerSqliteBackup.Tests
{
    using Xunit;
    using System;
    using System.Globalization;
    using DockerSqliteBackup.Domain;

    public class AzureConfigurationExtensionsTests
    {
        [Fact]
        public void GetBlobUri_HappyPath_ReturnsUri()
        {
            // Arrange
            var configuration = new AzureConfiguration
            {
                SasUri = "https://account.blob.core.windows.net/",
                ContainerName = "container",
                BlobPrefix = "prefix"
            };

            // Act
            var result = AzureConfigurationExtensions.GetBlobUri(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("https://account.blob.core.windows.net/container/prefix", result.ToString());
        }

        [Fact]
        public void GetBlobUri_EmptyContainerName_ReturnsNull()
        {
            // Arrange
            var configuration = new AzureConfiguration
            {
                SasUri = "https://account.blob.core.windows.net/",
                ContainerName = string.Empty,
                BlobPrefix = "prefix"
            };

            // Act
            var result = AzureConfigurationExtensions.GetBlobUri(configuration);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBlobUri_NullConfiguration_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => AzureConfigurationExtensions.GetBlobUri(null));
        }

        [Fact]
        public void GetEffectiveConnectionString_HappyPath_ReturnsConnectionString()
        {
            // Arrange
            var configuration = new AzureConfiguration
            {
                SasUri = "https://account.blob.core.windows.net/",
                ContainerName = "container"
            };

            // Act
            var result = AzureConfigurationExtensions.GetEffectiveConnectionString(configuration);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BlobEndpoint=https://account.blob.core.windows.net/;SharedAccessSignature=...", result);
        }

        [Fact]
        public void GetEffectiveConnectionString_EmptySasUri_ReturnsNull()
        {
            // Arrange
            var configuration = new AzureConfiguration
            {
                SasUri = string.Empty,
                ContainerName = "container"
            };

            // Act
            var result = AzureConfigurationExtensions.GetEffectiveConnectionString(configuration);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetEffectiveConnectionString_NullConfiguration_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => AzureConfigurationExtensions.GetEffectiveConnectionString(null));
        }

        [Fact]
        public void WithImmutability_HappyPath_ReturnsModifiedConfiguration()
        {
            // Arrange
            var configuration = new AzureConfiguration();

            // Act
            var result = AzureConfigurationExtensions.WithImmutability(configuration, true);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.EnableImmutability);
        }

        [Fact]
        public void WithImmutability_NullConfiguration_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => AzureConfigurationExtensions.WithImmutability(null, true));
        }
    }
}
