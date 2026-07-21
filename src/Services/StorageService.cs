#nullable enable
// Author: Vladyslav Zaiets

using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for managing backup storage operations across multiple backends.
/// Uses strategy pattern with IStorageBackend implementations.
/// </summary>
public sealed class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public StorageService(ILogger<StorageService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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
            _logger.LogWarning("File not found: {FilePath}", filePath);
            throw new LocalStorageException($"File not found: {filePath}");
        }

        try
        {
            _logger.LogInformation("Uploading backup to storage: {FilePath}", filePath);
            var backend = GetBackendForConfig(config);
            return await backend.UploadBackupAsync(filePath, config);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            _logger.LogError(ex, "Failed to upload backup to storage: {FilePath}", filePath);
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

        try
        {
            var backend = GetBackendForConfig(config);
            return await backend.DownloadBackupAsync(storagePath, config);
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
        try
        {
            var backend = GetBackendForConfig(config);
            await backend.DeleteBackupAsync(storagePath, config);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new StorageException("Failed to delete backup from storage", ex);
        }
    }

    /// <summary>
    /// Lists all backup files in the storage backend.
    /// </summary>
    public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)
    {
        try
        {
            var backend = GetBackendForConfig(config);
            return await backend.ListBackupsAsync(config);
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new StorageException("Failed to list backups from storage", ex);
        }
    }

    /// <summary>
    /// Tests the connection to the storage backend.
    /// </summary>
    public async Task<bool> TestConnectionAsync(StorageConfiguration config)
    {
        try
        {
            var backend = GetBackendForConfig(config);
            return await backend.TestConnectionAsync(config);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the available free space in storage.
    /// </summary>
    public async Task<long> GetAvailableSpaceAsync(StorageConfiguration config)
    {
        try
        {
            var backend = GetBackendForConfig(config);
            return await backend.GetAvailableSpaceAsync(config);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Gets the appropriate storage backend implementation based on the configuration type.
    /// </summary>
    /// <param name="config">The storage configuration.</param>
    /// <returns>An instance of IStorageBackend.</returns>
    private IStorageBackend GetBackendForConfig(StorageConfiguration config)
    {
        return config switch
        {
            LocalStorageConfiguration => _serviceProvider.GetRequiredService<LocalStorageBackend>(),
            S3Configuration => _serviceProvider.GetRequiredService<S3StorageBackend>(),
            AzureConfiguration => _serviceProvider.GetRequiredService<AzureStorageBackend>(),
            _ => throw new StorageException($"Unknown storage configuration type: {config.GetType().Name}")
        };
    }
}
