// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Constants;

/// <summary>
/// Constant values and configuration defaults used throughout the application.
/// </summary>
public static class BackupConstants
{
    // Default Configuration Values
    public const int DefaultMaxRetentionCount = 30;
    public const int DefaultMaxRetentionDays = 90;
    public const int DefaultCompressionLevel = 6;
    public const int DefaultConnectionTimeoutSeconds = 30;
    public const int DefaultMaxRetries = 3;

    // Backup Naming
    public const string BackupFileExtension = ".db";
    public const string CompressedBackupExtension = ".gz";
    public const string BackupMetadataExtension = ".meta.json";
    public const string BackupTempPrefix = "backup-";
    public const string RestoreTempPrefix = "restore-";

    // CRON Expressions
    public const string CronEveryHour = "0 * * * *";
    public const string CronDailyAtMidnight = "0 0 * * *";
    public const string CronDailyAt2Am = "0 2 * * *";
    public const string CronWeeklyOnSunday = "0 0 ? * SUN";
    public const string CronMonthlyFirstDay = "0 0 1 * *";

    // Timeout Values (in seconds)
    public const int BackupTimeoutDefault = 3600; // 1 hour
    public const int VerificationTimeoutDefault = 600; // 10 minutes
    public const int RestoreTimeoutDefault = 3600; // 1 hour
    public const int StorageOperationTimeoutDefault = 300; // 5 minutes

    // File Size Limits (in bytes)
    public const long MaxLocalBackupSizeBytes = 10L * 1024 * 1024 * 1024; // 10 GB
    public const long MaxS3BackupSizeBytes = 100L * 1024 * 1024 * 1024; // 100 GB
    public const long CompressionThresholdDefault = 1024 * 1024; // 1 MB

    // Database
    public const int PageSizeDefault = 4096;
    public const int MaxPagesDefault = 1000000;

    // Error Messages
    public const string ErrorDatabaseFileNotFound = "Database file not found at specified path";
    public const string ErrorInvalidBackupJob = "Backup job configuration is invalid";
    public const string ErrorStorageConnectionFailed = "Failed to connect to storage provider";
    public const string ErrorVerificationFailed = "Backup verification failed";
    public const string ErrorBackupFileCorrupted = "Backup file appears to be corrupted";
    public const string ErrorRestoreFailed = "Database restoration failed";

    // Success Messages
    public const string SuccessBackupCompleted = "Backup completed successfully";
    public const string SuccessVerificationPassed = "Backup verification passed";
    public const string SuccessRestoreCompleted = "Database restore completed successfully";
    public const string SuccessCleanupCompleted = "Backup cleanup completed successfully";

    // Logging
    public const string LogBackupStarted = "Backup operation started";
    public const string LogBackupCompleted = "Backup operation completed";
    public const string LogBackupFailed = "Backup operation failed";
    public const string LogVerificationStarted = "Backup verification started";
    public const string LogVerificationCompleted = "Backup verification completed";
    public const string LogRestoreStarted = "Database restore started";
    public const string LogRestoreCompleted = "Database restore completed";

    // Regex Patterns
    public const string UuidPattern = @"^[a-fA-F0-9]{8}-?[a-fA-F0-9]{4}-?[a-fA-F0-9]{4}-?[a-fA-F0-9]{4}-?[a-fA-F0-9]{12}$";
    public const string FileHashPattern = @"^[a-fA-F0-9]{64}$"; // SHA256
    public const string CronExpressionPattern = @"^(\*|([0-9]|1[0-9]|2[0-9]|3[0-9]|4[0-9]|5[0-9])|\*/[0-9]+)(\s(\*|([0-9]|1[0-9]|2[0-3])|\*/[0-9]+)){4}$";

    // S3 Configuration
    public const string S3StorageClassStandard = "STANDARD";
    public const string S3StorageClassIA = "STANDARD_IA";
    public const string S3StorageClassGlacier = "GLACIER";
    public const string S3StorageClassDeepArchive = "DEEP_ARCHIVE";

    // HTTP Status Codes for S3 Operations
    public const int HttpStatusOk = 200;
    public const int HttpStatusCreated = 201;
    public const int HttpStatusNotFound = 404;
    public const int HttpStatusConflict = 409;
    public const int HttpStatusUnavailable = 503;

    // Application
    public const string ApplicationVersion = "1.0.0";

    // User Agent String
    public const string UserAgent = "docker-sqlite-backup/1.0.0 (.NET 10.0)";
}
