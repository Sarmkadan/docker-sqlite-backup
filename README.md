# README

// ... existing content ...

## BackupService

The `BackupService` class is responsible for executing and managing SQLite backup operations, including scheduling, snapshot creation, incremental support, encryption, and storage integration. It provides a robust and flexible way to manage backups.

Here's an example usage of the `BackupService` class:

```csharp
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Domain;

// Create a new instance of BackupService
var backupService = new BackupService(
    new BackupRepository(), 
    new S3StorageService(), 
    new AppSettings(), 
    new Logger<BackupService>()
);

// Execute a backup
var schedule = new BackupSchedule { Id = Guid.NewGuid() };
var result = await backupService.ExecuteBackupAsync(schedule);

// Calculate a backup checksum
var checksum = await backupService.CalculateBackupChecksumAsync(result.BackupFilePath);

// Get backup history
var history = await backupService.GetBackupHistoryAsync(schedule.Id);

// Delete a backup
await backupService.DeleteBackupAsync(result.Id);

// Get a backup result
var backupResult = await backupService.GetBackupResultAsync(result.Id);
```

## BackupRepository

The `BackupRepository` class provides data access and persistence operations for backup-related entities including schedules, backup results, rotation policies, restore verifications, and backup jobs. It serves as the data layer for the backup system, handling all database operations through Entity Framework Core.

Here's an example usage of the `BackupRepository` class:

```csharp
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

// Create a new instance of BackupRepository
var loggerFactory = LoggerFactory.Create(builder => { });
var logger = loggerFactory.CreateLogger<BackupRepository>();
var repository = new BackupRepository(logger);

// Initialize the repository
await repository.InitializeAsync();

// Health check
var isHealthy = await repository.HealthCheckAsync();
Console.WriteLine($"Repository health: {isHealthy}");

// Create a backup schedule
var schedule = new BackupSchedule
{
    Id = Guid.NewGuid(),
    Name = "Daily Database Backup",
    ScheduleType = BackupScheduleType.Daily,
    ScheduleTime = "02:00",
    DatabasePath = @"/data/app.db",
    IsActive = true,
    CreatedAt = DateTime.UtcNow
};
var createdSchedule = await repository.CreateScheduleAsync(schedule);
Console.WriteLine($"Created schedule: {createdSchedule.Name} (ID: {createdSchedule.Id})");

// Get a specific schedule
var retrievedSchedule = await repository.GetScheduleAsync(createdSchedule.Id);
if (retrievedSchedule != null)
{
    Console.WriteLine($"Retrieved schedule: {retrievedSchedule.Name}");
}

// Update a schedule
createdSchedule.IsActive = false;
var updatedSchedule = await repository.UpdateScheduleAsync(createdSchedule);
Console.WriteLine($"Updated schedule active status to: {updatedSchedule.IsActive}");

// Get all active schedules
var activeSchedules = await repository.GetActiveSchedulesAsync();
Console.WriteLine($"Active schedules count: {activeSchedules.Count()}");

// Create a backup result
var backupResult = new BackupResult
{
    Id = Guid.NewGuid(),
    ScheduleId = createdSchedule.Id,
    BackupFilePath = @"/backups/app-2024-01-15.sqlite",
    BackupSize = 1024 * 1024,
    Status = BackupStatus.Success,
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow,
    Checksum = "sha256-abc123..."
};
var createdResult = await repository.CreateBackupResultAsync(backupResult);
Console.WriteLine($"Created backup result with status: {createdResult.Status}");

// Get backup history for a schedule
var backupHistory = await repository.GetBackupHistoryAsync(createdSchedule.Id);
Console.WriteLine($"Backup history count: {backupHistory.Count()}");

// Create a rotation policy
var rotationPolicy = new RotationPolicy
{
    ScheduleId = createdSchedule.Id,
    Strategy = (int)RotationStrategy.KeepLast,
    MinimumBackupCount = 7,
    MaximumBackupCount = 30,
    DeleteFailedBackups = true,
    RetentionDays = 90
};
var savedPolicy = await repository.SaveRotationPolicyAsync(rotationPolicy);
Console.WriteLine($"Saved rotation policy with strategy: {(RotationStrategy)savedPolicy.Strategy}");

// Get rotation policy
var policy = await repository.GetRotationPolicyAsync(createdSchedule.Id);
if (policy != null)
{
    Console.WriteLine($"Rotation policy: Min={policy.MinimumBackupCount}, Max={policy.MaximumBackupCount}");
}

// Create a restore verification
var verification = new RestoreVerification
{
    Id = Guid.NewGuid(),
    ScheduleId = createdSchedule.Id,
    BackupResultId = createdResult.Id,
    VerifiedAt = DateTime.UtcNow,
    Success = true,
    Logs = "Restore completed successfully"
};
var savedVerification = await repository.SaveRestoreVerificationAsync(verification);
Console.WriteLine($"Restore verification saved: {savedVerification.Success}");

// Get verification history
var verifications = await repository.GetVerificationHistoryAsync(createdSchedule.Id);
Console.WriteLine($"Verification history count: {verifications.Count()}");

// Create a backup job
var job = new BackupJob
{
    Id = Guid.NewGuid(),
    ScheduleId = createdSchedule.Id,
    DatabasePath = @"/data/app.db",
    BackupPath = @"/backups/app-2024-01-15.sqlite",
    JobType = BackupJobType.Full,
    Status = JobStatus.Completed,
    StartedAt = DateTime.UtcNow.AddMinutes(-10),
    CompletedAt = DateTime.UtcNow
};
var createdJob = await repository.CreateBackupJobAsync(job);
Console.WriteLine($"Created backup job: {createdJob.JobType}");

// Delete entities when needed
await repository.DeleteScheduleAsync(createdSchedule.Id);
await repository.DeleteBackupResultAsync(createdResult.Id);
```

