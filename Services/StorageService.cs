// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Defines storage operations for backup artifacts.
/// </summary>
public interface IStorageService
{
    Task UploadBackupAsync(string localPath, string storagePath, StorageProvider provider, CancellationToken cancellationToken);
    Task DownloadBackupAsync(string storagePath, string localPath, StorageProvider provider, CancellationToken cancellationToken);
    Task DeleteBackupAsync(string storagePath, StorageProvider provider, CancellationToken cancellationToken);
    Task<bool> BackupExistsAsync(string storagePath, StorageProvider provider, CancellationToken cancellationToken);
    Task<long> GetBackupSizeAsync(string storagePath, StorageProvider provider, CancellationToken cancellationToken);
}

/// <summary>
/// Implements storage operations for multiple backend providers.
/// </summary>
public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;

    public StorageService(ILogger<StorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a backup file to the configured storage provider.
    /// </summary>
    public async Task UploadBackupAsync(
        string localPath,
        string storagePath,
        StorageProvider provider,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(localPath))
            throw new StorageException($"Local file not found: {localPath}", provider.Name, localPath, "Upload");

        _logger.LogInformation("Uploading backup to {Provider} at {StoragePath}", provider.Name, storagePath);

        try
        {
            switch (provider.Type)
            {
                case StorageType.Local:
                    await UploadToLocalAsync(localPath, storagePath, provider, cancellationToken);
                    break;

                case StorageType.S3:
                    await UploadToS3Async(localPath, storagePath, provider, cancellationToken);
                    break;

                default:
                    throw new StorageException($"Unsupported storage type: {provider.Type}", provider.Name, storagePath, "Upload");
            }

            _logger.LogInformation("Backup uploaded successfully to {Provider} at {StoragePath}", provider.Name, storagePath);
        }
        catch (Exception ex) when (!(ex is StorageException))
        {
            throw new StorageException($"Upload failed: {ex.Message}", ex, provider.Name, storagePath, "Upload");
        }
    }

    /// <summary>
    /// Downloads a backup file from the configured storage provider.
    /// </summary>
    public async Task DownloadBackupAsync(
        string storagePath,
        string localPath,
        StorageProvider provider,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading backup from {Provider} at {StoragePath}", provider.Name, storagePath);

        try
        {
            switch (provider.Type)
            {
                case StorageType.Local:
                    await DownloadFromLocalAsync(storagePath, localPath, provider, cancellationToken);
                    break;

                case StorageType.S3:
                    await DownloadFromS3Async(storagePath, localPath, provider, cancellationToken);
                    break;

                default:
                    throw new StorageException($"Unsupported storage type: {provider.Type}", provider.Name, storagePath, "Download");
            }

            _logger.LogInformation("Backup downloaded successfully from {Provider}", provider.Name);
        }
        catch (Exception ex) when (!(ex is StorageException))
        {
            throw new StorageException($"Download failed: {ex.Message}", ex, provider.Name, storagePath, "Download");
        }
    }

    /// <summary>
    /// Deletes a backup file from storage.
    /// </summary>
    public async Task DeleteBackupAsync(
        string storagePath,
        StorageProvider provider,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting backup from {Provider} at {StoragePath}", provider.Name, storagePath);

        try
        {
            switch (provider.Type)
            {
                case StorageType.Local:
                    await DeleteFromLocalAsync(storagePath, provider, cancellationToken);
                    break;

                case StorageType.S3:
                    await DeleteFromS3Async(storagePath, provider, cancellationToken);
                    break;

                default:
                    throw new StorageException($"Unsupported storage type: {provider.Type}", provider.Name, storagePath, "Delete");
            }

            _logger.LogInformation("Backup deleted successfully from {Provider}", provider.Name);
        }
        catch (Exception ex) when (!(ex is StorageException))
        {
            throw new StorageException($"Deletion failed: {ex.Message}", ex, provider.Name, storagePath, "Delete");
        }
    }

    /// <summary>
    /// Checks if a backup file exists in storage.
    /// </summary>
    public async Task<bool> BackupExistsAsync(
        string storagePath,
        StorageProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (provider.Type)
            {
                case StorageType.Local:
                    return await Task.FromResult(File.Exists(storagePath));

                case StorageType.S3:
                    return await BackupExistsInS3Async(storagePath, provider, cancellationToken);

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking backup existence in {Provider}", provider.Name);
            return false;
        }
    }

    /// <summary>
    /// Gets the size of a backup file in storage.
    /// </summary>
    public async Task<long> GetBackupSizeAsync(
        string storagePath,
        StorageProvider provider,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (provider.Type)
            {
                case StorageType.Local:
                    return await Task.Run(() =>
                    {
                        var fileInfo = new FileInfo(storagePath);
                        return fileInfo.Exists ? fileInfo.Length : 0;
                    }, cancellationToken);

                case StorageType.S3:
                    return await GetBackupSizeFromS3Async(storagePath, provider, cancellationToken);

                default:
                    return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting backup size from {Provider}", provider.Name);
            return 0;
        }
    }

    // Local Storage Implementation
    private async Task UploadToLocalAsync(string localPath, string storagePath, StorageProvider provider, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(storagePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await Task.Run(() => File.Copy(localPath, storagePath, overwrite: true), ct);
    }

    private async Task DownloadFromLocalAsync(string storagePath, string localPath, StorageProvider provider, CancellationToken ct)
    {
        if (!File.Exists(storagePath))
            throw new StorageException($"Backup file not found: {storagePath}", provider.Name, storagePath, "Download");

        await Task.Run(() => File.Copy(storagePath, localPath, overwrite: true), ct);
    }

    private async Task DeleteFromLocalAsync(string storagePath, StorageProvider provider, CancellationToken ct)
    {
        if (File.Exists(storagePath))
            await Task.Run(() => File.Delete(storagePath), ct);
    }

    // S3 Storage Implementation
    private async Task UploadToS3Async(string localPath, string storagePath, StorageProvider provider, CancellationToken ct)
    {
        using (var s3Client = CreateS3Client(provider))
        using (var fileTransferUtility = new TransferUtility(s3Client))
        {
            var request = new TransferUtilityUploadRequest
            {
                FilePath = localPath,
                BucketName = provider.Location,
                Key = storagePath,
                ServerSideEncryptionMethod = provider.UseEncryption ? ServerSideEncryptionMethod.AES256 : null,
                StorageClass = GetStorageClass(provider.StorageClass)
            };

            await fileTransferUtility.UploadAsync(request, ct);
        }
    }

    private async Task DownloadFromS3Async(string storagePath, string localPath, StorageProvider provider, CancellationToken ct)
    {
        using (var s3Client = CreateS3Client(provider))
        using (var fileTransferUtility = new TransferUtility(s3Client))
        {
            var request = new TransferUtilityDownloadRequest
            {
                BucketName = provider.Location,
                Key = storagePath,
                FilePath = localPath
            };

            await fileTransferUtility.DownloadAsync(request, ct);
        }
    }

    private async Task DeleteFromS3Async(string storagePath, StorageProvider provider, CancellationToken ct)
    {
        using (var s3Client = CreateS3Client(provider))
        {
            await s3Client.DeleteObjectAsync(provider.Location, storagePath, ct);
        }
    }

    private async Task<bool> BackupExistsInS3Async(string storagePath, StorageProvider provider, CancellationToken ct)
    {
        using (var s3Client = CreateS3Client(provider))
        {
            try
            {
                await s3Client.GetObjectMetadataAsync(provider.Location, storagePath, ct);
                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }

    private async Task<long> GetBackupSizeFromS3Async(string storagePath, StorageProvider provider, CancellationToken ct)
    {
        using (var s3Client = CreateS3Client(provider))
        {
            var metadata = await s3Client.GetObjectMetadataAsync(provider.Location, storagePath, ct);
            return metadata.ContentLength;
        }
    }

    private AmazonS3Client CreateS3Client(StorageProvider provider)
    {
        var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(provider.AwsRegion!) };
        return new AmazonS3Client(provider.AwsAccessKeyId, provider.AwsSecretAccessKey, config);
    }

    private static Amazon.S3.S3StorageClass GetStorageClass(string? storageClass)
    {
        return storageClass switch
        {
            "STANDARD_IA" => Amazon.S3.S3StorageClass.StandardInfrequentAccess,
            "GLACIER" => Amazon.S3.S3StorageClass.Glacier,
            "DEEP_ARCHIVE" => Amazon.S3.S3StorageClass.DeepArchive,
            _ => Amazon.S3.S3StorageClass.Standard
        };
    }
}
