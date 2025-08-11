#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Amazon.S3;
using Amazon.S3.Model;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for managing backup storage operations across multiple backends.
/// </summary>
public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;

    public StorageService(ILogger<StorageService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Uploads a backup file to the configured storage backend.
    /// </summary>
    public async Task<string> UploadBackupAsync(string filePath, StorageConfiguration config)
    {
        if (!File.Exists(filePath))
        {
            throw new LocalStorageException($"File not found: {filePath}");
        }

        switch (config)
        {
            case S3Configuration s3Config:
                return await UploadToS3Async(filePath, s3Config);
            case LocalStorageConfiguration localConfig:
                return UploadToLocal(filePath, localConfig);
            default:
                throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
        }
    }

    /// <summary>
    /// Downloads a backup file from storage to a local temporary location.
    /// </summary>
    public async Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath));

        switch (config)
        {
            case S3Configuration s3Config:
                await DownloadFromS3Async(storagePath, tempFile, s3Config);
                break;
            case LocalStorageConfiguration:
                File.Copy(storagePath, tempFile, overwrite: true);
                break;
            default:
                throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
        }

        return tempFile;
    }

    /// <summary>
    /// Deletes a backup file from storage.
    /// </summary>
    public async Task DeleteBackupAsync(string storagePath, StorageConfiguration config)
    {
        switch (config)
        {
            case S3Configuration s3Config:
                await DeleteFromS3Async(storagePath, s3Config);
                break;
            case LocalStorageConfiguration:
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                }
                break;
            default:
                throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
        }
    }

    /// <summary>
    /// Lists all backup files in the storage backend.
    /// </summary>
    public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)
    {
        switch (config)
        {
            case S3Configuration s3Config:
                return await ListS3BackupsAsync(s3Config);
            case LocalStorageConfiguration localConfig:
                return ListLocalBackups(localConfig);
            default:
                throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
        }
    }

    /// <summary>
    /// Tests the connection to the storage backend.
    /// </summary>
    public async Task<bool> TestConnectionAsync(StorageConfiguration config)
    {
        return await config.TestConnectionAsync();
    }

    /// <summary>
    /// Gets the available free space in storage.
    /// </summary>
    public async Task<long> GetAvailableSpaceAsync(StorageConfiguration config)
    {
        switch (config)
        {
            case LocalStorageConfiguration localConfig:
                var drive = new DriveInfo(Path.GetPathRoot(localConfig.BaseDirectory) ?? "/");
                return drive.AvailableFreeSpace;
            case S3Configuration:
                return long.MaxValue;
            default:
                return 0;
        }
    }

    private static string UploadToLocal(string filePath, LocalStorageConfiguration config)
    {
        var destDir = config.BaseDirectory;
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        var fileName = Path.GetFileName(filePath);
        var destPath = Path.Combine(destDir, fileName);
        File.Copy(filePath, destPath, overwrite: true);

        return destPath;
    }

    private async Task<string> UploadToS3Async(string filePath, S3Configuration config)
    {
        try
        {
            var s3Client = CreateS3Client(config);
            var fileName = Path.GetFileName(filePath);
            var key = Path.Combine(config.ObjectKeyPrefix, fileName).Replace("\\", "/");

            var request = new PutObjectRequest
            {
                BucketName = config.BucketName,
                Key = key,
                FilePath = filePath,
                ServerSideEncryptionMethod = config.EnableServerSideEncryption ? ServerSideEncryptionMethod.AES256 : null,
                StorageClass = (S3StorageClass)(object)(config.StorageClass ?? "STANDARD")
            };

            await s3Client.PutObjectAsync(request);
            _logger.LogInformation("Successfully uploaded backup to S3: {Key}", key);

            return key;
        }
        catch (Exception ex)
        {
            throw new S3StorageException($"Failed to upload backup to S3: {ex.Message}", ex);
        }
    }

    private async Task DownloadFromS3Async(string s3Key, string localPath, S3Configuration config)
    {
        try
        {
            var s3Client = CreateS3Client(config);
            var request = new GetObjectRequest
            {
                BucketName = config.BucketName,
                Key = s3Key
            };

            using var response = await s3Client.GetObjectAsync(request);
            await response.WriteResponseStreamToFileAsync(localPath, false, CancellationToken.None);
            _logger.LogInformation("Successfully downloaded backup from S3: {Key}", s3Key);
        }
        catch (Exception ex)
        {
            throw new S3StorageException($"Failed to download backup from S3: {ex.Message}", ex);
        }
    }

    private async Task DeleteFromS3Async(string s3Key, S3Configuration config)
    {
        try
        {
            var s3Client = CreateS3Client(config);
            var request = new DeleteObjectRequest
            {
                BucketName = config.BucketName,
                Key = s3Key
            };

            await s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("Successfully deleted backup from S3: {Key}", s3Key);
        }
        catch (Exception ex)
        {
            throw new S3StorageException($"Failed to delete backup from S3: {ex.Message}", ex);
        }
    }

    private async Task<IEnumerable<(string, long, DateTime)>> ListS3BackupsAsync(S3Configuration config)
    {
        try
        {
            var s3Client = CreateS3Client(config);
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
        catch (Exception ex)
        {
            throw new S3StorageException($"Failed to list S3 backups: {ex.Message}", ex);
        }
    }

    private static IEnumerable<(string, long, DateTime)> ListLocalBackups(LocalStorageConfiguration config)
    {
        if (!Directory.Exists(config.BaseDirectory))
        {
            return Enumerable.Empty<(string, long, DateTime)>();
        }

        var result = new List<(string, long, DateTime)>();
        var dir = new DirectoryInfo(config.BaseDirectory);

        foreach (var file in dir.EnumerateFiles("*.sqlite*", SearchOption.AllDirectories))
        {
            result.Add((file.FullName, file.Length, file.LastWriteTimeUtc));
        }

        return result;
    }

    private static AmazonS3Client CreateS3Client(S3Configuration config)
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
