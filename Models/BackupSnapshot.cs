// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Represents a single point-in-time backup snapshot of a SQLite database.
/// </summary>
public class BackupSnapshot
{
    /// <summary>
    /// Unique identifier for the backup snapshot.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// ID of the backup job that created this snapshot.
    /// </summary>
    public string BackupJobId { get; set; } = null!;

    /// <summary>
    /// Timestamp when the backup was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Size of the backup file in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Storage path where the backup file is located.
    /// </summary>
    public string StoragePath { get; set; } = null!;

    /// <summary>
    /// SHA256 hash of the backup file for integrity verification.
    /// </summary>
    public string? FileHash { get; set; }

    /// <summary>
    /// Current status of the snapshot.
    /// </summary>
    public BackupStatus Status { get; set; } = BackupStatus.Pending;

    /// <summary>
    /// Duration of the backup operation in milliseconds.
    /// </summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>
    /// Detailed error message if the backup failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Compression ratio if compression was applied (0.0 to 1.0).
    /// </summary>
    public double? CompressionRatio { get; set; }

    /// <summary>
    /// Size of the original uncompressed backup in bytes.
    /// </summary>
    public long? OriginalSizeBytes { get; set; }

    /// <summary>
    /// Whether this snapshot has been verified successfully.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Timestamp of when verification was last performed.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// Metadata about the database at the time of backup.
    /// </summary>
    public BackupMetadata? Metadata { get; set; }

    /// <summary>
    /// Additional tags or labels for the snapshot.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Calculates the compression percentage.
    /// </summary>
    /// <returns>Compression percentage (0-100), or 0 if not applicable.</returns>
    public double GetCompressionPercentage()
    {
        if (!OriginalSizeBytes.HasValue || OriginalSizeBytes == 0)
            return 0;

        var ratio = (double)SizeBytes / OriginalSizeBytes.Value;
        return (1 - ratio) * 100;
    }

    /// <summary>
    /// Determines if the snapshot is safe to delete based on retention policies.
    /// </summary>
    public bool IsEligibleForDeletion(int maxRetentionDays)
    {
        var age = DateTime.UtcNow - CreatedAt;
        return age.TotalDays > maxRetentionDays && Status == BackupStatus.Completed;
    }

    /// <summary>
    /// Marks the snapshot as verified and records the verification timestamp.
    /// </summary>
    public void MarkAsVerified()
    {
        IsVerified = true;
        VerifiedAt = DateTime.UtcNow;
        Status = BackupStatus.Completed;
    }

    /// <summary>
    /// Marks the snapshot as failed with an error message.
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = BackupStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
