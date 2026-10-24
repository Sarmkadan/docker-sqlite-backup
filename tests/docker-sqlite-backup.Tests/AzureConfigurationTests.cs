#nullable enable
// Author: Vladyslav Zaiets

using Xunit;
using DockerSqliteBackup.Domain;
using System;
using System.Threading.Tasks;

namespace DockerSqliteBackup.Tests
{
    public class AzureConfigurationTests
    {
        private static AzureConfiguration CreateValidConfiguration() => new()
        {
            Name = "azure-main",
            ContainerName = "backups",
            ConnectionString = "UseDevelopmentStorage=true"
        };

        [Fact]
        public void Constructor_Defaults_MatchDocumentedValues()
        {
            // Arrange & Act
            var config = new AzureConfiguration();

            // Assert
            Assert.Equal((int)DockerSqliteBackup.Constants.StorageType.Azure, config.StorageType);
            Assert.Null(config.ConnectionString);
            Assert.Null(config.SasUri);
            Assert.Equal(string.Empty, config.ContainerName);
            Assert.Equal("backups/", config.BlobPrefix);
            Assert.Equal("Cool", config.AccessTier);
            Assert.False(config.EnableImmutability);
            Assert.Equal(0, config.SoftDeleteRetentionDays);
        }

        [Fact]
        public void IsValid_ConnectionStringOrSasUriWithNameAndContainer_ReturnsTrue()
        {
            // Arrange
            var connectionStringConfig = CreateValidConfiguration();
            var sasConfig = new AzureConfiguration
            {
                Name = "azure-sas",
                ContainerName = "backups",
                SasUri = "https://account.blob.core.windows.net/backups?sv=2024-01-01"
            };

            // Act & Assert
            Assert.True(connectionStringConfig.IsValid());
            Assert.True(sasConfig.IsValid());
        }

        [Theory]
        [InlineData("Hot")]
        [InlineData("Cool")]
        [InlineData("Archive")]
        public void IsValid_SupportedAccessTier_ReturnsTrue(string tier)
        {
            // Arrange
            var config = CreateValidConfiguration();
            config.AccessTier = tier;

            // Act & Assert
            Assert.True(config.IsValid());
        }

        [Theory]
        [InlineData("hot")]
        [InlineData("COOL")]
        [InlineData("Standard")]
        [InlineData(" Cool")]
        [InlineData("")]
        public void IsValid_UnsupportedAccessTier_ReturnsFalse(string tier)
        {
            // Arrange: tier names are case-sensitive, so "hot" must be rejected
            var config = CreateValidConfiguration();
            config.AccessTier = tier;

            // Act & Assert
            Assert.False(config.IsValid());
        }

        [Fact]
        public void IsValid_MissingRequiredFields_ReturnsFalse()
        {
            // Arrange
            var noName = CreateValidConfiguration();
            noName.Name = "   ";

            var blankContainer = CreateValidConfiguration();
            blankContainer.ContainerName = " ";

            var noCredentials = CreateValidConfiguration();
            noCredentials.ConnectionString = null;

            var blankCredentials = CreateValidConfiguration();
            blankCredentials.ConnectionString = "  ";

            var nullTier = CreateValidConfiguration();
            nullTier.AccessTier = null!;

            // Act & Assert
            Assert.False(noName.IsValid());
            Assert.False(blankContainer.IsValid());
            Assert.False(noCredentials.IsValid());
            Assert.False(blankCredentials.IsValid());
            Assert.False(nullTier.IsValid());
        }

        [Fact]
        public void Name_SetToNull_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new AzureConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config.Name = null!);
        }

        [Fact]
        public async Task TestConnectionAsync_NoUsableCredentials_ReturnsFalseWithoutThrowing()
        {
            // Arrange: no credentials at all, then a SAS URI that cannot be parsed.
            // Both paths fail before any network call and must be swallowed into false.
            var noCredentials = new AzureConfiguration { ContainerName = "backups" };
            var malformedSas = new AzureConfiguration { ContainerName = "backups", SasUri = "not a uri" };

            // Act
            var noCredentialsResult = await noCredentials.TestConnectionAsync();
            var malformedSasResult = await malformedSas.TestConnectionAsync();

            // Assert
            Assert.False(noCredentialsResult);
            Assert.False(malformedSasResult);
        }
    }
}
