// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Represents a configured backup job for a SQLite database.
/// </summary>
public class BackupJob
{
    /// <summary>
    /// Unique identifier for the backup job.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Display name for the backup job.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Full path to the SQLite database file to backup.
    /// </summary>
    public string DatabasePath { get; set; } = null!;

    /// <summary>
    /// Associated backup schedule configuration.
    /// </summary>
    public BackupSchedule Schedule { get; set; } = null!;

    /// <summary>
    /// Storage provider configuration for this job.
    /// </summary>
    public StorageProvider StorageProvider { get; set; } = null!;

    /// <summary>
    /// Maximum number of backup snapshots to retain. Older snapshots are automatically deleted.
    /// </summary>
    public int MaxRetentionCount { get; set; } = 30;

    /// <summary>
    /// Maximum age in days for backup snapshots. Snapshots older than this are automatically deleted.
    /// </summary>
    public int MaxRetentionDays { get; set; } = 90;

    /// <summary>
    /// Whether verification is enabled after each backup.
    /// </summary>
    public bool EnableVerification { get; set; } = true;

    /// <summary>
    /// Whether compression should be applied to backup files.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public BackupStatus Status { get; set; } = BackupStatus.Pending;

    /// <summary>
    /// Date and time when the job was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of the last successful backup execution.
    /// </summary>
    public DateTime? LastBackupAt { get; set; }

    /// <summary>
    /// Additional metadata and tags for the backup job.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// Validates the backup job configuration for required fields and constraints.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Backup job ID is required.");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Backup job name is required.");

        if (string.IsNullOrWhiteSpace(DatabasePath))
            errors.Add("Database path is required.");

        if (!System.IO.File.Exists(DatabasePath))
            errors.Add($"Database file not found at path: {DatabasePath}");

        if (Schedule == null)
            errors.Add("Backup schedule is required.");
        else
        {
            var scheduleErrors = Schedule.Validate();
            errors.AddRange(scheduleErrors);
        }

        if (StorageProvider == null)
            errors.Add("Storage provider configuration is required.");
        else
        {
            var storageErrors = StorageProvider.Validate();
            errors.AddRange(storageErrors);
        }

        if (MaxRetentionCount < 1)
            errors.Add("Max retention count must be at least 1.");

        if (MaxRetentionDays < 1)
            errors.Add("Max retention days must be at least 1.");

        return errors;
    }
}
