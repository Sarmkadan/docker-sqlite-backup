// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Configuration settings for the backup service.
/// </summary>
public class BackupServiceConfiguration
{
    /// <summary>
    /// Path to the SQLite database that stores backup metadata.
    /// </summary>
    public string MetadataDatabasePath { get; set; } = "backup-metadata.db";

    /// <summary>
    /// Maximum number of concurrent backup operations.
    /// </summary>
    public int MaxConcurrentBackups { get; set; } = 3;

    /// <summary>
    /// Base directory for local backup storage.
    /// </summary>
    public string LocalBackupBasePath { get; set; } = "./backups";

    /// <summary>
    /// Whether to enable automatic backup cleanup based on retention policies.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// How often (in hours) to run the automatic cleanup task.
    /// </summary>
    public int AutoCleanupIntervalHours { get; set; } = 24;

    /// <summary>
    /// Maximum size of backup files in MB before compression is forced.
    /// </summary>
    public long CompressionThresholdMb { get; set; } = 1024;

    /// <summary>
    /// Whether to enable backup verification after completion.
    /// </summary>
    public bool EnableDefaultVerification { get; set; } = true;

    /// <summary>
    /// Default compression level (0-9, 6 is recommended).
    /// </summary>
    public int CompressionLevel { get; set; } = 6;

    /// <summary>
    /// Timeout in seconds for individual backup operations.
    /// </summary>
    public int BackupTimeoutSeconds { get; set; } = 3600;

    /// <summary>
    /// AWS S3 configuration for cloud backups.
    /// </summary>
    public S3Configuration? S3Config { get; set; }

    /// <summary>
    /// Email notification settings.
    /// </summary>
    public NotificationConfiguration? Notifications { get; set; }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(MetadataDatabasePath))
            errors.Add("MetadataDatabasePath is required");

        if (MaxConcurrentBackups < 1)
            errors.Add("MaxConcurrentBackups must be at least 1");

        if (string.IsNullOrWhiteSpace(LocalBackupBasePath))
            errors.Add("LocalBackupBasePath is required");

        if (AutoCleanupIntervalHours < 1)
            errors.Add("AutoCleanupIntervalHours must be at least 1");

        if (CompressionThresholdMb < 1)
            errors.Add("CompressionThresholdMb must be at least 1");

        if (CompressionLevel < 0 || CompressionLevel > 9)
            errors.Add("CompressionLevel must be between 0 and 9");

        if (BackupTimeoutSeconds < 60)
            errors.Add("BackupTimeoutSeconds must be at least 60");

        return errors;
    }
}

/// <summary>
/// AWS S3 configuration for cloud backup storage.
/// </summary>
public class S3Configuration
{
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? Prefix { get; set; }
    public bool UseEncryption { get; set; } = true;
    public string? StorageClass { get; set; } = "STANDARD";
}

/// <summary>
/// Email notification configuration.
/// </summary>
public class NotificationConfiguration
{
    public bool EnableEmailNotifications { get; set; } = false;
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? FromAddress { get; set; }
    public List<string> ToAddresses { get; set; } = new();
    public bool NotifyOnSuccess { get; set; } = false;
    public bool NotifyOnFailure { get; set; } = true;
}
