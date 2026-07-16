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

// ... existing content ...
