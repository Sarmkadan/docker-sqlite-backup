// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Represents a snapshot that can be used for database restoration.
/// </summary>
public class RestorePoint
{
    /// <summary>
    /// Unique identifier for the restore point.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// ID of the source backup snapshot.
    /// </summary>
    public string BackupSnapshotId { get; set; } = null!;

    /// <summary>
    /// ID of the backup job this restore point belongs to.
    /// </summary>
    public string BackupJobId { get; set; } = null!;

    /// <summary>
    /// Timestamp of the backup snapshot.
    /// </summary>
    public DateTime SnapshotTime { get; set; }

    /// <summary>
    /// Whether this restore point is readily available for restoration.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Whether this restore point has been tested for restore capability.
    /// </summary>
    public bool HasBeenRestored { get; set; }

    /// <summary>
    /// Timestamp of the last successful restoration test.
    /// </summary>
    public DateTime? LastRestoredAt { get; set; }

    /// <summary>
    /// Retention expiration date for this restore point.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Priority level for retention (higher values are retained longer).
    /// </summary>
    public int RetentionPriority { get; set; } = 1;

    /// <summary>
    /// Custom label for the restore point (e.g., "Pre-Release", "Checkpoint").
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Detailed description of this restore point.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Metadata about the database at this restore point.
    /// </summary>
    public BackupMetadata? DatabaseMetadata { get; set; }

    /// <summary>
    /// Timestamp when the restore point was registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional custom tags for the restore point.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Determines if the restore point has expired.
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Marks this restore point as tested by recording a successful restoration attempt.
    /// </summary>
    public void MarkAsRestored()
    {
        HasBeenRestored = true;
        LastRestoredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this restore point as unavailable for restoration.
    /// </summary>
    public void MarkAsUnavailable()
    {
        IsAvailable = false;
    }

    /// <summary>
    /// Calculates the age of this restore point in days.
    /// </summary>
    public double GetAgeInDays()
    {
        return (DateTime.UtcNow - SnapshotTime).TotalDays;
    }

    /// <summary>
    /// Gets the time remaining before expiration in days, or null if no expiration.
    /// </summary>
    public double? GetDaysUntilExpiration()
    {
        if (!ExpiresAt.HasValue)
            return null;

        var remaining = ExpiresAt.Value - DateTime.UtcNow;
        return remaining.TotalDays > 0 ? remaining.TotalDays : 0;
    }
}
