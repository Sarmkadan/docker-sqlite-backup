// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for managing backup storage operations.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads a backup file to the configured storage backend.
    /// </summary>
    Task<string> UploadBackupAsync(string filePath, StorageConfiguration config);

    /// <summary>
    /// Downloads a backup file from storage to a local temporary location.
    /// </summary>
    Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Deletes a backup file from storage.
    /// </summary>
    Task DeleteBackupAsync(string storagePath, StorageConfiguration config);

    /// <summary>
    /// Lists all backup files in the storage backend.
    /// </summary>
    Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config);

    /// <summary>
    /// Tests the connection to the storage backend.
    /// </summary>
    Task<bool> TestConnectionAsync(StorageConfiguration config);

    /// <summary>
    /// Gets the available free space in storage.
    /// </summary>
    Task<long> GetAvailableSpaceAsync(StorageConfiguration config);
}
