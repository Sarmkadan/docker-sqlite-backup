// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Defines the policy for rotating and deleting old backup files.
/// </summary>
public class RotationPolicy
{
    /// <summary>Gets or sets the unique identifier for this policy.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the associated schedule ID.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the rotation strategy to use.</summary>
    public int Strategy { get; set; } = (int)Constants.RotationStrategy.Combined;

    /// <summary>Gets or sets the maximum number of backup files to keep.</summary>
    public int MaxBackupCount { get; set; } = 10;

    /// <summary>Gets or sets the maximum age of backup files in days.</summary>
    public int MaxAgeDays { get; set; } = 30;

    /// <summary>Gets or sets whether to verify backups before deletion during rotation.</summary>
    public bool VerifyBeforeDeletion { get; set; } = false;

    /// <summary>Gets or sets the minimum number of backups to always keep, regardless of age.</summary>
    public int MinimumBackupCount { get; set; } = 3;

    /// <summary>Gets or sets whether failed backups should be deleted during rotation.</summary>
    public bool DeleteFailedBackups { get; set; } = true;

    /// <summary>Gets or sets when the policy was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the policy was last modified.</summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when rotation was last executed.</summary>
    public DateTime? LastRotatedAt { get; set; }

    /// <summary>
    /// Validates the rotation policy.
    /// </summary>
    public bool IsValid()
    {
        if (MaxBackupCount < 1)
            return false;

        if (MaxAgeDays < 1)
            return false;

        if (MinimumBackupCount < 1 || MinimumBackupCount > MaxBackupCount)
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a backup should be rotated (deleted) based on this policy.
    /// </summary>
    public bool ShouldRotate(int totalBackupCount, DateTime backupDate, bool isFailed)
    {
        var strategy = (Constants.RotationStrategy)Strategy;

        return strategy switch
        {
            Constants.RotationStrategy.NoRotation => false,
            Constants.RotationStrategy.MaxFileCount => totalBackupCount > MaxBackupCount && totalBackupCount > MinimumBackupCount,
            Constants.RotationStrategy.MaxAge => (DateTime.UtcNow - backupDate).TotalDays > MaxAgeDays && totalBackupCount > MinimumBackupCount,
            Constants.RotationStrategy.Combined => ShouldRotateByCount(totalBackupCount) || ShouldRotateByAge(backupDate),
            _ => false
        };
    }

    private bool ShouldRotateByCount(int totalBackupCount)
    {
        return totalBackupCount > MaxBackupCount && totalBackupCount > MinimumBackupCount;
    }

    private bool ShouldRotateByAge(DateTime backupDate)
    {
        return (DateTime.UtcNow - backupDate).TotalDays > MaxAgeDays;
    }
}
