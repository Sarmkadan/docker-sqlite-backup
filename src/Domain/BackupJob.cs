// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Represents a single backup job execution.
/// </summary>
public class BackupJob
{
    /// <summary>Gets or sets the unique identifier for this backup job.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the identifier of the associated schedule.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the job status.</summary>
    public int Status { get; set; } = (int)Constants.BackupStatus.Pending;

    /// <summary>Gets or sets when the job was created/scheduled.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the job execution started.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>Gets or sets when the job execution completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the number of retry attempts made.</summary>
    public int RetryCount { get; set; }

    /// <summary>Gets or sets the maximum number of retries allowed.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Gets or sets whether the job is currently being processed.</summary>
    public bool IsProcessing { get; set; }

    /// <summary>Gets or sets the associated backup result.</summary>
    public BackupResult? Result { get; set; }

    /// <summary>
    /// Determines if the job can be retried.
    /// </summary>
    public bool CanRetry => RetryCount < MaxRetries && Status == (int)Constants.BackupStatus.Failed;

    /// <summary>
    /// Gets the total elapsed time of the job.
    /// </summary>
    public TimeSpan GetElapsedTime()
    {
        if (StartedAt == null)
            return TimeSpan.Zero;

        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt.Value;
    }

    /// <summary>
    /// Marks the job as started with the current timestamp.
    /// </summary>
    public void MarkStarted()
    {
        StartedAt = DateTime.UtcNow;
        Status = (int)Constants.BackupStatus.InProgress;
        IsProcessing = true;
    }

    /// <summary>
    /// Marks the job as completed with the specified status.
    /// </summary>
    public void MarkCompleted(int status)
    {
        CompletedAt = DateTime.UtcNow;
        Status = status;
        IsProcessing = false;
    }

    /// <summary>
    /// Increments the retry counter.
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
    }
}
