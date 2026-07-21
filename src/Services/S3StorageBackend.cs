#nullable enable

using Amazon.S3;
using Amazon.S3.Model;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Storage backend implementation for AWS S3 storage.
/// </summary>
public sealed class S3StorageBackend : IStorageBackend
{
    private readonly ILogger<S3StorageBackend> _logger;

    public S3StorageBackend(ILogger<S3StorageBackend> logger)
    {
        _logger = logger;
    }

    public async Task<string> UploadBackupAsync(string filePath, StorageConfiguration config)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(nameof(filePath), "File path cannot be null or whitespace.");
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File not found: {FilePath}", filePath);
            throw new LocalStorageException($"File not found: {filePath}");
        }

        if (config is not S3Configuration s3Config)
        {
            throw new ArgumentException("Configuration must be S3Configuration for S3StorageBackend", nameof(config));
        }

        try
        {
            _logger.LogInformation("Uploading backup to S3: {FilePath}", filePath);
            return await UploadToS3Async(filePath, s3Config);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            _logger.LogError(ex, "Failed to upload backup to S3: {FilePath}", filePath);
            throw new S3StorageException("Failed to upload backup to S3", ex);
        }
    }

    public async Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config)
    {
        if (storagePath == null)
        {
            throw new ArgumentNullException(nameof(storagePath));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ArgumentException(nameof(storagePath), "Storage path cannot be null or whitespace.");
        }

        if (config is not S3Configuration s3Config)
        {
            throw new ArgumentException("Configuration must be S3Configuration for S3StorageBackend", nameof(config));
        }

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath));

        try
        {
            await DownloadFromS3Async(storagePath, tempFile, s3Config);
            _logger.LogInformation("Successfully downloaded backup from S3: {StoragePath}", storagePath);
            return tempFile;
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new S3StorageException("Failed to download backup from S3", ex);
        }
    }

    public async Task DeleteBackupAsync(string storagePath, StorageConfiguration config)
    {
        if (storagePath == null)
        {
            throw new ArgumentNullException(nameof(storagePath));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not S3Configuration s3Config)
        {
            throw new ArgumentException("Configuration must be S3Configuration for S3StorageBackend", nameof(config));
        }

        try
        {
            await DeleteFromS3Async(storagePath, s3Config);
            _logger.LogInformation("Successfully deleted backup from S3: {StoragePath}", storagePath);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new S3StorageException("Failed to delete backup from S3", ex);
        }
    }

    public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not S3Configuration s3Config)
        {
            throw new ArgumentException("Configuration must be S3Configuration for S3StorageBackend", nameof(config));
        }

        try
        {
            return await ListS3BackupsAsync(s3Config);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new S3StorageException("Failed to list S3 backups", ex);
        }
    }

    public async Task<bool> TestConnectionAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not S3Configuration s3Config)
        {
            throw new ArgumentException("Configuration must be S3Configuration for S3StorageBackend", nameof(config));
        }

        try
        {
            var configObj = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Config.RegionName ?? "us-east-1"),
                UseAccelerateEndpoint = false
            };

            if (!string.IsNullOrEmpty(s3Config.CustomEndpoint))
            {
                configObj.ServiceURL = s3Config.CustomEndpoint;
            }

            using var client = new AmazonS3Client(s3Config.AccessKeyId, s3Config.SecretAccessKey, configObj);
            var response = await client.ListBucketsAsync();
            return response?.Buckets?.Any(b => b.BucketName == s3Config.BucketName) ?? false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> GetAvailableSpaceAsync(StorageConfiguration config)
    {
        // S3 doesn't have a concept of "available space" in the same way as local storage
        // Return long.MaxValue to indicate unlimited space
        await Task.CompletedTask;
        return long.MaxValue;
    }

    private async Task<string> UploadToS3Async(string filePath, S3Configuration config)
    {
        using var s3Client = CreateS3Client(config);
        var fileName = Path.GetFileName(filePath);
        var key = Path.Combine(config.ObjectKeyPrefix, fileName).Replace("\\", "/");

        var request = new PutObjectRequest
        {
            BucketName = config.BucketName,
            Key = key,
            FilePath = filePath,
            ServerSideEncryptionMethod = config.EnableServerSideEncryption ? ServerSideEncryptionMethod.AES256 : null,
            StorageClass = S3StorageClass.FindValue(
                string.IsNullOrWhiteSpace(config.StorageClass) ? "STANDARD" : config.StorageClass)
        };

        await s3Client.PutObjectAsync(request);
        _logger.LogInformation("Successfully uploaded backup to S3: {Key}", key);

        return key;
    }

    private async Task DownloadFromS3Async(string s3Key, string localPath, S3Configuration config)
    {
        using var s3Client = CreateS3Client(config);
        var request = new GetObjectRequest
        {
            BucketName = config.BucketName,
            Key = s3Key
        };

        using var response = await s3Client.GetObjectAsync(request);
        await response.WriteResponseStreamToFileAsync(localPath, false, CancellationToken.None);
    }

    private async Task DeleteFromS3Async(string s3Key, S3Configuration config)
    {
        using var s3Client = CreateS3Client(config);
        var request = new DeleteObjectRequest
        {
            BucketName = config.BucketName,
            Key = s3Key
        };

        await s3Client.DeleteObjectAsync(request);
    }

    private async Task<IEnumerable<(string, long, DateTime)>> ListS3BackupsAsync(S3Configuration config)
    {
        using var s3Client = CreateS3Client(config);
        var request = new ListObjectsV2Request
        {
            BucketName = config.BucketName,
            Prefix = config.ObjectKeyPrefix
        };

        var response = await s3Client.ListObjectsV2Async(request);
        return response.S3Objects.Select(obj =>
            (obj.Key, obj.Size, obj.LastModified.ToUniversalTime())
        ).ToList();
    }

    private AmazonS3Client CreateS3Client(S3Configuration config)
    {
        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.RegionName ?? "us-east-1")
        };

        if (!string.IsNullOrEmpty(config.CustomEndpoint))
        {
            s3Config.ServiceURL = config.CustomEndpoint;
        }

        return new AmazonS3Client(config.AccessKeyId, config.SecretAccessKey, s3Config);
    }
}