## RotationService

The `RotationService` class manages backup rotation and cleanup operations for SQLite backups. It handles the deletion of old backups according to rotation policies, tracks rotation history, and calculates disk space that would be freed by rotation operations. The service supports different rotation strategies and can optionally delete failed backups during rotation.

Here's an example usage of the `RotationService` class:

```csharp
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

// Create a new instance of RotationService
var loggerFactory = LoggerFactory.Create(builder => { });
var logger = loggerFactory.CreateLogger<RotationService>();
var rotationService = new RotationService(
    new BackupRepository(),
    logger
);

// Get rotation policy for a schedule
var scheduleId = Guid.Parse("12345678-1234-1234-1234-123456789012");
var policy = await rotationService.GetRotationPolicyAsync(scheduleId);
if (policy != null)
{
    Console.WriteLine($"Rotation policy: Min backups={policy.MinimumBackupCount}, Strategy={(RotationStrategy)policy.Strategy}");
}

// Save or update a rotation policy
var newPolicy = new RotationPolicy
{
    ScheduleId = scheduleId,
    Strategy = (int)RotationStrategy.KeepLast,
    MinimumBackupCount = 7,
    MaximumBackupCount = 30,
    DeleteFailedBackups = true,
    RetentionDays = 90
};
var savedPolicy = await rotationService.SaveRotationPolicyAsync(newPolicy);
Console.WriteLine($"Saved policy for schedule {savedPolicy.ScheduleId}");

// Execute rotation to delete old backups
var deletedCount = await rotationService.ExecuteRotationAsync(scheduleId);
Console.WriteLine($"Rotation completed. Deleted {deletedCount} backups");

// Get list of backups that would be deleted by rotation
var backupsToDelete = await rotationService.GetBackupsForRotationAsync(scheduleId);
Console.WriteLine($"Backups to be deleted: {backupsToDelete.Count()}");

// Calculate disk space that would be freed by rotation
var spaceFreed = await rotationService.CalculateDiskSpaceFreedAsync(scheduleId);
Console.WriteLine($"Disk space that would be freed: {spaceFreed} bytes");
```

## StorageService

The `StorageService` class provides a unified interface for managing backup storage operations across multiple storage backends including S3, Azure Blob Storage, and local file systems. It handles uploads, downloads, deletion, listing, connection testing, and space availability checks for different storage configurations.

Here's an example usage of the `StorageService` class:

```csharp
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

// Create a new instance of StorageService
var loggerFactory = LoggerFactory.Create(builder => { });
var logger = loggerFactory.CreateLogger<StorageService>();
var storageService = new StorageService(logger);

// Test connection to storage
var s3Config = new S3Configuration
{
    BucketName = "my-backups",
    RegionName = "us-east-1",
    AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
    SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
};

var isConnected = await storageService.TestConnectionAsync(s3Config);
Console.WriteLine($"Connection successful: {isConnected}");

// Upload a backup file
var uploadResult = await storageService.UploadBackupAsync(
    @"./backups/my-database-2024-01-15.sqlite",
    s3Config
);
Console.WriteLine($"Uploaded to: {uploadResult}");

// List available backups
var backups = await storageService.ListBackupsAsync(s3Config);
foreach (var (path, size, modified) in backups)
{
    Console.WriteLine($"Backup: {path}, Size: {size} bytes, Modified: {modified}");
}

// Get available space
var availableSpace = await storageService.GetAvailableSpaceAsync(s3Config);
Console.WriteLine($"Available space: {availableSpace} bytes");

// Download a backup
var downloadedFile = await storageService.DownloadBackupAsync(
    uploadResult,
    s3Config
);
Console.WriteLine($"Downloaded to: {downloadedFile}");

// Delete a backup
await storageService.DeleteBackupAsync(uploadResult, s3Config);
```

// ... existing content ...
