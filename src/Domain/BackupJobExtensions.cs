#nullable enable

using DockerSqliteBackup.Constants;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="BackupJob"/> providing additional functionality for backup job status checks and formatting.
/// </summary>
public static class BackupJobExtensions
{
    /// <summary>
    /// Determines if the backup job has completed successfully.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job completed successfully; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsSuccessful(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status is (int)BackupStatus.Success or (int)BackupStatus.VerifiedSuccess;
    }

    /// <summary>
    /// Determines if the backup job has failed.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job failed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsFailed(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status is (int)BackupStatus.Failed or (int)BackupStatus.VerificationFailed;
    }

    /// <summary>
    /// Determines if the backup job is still pending or has not started.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job is pending; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsPending(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status == (int)BackupStatus.Pending &&
               !job.IsProcessing &&
               job.StartedAt is null;
    }

    /// <summary>
    /// Determines if the backup job is currently in progress.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job is in progress; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsInProgress(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status == (int)BackupStatus.InProgress && job.IsProcessing;
    }

    /// <summary>
    /// Determines if the backup job has been cancelled.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job was cancelled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsCancelled(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status == (int)BackupStatus.Cancelled;
    }

    /// <summary>
    /// Gets the formatted duration of the backup job execution.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>A formatted string representing the elapsed time (e.g., "2m 30s").</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static string GetFormattedDuration(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var elapsed = job.GetElapsedTime();
        return FormatTimeSpan(elapsed);
    }

    /// <summary>
    /// Gets the formatted retry count as a percentage of max retries.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>A string representing retry progress (e.g., "2/3").</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static string GetRetryProgress(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return $"{job.RetryCount}/{job.MaxRetries}";
    }

    /// <summary>
    /// Determines if the backup job has exceeded its maximum retry attempts.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if max retries exceeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool HasExceededRetries(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.RetryCount >= job.MaxRetries;
    }

    /// <summary>
    /// Gets the backup result if the job has one.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>The backup result if available; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static BackupResult? GetResult(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Result;
    }

    /// <summary>
    /// Determines if the backup job has a completed status (success, verified success, failed, or cancelled).
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job has reached a terminal status; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="job"/> is null.</exception>
    public static bool IsCompleted(this BackupJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        return job.Status switch
        {
            (int)BackupStatus.Success => true,
            (int)BackupStatus.Failed => true,
            (int)BackupStatus.Cancelled => true,
            (int)BackupStatus.VerifiedSuccess => true,
            (int)BackupStatus.VerificationFailed => true,
            _ => false
        };
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        var parts = new List<string>(3);

        if (span.TotalHours >= 1)
        {
            parts.Add($"{span.TotalHours:0}h");
        }

        if (span.Minutes > 0 || parts.Count > 0)
        {
            parts.Add($"{span.Minutes}m");
        }

        if (span.Seconds > 0 || parts.Count == 0)
        {
            parts.Add($"{span.Seconds}s");
        }

        return string.Join(" ", parts);
    }
}