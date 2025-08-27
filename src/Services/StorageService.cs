#nullable enable
// Author: Vladyslav Zaiets

using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;
using ArgumentException = DockerSqliteBackup.Exceptions.ArgumentException;

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
    /// <param name="filePath">The path to the backup file to upload.</param>
    /// <param name="config">The storage configuration.</param>
    /// <returns>The remote key/path where the file was uploaded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath or config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when filePath is empty or invalid.</exception>
    /// <exception cref="StorageException">Thrown when upload fails.</exception>
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
            throw new LocalStorageException($"File not found: {filePath}");
        }

        try
        {
            switch (config)
            {
                case S3Configuration s3Config:
                    return await UploadToS3Async(filePath, s3Config);
                case AzureConfiguration azureConfig:
                    return await UploadToAzureAsync(filePath, azureConfig);
                case LocalStorageConfiguration localConfig:
                    return UploadToLocal(filePath, localConfig);
                default:
                    throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
            }
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new StorageException("Failed to upload backup to storage", ex);
        }
    }

    /// <summary>
    /// Downloads a backup file from storage to a local temporary location.
    /// </summary>
    /// <param name="storagePath">The remote path/key of the backup file.</param>
    /// <param name="config">The storage configuration.</param>
    /// <returns>The path to the downloaded file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when storagePath or config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when storagePath is empty or invalid.</exception>
    /// <exception cref="StorageException">Thrown when download fails.</exception>
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

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath));

        try
        {
            switch (config)
            {
                case S3Configuration s3Config:
                    await DownloadFromS3Async(storagePath, tempFile, s3Config);
                    break;
                case AzureConfiguration azureConfig:
                    await DownloadFromAzureAsync(storagePath, tempFile, azureConfig);
                    break;
                case LocalStorageConfiguration:
                    File.Copy(storagePath, tempFile, overwrite: true);
                    break;
                default:
                    throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}");
            }

            return tempFile;
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new StorageException("Failed to download backup from storage", ex);
        }
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
            case AzureConfiguration azureConfig:
                await DeleteFromAzureAsync(storagePath, azureConfig);
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
            case AzureConfiguration azureConfig:
                return await ListAzureBackupsAsync(azureConfig);
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
            case AzureConfiguration:
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

    // ── Azure helpers ─────────────────────────────────────────────────────────

    private async Task<string> UploadToAzureAsync(string filePath, AzureConfiguration config)
    {
        try
        {
            var container = config.CreateContainerClient();
            await container.CreateIfNotExistsAsync();

            var fileName = Path.GetFileName(filePath);
            var blobName = (config.BlobPrefix.TrimEnd('/') + "/" + fileName).TrimStart('/');
            var blobClient = container.GetBlobClient(blobName);

            var uploadOptions = new BlobUploadOptions
            {
                AccessTier = new AccessTier(config.AccessTier)
            };

            await using var stream = File.OpenRead(filePath);
            await blobClient.UploadAsync(stream, uploadOptions);

            _logger.LogInformation("Successfully uploaded backup to Azure Blob Storage: {BlobName}", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            throw new AzureStorageException($"Failed to upload backup to Azure: {ex.Message}", ex);
        }
    }

    private async Task DownloadFromAzureAsync(string blobName, string localPath, AzureConfiguration config)
    {
        try
        {
            var container = config.CreateContainerClient();
            var blobClient = container.GetBlobClient(blobName);

            await blobClient.DownloadToAsync(localPath);
            _logger.LogInformation("Successfully downloaded backup from Azure Blob Storage: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            throw new AzureStorageException($"Failed to download backup from Azure: {ex.Message}", ex);
        }
    }

    private async Task DeleteFromAzureAsync(string blobName, AzureConfiguration config)
    {
        try
        {
            var container = config.CreateContainerClient();
            var blobClient = container.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
            _logger.LogInformation("Successfully deleted backup from Azure Blob Storage: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            throw new AzureStorageException($"Failed to delete backup from Azure: {ex.Message}", ex);
        }
    }

    private async Task<IEnumerable<(string, long, DateTime)>> ListAzureBackupsAsync(AzureConfiguration config)
    {
        try
        {
            var container = config.CreateContainerClient();
            var result = new List<(string, long, DateTime)>();

            await foreach (var blob in container.GetBlobsAsync(prefix: config.BlobPrefix))
            {
                var size = blob.Properties.ContentLength ?? 0;
                var modified = blob.Properties.LastModified?.UtcDateTime ?? DateTime.UtcNow;
                result.Add((blob.Name, size, modified));
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new AzureStorageException($"Failed to list Azure backups: {ex.Message}", ex);
        }
    }
}
