// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Manages cleanup of old and expired backup snapshots based on retention policies.
/// </summary>
public class BackupCleanupService
{
    private readonly IBackupRepository _repository;
    private readonly IStorageService _storageService;
    private readonly ILogger<BackupCleanupService> _logger;

    public BackupCleanupService(
        IBackupRepository repository,
        IStorageService storageService,
        ILogger<BackupCleanupService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs cleanup for all backup jobs based on their retention policies.
    /// </summary>
    public async Task<CleanupResult> RunGlobalCleanupAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult { StartTime = DateTime.UtcNow };

        _logger.LogInformation("Starting global backup cleanup");

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var jobResult = await RunCleanupForJobAsync(job, cancellationToken);
                result.JobResults.Add(jobResult);
            }

            result.EndTime = DateTime.UtcNow;
            result.TotalSnapshotsDeleted = result.JobResults.Sum(r => r.SnapshotsDeleted);
            result.TotalStorageFreedBytes = result.JobResults.Sum(r => r.StorageFreedBytes);

            _logger.LogInformation(
                "Global cleanup completed: {SnapshotsDeleted} snapshots deleted, {StorageFreed} bytes freed",
                result.TotalSnapshotsDeleted, result.TotalStorageFreedBytes);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Global cleanup failed");
            return result;
        }
    }

    /// <summary>
    /// Runs cleanup for a specific backup job.
    /// </summary>
    public async Task<JobCleanupResult> RunCleanupForJobAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        var result = new JobCleanupResult
        {
            JobId = job.Id,
            JobName = job.Name,
            StartTime = DateTime.UtcNow
        };

        _logger.LogInformation("Running cleanup for job {JobId}", job.Id);

        try
        {
            var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
            var completedSnapshots = snapshots
                .Where(s => s.Status == BackupStatus.Completed)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            // Identify snapshots to delete based on retention policies
            var snapshotsToDelete = IdentifySnapshotsForDeletion(
                completedSnapshots,
                job.MaxRetentionCount,
                job.MaxRetentionDays);

            // Delete each snapshot
            foreach (var snapshot in snapshotsToDelete)
            {
                try
                {
                    // Delete from storage
                    await _storageService.DeleteBackupAsync(snapshot.StoragePath, job.StorageProvider, cancellationToken);
                    result.StorageFreedBytes += snapshot.SizeBytes;

                    // Delete from repository
                    await _repository.DeleteSnapshotAsync(snapshot.Id, cancellationToken);

                    result.SnapshotsDeleted++;

                    _logger.LogInformation("Deleted backup snapshot {SnapshotId} for job {JobId}",
                        snapshot.Id, job.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete snapshot {SnapshotId}", snapshot.Id);
                    result.FailedDeletions++;
                }
            }

            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = true;

            _logger.LogInformation(
                "Cleanup for job {JobId} completed: {SnapshotsDeleted} deleted, {StorageFreed} freed",
                job.Id, result.SnapshotsDeleted, result.StorageFreedBytes);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTime = DateTime.UtcNow;
            result.IsSuccessful = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Cleanup failed for job {JobId}", job.Id);
            return result;
        }
    }

    /// <summary>
    /// Cleans up expired restore points.
    /// </summary>
    public async Task<int> CleanupExpiredRestorePointsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up expired restore points");

        int deletedCount = 0;

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var restorePoints = await _repository.GetRestorePointsByJobIdAsync(job.Id, cancellationToken);
                var expiredPoints = restorePoints.Where(rp => rp.IsExpired()).ToList();

                foreach (var restorePoint in expiredPoints)
                {
                    try
                    {
                        await _repository.DeleteRestorePointAsync(restorePoint.Id, cancellationToken);
                        deletedCount++;

                        _logger.LogInformation("Deleted expired restore point {RestorePointId}", restorePoint.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete restore point {RestorePointId}", restorePoint.Id);
                    }
                }
            }

            _logger.LogInformation("Cleanup of expired restore points completed: {DeletedCount} deleted", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired restore points");
        }

        return deletedCount;
    }

    /// <summary>
    /// Cleans up backup snapshots that failed or are in incomplete state.
    /// </summary>
    public async Task<int> CleanupFailedSnapshotsAsync(
        TimeSpan ageThreshold,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up failed backup snapshots older than {AgeThreshold}", ageThreshold);

        int deletedCount = 0;

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);
            var cutoffTime = DateTime.UtcNow - ageThreshold;

            foreach (var job in jobs)
            {
                var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
                var failedSnapshots = snapshots
                    .Where(s => (s.Status == BackupStatus.Failed || s.Status == BackupStatus.VerificationFailed) &&
                               s.CreatedAt < cutoffTime)
                    .ToList();

                foreach (var snapshot in failedSnapshots)
                {
                    try
                    {
                        await _repository.DeleteSnapshotAsync(snapshot.Id, cancellationToken);
                        deletedCount++;

                        _logger.LogInformation("Deleted failed snapshot {SnapshotId}", snapshot.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete snapshot {SnapshotId}", snapshot.Id);
                    }
                }
            }

            _logger.LogInformation("Cleanup of failed snapshots completed: {DeletedCount} deleted", deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up failed snapshots");
        }

        return deletedCount;
    }

    /// <summary>
    /// Identifies which snapshots should be deleted based on retention policies.
    /// </summary>
    private List<BackupSnapshot> IdentifySnapshotsForDeletion(
        List<BackupSnapshot> snapshots,
        int maxRetentionCount,
        int maxRetentionDays)
    {
        var toDelete = new List<BackupSnapshot>();
        var cutoffDate = DateTime.UtcNow.AddDays(-maxRetentionDays);

        // Snapshots beyond retention count
        if (snapshots.Count > maxRetentionCount)
        {
            toDelete.AddRange(snapshots.Skip(maxRetentionCount));
        }

        // Snapshots beyond retention days
        toDelete.AddRange(snapshots.Where(s => s.CreatedAt < cutoffDate && !toDelete.Contains(s)));

        return toDelete.Distinct().ToList();
    }
}

/// <summary>
/// Result of a cleanup operation.
/// </summary>
public class CleanupResult
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; } = true;
    public List<JobCleanupResult> JobResults { get; set; } = new();
    public int TotalSnapshotsDeleted { get; set; }
    public long TotalStorageFreedBytes { get; set; }
    public string? ErrorMessage { get; set; }

    public TimeSpan GetDuration() => (EndTime ?? DateTime.UtcNow) - StartTime;
}

/// <summary>
/// Result of cleanup for a single job.
/// </summary>
public class JobCleanupResult
{
    public string? JobId { get; set; }
    public string? JobName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsSuccessful { get; set; }
    public int SnapshotsDeleted { get; set; }
    public int FailedDeletions { get; set; }
    public long StorageFreedBytes { get; set; }
    public string? ErrorMessage { get; set; }

    public TimeSpan GetDuration() => (EndTime ?? DateTime.UtcNow) - StartTime;
}
