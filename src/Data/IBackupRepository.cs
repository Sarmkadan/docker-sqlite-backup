#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Repository interface for backup data access operations.
/// </summary>
public interface IBackupRepository
{
    // Schedule operations
    Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule);
    Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule);
    Task DeleteScheduleAsync(Guid scheduleId);
    Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId);
    Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync();
    Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync();

    // Backup result operations
    Task<BackupResult> CreateBackupResultAsync(BackupResult result);
    Task<BackupResult> UpdateBackupResultAsync(BackupResult result);
    Task DeleteBackupResultAsync(Guid resultId);
    Task<BackupResult?> GetBackupResultAsync(Guid resultId);
    Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit);

    // Rotation policy operations
    Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId);
    Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy);

    // Verification operations
    Task<RestoreVerification> SaveRestoreVerificationAsync(RestoreVerification verification);
    Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId);

    // Backup job operations
    Task<BackupJob> CreateBackupJobAsync(BackupJob job);
    Task<BackupJob> UpdateBackupJobAsync(BackupJob job);
    Task<BackupJob?> GetBackupJobAsync(Guid jobId);

    // Connection management
    Task InitializeAsync();
    Task<bool> HealthCheckAsync();
}
