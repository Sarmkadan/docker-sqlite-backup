#nullable enable

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Strategy interface for storage backend implementations (Local, S3, Azure).
/// </summary>
public interface IStorageBackend
{
    /// <summary>
    /// Uploads a backup file to the storage backend.
    /// </summary>
    /// <param name="filePath">The path to the backup file to upload.</param>
    /// <param name="config">The storage configuration.</param>
    /// <returns>The remote key/path where the file was uploaded.</returns>
    Task<string> UploadBackupAsync(string filePath, StorageConfiguration config);

    /// <summary>
    /// Downloads a backup file from storage to a local temporary location.
    /// </summary>
    /// <param name="storagePath">The remote path/key of the backup file.</param>
    /// <param name="config">The storage configuration.</param>
    /// <returns>The path to the downloaded file.</returns>
    Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Deletes a backup file from storage.
    /// </summary>
    /// <param name="storagePath">The remote path/key of the backup file.</param>
    /// <param name="config">The storage configuration.</param>
    Task DeleteBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Lists all backup files in the storage backend.
    /// </summary>
    /// <param name="config">The storage configuration.</param>
    /// <returns>Collection of backup metadata (path, size, modified date).</returns>
    Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config);

    /// <summary>
    /// Tests the connection to the storage backend.
    /// </summary>
    /// <param name="config">The storage configuration.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(StorageConfiguration config);

    /// <summary>
    /// Gets the available free space in storage.
    /// </summary>
    /// <param name="config">The storage configuration.</param>
    /// <returns>The amount of free space in bytes.</returns>
    Task<long> GetAvailableSpaceAsync(StorageConfiguration config);
}