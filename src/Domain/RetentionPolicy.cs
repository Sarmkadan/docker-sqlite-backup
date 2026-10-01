#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Defines a time-based and size-based retention policy for backup files.
/// Unlike <see cref="RotationPolicy"/> which focuses on per-schedule rotation
/// strategies, <see cref="RetentionPolicy"/> expresses global or per-tier
/// retention constraints (e.g. keep daily backups for 7 days, weekly for 30,
/// monthly for 365) and total storage caps.
/// </summary>
public class RetentionPolicy
{
    /// <summary>Gets or sets the unique identifier for this policy.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets a human-readable name for this policy (e.g. "Production 30-day").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of days to retain daily backups.</summary>
    public int DailyRetentionDays { get; set; } = 7;

    /// <summary>Gets or sets the number of weeks to retain weekly backups (kept on Sunday).</summary>
    public int WeeklyRetentionWeeks { get; set; } = 4;

    /// <summary>Gets or sets the number of months to retain monthly backups (kept on the 1st).</summary>
    public int MonthlyRetentionMonths { get; set; } = 12;

    /// <summary>
    /// Gets or sets the maximum total storage in bytes for backups governed by this policy.
    /// Zero means unlimited.
    /// </summary>
    public long MaxTotalStorageBytes { get; set; }

    /// <summary>Gets or sets whether to delete unverified backups when storage pressure is high.</summary>
    public bool PrioritizeVerifiedBackups { get; set; } = true;

    /// <summary>Gets or sets when the policy was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the policy was last modified.</summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the retention policy configuration.
    /// </summary>
    /// <returns>True if the policy is valid.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (DailyRetentionDays < 1)
            return false;

        if (WeeklyRetentionWeeks < 0)
            return false;

        if (MonthlyRetentionMonths < 0)
            return false;

        if (MaxTotalStorageBytes < 0)
            return false;

        return true;
    }

    /// <summary>
    /// Determines the retention tier a backup falls into based on its age.
    /// </summary>
    /// <param name="backupDate">The creation date of the backup.</param>
    /// <returns>The <see cref="RetentionTier"/> the backup belongs to.</returns>
    public RetentionTier ClassifyBackup(DateTime backupDate)
    {
        var age = DateTime.UtcNow - backupDate;

        if (age.TotalDays <= DailyRetentionDays)
            return RetentionTier.Daily;

        if (WeeklyRetentionWeeks > 0 && age.TotalDays <= WeeklyRetentionWeeks * 7)
            return RetentionTier.Weekly;

        if (MonthlyRetentionMonths > 0 && age.TotalDays <= MonthlyRetentionMonths * 30)
            return RetentionTier.Monthly;

        return RetentionTier.Expired;
    }

    /// <summary>
    /// Checks whether a backup should be kept in its weekly tier.
    /// Only the backup closest to the start of each ISO week is retained.
    /// </summary>
    /// <param name="backupDate">The creation date of the backup.</param>
    /// <returns>True if the backup is the representative for its week.</returns>
    public static bool IsWeeklyRepresentative(DateTime backupDate)
    {
        return backupDate.DayOfWeek == DayOfWeek.Sunday;
    }

    /// <summary>
    /// Checks whether a backup should be kept in its monthly tier.
    /// Only the backup created on the first day of the month is retained.
    /// </summary>
    /// <param name="backupDate">The creation date of the backup.</param>
    /// <returns>True if the backup is the representative for its month.</returns>
    public static bool IsMonthlyRepresentative(DateTime backupDate)
    {
        return backupDate.Day == 1;
    }

    /// <summary>
    /// Determines whether a backup has exceeded all retention tiers and should be deleted.
    /// </summary>
    /// <param name="backupDate">The creation date of the backup.</param>
    /// <returns>True if the backup is expired under this policy.</returns>
    public bool IsExpired(DateTime backupDate)
    {
        return ClassifyBackup(backupDate) == RetentionTier.Expired;
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"RetentionPolicy {{ Id = {Id}, Name = {Name}, Daily = {DailyRetentionDays}d, Weekly = {WeeklyRetentionWeeks}w, Monthly = {MonthlyRetentionMonths}m }}";
}

/// <summary>
/// Represents the retention tier a backup is classified into.
/// </summary>
public enum RetentionTier
{
    /// <summary>Backup is within the daily retention window.</summary>
    Daily,

    /// <summary>Backup is within the weekly retention window.</summary>
    Weekly,

    /// <summary>Backup is within the monthly retention window.</summary>
    Monthly,

    /// <summary>Backup has exceeded all retention windows and is eligible for deletion.</summary>
    Expired
}
