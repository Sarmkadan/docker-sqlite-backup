#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for backup rotation and cleanup.
/// </summary>
public interface IRotationService
{
    /// <summary>
    /// Executes the rotation policy for a schedule, deleting old backups.
    /// </summary>
    Task<int> ExecuteRotationAsync(Guid scheduleId);

    /// <summary>
    /// Gets the rotation policy for a schedule.
    /// </summary>
    Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId);

    /// <summary>
    /// Creates or updates the rotation policy for a schedule.
    /// </summary>
    Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy);

    /// <summary>
    /// Gets all backups that would be deleted by the rotation policy.
    /// </summary>
    Task<IEnumerable<BackupResult>> GetBackupsForRotationAsync(Guid scheduleId);

    /// <summary>
    /// Calculates the disk space that would be freed by rotation.
    /// </summary>
    Task<long> CalculateDiskSpaceFreedAsync(Guid scheduleId);
}
