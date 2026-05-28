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
    Task<RestoreVerification> VerifyBackupAsync(BackupResult backup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the verification history for a backup.
    /// </summary>
    Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId);

    /// <summary>
    /// Performs an integrity check on a SQLite database file.
    /// </summary>
    Task<(bool IsValid, string? Errors)> PerformIntegrityCheckAsync(string databasePath);

    /// <summary>
    /// Verifies the checksum of a backup file.
    /// </summary>
    Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum);

    /// <summary>
    /// Restores a backup to a temporary location for verification.
    /// </summary>
    Task<string> RestoreToTemporaryAsync(BackupResult backup);

    /// <summary>
    /// Cleans up temporary files from verification attempts.
    /// </summary>
    Task CleanupTemporaryFilesAsync(string tempDirectory);
}
