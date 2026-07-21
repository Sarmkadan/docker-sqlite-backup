#nullable enable

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Storage backend implementation for Azure Blob Storage.
/// </summary>
public sealed class AzureStorageBackend : IStorageBackend
{
    private readonly ILogger<AzureStorageBackend> _logger;

    public AzureStorageBackend(ILogger<AzureStorageBackend> logger)
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

        if (config is not AzureConfiguration azureConfig)
        {
            throw new ArgumentException("Configuration must be AzureConfiguration for AzureStorageBackend", nameof(config));
        }

        try
        {
            _logger.LogInformation("Uploading backup to Azure Blob Storage: {FilePath}", filePath);
            return await UploadToAzureAsync(filePath, azureConfig);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            _logger.LogError(ex, "Failed to upload backup to Azure: {FilePath}", filePath);
            throw new AzureStorageException("Failed to upload backup to Azure", ex);
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

        if (config is not AzureConfiguration azureConfig)
        {
            throw new ArgumentException("Configuration must be AzureConfiguration for AzureStorageBackend", nameof(config));
        }

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath));

        try
        {
            await DownloadFromAzureAsync(storagePath, tempFile, azureConfig);
            _logger.LogInformation("Successfully downloaded backup from Azure Blob Storage: {StoragePath}", storagePath);
            return tempFile;
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new AzureStorageException("Failed to download backup from Azure", ex);
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

        if (config is not AzureConfiguration azureConfig)
        {
            throw new ArgumentException("Configuration must be AzureConfiguration for AzureStorageBackend", nameof(config));
        }

        try
        {
            await DeleteFromAzureAsync(storagePath, azureConfig);
            _logger.LogInformation("Successfully deleted backup from Azure Blob Storage: {StoragePath}", storagePath);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new AzureStorageException("Failed to delete backup from Azure", ex);
        }
    }

    public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not AzureConfiguration azureConfig)
        {
            throw new ArgumentException("Configuration must be AzureConfiguration for AzureStorageBackend", nameof(config));
        }

        try
        {
            return await ListAzureBackupsAsync(azureConfig);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new AzureStorageException("Failed to list Azure backups", ex);
        }
    }

    public async Task<bool> TestConnectionAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not AzureConfiguration azureConfig)
        {
            throw new ArgumentException("Configuration must be AzureConfiguration for AzureStorageBackend", nameof(config));
        }

        try
        {
            var container = azureConfig.CreateContainerClient();
            return await container.ExistsAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> GetAvailableSpaceAsync(StorageConfiguration config)
    {
        // Azure Blob Storage doesn't have a concept of "available space" in the same way as local storage
        // Return long.MaxValue to indicate unlimited space
        await Task.CompletedTask;
        return long.MaxValue;
    }

    private async Task<string> UploadToAzureAsync(string filePath, AzureConfiguration config)
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

    private async Task DownloadFromAzureAsync(string blobName, string localPath, AzureConfiguration config)
    {
        var container = config.CreateContainerClient();
        var blobClient = container.GetBlobClient(blobName);

        await blobClient.DownloadToAsync(localPath);
    }

    private async Task DeleteFromAzureAsync(string blobName, AzureConfiguration config)
    {
        var container = config.CreateContainerClient();
        var blobClient = container.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }

    private async Task<IEnumerable<(string, long, DateTime)>> ListAzureBackupsAsync(AzureConfiguration config)
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
}