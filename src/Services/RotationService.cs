#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for managing backup rotation and cleanup.
/// </summary>
public sealed class RotationService : IRotationService
{
    private readonly IBackupRepository _repository;
    private readonly ILogger<RotationService> _logger;

    public RotationService(
        IBackupRepository repository,
        ILogger<RotationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Executes the rotation policy for a schedule, deleting old backups.
    /// </summary>
    public async Task<int> ExecuteRotationAsync(Guid scheduleId)
    {
        var policy = await _repository.GetRotationPolicyAsync(scheduleId);
        if (policy  is null || policy.Strategy == (int)Constants.RotationStrategy.NoRotation)
        {
            _logger.LogInformation("No rotation policy or rotation disabled for schedule {ScheduleId}", scheduleId);
            return 0;
        }

        var backups = await GetBackupsForRotationAsync(scheduleId);
        var backupsToDelete = backups.ToList();

        int deletedCount = 0;
        foreach (var backup in backupsToDelete)
        {
            try
            {
                if (!string.IsNullOrEmpty(backup.BackupFilePath) && File.Exists(backup.BackupFilePath))
                {
                    File.Delete(backup.BackupFilePath);
                    _logger.LogInformation("Deleted backup file during rotation: {FilePath}", backup.BackupFilePath);
                }

                await _repository.DeleteBackupResultAsync(backup.Id);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete backup {BackupId} during rotation", backup.Id);
            }
        }

        policy.LastRotatedAt = DateTime.UtcNow;
        await _repository.SaveRotationPolicyAsync(policy);

        _logger.LogInformation("Rotation completed for schedule {ScheduleId}. Deleted {DeletedCount} backups",
            scheduleId, deletedCount);

        return deletedCount;
    }

    /// <summary>
    /// Gets the rotation policy for a schedule.
    /// </summary>
    public async Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId)
    {
        return await _repository.GetRotationPolicyAsync(scheduleId);
    }

    /// <summary>
    /// Creates or updates the rotation policy for a schedule.
    /// </summary>
    /// <param name="policy">The rotation policy to save.</param>
    /// <returns>The saved rotation policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ValidationException">Thrown when policy configuration is invalid.</exception>
    /// <exception cref="RotationException">Thrown when save fails.</exception>
    public async Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (!policy.IsValid())
        {
            throw new ValidationException(nameof(policy), "Rotation policy configuration is invalid");
        }

        policy.LastModifiedAt = DateTime.UtcNow;

        try
        {
            var saved = await _repository.SaveRotationPolicyAsync(policy);
            _logger.LogInformation("Rotation policy saved for schedule {ScheduleId}", policy.ScheduleId);
            return saved;
        }
        catch (Exception ex)
        {
            throw new RotationException("Failed to save rotation policy", ex);
        }
    }

    /// <summary>
    /// Gets all backups that would be deleted by the rotation policy.
    /// </summary>
    public async Task<IEnumerable<BackupResult>> GetBackupsForRotationAsync(Guid scheduleId)
    {
        var history = await _repository.GetBackupHistoryAsync(scheduleId, int.MaxValue);
        var policy = await _repository.GetRotationPolicyAsync(scheduleId);

        if (policy  is null)
        {
            return Enumerable.Empty<BackupResult>();
        }

        var backupsList = history.OrderByDescending(b => b.StartedAt).ToList();
        var backupsToDelete = new List<BackupResult>();

        for (int i = policy.MinimumBackupCount; i < backupsList.Count; i++)
        {
            var backup = backupsList[i];

            // Skip failed backups if configured to delete them
            if (policy.DeleteFailedBackups && !backup.IsSuccess)
            {
                backupsToDelete.Add(backup);
                continue;
            }

            // Skip backups that don't meet rotation criteria
            if (!policy.ShouldRotate(backupsList.Count, backup.StartedAt, !backup.IsSuccess))
            {
                continue;
            }

            // If verification is required before deletion and backup is not verified, skip deletion
            if (policy.VerifyBeforeDeletion && backup.Status != (int)BackupStatus.VerifiedSuccess)
            {
                _logger.LogInformation(
                    "Skipping deletion of backup {BackupId} for schedule {ScheduleId} - backup is not verified and VerifyBeforeDeletion is enabled",
                    backup.Id,
                    scheduleId);
                continue;
            }

            backupsToDelete.Add(backup);
        }

        return backupsToDelete;
    }

    /// <summary>
    /// Calculates the disk space that would be freed by rotation.
    /// </summary>
    public async Task<long> CalculateDiskSpaceFreedAsync(Guid scheduleId)
    {
        var backupsForRotation = await GetBackupsForRotationAsync(scheduleId);
        return backupsForRotation.Sum(b => b.BackupFileSizeBytes);
    }

    /// <summary>
    /// Previews which backups would be deleted by the rotation policy without actually deleting them.
    /// Returns a tuple containing the list of backups that would be deleted and the total disk space that would be freed.
    /// </summary>
    public async Task<(IEnumerable<BackupResult> backupsToDelete, long diskSpaceFreed)> PreviewRotationAsync(Guid scheduleId)
    {
        var policy = await _repository.GetRotationPolicyAsync(scheduleId);
        if (policy is null || policy.Strategy == (int)Constants.RotationStrategy.NoRotation)
        {
            _logger.LogInformation("No rotation policy or rotation disabled for schedule {ScheduleId}", scheduleId);
            return (Enumerable.Empty<BackupResult>(), 0);
        }

        var backupsForRotation = await GetBackupsForRotationAsync(scheduleId);
        var backupsToDelete = backupsForRotation.ToList();
        var diskSpaceFreed = backupsToDelete.Sum(b => b.BackupFileSizeBytes);

        _logger.LogInformation("Rotation preview completed for schedule {ScheduleId}. Would delete {DeletedCount} backups, freeing {DiskSpaceFreed} bytes",
            scheduleId, backupsToDelete.Count, diskSpaceFreed);

        return (backupsToDelete, diskSpaceFreed);
    }
}
