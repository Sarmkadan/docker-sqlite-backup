# BackupSchedule

Represents a scheduled backup configuration for SQLite databases, including scheduling, retention, storage, and notification settings. Used by the `docker-sqlite-backup` project to define and manage automated database backup jobs.

## API

### `Id`
- **Type**: `Guid`
- **Purpose**: Unique identifier for the backup schedule.
- **Mutability**: Read-only (set at creation).

### `Name`
- **Type**: `string`
- **Purpose**: Human-readable name for the backup schedule (e.g., "Daily Production Backup").
- **Mutability**: Read/write.
- **Constraints**: Must not be null or empty.

### `Description`
- **Type**: `string`
- **Purpose**: Optional descriptive text for the backup schedule.
- **Mutability**: Read/write.

### `DatabasePath`
- **Type**: `string`
- **Purpose**: Absolute or relative path to the SQLite database file to back up.
- **Mutability**: Read/write.
- **Constraints**: Must point to a valid SQLite file when `ValidateDatabasePath` is `true`.

### `CronExpression`
- **Type**: `string`
- **Purpose**: Cron expression defining the backup schedule (e.g., `"0 2 * * *"` for daily at 2 AM).
- **Mutability**: Read/write.
- **Constraints**: Must be a valid cron expression; otherwise, `IsValid` returns `false`.

### `IsActive`
- **Type**: `bool`
- **Purpose**: Indicates whether the schedule is active and should be executed.
- **Mutability**: Read/write.

### `IsEnabled`
- **Type**: `bool`
- **Purpose**: Indicates whether the schedule is enabled (e.g., temporarily disabled without deleting configuration).
- **Mutability**: Read/write.

### `NextRunTime`
- **Type**: `DateTime?`
- **Purpose**: Next scheduled run time based on `CronExpression` and current time. `null` if not active or invalid.
- **Mutability**: Read-only (computed).

### `CreatedAt`
- **Type**: `DateTime`
- **Purpose**: Timestamp when the schedule was created.
- **Mutability**: Read-only (set at creation).

### `LastModifiedAt`
- **Type**: `DateTime`
- **Purpose**: Timestamp when the schedule was last modified.
- **Mutability**: Read-only (updated on property changes).

### `LastBackupAt`
- **Type**: `DateTime?`
- **Purpose**: Timestamp of the last successful backup. `null` if no backups have been performed.
- **Mutability**: Read-only (updated after successful backups).

### `RetentionDays`
- **Type**: `int`
- **Purpose**: Number of days to retain backups before deletion. `0` means no retention (keep all).
- **Mutability**: Read/write.
- **Constraints**: Must be non-negative.

### `MaxBackupCount`
- **Type**: `int`
- **Purpose**: Maximum number of backups to retain, regardless of age. `0` means no limit.
- **Mutability**: Read/write.
- **Constraints**: Must be non-negative.

### `NotificationEmails`
- **Type**: `string`
- **Purpose**: Comma-separated list of email addresses for backup notifications (success/failure).
- **Mutability**: Read/write.

### `VerifyAfterBackup`
- **Type**: `bool`
- **Purpose**: Indicates whether to verify the backup integrity after completion (e.g., checksum validation).
- **Mutability**: Read/write.

### `StorageType`
- **Type**: `int`
- **Purpose**: Identifier for the storage backend (e.g., local filesystem, S3, Azure Blob). Interpretation depends on application context.
- **Mutability**: Read/write.

### `BackupMode`
- **Type**: `int`
- **Purpose**: Backup mode (e.g., full, incremental, differential). Interpretation depends on application context.
- **Mutability**: Read/write.

### `StorageConfiguration`
- **Type**: `StorageConfiguration?`
- **Purpose**: Configuration object for the storage backend (e.g., connection strings, credentials). `null` if not required.
- **Mutability**: Read/write.

### `IsValid`
- **Type**: `bool`
- **Purpose**: Indicates whether the schedule configuration is valid (e.g., `DatabasePath` exists, `CronExpression` is valid).
- **Mutability**: Read-only (computed).

### `ValidateDatabasePath`
- **Type**: `bool`
- **Purpose**: Indicates whether to validate the `DatabasePath` during schedule validation.
- **Mutability**: Read/write.

## Usage

### Example 1: Creating and Scheduling a Backup
