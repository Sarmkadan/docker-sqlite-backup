#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for backup verification.
/// </summary>
public interface IVerificationService
{
    /// <summary>
    /// Verifies a backup by attempting to restore and validate the database.
    /// </summary>
    /// <param name="backup">The backup result to verify.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the verification operation.</returns>
    Task<RestoreVerification> VerifyBackupAsync(BackupResult backup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the verification history for a backup.
    /// </summary>
    /// <param name="backupResultId">The backup result identifier.</param>
    /// <returns>A task representing the verification history retrieval operation.</returns>
    Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId);

    /// <summary>
    /// Performs an integrity check on a SQLite database file.
    /// </summary>
    /// <param name="databasePath">The path to the database file to check.</param>
    /// <returns>A task representing the integrity check operation. Returns a tuple indicating if the database is valid and any error messages.</returns>
    Task<(bool IsValid, string? Errors)> PerformIntegrityCheckAsync(string databasePath);

    /// <summary>
    /// Verifies the checksum of a backup file.
    /// </summary>
    /// <param name="filePath">The path to the backup file.</param>
    /// <param name="expectedChecksum">The expected checksum value.</param>
    /// <returns>A task representing the checksum verification operation. Returns true if the checksum matches, false otherwise.</returns>
    Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum);

    /// <summary>
    /// Restores a backup to a temporary location for verification.
    /// </summary>
    /// <param name="backup">The backup result to restore.</param>
    /// <returns>A task representing the restore operation. Returns the path to the temporary database file.</returns>
    Task<string> RestoreToTemporaryAsync(BackupResult backup);

    /// <summary>
    /// Cleans up temporary files from verification attempts.
    /// </summary>
    /// <param name="tempDirectory">The temporary directory to clean up.</param>
    /// <returns>A task representing the cleanup operation.</returns>
    Task CleanupTemporaryFilesAsync(string tempDirectory);
}
