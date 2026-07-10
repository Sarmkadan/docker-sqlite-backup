# BackupResult
The `BackupResult` type represents the outcome of a backup operation in the `docker-sqlite-backup` project. It encapsulates various details about the backup, including its identifier, schedule and job information, status, file path and size, checksum, start and completion times, duration, error details, verification status, and storage information. This type provides a comprehensive overview of a backup's lifecycle, facilitating tracking, monitoring, and management of backup operations.

## API
The `BackupResult` type exposes the following public members:
- `Id`: A unique identifier for the backup result, represented as a `Guid`.
- `ScheduleId`: The identifier of the schedule that triggered the backup, represented as a `Guid`.
- `BackupJobId`: The identifier of the backup job that executed the backup, represented as a `Guid`.
- `Status`: An integer indicating the status of the backup operation.
- `BackupFilePath`: The file path where the backup is stored, represented as a `string`.
- `BackupFileSizeBytes`: The size of the backup file in bytes, represented as a `long`.
- `Checksum`: The checksum of the backup file, represented as a `string`.
- `StartedAt`: The timestamp when the backup operation started, represented as a `DateTime`.
- `CompletedAt`: The timestamp when the backup operation completed, represented as a `DateTime?` (nullable).
- `DurationMilliseconds`: The duration of the backup operation in milliseconds, represented as a `long`.
- `ErrorMessage`: An error message if the backup operation failed, represented as a `string?` (nullable).
- `StackTrace`: The stack trace of the error if the backup operation failed, represented as a `string?` (nullable).
- `IsVerified`: A boolean indicating whether the backup has been verified.
- `VerifiedAt`: The timestamp when the backup was verified, represented as a `DateTime?` (nullable).
- `Notes`: Additional notes about the backup, represented as a `string`.
- `IsStoredInS3`: A boolean indicating whether the backup is stored in S3.
- `IsStoredLocally`: A boolean indicating whether the backup is stored locally.
- `S3ObjectKey`: The S3 object key if the backup is stored in S3, represented as a `string?` (nullable).
- `BackupMode`: An integer indicating the mode of the backup operation.
- `BaseBackupResultId`: The identifier of the base backup result if this is an incremental backup, represented as a `Guid?` (nullable).

## Usage
The following examples demonstrate how to use the `BackupResult` type in C#:
```csharp
// Example 1: Creating a new BackupResult instance
var backupResult = new BackupResult
{
    Id = Guid.NewGuid(),
    ScheduleId = Guid.NewGuid(),
    BackupJobId = Guid.NewGuid(),
    Status = 1,
    BackupFilePath = "/path/to/backup/file",
    BackupFileSizeBytes = 1024,
    Checksum = "checksum-value",
    StartedAt = DateTime.Now,
    CompletedAt = DateTime.Now.AddMinutes(1),
    DurationMilliseconds = 60000,
    IsVerified = true,
    VerifiedAt = DateTime.Now,
    Notes = "Additional notes about the backup",
    IsStoredInS3 = true,
    IsStoredLocally = false,
    S3ObjectKey = "s3-object-key",
    BackupMode = 1,
    BaseBackupResultId = Guid.NewGuid()
};

// Example 2: Updating an existing BackupResult instance
var existingBackupResult = new BackupResult
{
    Id = Guid.NewGuid(),
    ScheduleId = Guid.NewGuid(),
    BackupJobId = Guid.NewGuid(),
    Status = 1,
    BackupFilePath = "/path/to/backup/file",
    BackupFileSizeBytes = 1024,
    Checksum = "checksum-value",
    StartedAt = DateTime.Now,
    CompletedAt = null,
    DurationMilliseconds = 0,
    IsVerified = false,
    VerifiedAt = null,
    Notes = "Additional notes about the backup",
    IsStoredInS3 = true,
    IsStoredLocally = false,
    S3ObjectKey = "s3-object-key",
    BackupMode = 1,
    BaseBackupResultId = Guid.NewGuid()
};

existingBackupResult.CompletedAt = DateTime.Now;
existingBackupResult.DurationMilliseconds = 60000;
existingBackupResult.IsVerified = true;
existingBackupResult.VerifiedAt = DateTime.Now;
```

## Notes
When working with the `BackupResult` type, consider the following edge cases and thread-safety remarks:
- The `CompletedAt` and `VerifiedAt` properties are nullable, indicating that the backup operation may not have completed or been verified yet.
- The `ErrorMessage` and `StackTrace` properties are nullable, indicating that an error may not have occurred during the backup operation.
- The `IsStoredInS3` and `IsStoredLocally` properties are booleans, indicating whether the backup is stored in S3 or locally.
- The `BackupMode` property is an integer, indicating the mode of the backup operation.
- The `BaseBackupResultId` property is nullable, indicating that this backup result may not be based on another backup result.
- When updating an existing `BackupResult` instance, ensure that the `Id` property remains unchanged to maintain data consistency.
- When accessing or modifying `BackupResult` instances from multiple threads, consider implementing synchronization mechanisms to prevent data corruption or inconsistencies.
