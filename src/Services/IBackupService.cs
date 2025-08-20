#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Core service interface for SQLite backup operations. Handles backup execution,
/// history retrieval, cleanup, and integrity verification for Docker-mounted
/// SQLite database volumes.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates and executes a backup for the specified schedule. Copies the SQLite database
    /// file using the SQLite Online Backup API to ensure consistency during active writes,
    /// compresses the result, and stores it in the configured backup directory.
    /// </summary>
    /// <param name="schedule">The backup schedule defining source database path, retention policy, and compression settings.</param>
    /// <param name="cancellationToken">Token to cancel the backup operation.</param>
    /// <returns>
    /// A <see cref="BackupResult"/> containing the backup file path, size, checksum,
    /// duration, and success/failure status.
    /// </returns>
    Task<BackupResult> ExecuteBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent backup results for a given schedule, ordered by
    /// creation time descending.
    /// </summary>
    /// <param name="scheduleId">The unique identifier of the backup schedule.</param>
    /// <param name="limit">Maximum number of results to return. Defaults to 10.</param>
    /// <returns>An enumerable of <see cref="BackupResult"/> records.</returns>
    Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit = 10);

    /// <summary>
    /// Deletes a backup file from disk and removes its metadata record from the store.
    /// </summary>
    /// <param name="backupResultId">The unique identifier of the backup result to delete.</param>
    Task DeleteBackupAsync(Guid backupResultId);

    /// <summary>
    /// Retrieves a specific backup result by its unique identifier.
    /// </summary>
    /// <param name="backupResultId">The backup result identifier.</param>
    /// <returns>The matching <see cref="BackupResult"/>, or <c>null</c> if not found.</returns>
    Task<BackupResult?> GetBackupResultAsync(Guid backupResultId);

    /// <summary>
    /// Calculates the SHA-256 checksum of a backup file for integrity verification.
    /// Used to detect corruption during storage or transfer.
    /// </summary>
    /// <param name="filePath">Absolute path to the backup file.</param>
    /// <returns>The lowercase hex-encoded SHA-256 hash of the file contents.</returns>
    Task<string> CalculateBackupChecksumAsync(string filePath);
}
