#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Repository interface for backup data access operations.
/// </summary>
/// <remarks>
/// Persists backup metadata (schedules, results, rotation policies, verifications, jobs)
/// used by the backup services. Methods named <c>Get*</c> return <c>null</c> when the
/// requested entity does not exist; <c>Create*</c>, <c>Update*</c> and <c>Save*</c> return
/// the persisted entity.
/// </remarks>
public interface IBackupRepository
{
    // Schedule operations

    /// <summary>
    /// Persists a new backup schedule.
    /// </summary>
    /// <param name="schedule">The schedule to create.</param>
    /// <returns>The created schedule.</returns>
    Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    /// <param name="schedule">The schedule with updated values.</param>
    /// <returns>The updated schedule.</returns>
    Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Deletes a backup schedule by its identifier.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to delete.</param>
    Task DeleteScheduleAsync(Guid scheduleId);

    /// <summary>
    /// Gets a backup schedule by its identifier.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>The matching schedule, or <c>null</c> if none exists.</returns>
    Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId);

    /// <summary>
    /// Gets all backup schedules, newest first.
    /// </summary>
    /// <returns>All stored schedules, active or inactive.</returns>
    Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync();

    /// <summary>
    /// Gets only the active backup schedules, ordered by name.
    /// </summary>
    /// <returns>The schedules flagged as active.</returns>
    Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync();

    // Backup result operations

    /// <summary>
    /// Persists a new backup result.
    /// </summary>
    /// <param name="result">The backup result to create.</param>
    /// <returns>The created backup result.</returns>
    Task<BackupResult> CreateBackupResultAsync(BackupResult result);

    /// <summary>
    /// Updates an existing backup result.
    /// </summary>
    /// <param name="result">The backup result with updated values.</param>
    /// <returns>The updated backup result.</returns>
    Task<BackupResult> UpdateBackupResultAsync(BackupResult result);

    /// <summary>
    /// Deletes a backup result by its identifier.
    /// </summary>
    /// <param name="resultId">The identifier of the backup result to delete.</param>
    Task DeleteBackupResultAsync(Guid resultId);

    /// <summary>
    /// Gets a backup result by its identifier.
    /// </summary>
    /// <param name="resultId">The identifier of the backup result.</param>
    /// <returns>The matching backup result, or <c>null</c> if none exists.</returns>
    Task<BackupResult?> GetBackupResultAsync(Guid resultId);

    /// <summary>
    /// Gets the most recent backup results for a schedule, newest first.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>Up to <paramref name="limit"/> backup results ordered by start time, descending.</returns>
    Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit);

    // Rotation policy operations

    /// <summary>
    /// Gets the rotation policy assigned to a schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>The rotation policy, or <c>null</c> if the schedule has none.</returns>
    Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId);

    /// <summary>
    /// Saves a rotation policy, inserting it or overwriting the existing one.
    /// </summary>
    /// <param name="policy">The rotation policy to save.</param>
    /// <returns>The saved rotation policy.</returns>
    Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy);

    // Verification operations

    /// <summary>
    /// Persists the result of a restore verification run.
    /// </summary>
    /// <param name="verification">The verification record to save.</param>
    /// <returns>The saved verification record.</returns>
    Task<RestoreVerification> SaveRestoreVerificationAsync(RestoreVerification verification);

    /// <summary>
    /// Gets the restore verification history for a backup result, newest first.
    /// </summary>
    /// <param name="backupResultId">The identifier of the backup result.</param>
    /// <returns>All verification records for the backup result, ordered by start time, descending.</returns>
    Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId);

    // Backup job operations

    /// <summary>
    /// Persists a new backup job.
    /// </summary>
    /// <param name="job">The backup job to create.</param>
    /// <returns>The created backup job.</returns>
    Task<BackupJob> CreateBackupJobAsync(BackupJob job);

    /// <summary>
    /// Updates an existing backup job.
    /// </summary>
    /// <param name="job">The backup job with updated values.</param>
    /// <returns>The updated backup job.</returns>
    Task<BackupJob> UpdateBackupJobAsync(BackupJob job);

    /// <summary>
    /// Gets a backup job by its identifier.
    /// </summary>
    /// <param name="jobId">The identifier of the backup job.</param>
    /// <returns>The matching backup job, or <c>null</c> if none exists.</returns>
    Task<BackupJob?> GetBackupJobAsync(Guid jobId);

    // Connection management

    /// <summary>
    /// Initializes the underlying database, creating the schema if it does not exist.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Checks whether the underlying database is reachable and usable.
    /// </summary>
    /// <returns><c>true</c> if the database is healthy; otherwise <c>false</c>.</returns>
    Task<bool> HealthCheckAsync();
}
