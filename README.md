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
