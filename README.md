## BackupJobExtensions

The `BackupJobExtensions` class provides extension methods for `BackupJob` that simplify status checks, duration formatting, retry progress tracking, and result retrieval. It includes convenience methods for determining job status, formatting durations, and assessing retry attempts.

### Usage Examples

```csharp
// Get a backup job instance
var job = new BackupJob("production.db", DateTime.UtcNow);

// Check job status
bool isSuccessful = BackupJobExtensions.IsSuccessful(job);
bool isFailed = BackupJobExtensions.IsFailed(job);
bool isPending = BackupJobExtensions.IsPending(job);
bool isInProgress = BackupJobExtensions.IsInProgress(job);

// Format job duration
string duration = BackupJobExtensions.GetFormattedDuration(job);

// Get retry progress
string retryProgress = BackupJobExtensions.GetRetryProgress(job);

// Check if job has exceeded retries
bool hasExceededRetries = BackupJobExtensions.HasExceededRetries(job);

// Get job result
var result = BackupJobExtensions.GetResult(job);
```

## AppSettingsExtensions

The `AppSettingsExtensions` class offers a set of helper methods for reading configuration values that control backup verification, S3 storage usage, notification email handling, encryption settings, compression preferences, and overall configuration validation. These static methods make it easy to query feature flags and retrieve related settings from an `IConfiguration` (or similar) instance.

### Usage Example

```csharp
// Assume you have an IConfiguration instance named config
bool verificationEnabled = AppSettingsExtensions.IsVerificationEnabled(config);
bool s3Enabled = AppSettingsExtensions.IsS3StorageEnabled(config);
string? notificationEmails = AppSettingsExtensions.GetNotificationEmailsAsString(config);
bool encryptionConfigured = AppSettingsExtensions.IsEncryptionConfigured(config);
bool compressBackups = AppSettingsExtensions.ShouldCompressBackups(config);
bool configValid = AppSettingsExtensions.Validate(config);
```

## StorageServiceIntegrationTestsExtensions

`StorageServiceIntegrationTestsExtensions` provides a collection of helper methods used in integration tests for storage services. The utilities simplify creating temporary files and directories, configuring local storage, obtaining a `StorageService` instance, and verifying backup files and their properties.

### Usage Example

```csharp
using DockerSqliteBackup.Tests.Integration;

// Assume we are inside a test class that inherits from StorageServiceIntegrationTests
var tempDir = StorageServiceIntegrationTestsExtensions.GetTempDir(this);
var tempFile = StorageServiceIntegrationTestsExtensions.CreateTempFile;
var config = StorageServiceIntegrationTestsExtensions.MakeLocalConfig(tempDir);
var storageService = StorageServiceIntegrationTestsExtensions.GetStorageService(config);

// Create a random backup file for testing
string backupPath = StorageServiceIntegrationTestsExtensions.CreateRandomBackupFile(storageService);

// Verify that the backup file exists and is valid
StorageServiceIntegrationTestsExtensions.VerifyBackupFile(storageService, backupPath);

// Create multiple backup files and get their sizes
var backupFiles = StorageServiceIntegrationTestsExtensions.CreateMultipleBackupFiles(storageService, count: 3);
foreach (var file in backupFiles)
{
    long size = StorageServiceIntegrationTestsExtensions.GetFileSize(file);
    Console.WriteLine($"Backup file: {file}, Size: {size} bytes");
}

// Create a backup tuple (path, size, modified date) for further assertions
var (path, size, modified) = StorageServiceIntegrationTestsExtensions.CreateBackupTuple(storageService);

// Use the extension method to assert the backup path matches expectations
this.ShouldMatchBackupPath(storageService, path);
```

## ScheduleServiceTestsExtensions

The `ScheduleServiceTestsExtensions` class provides helper methods for creating test `BackupSchedule` instances and asserting their properties in unit tests. It simplifies testing schedule validation logic by generating valid/invalid schedules and verifying expected state.

### Usage Example

```csharp
public void TestValidSchedule()
{
    var schedule = ScheduleServiceTestsExtensions.CreateValidSchedule();
    ScheduleServiceTestsExtensions.AssertScheduleIsActive(schedule);
    ScheduleServiceTestsExtensions.AssertScheduleHasId(schedule);
    ScheduleServiceTestsExtensions.AssertScheduleHasName(schedule);
    ScheduleServiceTestsExtensions.AssertScheduleHasDatabasePath(schedule);
}
```

## StorageAdapterTestsExtensions

`StorageAdapterTestsExtensions` offers a set of utilities for unit‑testing storage adapters. It helps create temporary files and directories, configure local or Azure storage adapters, and provides fluent assertions to verify file existence, content, and expected exception behavior.

### Usage Example

```csharp
using DockerSqliteBackup.Tests.Services;

// Create temporary resources
string tempFile = StorageAdapterTestsExtensions.CreateTempFile;
string tempDir  = StorageAdapterTestsExtensions.CreateTempDirectory;

// Configure storage adapters
var localConfig = StorageAdapterTestsExtensions.WithLocalStorage(tempDir);
var azureConfig = StorageAdapterTestsExtensions.WithAzureStorage(
    accountName: "myaccount",
    accountKey:  "mykey",
    container:   "backups");

// Assert that a file exists and contains the expected data
await StorageAdapterTestsExtensions.ShouldExistWithContentAsync(
    path: tempFile,
    expectedContent: "sample data");

// Verify that an operation throws the expected local‑storage exception
await StorageAdapterTestsExtensions.ShouldThrowLocalStorageExceptionAsync(async () =>
{
    // code that triggers a local storage failure, e.g. accessing a locked file
    await File.ReadAllTextAsync(tempFile);
});

// Ensure a file does not exist
StorageAdapterTestsExtensions.ShouldNotExist("nonexistent.file");

// Verify directory existence expectations
StorageAdapterTestsExtensions.DirectoryShouldExist(tempDir);
StorageAdapterTestsExtensions.DirectoryShouldNotExist("some/other/dir");
```
