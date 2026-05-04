// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Data;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Manages backup job lifecycle, configuration, and persistence.
/// </summary>
public class BackupJobService
{
    private readonly IBackupRepository _repository;
    private readonly ConfigurationValidationService _validationService;
    private readonly ILogger<BackupJobService> _logger;

    public BackupJobService(
        IBackupRepository repository,
        ConfigurationValidationService validationService,
        ILogger<BackupJobService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new backup job with validation.
    /// </summary>
    public async Task<BackupJob> CreateBackupJobAsync(
        string name,
        string databasePath,
        BackupSchedule schedule,
        StorageProvider storageProvider,
        CancellationToken cancellationToken = default)
    {
        var job = new BackupJob
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            DatabasePath = databasePath,
            Schedule = schedule,
            StorageProvider = storageProvider,
            CreatedAt = DateTime.UtcNow
        };

        // Validate the job
        var validationResult = _validationService.ValidateBackupJob(job);
        if (!validationResult.IsValid)
        {
            var errorSummary = validationResult.GetErrorSummary();
            throw new BackupException($"Invalid backup job configuration:\n{errorSummary}", job.Id, null);
        }

        // Save to repository
        await _repository.SaveBackupJobAsync(job, cancellationToken);

        _logger.LogInformation("Backup job created: {JobId} ({JobName})", job.Id, job.Name);
        return job;
    }

    /// <summary>
    /// Retrieves a backup job by ID.
    /// </summary>
    public async Task<BackupJob?> GetBackupJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetBackupJobByIdAsync(jobId, cancellationToken);
    }

    /// <summary>
    /// Retrieves all backup jobs.
    /// </summary>
    public async Task<List<BackupJob>> GetAllBackupJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllBackupJobsAsync(cancellationToken);
    }

    /// <summary>
    /// Updates an existing backup job.
    /// </summary>
    public async Task<BackupJob> UpdateBackupJobAsync(
        string jobId,
        Action<BackupJob> updateAction,
        CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetBackupJobByIdAsync(jobId, cancellationToken)
            ?? throw new BackupException($"Backup job not found: {jobId}", jobId, null);

        updateAction(job);

        // Validate after update
        var validationResult = _validationService.ValidateBackupJob(job);
        if (!validationResult.IsValid)
        {
            var errorSummary = validationResult.GetErrorSummary();
            throw new BackupException($"Invalid backup job configuration after update:\n{errorSummary}", jobId, null);
        }

        await _repository.UpdateBackupJobAsync(job, cancellationToken);

        _logger.LogInformation("Backup job updated: {JobId}", jobId);
        return job;
    }

    /// <summary>
    /// Deletes a backup job and its associated backups.
    /// </summary>
    public async Task<bool> DeleteBackupJobAsync(
        string jobId,
        bool deleteBackups = false,
        CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetBackupJobByIdAsync(jobId, cancellationToken);
        if (job == null)
            return false;

        if (deleteBackups)
        {
            var snapshots = await _repository.GetSnapshotsByJobIdAsync(jobId, cancellationToken);
            foreach (var snapshot in snapshots)
            {
                await _repository.DeleteSnapshotAsync(snapshot.Id, cancellationToken);
            }
        }

        await _repository.DeleteBackupJobAsync(jobId, cancellationToken);

        _logger.LogInformation("Backup job deleted: {JobId}", jobId);
        return true;
    }

    /// <summary>
    /// Updates the job status.
    /// </summary>
    public async Task<BackupJob> UpdateJobStatusAsync(
        string jobId,
        BackupStatus status,
        CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job => job.Status = status, cancellationToken);
    }

    /// <summary>
    /// Updates the last backup timestamp.
    /// </summary>
    public async Task<BackupJob> UpdateLastBackupTimeAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job => job.LastBackupAt = DateTime.UtcNow, cancellationToken);
    }

    /// <summary>
    /// Adds a tag to a backup job.
    /// </summary>
    public async Task<BackupJob> AddJobTagAsync(
        string jobId,
        string tagKey,
        string tagValue,
        CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job =>
        {
            if (job.Tags == null)
                job.Tags = new();
            job.Tags[tagKey] = tagValue;
        }, cancellationToken);
    }

    /// <summary>
    /// Removes a tag from a backup job.
    /// </summary>
    public async Task<BackupJob> RemoveJobTagAsync(
        string jobId,
        string tagKey,
        CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job =>
        {
            if (job.Tags?.ContainsKey(tagKey) == true)
                job.Tags.Remove(tagKey);
        }, cancellationToken);
    }

    /// <summary>
    /// Gets statistics about a backup job.
    /// </summary>
    public async Task<BackupJobStatistics> GetBackupJobStatisticsAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetBackupJobByIdAsync(jobId, cancellationToken)
            ?? throw new BackupException($"Backup job not found: {jobId}", jobId, null);

        var snapshots = await _repository.GetSnapshotsByJobIdAsync(jobId, cancellationToken);
        var completedSnapshots = snapshots.Where(s => s.Status == BackupStatus.Completed).ToList();
        var failedSnapshots = snapshots.Where(s => s.Status == BackupStatus.Failed).ToList();

        return new BackupJobStatistics
        {
            BackupJobId = jobId,
            TotalBackups = snapshots.Count,
            SuccessfulBackups = completedSnapshots.Count,
            FailedBackups = failedSnapshots.Count,
            TotalBackupSizeBytes = completedSnapshots.Sum(s => s.SizeBytes),
            AverageBackupDurationMs = completedSnapshots.Any() ? (long)completedSnapshots.Average(s => s.DurationMilliseconds) : 0,
            LastBackupTime = job.LastBackupAt,
            OldestBackupTime = snapshots.OrderBy(s => s.CreatedAt).FirstOrDefault()?.CreatedAt,
            NewestBackupTime = snapshots.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt,
            SuccessRate = snapshots.Count > 0 ? (completedSnapshots.Count * 100.0) / snapshots.Count : 0
        };
    }

    /// <summary>
    /// Enables a backup job schedule.
    /// </summary>
    public async Task<BackupJob> EnableScheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job =>
        {
            if (job.Schedule != null)
                job.Schedule.IsEnabled = true;
        }, cancellationToken);
    }

    /// <summary>
    /// Disables a backup job schedule.
    /// </summary>
    public async Task<BackupJob> DisableScheduleAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await UpdateBackupJobAsync(jobId, job =>
        {
            if (job.Schedule != null)
                job.Schedule.IsEnabled = false;
        }, cancellationToken);
    }
}

/// <summary>
/// Statistics for a backup job.
/// </summary>
public class BackupJobStatistics
{
    public string BackupJobId { get; set; } = null!;
    public int TotalBackups { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public long TotalBackupSizeBytes { get; set; }
    public long AverageBackupDurationMs { get; set; }
    public DateTime? LastBackupTime { get; set; }
    public DateTime? OldestBackupTime { get; set; }
    public DateTime? NewestBackupTime { get; set; }
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets the health status based on success rate.
    /// </summary>
    public string GetHealthStatus()
    {
        return SuccessRate switch
        {
            >= 95 => "Healthy",
            >= 80 => "Warning",
            >= 50 => "Poor",
            _ => "Failed"
        };
    }
}
