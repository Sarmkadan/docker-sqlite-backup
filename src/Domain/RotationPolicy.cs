#nullable enable
// Author: Vladyslav Zaiets

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
    /// <returns>
    /// <c>true</c> if <see cref="MaxBackupCount"/>, <see cref="MaxAgeDays"/> and
    /// <see cref="MinimumBackupCount"/> are consistent; otherwise <c>false</c>.
    /// </returns>
    public bool IsValid()
    {
        // MaxBackupCount of 0 means unlimited — skip count-based rotation entirely.
        if (MaxBackupCount < 0)
            return false;

        if (MaxAgeDays < 1)
            return false;

        // When MaxBackupCount is 0 (unlimited), MinimumBackupCount only needs to be positive.
        if (MaxBackupCount > 0 && (MinimumBackupCount < 1 || MinimumBackupCount > MaxBackupCount))
            return false;

        if (MaxBackupCount == 0 && MinimumBackupCount < 1)
            return false;

        return true;
    }

    /// <summary>
    /// Determines if a backup should be rotated (deleted) based on this policy.
    /// </summary>
    /// <param name="totalBackupCount">The total number of backups currently stored.</param>
    /// <param name="backupDate">The UTC creation date of the backup being evaluated.</param>
    /// <param name="isFailed">
    /// Whether the backup failed. Currently not used by the decision logic; failed-backup
    /// handling is controlled by <see cref="DeleteFailedBackups"/> elsewhere.
    /// </param>
    /// <returns>
    /// <c>true</c> if the backup matches the active <see cref="Strategy"/> rules; otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// For <c>Combined</c>, the count rule respects <see cref="MinimumBackupCount"/>, but the age rule
    /// does not, so an over-age backup can be selected even when it is among the minimum kept.
    /// </remarks>
    public bool ShouldRotate(int totalBackupCount, DateTime backupDate, bool isFailed)
    {
        var strategy = (Constants.RotationStrategy)Strategy;

        return strategy switch
        {
            Constants.RotationStrategy.NoRotation => false,
            Constants.RotationStrategy.MaxFileCount => MaxBackupCount > 0 && totalBackupCount > MaxBackupCount && totalBackupCount > MinimumBackupCount,
            Constants.RotationStrategy.MaxAge => (DateTime.UtcNow - backupDate).TotalDays > MaxAgeDays && totalBackupCount > MinimumBackupCount,
            Constants.RotationStrategy.Combined => ShouldRotateByCount(totalBackupCount) || ShouldRotateByAge(backupDate),
            _ => false
        };
    }

    /// <summary>
    /// Checks the count-based rotation rule.
    /// </summary>
    /// <param name="totalBackupCount">The total number of backups currently stored.</param>
    /// <returns><c>true</c> if the count exceeds both <see cref="MaxBackupCount"/> and <see cref="MinimumBackupCount"/>.</returns>
    private bool ShouldRotateByCount(int totalBackupCount)
    {
        // MaxBackupCount of 0 means unlimited — never rotate by count.
        if (MaxBackupCount == 0)
            return false;

        return totalBackupCount > MaxBackupCount && totalBackupCount > MinimumBackupCount;
    }

    /// <summary>
    /// Checks the age-based rotation rule.
    /// </summary>
    /// <param name="backupDate">The UTC creation date of the backup being evaluated.</param>
    /// <returns><c>true</c> if the backup is older than <see cref="MaxAgeDays"/> days.</returns>
    private bool ShouldRotateByAge(DateTime backupDate)
    {
        return (DateTime.UtcNow - backupDate).TotalDays > MaxAgeDays;
    }

    /// <summary>
    /// Returns a short diagnostic description of the policy (identifiers and key settings, no timestamps).
    /// </summary>
    /// <returns>A string representation of the policy.</returns>
    public override string ToString() => $"RotationPolicy {{ Id = {Id}, ScheduleId = {ScheduleId}, Strategy = {Strategy}, MaxBackupCount = {MaxBackupCount}, MaxAgeDays = {MaxAgeDays}, VerifyBeforeDeletion = {VerifyBeforeDeletion} }}";
}
