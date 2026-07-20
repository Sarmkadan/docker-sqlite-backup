#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Application settings configuration model.
/// </summary>
public class AppSettings
{
    private string _databasePath = "backups.sqlite";
    private int _maxConcurrentBackups = 3;
    private int _backupTimeoutSeconds = 3600;
    private int _scheduleCheckIntervalSeconds = 60;
    private string _logLevel = "Information";
    private int _retentionDays = 30;
    private int _maxBackupCount = 10;
    private string _localStoragePath = "backups";

    /// <summary>
    /// Gets or sets the database path. Validates that it's not null or whitespace.
    /// </summary>
    public string DatabasePath
    {
        get => _databasePath;
        set => _databasePath = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ConfigurationException(nameof(DatabasePath), "Database path cannot be null or whitespace.");
    }

    /// <summary>
    /// Gets or sets the maximum number of concurrent backups. Must be positive.
    /// </summary>
    public int MaxConcurrentBackups
    {
        get => _maxConcurrentBackups;
        set => _maxConcurrentBackups = value > 0
            ? value
            : throw new ConfigurationException(nameof(MaxConcurrentBackups), "Max concurrent backups must be a positive number.");
    }

    /// <summary>
    /// Gets or sets the backup timeout in seconds. Must be positive.
    /// </summary>
    public int BackupTimeoutSeconds
    {
        get => _backupTimeoutSeconds;
        set => _backupTimeoutSeconds = value > 0
            ? value
            : throw new ConfigurationException(nameof(BackupTimeoutSeconds), "Backup timeout must be a positive number.");
    }

    /// <summary>
    /// Gets or sets the schedule check interval in seconds. Must be positive.
    /// </summary>
    public int ScheduleCheckIntervalSeconds
    {
        get => _scheduleCheckIntervalSeconds;
        set => _scheduleCheckIntervalSeconds = value > 0
            ? value
            : throw new ConfigurationException(nameof(ScheduleCheckIntervalSeconds), "Schedule check interval must be a positive number.");
    }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string LogLevel
    {
        get => _logLevel;
        set => _logLevel = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ConfigurationException(nameof(LogLevel), "Log level cannot be null or whitespace.");
    }

    /// <summary>
    /// Gets or sets the retention days. Must be non-negative.
    /// </summary>
    public int RetentionDays
    {
        get => _retentionDays;
        set => _retentionDays = value >= 0
            ? value
            : throw new ConfigurationException(nameof(RetentionDays), "Retention days cannot be negative.");
    }

    /// <summary>
    /// Gets or sets the maximum backup count. Must be positive.
    /// </summary>
    public int MaxBackupCount
    {
        get => _maxBackupCount;
        set => _maxBackupCount = value > 0
            ? value
            : throw new ConfigurationException(nameof(MaxBackupCount), "Max backup count must be a positive number.");
    }

    /// <summary>
    /// Gets or sets the local storage path. Validates that it's not null or whitespace.
    /// </summary>
    public string LocalStoragePath
    {
        get => _localStoragePath;
        set => _localStoragePath = !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ConfigurationException(nameof(LocalStoragePath), "Local storage path cannot be null or whitespace.");
    }

    public bool EnableVerificationByDefault { get; set; } = true;
    public bool EnableS3StorageByDefault { get; set; } = false;
    public bool CompressBackups { get; set; } = false;

    /// <summary>
    /// Gets or sets the compression level for gzip compression (1-9, where 1 is fastest and 9 is best compression).
    /// Default is 6 (balanced compression).
    /// </summary>
    public int CompressionLevel { get; set; } = BackupConstants.DefaultCompressionLevel;
    public string[] NotificationEmails { get; set; } = [];

    /// <summary>
    /// Gets or sets the webhook secret used to sign webhook payloads with HMAC-SHA256.
    /// This secret should be a secure random string (at least 32 characters recommended).
    /// Set via the <c>BACKUP_WEBHOOK_SECRET</c> environment variable or in appsettings.json.
    /// </summary>
    public string? WebhookSecret { get; set; }

// Encryption settings
/// <summary>
/// When true, backup archives are encrypted with AES-256-CBC before being written to storage.
/// Set <see cref="EncryptionKey"/> or the <c>BACKUP_ENCRYPTION_KEY</c> environment variable
/// to supply the Base64-encoded 32-byte key.
/// </summary>
public bool EnableEncryption { get; set; } = false;

/// <summary>
/// Base64-encoded 32-byte AES-256 key used to encrypt and decrypt backup files.
/// Prefer injecting this via the <c>AppSettings__EncryptionKey</c> environment variable
/// rather than storing it in appsettings.json.
/// </summary>
public string? EncryptionKey { get; set; }
}