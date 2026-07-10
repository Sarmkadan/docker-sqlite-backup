#nullable enable

using DockerSqliteBackup.Constants;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="BackupJob"/> providing additional functionality.
/// </summary>
public static class BackupJobExtensions
{
    /// <summary>
    /// Determines if the backup job has completed successfully.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job completed successfully; otherwise, false.</returns>
    public static bool IsSuccessful(this BackupJob job)
    {
        return job.Status == (int)BackupStatus.Success ||
               job.Status == (int)BackupStatus.VerifiedSuccess;
    }

    /// <summary>
    /// Determines if the backup job has failed.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job failed; otherwise, false.</returns>
    public static bool IsFailed(this BackupJob job)
    {
        return job.Status == (int)BackupStatus.Failed ||
               job.Status == (int)BackupStatus.VerificationFailed;
    }

    /// <summary>
    /// Determines if the backup job is still pending or has not started.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job is pending; otherwise, false.</returns>
    public static bool IsPending(this BackupJob job)
    {
        return job.Status == (int)BackupStatus.Pending &&
               !job.IsProcessing &&
               job.StartedAt is null;
    }

    /// <summary>
    /// Determines if the backup job is currently in progress.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if the job is in progress; otherwise, false.</returns>
    public static bool IsInProgress(this BackupJob job)
    {
        return job.Status == (int)BackupStatus.InProgress &&
               job.IsProcessing;
    }

    /// <summary>
    /// Gets the formatted duration of the backup job execution.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>A formatted string representing the elapsed time (e.g., "2m 30s").</returns>
    public static string GetFormattedDuration(this BackupJob job)
    {
        var elapsed = job.GetElapsedTime();
        return FormatTimeSpan(elapsed);
    }

    /// <summary>
    /// Gets the formatted retry count as a percentage of max retries.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>A string representing retry progress (e.g., "2/3").</returns>
    public static string GetRetryProgress(this BackupJob job)
    {
        return $"{job.RetryCount}/{job.MaxRetries}";
    }

    /// <summary>
    /// Determines if the backup job has exceeded its maximum retry attempts.
    /// </summary>
    /// <param name="job">The backup job to check.</param>
    /// <returns>True if max retries exceeded; otherwise, false.</returns>
    public static bool HasExceededRetries(this BackupJob job)
    {
        return job.RetryCount >= job.MaxRetries;
    }

    /// <summary>
    /// Gets the backup result if the job has one.
    /// </summary>
    /// <param name="job">The backup job.</param>
    /// <returns>The backup result if available; otherwise, null.</returns>
    public static BackupResult? GetResult(this BackupJob job)
    {
        return job.Result;
    }

    private static string FormatTimeSpan(TimeSpan span)
    {
        var parts = new List<string>();

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
