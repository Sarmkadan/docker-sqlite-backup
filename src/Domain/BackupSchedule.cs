#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Represents a scheduled backup configuration for a SQLite database.
/// </summary>
public class BackupSchedule
{
    /// <summary>Gets or sets the unique identifier for this schedule.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the human-readable name of the schedule.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description of the schedule.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the path to the SQLite database file.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the cron expression for scheduling backups.</summary>
    public string CronExpression { get; set; } = "0 2 * * *"; // Default: 2 AM daily

    /// <summary>Gets or sets whether this schedule is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets whether this schedule is enabled (alias for IsActive).</summary>
    public bool IsEnabled { get => IsActive; set => IsActive = value; }

    /// <summary>Gets or sets the next calculated run time for this schedule.</summary>
    public DateTime? NextRunTime { get; set; }

    /// <summary>Gets or sets the timestamp when this schedule was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the timestamp when this schedule was last modified.</summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the timestamp of the last backup execution for this schedule.</summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>Gets or sets the retention days for backups created by this schedule.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>Gets or sets the maximum number of backup files to keep.</summary>
    public int MaxBackupCount { get; set; } = 10;

    /// <summary>Gets or sets the notification email addresses for backup completion.</summary>
    public string NotificationEmails { get; set; } = string.Empty;

    /// <summary>Gets or sets whether verification should be performed after backup.</summary>
    public bool VerifyAfterBackup { get; set; } = true;

    /// <summary>Gets or sets the storage type for this schedule's backups.</summary>
    public int StorageType { get; set; } = 0; // StorageType.Local

    /// <summary>
    /// Gets or sets the backup mode for this schedule.
    /// <see cref="Constants.BackupMode.Full"/> (default) captures a complete database snapshot each run.
    /// <see cref="Constants.BackupMode.Incremental"/> captures only WAL pages changed since the last
    /// successful backup, referencing the previous snapshot as the base. Falls back to a full backup
    /// when no prior successful backup exists.
    /// </summary>
    public int BackupMode { get; set; } = (int)Constants.BackupMode.Full;

    /// <summary>
    /// Gets or sets an optional storage configuration for non-local backends (e.g. S3).
    /// When set, <see cref="BackupService"/> will upload the completed backup to this
    /// backend after the local snapshot is created. Any upload failure will be surfaced
    /// as an exception, preventing a silent success.
    /// </summary>
    public StorageConfiguration? StorageConfiguration { get; set; }

    /// <summary>
    /// Validates the schedule configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (string.IsNullOrWhiteSpace(DatabasePath))
            return false;

        if (string.IsNullOrWhiteSpace(CronExpression))
            return false;

        if (RetentionDays < 1)
            return false;

        if (MaxBackupCount < 1)
            return false;

        return true;
    }

    /// <summary>
    /// Validates that the specified database path exists and is accessible.
    /// </summary>
    public bool ValidateDatabasePath()
    {
        return File.Exists(DatabasePath);
    }
}
