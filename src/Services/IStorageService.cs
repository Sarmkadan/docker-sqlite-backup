#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for abstracting backup storage operations across different
/// storage backends like local filesystem, AWS S3, and Azure Blob Storage.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a backup file from the local filesystem to the configured storage backend.
    /// </summary>
    /// <param name="filePath">The absolute path to the local backup file.</param>
    /// <param name="config">The storage configuration details for the target backend.</param>
    /// <returns>A string representing the path or identifier of the backup in the storage backend.</returns>
    Task<string> UploadBackupAsync(string filePath, StorageConfiguration config);

    /// <summary>
    /// Downloads a backup file from the storage backend to a local temporary location.
    /// </summary>
    /// <param name="storagePath">The path or identifier of the backup in storage.</param>
    /// <param name="config">The storage configuration details for the source backend.</param>
    /// <returns>The local path where the backup was downloaded.</returns>
    Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Deletes a specific backup file from the storage backend.
    /// </summary>
    /// <param name="storagePath">The path or identifier of the backup in storage.</param>
    /// <param name="config">The storage configuration details for the backend.</param>
    Task DeleteBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Lists all backup files currently residing in the storage backend.
    /// </summary>
    /// <param name="config">The storage configuration details for the backend.</param>
    /// <returns>An enumerable of tuples containing the path, size, and last modified date of each backup.</returns>
    Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config);

    /// <summary>
    /// Verifies the connection to the storage backend.
    /// </summary>
    /// <param name="config">The storage configuration details for the backend.</param>
    /// <returns>True if the connection was successful, false otherwise.</returns>
    Task<bool> TestConnectionAsync(StorageConfiguration config);

    /// <summary>
    /// Gets the available storage space (in bytes) on the configured storage backend.
    /// </summary>
    /// <param name="config">The storage configuration details for the backend.</param>
    /// <returns>The amount of free space in bytes.</returns>
    Task<long> GetAvailableSpaceAsync(StorageConfiguration config);
}
