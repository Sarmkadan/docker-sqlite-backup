// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Models;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Defines repository operations for backup data persistence.
/// </summary>
public interface IBackupRepository
{
    // Backup Job Operations
    Task<BackupJob?> GetBackupJobByIdAsync(string jobId, CancellationToken cancellationToken = default);
    Task<List<BackupJob>> GetAllBackupJobsAsync(CancellationToken cancellationToken = default);
    Task SaveBackupJobAsync(BackupJob job, CancellationToken cancellationToken = default);
    Task UpdateBackupJobAsync(BackupJob job, CancellationToken cancellationToken = default);
    Task DeleteBackupJobAsync(string jobId, CancellationToken cancellationToken = default);

    // Backup Snapshot Operations
    Task SaveSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<BackupSnapshot?> GetSnapshotByIdAsync(string snapshotId, CancellationToken cancellationToken = default);
    Task<List<BackupSnapshot>> GetSnapshotsByJobIdAsync(string backupJobId, CancellationToken cancellationToken = default);
    Task UpdateSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default);

    // Restore Point Operations
    Task SaveRestorePointAsync(RestorePoint restorePoint, CancellationToken cancellationToken = default);
    Task<RestorePoint?> GetRestorePointByIdAsync(string restorePointId, CancellationToken cancellationToken = default);
    Task<List<RestorePoint>> GetRestorePointsByJobIdAsync(string backupJobId, CancellationToken cancellationToken = default);
    Task UpdateRestorePointAsync(RestorePoint restorePoint, CancellationToken cancellationToken = default);
    Task DeleteRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default);

    // Metadata Operations
    Task SaveMetadataAsync(BackupMetadata metadata, CancellationToken cancellationToken = default);
    Task<BackupMetadata?> GetMetadataBySnapshotIdAsync(string snapshotId, CancellationToken cancellationToken = default);
}
