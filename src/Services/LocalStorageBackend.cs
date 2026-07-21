#nullable enable

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Storage backend implementation for local filesystem storage.
/// </summary>
public sealed class LocalStorageBackend : IStorageBackend
{
    private readonly ILogger<LocalStorageBackend> _logger;

    public LocalStorageBackend(ILogger<LocalStorageBackend> logger)
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

        if (config is not LocalStorageConfiguration localConfig)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        try
        {
            _logger.LogInformation("Uploading backup to local storage: {FilePath}", filePath);
            return await Task.FromResult(UploadToLocal(filePath, localConfig));
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            _logger.LogError(ex, "Failed to upload backup to local storage: {FilePath}", filePath);
            throw new LocalStorageException("Failed to upload backup to local storage", ex);
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

        if (config is not LocalStorageConfiguration localConfig)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(storagePath));

        try
        {
            // For local storage, the storagePath is already a local file path
            File.Copy(storagePath, tempFile, overwrite: true);
            _logger.LogInformation("Successfully downloaded backup from local storage: {StoragePath}", storagePath);
            return tempFile;
        }
        catch (Exception ex) when (ex is not StorageException)
        {
            throw new LocalStorageException("Failed to download backup from local storage", ex);
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

        if (config is not LocalStorageConfiguration)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
        }
    }

    public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not LocalStorageConfiguration localConfig)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        return await Task.FromResult(ListLocalBackups(localConfig));
    }

    public async Task<bool> TestConnectionAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not LocalStorageConfiguration localConfig)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        return await localConfig.TestConnectionAsync();
    }

    public async Task<long> GetAvailableSpaceAsync(StorageConfiguration config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (config is not LocalStorageConfiguration localConfig)
        {
            throw new ArgumentException("Configuration must be LocalStorageConfiguration for LocalStorageBackend", nameof(config));
        }

        var drive = new DriveInfo(Path.GetPathRoot(localConfig.BaseDirectory) ?? "/");
        return drive.AvailableFreeSpace;
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
}