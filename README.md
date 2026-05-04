// entire file content ...
// ... goes in between

## RotationPolicy

The `RotationPolicy` class defines how backup files are rotated and deleted. It allows you to set limits on the number of backups, their age, and whether failed backups should be removed. The policy can be validated and used to decide if a particular backup should be rotated.

```csharp
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Constants;
using System;

var policy = new RotationPolicy
{
    ScheduleId = Guid.NewGuid(),
    Strategy = (int)RotationStrategy.Combined,
    MaxBackupCount = 10,
    MaxAgeDays = 30,
    VerifyBeforeDeletion = true,
    MinimumBackupCount = 3,
    DeleteFailedBackups = true,
    CreatedAt = DateTime.UtcNow,
    LastModifiedAt = DateTime.UtcNow
};

if (!policy.IsValid())
{
    throw new InvalidOperationException("Rotation policy is not valid.");
}

// Example: decide whether to rotate a backup that was created 40 days ago
var backupDate = DateTime.UtcNow.AddDays(-40);
bool shouldRotate = policy.ShouldRotate(
    totalBackupCount: 12,
    backupDate: backupDate,
    isFailed: false);

Console.WriteLine($"Should rotate: {shouldRotate}");
```

## S3Configuration

The `S3Configuration` class provides configuration for uploading backup files to S3-compatible storage. It includes properties for AWS credentials, bucket settings, region configuration, encryption options, and lifecycle management parameters like glacier transition settings.

```csharp
using DockerSqliteBackup.Domain;
using System;
using System.Threading.Tasks;

var config = new S3Configuration
{
    AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
    SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    BucketName = "my-backup-bucket",
    RegionName = "us-east-1",
    ObjectKeyPrefix = "sqlite-backups/",
    UseSSL = true,
    EnableServerSideEncryption = true,
    StorageClass = "STANDARD_IA",
    CustomEndpoint = null,
    TransitionToGlacierDays = 30,
};

if (!config.IsValid)
{
    throw new InvalidOperationException("S3 configuration is not valid.");
}

// Example: test the connection to S3 storage
bool isConnected = await config.TestConnectionAsync();

Console.WriteLine($"S3 connection successful: {isConnected}");
```

## BackupResult

The `BackupResult` class represents the outcome of a backup operation, containing metadata about the backup process, file characteristics, verification status, and storage locations. It tracks the entire backup lifecycle from initiation to completion, including timing metrics, file integrity, and storage destinations.

```csharp
using DockerSqliteBackup.Domain;
using System;

var backupResult = new BackupResult
{
    Id = Guid.NewGuid(),
    ScheduleId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    BackupJobId = Guid.Parse("123e4567-e89b-12d3-a456-426614174001"),
    Status = (int)BackupStatus.Completed,
    BackupFilePath = "/backups/app_2026-07-15_14-30-00.db.backup",
    BackupFileSizeBytes = 1073741824, // 1 GB
    Checksum = "a1b2c3d4e5f67890abcdef1234567890abcdef12",
    StartedAt = DateTime.UtcNow.AddMinutes(-10),
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 600000, // 10 minutes
    ErrorMessage = null,
    StackTrace = null,
    IsVerified = true,
    VerifiedAt = DateTime.UtcNow.AddMinutes(-2),
    Notes = "Weekly full backup completed successfully",
    IsStoredInS3 = true,
    IsStoredLocally = true,
    S3ObjectKey = "sqlite-backups/app_2026-07-15_14-30-00.db.backup",
    BackupMode = (int)BackupMode.Full,
    BaseBackupResultId = null
};

// Example: Verify backup integrity and log results
Console.WriteLine($"Backup completed: {backupResult.BackupFilePath}");
Console.WriteLine($"Size: {backupResult.BackupFileSizeBytes / 1024 / 1024} MB");
Console.WriteLine($"Duration: {TimeSpan.FromMilliseconds(backupResult.DurationMilliseconds).TotalMinutes} minutes");
Console.WriteLine($"Checksum: {backupResult.Checksum}");
Console.WriteLine($"Status: {(BackupStatus)backupResult.Status}");
Console.WriteLine($"Verified: {backupResult.IsVerified} at {backupResult.VerifiedAt}");
```

## RestoreVerification

The `RestoreVerification` class represents the result of a database restore verification process. It tracks the verification status, timing metrics, record counts, database size, and integrity check results to ensure that restored databases are consistent and functional. The class provides methods to mark completion and calculate elapsed duration.

```csharp
using DockerSqliteBackup.Domain;
using System;

var verification = new RestoreVerification
{
    Id = Guid.NewGuid(),
    BackupResultId = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
    IsSuccessful = true,
    StatusMessage = "Database restored and verified successfully",
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 300000,
    RecordCount = 15000,
    DatabaseSizeBytes = 256000000,
    IntegrityCheckPassed = true,
    IntegrityCheckErrors = null,
    TemporaryDirectory = "/tmp/restore-verification-12345",
    ErrorMessage = null
};

// Example: mark completion and get elapsed duration
verification.MarkCompleted();
TimeSpan elapsed = verification.GetElapsedDuration();

Console.WriteLine($"Restore verification completed in {elapsed.TotalSeconds} seconds");
Console.WriteLine($"Database has {verification.RecordCount} records, size: {verification.DatabaseSizeBytes / 1024 / 1024} MB");
Console.WriteLine($"Integrity check: {(verification.IntegrityCheckPassed ? "PASSED" : "FAILED")}");
```

## IntegrityReport

The `IntegrityReport` class provides a comprehensive analysis of SQLite database integrity by performing multiple validation checks including quick integrity checks, full integrity scans, and foreign key validation. It captures detailed metadata about the database structure and provides a health assessment based on all validation results.

```csharp
using DockerSqliteBackup.Domain;
using System;

var report = new IntegrityReport
{
    Id = Guid.NewGuid(),
    DatabasePath = "/data/app.db",
    CheckedAt = DateTime.UtcNow,
    Duration = TimeSpan.FromSeconds(45),
    PassedQuickCheck = true,
    QuickCheckErrors = null,
    PassedFullCheck = true,
    FullCheckErrors = null,
    PassedForeignKeyCheck = true,
    ForeignKeyErrors = null,
    PageCount = 12500,
    PageSize = 4096,
    FreePageCount = 250,
    JournalMode = "WAL",
    HasUncheckpointedWal = false,
    TableCount = 15
};

// Example: check overall health status
if (report.IsHealthy)
{
    Console.WriteLine($"Database is healthy: {report.Summary}");
    Console.WriteLine($"Database has {report.PageCount} pages ({report.FreePageCount} free), {report.TableCount} tables");
    Console.WriteLine($"Journal mode: {report.JournalMode}, Page size: {report.PageSize} bytes");
}
else
{
    Console.WriteLine($"Database integrity issues detected!");
    Console.WriteLine(report.Summary);
    if (!report.PassedQuickCheck && report.QuickCheckErrors != null)
    {
        Console.WriteLine($"Quick check errors: {report.QuickCheckErrors}");
    }
}
```
