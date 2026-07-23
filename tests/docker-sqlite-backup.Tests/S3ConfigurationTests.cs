using Xunit;
using DockerSqliteBackup.Domain;
using System;
using System.Threading.Tasks;

namespace DockerSqliteBackup.Tests
{
    public class S3ConfigurationTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var config = new S3Configuration();

            // Assert
            Assert.Equal(string.Empty, config.AccessKeyId);
            Assert.Equal(string.Empty, config.SecretAccessKey);
            Assert.Equal(string.Empty, config.BucketName);
            Assert.Equal("us-east-1", config.RegionName);
            Assert.Equal("backups/", config.ObjectKeyPrefix);
            Assert.True(config.UseSSL);
            Assert.True(config.EnableServerSideEncryption);
            Assert.Equal("STANDARD", config.StorageClass);
            Assert.Null(config.CustomEndpoint);
            Assert.Null(config.TransitionToGlacierDays);
            Assert.True(config.EnableStreamingUploads);
            Assert.Equal(16 * 1024 * 1024, config.MultipartPartSizeBytes);
        }

        [Fact]
        public void Properties_SetAndGet_ReturnsCorrectValues()
        {
            // Arrange
            var config = new S3Configuration();
            var accessKey = "AKIAIOSFODNN7EXAMPLE";
            var secretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
            var bucketName = "my-backup-bucket";
            var regionName = "eu-west-1";
            var objectKeyPrefix = "prod-backups/";
            var useSSL = false;
            var enableSSE = false;
            var storageClass = "GLACIER";
            var customEndpoint = "https://s3.custom-endpoint.com";
            var transitionDays = 30;

            // Act
            config.AccessKeyId = accessKey;
            config.SecretAccessKey = secretKey;
            config.BucketName = bucketName;
            config.RegionName = regionName;
            config.ObjectKeyPrefix = objectKeyPrefix;
            config.UseSSL = useSSL;
            config.EnableServerSideEncryption = enableSSE;
            config.StorageClass = storageClass;
            config.CustomEndpoint = customEndpoint;
            config.TransitionToGlacierDays = transitionDays;

            // Assert
            Assert.Equal(accessKey, config.AccessKeyId);
            Assert.Equal(secretKey, config.SecretAccessKey);
            Assert.Equal(bucketName, config.BucketName);
            Assert.Equal(regionName, config.RegionName);
            Assert.Equal(objectKeyPrefix, config.ObjectKeyPrefix);
            Assert.Equal(useSSL, config.UseSSL);
            Assert.Equal(enableSSE, config.EnableServerSideEncryption);
            Assert.Equal(storageClass, config.StorageClass);
            Assert.Equal(customEndpoint, config.CustomEndpoint);
            Assert.Equal(transitionDays, config.TransitionToGlacierDays);
        }

        [Fact]
        public void IsValid_WithValidConfiguration_ReturnsTrue()
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithNullOrEmptyName_ReturnsFalse(string? name)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = name,
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithNullOrEmptyAccessKeyId_ReturnsFalse(string? accessKeyId)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = accessKeyId,
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValid_WithShortAccessKeyId_ReturnsFalse()
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "SHORT",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithNullOrEmptySecretAccessKey_ReturnsFalse(string? secretAccessKey)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = secretAccessKey,
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void IsValid_WithShortSecretAccessKey_ReturnsFalse()
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "SHORT",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithNullOrEmptyBucketName_ReturnsFalse(string? bucketName)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = bucketName,
                RegionName = "us-east-1"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void IsValid_WithNullOrEmptyRegionName_ReturnsFalse(string? regionName)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = regionName
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("INVALID_CLASS")]
        [InlineData("invalid")]
        public void IsValid_WithInvalidStorageClass_ReturnsFalse(string storageClass)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1",
                StorageClass = storageClass
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData("STANDARD")]
        [InlineData("standard")]
        [InlineData("Standard")]
        [InlineData("REDUCED_REDUNDANCY")]
        [InlineData("STANDARD_IA")]
        [InlineData("ONEZONE_IA")]
        [InlineData("INTELLIGENT_TIERING")]
        [InlineData("GLACIER")]
        [InlineData("DEEP_ARCHIVE")]
        public void IsValid_WithValidStorageClass_ReturnsTrue(string storageClass)
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
                SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                BucketName = "my-backup-bucket",
                RegionName = "us-east-1",
                StorageClass = storageClass
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValidStorageClass_WithValidClasses_ReturnsTrue()
        {
            // Arrange
            var validClasses = new[] { "STANDARD", "REDUCED_REDUNDANCY", "STANDARD_IA", "ONEZONE_IA", "INTELLIGENT_TIERING", "GLACIER", "DEEP_ARCHIVE" };

            // Act & Assert
            foreach (var storageClass in validClasses)
            {
                Assert.True(S3Configuration.IsValidStorageClass(storageClass));
            }
        }

        [Fact]
        public void IsValidStorageClass_WithInvalidClass_ReturnsFalse()
        {
            // Arrange
            var invalidClasses = new[] { "INVALID_CLASS", "", " ", null };

            // Act & Assert
            foreach (var storageClass in invalidClasses)
            {
                Assert.False(S3Configuration.IsValidStorageClass(storageClass));
            }
        }

        [Fact]
        public void IsValidStorageClass_WithCaseInsensitiveComparison_ReturnsTrue()
        {
            // Arrange
            var caseVariants = new[] { "standard", "Standard", "STANDARD", "glacier", "GLACIER", "deep_archive" };

            // Act & Assert
            foreach (var storageClass in caseVariants)
            {
                Assert.True(S3Configuration.IsValidStorageClass(storageClass));
            }
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidConfiguration_ReturnsFalseWithoutCredentials()
        {
            // Arrange
            var config = new S3Configuration
            {
                Name = "TestS3Config",
                AccessKeyId = "",
                SecretAccessKey = "",
                BucketName = "test-bucket",
                RegionName = "us-east-1"
            };

            // Act
            var result = await config.TestConnectionAsync();

            // Assert - Should return false since credentials are empty
            Assert.False(result);
        }

        [Fact]
        public void EnableStreamingUploads_DefaultValue_IsTrue()
        {
            // Arrange & Act
            var config = new S3Configuration();

            // Assert
            Assert.True(config.EnableStreamingUploads);
        }

        [Fact]
        public void MultipartPartSizeBytes_DefaultValue_Is16MB()
        {
            // Arrange & Act
            var config = new S3Configuration();

            // Assert
            Assert.Equal(16 * 1024 * 1024, config.MultipartPartSizeBytes);
        }
    }
}