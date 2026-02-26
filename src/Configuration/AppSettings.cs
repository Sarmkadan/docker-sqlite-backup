#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Application settings configuration model.
/// </summary>
public class AppSettings
{
    public string DatabasePath { get; set; } = "backups.sqlite";
    public int MaxConcurrentBackups { get; set; } = 3;
    public int BackupTimeoutSeconds { get; set; } = 3600; // 1 hour
    public int ScheduleCheckIntervalSeconds { get; set; } = 60;
    public bool EnableVerificationByDefault { get; set; } = true;
    public bool EnableS3StorageByDefault { get; set; } = false;
    public string LogLevel { get; set; } = "Information";
    public int RetentionDays { get; set; } = 30;
    public int MaxBackupCount { get; set; } = 10;
    public string LocalStoragePath { get; set; } = "backups";
    public bool CompressBackups { get; set; } = false;
    public string[] NotificationEmails { get; set; } = [];

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
