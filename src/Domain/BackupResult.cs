// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Represents the result of a backup operation.
/// </summary>
public class BackupResult
{
    /// <summary>Gets or sets the unique identifier for this backup result.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the identifier of the associated schedule.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the identifier of the associated backup job.</summary>
    public Guid BackupJobId { get; set; }

    /// <summary>Gets or sets the current status of the backup.</summary>
    public int Status { get; set; } = (int)Constants.BackupStatus.Pending;

    /// <summary>Gets or sets the path where the backup file is stored.</summary>
    public string BackupFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the size of the backup file in bytes.</summary>
    public long BackupFileSizeBytes { get; set; }

    /// <summary>Gets or sets the checksum (SHA256) of the backup file.</summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the backup started.</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Gets or sets the timestamp when the backup completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the duration of the backup operation in milliseconds.</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>Gets or sets any error message if the backup failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the stack trace if an error occurred.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Gets or sets whether this backup has been verified.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Gets or sets the timestamp when the backup was verified.</summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>Gets or sets notes about the backup operation.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the backup is stored in S3.</summary>
    public bool IsStoredInS3 { get; set; }

    /// <summary>Gets or sets whether the backup is stored locally.</summary>
    public bool IsStoredLocally { get; set; } = true;

    /// <summary>Gets or sets the S3 object key if stored in S3.</summary>
    public string? S3ObjectKey { get; set; }

    /// <summary>
    /// Indicates whether the backup operation was successful.
    /// </summary>
    public bool IsSuccess => Status == (int)Constants.BackupStatus.Success || 
                            Status == (int)Constants.BackupStatus.VerifiedSuccess;

    /// <summary>
    /// Indicates whether the backup is still in progress.
    /// </summary>
    public bool IsInProgress => Status == (int)Constants.BackupStatus.InProgress;

    /// <summary>
    /// Gets the elapsed duration since the backup started.
    /// </summary>
    public TimeSpan GetElapsedDuration()
    {
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }
}
