// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for backup operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates and executes a backup for the specified schedule.
    /// </summary>
    Task<BackupResult> ExecuteBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves backup history for a schedule.
    /// </summary>
    Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit = 10);

    /// <summary>
    /// Deletes a backup file and removes it from storage.
    /// </summary>
    Task DeleteBackupAsync(Guid backupResultId);

    /// <summary>
    /// Gets a specific backup result by ID.
    /// </summary>
    Task<BackupResult?> GetBackupResultAsync(Guid backupResultId);

    /// <summary>
    /// Calculates the checksum of a backup file.
    /// </summary>
    Task<string> CalculateBackupChecksumAsync(string filePath);
}
