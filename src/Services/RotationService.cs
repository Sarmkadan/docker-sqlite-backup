#nullable enable
// Author: Vladyslav Zaiets

using System.Diagnostics;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationService"/> class.
    /// </summary>
    /// <param name="repository">The repository used to access backup data and rotation policies.</param>
    /// <param name="logger">The logger instance for recording rotation operations.</param>
    public RotationService(
        IBackupRepository repository,
        ILogger<RotationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the rotation policy for a schedule, deleting old backups.
    /// </summary>
    /// <param name="scheduleId">The identifier of the backup schedule to rotate.</param>
    /// <returns>
    /// A task that resolves to the number of backups that were deleted as a result of the rotation.
    /// </returns>
    public async Task<int> ExecuteRotationAsync(Guid scheduleId)
    {
        if (scheduleId == Guid.Empty)
        {
            throw new System.ArgumentException("Schedule ID cannot be empty.", nameof(scheduleId));
        }

        using var scope = _logger.BeginScope("Rotation for schedule {ScheduleId}", scheduleId);
        var stopwatch = Stopwatch.StartNew();

        var policy = await _repository.GetRotationPolicyAsync(scheduleId);
        if (policy  is null || policy.Strategy == (int)Constants.RotationStrategy.NoRotation)
        {
            _logger.LogInformation("No rotation policy or rotation disabled for schedule {ScheduleId}", scheduleId);
            return 0;
        }

        var backups = await GetBackupsForRotationAsync(scheduleId);
        var backupsToDelete = backups.ToList();

        _logger.LogDebug(
            "Applying rotation policy strategy {PolicyStrategy} to {CandidateBackupCount} candidate backups",
            policy.Strategy,
            backupsToDelete.Count);

        int deletedCount = 0;
        foreach (var backup in backupsToDelete)
        {
            try
            {
                if (!string.IsNullOrEmpty(backup.BackupFilePath) && File.Exists(backup.BackupFilePath))
                {
                    File.Delete(backup.BackupFilePath);
                }

                await _repository.DeleteBackupResultAsync(backup.Id);
                deletedCount++;
                _logger.LogDebug(
                    "Deleted backup {BackupId} at {BackupFilePath} with size {BackupFileSizeBytes} bytes",
                    backup.Id,
                    backup.BackupFilePath,
                    backup.BackupFileSizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete backup {BackupId} during rotation", backup.Id);
            }
        }

        policy.LastRotatedAt = DateTime.UtcNow;
        await _repository.SaveRotationPolicyAsync(policy);

        stopwatch.Stop();
        _logger.LogInformation(
            "Rotation completed. Deleted {DeletedCount} backups in {ElapsedMilliseconds} ms",
            deletedCount,
            stopwatch.ElapsedMilliseconds);

        return deletedCount;
    }

    /// <summary>
    /// Gets the rotation policy for a schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the backup schedule.</param>
    /// <returns>
    /// A task that resolves to the <see cref="RotationPolicy"/> associated with the schedule,
    /// or <c>null</c> if no policy is defined.
    /// </returns>
    public async Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId)
    {
        return await _repository.GetRotationPolicyAsync(scheduleId);
    }

    /// <summary>
    /// Creates or updates the rotation policy for a schedule.
    /// </summary>
    /// <param name="policy">The rotation policy to save.</param>
    /// <returns>
    /// A task that resolves to the saved <see cref="RotationPolicy"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    /// <exception cref="ValidationException">Thrown when policy configuration is invalid.</exception>
    /// <exception cref="RotationException">Thrown when saving the policy fails.</exception>
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
    /// <param name="scheduleId">The identifier of the backup schedule.</param>
    /// <returns>
    /// A task that resolves to an <see cref="IEnumerable{BackupResult}"/> containing the backups
    /// that would be removed according to the current rotation policy.
    /// </returns>
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
    /// <param name="scheduleId">The identifier of the backup schedule.</param>
    /// <returns>
    /// A task that resolves to the total number of bytes that would be reclaimed after rotation.
    /// </returns>
    public async Task<long> CalculateDiskSpaceFreedAsync(Guid scheduleId)
    {
        var backupsForRotation = await GetBackupsForRotationAsync(scheduleId);
        return backupsForRotation.Sum(b => b.BackupFileSizeBytes);
    }

    /// <summary>
    /// Previews which backups would be deleted by the rotation policy without actually deleting them.
    /// Returns a tuple containing the list of backups that would be deleted and the total disk space that would be freed.
    /// </summary>
    /// <param name="scheduleId">The identifier of the backup schedule.</param>
    /// <returns>
    /// A task that resolves to a tuple where the first item is an <see cref="IEnumerable{BackupResult}"/>
    /// of the backups that would be deleted, and the second item is the total disk space (in bytes) that would be freed.
    /// </returns>
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
