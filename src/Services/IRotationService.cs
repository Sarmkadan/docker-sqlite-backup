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
    /// <param name="scheduleId">The identifier of the schedule whose rotation policy is applied.</param>
    /// <returns>The number of backups deleted.</returns>
    Task<int> ExecuteRotationAsync(Guid scheduleId);

    /// <summary>
    /// Gets the rotation policy for a schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>The rotation policy, or <c>null</c> if none is configured for the schedule.</returns>
    Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId);

    /// <summary>
    /// Creates or updates the rotation policy for a schedule.
    /// </summary>
    /// <param name="policy">The rotation policy to persist.</param>
    /// <returns>The saved rotation policy.</returns>
    Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy);

    /// <summary>
    /// Gets all backups that would be deleted by the rotation policy.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>The backups selected for deletion by the rotation policy. Empty if none qualify.</returns>
    Task<IEnumerable<BackupResult>> GetBackupsForRotationAsync(Guid scheduleId);

    /// <summary>
    /// Calculates the disk space that would be freed by rotation.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>The total size, in bytes, of the backups selected by the rotation policy.</returns>
    Task<long> CalculateDiskSpaceFreedAsync(Guid scheduleId);

    /// <summary>
    /// Previews which backups would be deleted by the rotation policy without actually deleting them.
    /// Returns a tuple containing the list of backups that would be deleted and the total disk space that would be freed.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <returns>
    /// A tuple of the backups that would be deleted (<c>backupsToDelete</c>) and
    /// the total disk space in bytes that would be freed (<c>diskSpaceFreed</c>).
    /// </returns>
    Task<(IEnumerable<BackupResult> backupsToDelete, long diskSpaceFreed)> PreviewRotationAsync(Guid scheduleId);
}
