#nullable enable
using System;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class StorageConfigurationExtensionsTests
{
    // ---------- DeepCopy -----------------------------------------------------

    [Fact]
    public void DeepCopy_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).DeepCopy());
    }

    [Fact]
    public void DeepCopy_Local_ReturnsEqualButSeparateInstance()
    {
        var original = new LocalStorageConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "LocalTest",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastModifiedAt = DateTime.UtcNow,
            BaseDirectory = "/tmp/backups",
            CreateSubdirectoriesBySchedule = true,
            FilePermissions = "644",
            CompressBackups = true,
            MinimumFreeSpaceBytes = 1_000_000,
            PreserveFileTimestamp = false
        };

        var copy = original.DeepCopy();

        Assert.IsType<LocalStorageConfiguration>(copy);
        var copyLocal = (LocalStorageConfiguration)copy;

        // All properties should be equal
        Assert.Equal(original.Id, copyLocal.Id);
        Assert.Equal(original.Name, copyLocal.Name);
        Assert.Equal(original.IsDefault, copyLocal.IsDefault);
        Assert.Equal(original.CreatedAt, copyLocal.CreatedAt);
        Assert.Equal(original.LastModifiedAt, copyLocal.LastModifiedAt);
        Assert.Equal(original.BaseDirectory, copyLocal.BaseDirectory);
        Assert.Equal(original.CreateSubdirectoriesBySchedule, copyLocal.CreateSubdirectoriesBySchedule);
        Assert.Equal(original.FilePermissions, copyLocal.FilePermissions);
        Assert.Equal(original.CompressBackups, copyLocal.CompressBackups);
        Assert.Equal(original.MinimumFreeSpaceBytes, copyLocal.MinimumFreeSpaceBytes);
        Assert.Equal(original.PreserveFileTimestamp, copyLocal.PreserveFileTimestamp);

        // Must be a different instance
        Assert.NotSame(original, copyLocal);
    }

    [Fact]
    public void DeepCopy_S3_ReturnsEqualButSeparateInstance()
    {
        var original = new S3Configuration
        {
            Id = Guid.NewGuid(),
            Name = "S3Test",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            LastModifiedAt = DateTime.UtcNow,
            AccessKeyId = "AKIA...",
            SecretAccessKey = "secret",
            BucketName = "my-bucket",
            RegionName = "us-east-1",
            ObjectKeyPrefix = "backups/",
            UseSSL = true,
            EnableServerSideEncryption = false,
            StorageClass = "STANDARD",
            CustomEndpoint = null,
            TransitionToGlacierDays = 30
        };

        var copy = original.DeepCopy();

        Assert.IsType<S3Configuration>(copy);
        var copyS3 = (S3Configuration)copy;

        Assert.Equal(original.Id, copyS3.Id);
        Assert.Equal(original.Name, copyS3.Name);
        Assert.Equal(original.IsDefault, copyS3.IsDefault);
        Assert.Equal(original.CreatedAt, copyS3.CreatedAt);
        Assert.Equal(original.LastModifiedAt, copyS3.LastModifiedAt);
        Assert.Equal(original.AccessKeyId, copyS3.AccessKeyId);
        Assert.Equal(original.SecretAccessKey, copyS3.SecretAccessKey);
        Assert.Equal(original.BucketName, copyS3.BucketName);
        Assert.Equal(original.RegionName, copyS3.RegionName);
        Assert.Equal(original.ObjectKeyPrefix, copyS3.ObjectKeyPrefix);
        Assert.Equal(original.UseSSL, copyS3.UseSSL);
        Assert.Equal(original.EnableServerSideEncryption, copyS3.EnableServerSideEncryption);
        Assert.Equal(original.StorageClass, copyS3.StorageClass);
        Assert.Equal(original.CustomEndpoint, copyS3.CustomEndpoint);
        Assert.Equal(original.TransitionToGlacierDays, copyS3.TransitionToGlacierDays);
        Assert.NotSame(original, copyS3);
    }

    [Fact]
    public void DeepCopy_Azure_ReturnsEqualButSeparateInstance()
    {
        var original = new AzureConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "AzureTest",
            IsDefault = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            LastModifiedAt = DateTime.UtcNow,
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=...",
            SasUri = "https://myaccount.blob.core.windows.net/?sv=...",
            ContainerName = "backups",
            BlobPrefix = "2023/",
            AccessTier = "Hot",
            EnableImmutability = true,
            SoftDeleteRetentionDays = 7
        };

        var copy = original.DeepCopy();

        Assert.IsType<AzureConfiguration>(copy);
        var copyAzure = (AzureConfiguration)copy;

        Assert.Equal(original.Id, copyAzure.Id);
        Assert.Equal(original.Name, copyAzure.Name);
        Assert.Equal(original.IsDefault, copyAzure.IsDefault);
        Assert.Equal(original.CreatedAt, copyAzure.CreatedAt);
        Assert.Equal(original.LastModifiedAt, copyAzure.LastModifiedAt);
        Assert.Equal(original.ConnectionString, copyAzure.ConnectionString);
        Assert.Equal(original.SasUri, copyAzure.SasUri);
        Assert.Equal(original.ContainerName, copyAzure.ContainerName);
        Assert.Equal(original.BlobPrefix, copyAzure.BlobPrefix);
        Assert.Equal(original.AccessTier, copyAzure.AccessTier);
        Assert.Equal(original.EnableImmutability, copyAzure.EnableImmutability);
        Assert.Equal(original.SoftDeleteRetentionDays, copyAzure.SoftDeleteRetentionDays);
        Assert.NotSame(original, copyAzure);
    }

    // ---------- Touch ---------------------------------------------------------

    [Fact]
    public void Touch_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).Touch());
    }

    [Fact]
    public void Touch_UpdatesLastModifiedAt()
    {
        var config = new LocalStorageConfiguration { LastModifiedAt = DateTime.UtcNow.AddHours(-1) };
        var before = config.LastModifiedAt;

        var result = config.Touch();

        Assert.Same(config, result);
        Assert.True(config.LastModifiedAt > before);
        // The timestamp should be very recent (within a second)
        Assert.InRange(config.LastModifiedAt, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
    }

    // ---------- IsCloudStorage ------------------------------------------------

    [Fact]
    public void IsCloudStorage_ReturnsTrueForS3AndAzure()
    {
        StorageConfiguration s3 = new S3Configuration();
        StorageConfiguration azure = new AzureConfiguration();

        Assert.True(s3.IsCloudStorage());
        Assert.True(azure.IsCloudStorage());
    }

    [Fact]
    public void IsCloudStorage_ReturnsFalseForLocal()
    {
        StorageConfiguration local = new LocalStorageConfiguration();

        Assert.False(local.IsCloudStorage());
    }

    [Fact]
    public void IsCloudStorage_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).IsCloudStorage());
    }

    // ---------- IsLocalStorage ------------------------------------------------

    [Fact]
    public void IsLocalStorage_ReturnsTrueForLocal()
    {
        StorageConfiguration local = new LocalStorageConfiguration();

        Assert.True(local.IsLocalStorage());
    }

    [Fact]
    public void IsLocalStorage_ReturnsFalseForCloud()
    {
        StorageConfiguration s3 = new S3Configuration();
        StorageConfiguration azure = new AzureConfiguration();

        Assert.False(s3.IsLocalStorage());
        Assert.False(azure.IsLocalStorage());
    }

    [Fact]
    public void IsLocalStorage_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).IsLocalStorage());
    }

    // ---------- GetDisplayName ------------------------------------------------

    [Fact]
    public void GetDisplayName_ReturnsCorrectStringForEachType()
    {
        var local = new LocalStorageConfiguration { Name = "LocalOne" };
        var s3 = new S3Configuration { Name = "S3One" };
        var azure = new AzureConfiguration { Name = "AzureOne" };
        var unknown = new TestStorageConfiguration { Name = "Base" };

        Assert.Equal("Local Storage: LocalOne", local.GetDisplayName());
        Assert.Equal("S3: S3One", s3.GetDisplayName());
        Assert.Equal("Azure: AzureOne", azure.GetDisplayName());
        // Fallback to Name for unknown types
        Assert.Equal("Base", unknown.GetDisplayName());
    }

    // ---------- ValidateName --------------------------------------------------

    [Fact]
    public void ValidateName_ReturnsTrueWhenNameIsValid()
    {
        var config = new LocalStorageConfiguration { Name = "ValidName" };
        Assert.True(config.ValidateName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateName_ReturnsFalseWhenNameIsInvalid(string? name)
    {
        var config = new LocalStorageConfiguration { Name = name! };
        Assert.False(config.ValidateName());
    }

    [Fact]
    public void ValidateName_NullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).ValidateName());
    }

    // ---------- GetAgeInDays --------------------------------------------------

    [Fact]
    public void GetAgeInDays_ReturnsCorrectNumberOfDays()
    {
        var config = new LocalStorageConfiguration
        {
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        Assert.Equal(5, config.GetAgeInDays());
    }

    [Fact]
    public void GetAgeInDays_FutureCreatedAt_ReturnsZero()
    {
        var config = new LocalStorageConfiguration
        {
            CreatedAt = DateTime.UtcNow.AddDays(2)
        };

        Assert.Equal(0, config.GetAgeInDays());
    }

    [Fact]
    public void GetAgeInDays_NullConfiguration_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((StorageConfiguration)null!).GetAgeInDays());
    }

    // -------------------------------------------------------------------------

    // Helper concrete class for the fallback case in GetDisplayName test.
    private sealed class TestStorageConfiguration : StorageConfiguration
    {
        public override int StorageType => -1;
        public override bool IsValid() => true;
        public override System.Threading.Tasks.Task<bool> TestConnectionAsync() => System.Threading.Tasks.Task.FromResult(true);
    }
}
