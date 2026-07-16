// ... existing content ...

## ChecksumUtility

The `ChecksumUtility` class provides a set of static methods for calculating various types of checksums and hashes. It includes methods for calculating SHA256, MD5, and CRC32 hashes, as well as generating a quick checksum based on file size and boundary bytes.

```csharp
using DockerSqliteBackup.Utilities;

// Calculate the SHA256 hash of a file
var sha256Hash = await ChecksumUtility.CalculateFileSha256Async("path/to/file.db");

// Calculate the SHA256 hash of a string
var stringHash = ChecksumUtility.CalculateStringSha256("Hello, world!");

// Verify that a file's SHA256 hash matches the expected value
var isValid = await ChecksumUtility.VerifyFileSha256Async("path/to/file.db", "expected_hash");

// Calculate the CRC32 checksum of a file
var crc32Checksum = await ChecksumUtility.CalculateFileCrc32Async("path/to/file.db");

// Generate a quick checksum based on file size and boundary bytes
var quickChecksum = ChecksumUtility.GenerateQuickChecksum("path/to/file.db");

// Calculate a checksum for a collection of values
var collectionChecksum = ChecksumUtility.CalculateCollectionChecksum("value1", "value2", "value3");
```

## PathUtility

`PathUtility` offers a collection of static helper methods for common file‑system path operations. It handles sanitising file names, normalising paths, checking whether a path is absolute, combining path segments, retrieving file sizes, validating file paths, and generating timestamped backup file names in a platform‑agnostic way.

```csharp
using DockerSqliteBackup.Utilities;

// Sanitize a file name
var safeName = PathUtility.SanitizeFileName("my*invalid?.db");

// Normalise a path
var normalized = PathUtility.Normalize(@"C:\temp\..\backup\file.db");

// Check if a path is absolute
bool isAbs = PathUtility.IsAbsolute(normalized);

// Get a relative path
var relative = PathUtility.GetRelativePath("/var/backups", "/var/backups/2023/file.db");

// Ensure a directory exists
PathUtility.EnsureDirectoryExists("/var/backups/2023");

// Combine multiple path segments
var combined = PathUtility.CombinePath("/var", "backups", "2023", "file.db");

// Get file size
long size = PathUtility.GetFileSize(combined);

// Validate file path
bool valid = PathUtility.IsValidFilePath(combined);

// Generate a backup file name
var backupName = PathUtility.GenerateBackupFileName("mydb");
```

## StringUtility

The `StringUtility` class provides a set of static methods for common string operations including formatting, validation, and transformation. It offers utilities for converting between different string formats (PascalCase, camelCase, kebab-case, snake_case), validating email addresses and GUIDs, truncating strings, masking sensitive data, and formatting byte sizes into human-readable strings.

```csharp
using DockerSqliteBackup.Utilities;

// Format a byte size into a human-readable string
var readableSize = StringUtility.FormatBytes(1572864); // "1.5 MB"

// Convert between different string case formats
var kebab = StringUtility.ToKebabCase("MyDatabaseName"); // "my-database-name"
var snake = StringUtility.ToSnakeCase("MyDatabaseName"); // "my_database_name"
var pascal = StringUtility.ToPascalCase("my-database-name"); // "MyDatabaseName"
var camel = StringUtility.ToCamelCase("MyDatabaseName"); // "myDatabaseName"

// Validate strings
bool isValidEmail = StringUtility.IsValidEmail("user@example.com"); // true
bool isValidGuid = StringUtility.IsValidGuid("550e8400-e29b-41d4-a716-446655440000"); // true

// Truncate a string
var truncated = StringUtility.Truncate("This is a very long string that needs to be shortened", 20); // "This is a very..."

// Mask sensitive information
var masked = StringUtility.MaskSensitive("api_key_123456789"); // "api_***********"

// Split and join strings
var lines = StringUtility.SplitLines("line1\r\nline2\nline3");
var joined = StringUtility.JoinReadable("apples", "oranges", "bananas"); // "apples, oranges, and bananas"

// Remove whitespace and repeat strings
var noSpaces = StringUtility.RemoveWhitespace("hello world"); // "helloworld"
var repeated = StringUtility.Repeat("abc", 3); // "abcabcabc"

// Quote strings if needed
var quoted = StringUtility.QuoteIfNeeded("value with spaces"); // "\"value with spaces\""
```

## DateTimeUtility

The `DateTimeUtility` class offers static helper methods for common date‑ and time‑related tasks, such as ISO‑8601 formatting and parsing, human‑readable display, relative‑time strings, duration formatting, day/month boundary calculations, time‑until calculations, and rounding to arbitrary intervals.

```csharp
using DockerSqliteBackup.Utilities;

// ISO‑8601 formatting and parsing
var iso = DateTimeUtility.ToIso8601(DateTime.Now);
if (DateTimeUtility.TryParseIso8601(iso, out var parsed))
{
    // parsed now holds the original DateTime value
}

// Human‑readable display
var display = DateTimeUtility.FormatForDisplay(DateTime.Now, "MMM dd, yyyy HH:mm");

// Relative time (e.g., "5h ago")
var relative = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddHours(-5));

// Duration formatting (e.g., "2h 5m")
var duration = DateTimeUtility.FormatDuration(TimeSpan.FromMinutes(125));

// Day and month boundaries
var dayStart = DateTimeUtility.GetDayStart();
var dayEnd = DateTimeUtility.GetDayEnd();
var monthStart = DateTimeUtility.GetMonthStart();
var monthEnd = DateTimeUtility.GetMonthEnd();

// Time until a specific time of day (e.g., 02:30 UTC)
var until = DateTimeUtility.GetTimeUntil(new TimeOnly(2, 30));

// Rounding to the nearest 15‑minute interval
var roundedDown = DateTimeUtility.RoundDown(DateTime.Now, TimeSpan.FromMinutes(15));
var roundedUp = DateTimeUtility.RoundUp(DateTime.Now, TimeSpan.FromMinutes(15));
```

## HealthCheckService

The `HealthCheckService` performs comprehensive health checks on the backup system, including storage accessibility, disk space availability, and database connectivity. It returns a detailed report of each component's status and an overall system health assessment.

```csharp
using DockerSqliteBackup.Health;
using Microsoft.Extensions.Logging;

// Create the health check service
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<HealthCheckService>();
var healthCheckService = new HealthCheckService(logger);

// Perform a health check
var result = await healthCheckService.PerformHealthCheckAsync();

// Check overall status
Console.WriteLine($"Overall status: {result.Status}"); // "healthy" or "degraded"
Console.WriteLine($"Checked at: {result.CheckedAt}");

// Access individual component health
if (result.Components.TryGetValue("storage", out var storageHealth))
{
    Console.WriteLine($"Storage health: {storageHealth.IsHealthy}");
    Console.WriteLine($"Storage message: {storageHealth.Message}");
}

// Check database health
if (result.Components.TryGetValue("database", out var dbHealth))
{
    Console.WriteLine($"Database health: {dbHealth.IsHealthy}");
    Console.WriteLine($"Database message: {dbHealth.Message}");
}

// Use the ToString() method for a quick summary
Console.WriteLine(result.ToString());
```

```csharp
using DockerSqliteBackup.Utilities;

// ISO‑8601 formatting and parsing
var iso = DateTimeUtility.ToIso8601(DateTime.Now);
if (DateTimeUtility.TryParseIso8601(iso, out var parsed))
{
    // parsed now holds the original DateTime value
}

// Human‑readable display
var display = DateTimeUtility.FormatForDisplay(DateTime.Now, "MMM dd, yyyy HH:mm");

// Relative time (e.g., "5h ago")
var relative = DateTimeUtility.GetRelativeTime(DateTime.UtcNow.AddHours(-5));

// Duration formatting (e.g., "2h 5m")
var duration = DateTimeUtility.FormatDuration(TimeSpan.FromMinutes(125));

// Day and month boundaries
var dayStart   = DateTimeUtility.GetDayStart();
var dayEnd     = DateTimeUtility.GetDayEnd();
var monthStart = DateTimeUtility.GetMonthStart();
var monthEnd   = DateTimeUtility.GetMonthEnd();

// Time until a specific time of day (e.g., 02:30 UTC)
var until = DateTimeUtility.GetTimeUntil(new TimeOnly(2, 30));

// Rounding to the nearest 15‑minute interval
var roundedDown = DateTimeUtility.RoundDown(DateTime.Now, TimeSpan.FromMinutes(15));
var roundedUp   = DateTimeUtility.RoundUp(DateTime.Now, TimeSpan.FromMinutes(15));
```

## BackupEventPublisherTests

The `BackupEventPublisherTests` class contains comprehensive unit tests for the `BackupEventPublisher` class, verifying that event publishing correctly handles listener registration, event dispatch, and exception isolation. It tests scenarios including publishing with no listeners, matching and non-matching listeners, exception propagation, multiple listener invocation, duplicate subscription handling, and unsubscribe behavior.

```csharp
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Tests.Events;

// Create a publisher instance
var publisher = new BackupEventPublisherTests();

// Test publishing with no listeners - should not throw
await publisher.PublishAsync_NoListeners_DoesNotThrow();

// Subscribe a listener and publish an event
var listener = new Mock<IBackupEventListener>();
listener.Setup(l => l.CanHandle("backup.started")).Returns(true);
listener.Setup(l => l.GetSupportedEventTypes()).Returns(new[] { "backup.started" });
publisher.Subscribe(listener.Object);

var backupEvent = new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } };
await publisher.PublishAsync(new BackupStartedEvent { Schedule = new BackupSchedule { Name = "Test" } });

// Verify listener was invoked
listener.Verify(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()), Times.Once);

// Test unsubscribing
publisher.Unsubscribe(listener.Object);
await publisher.PublishAsync(backupEvent);

// Verify listener no longer receives events
listener.Verify(l => l.HandleAsync(It.IsAny<BackupEvent>(), It.IsAny<CancellationToken>()), Times.Once);
```

## StorageServiceIntegrationTests

The StorageServiceIntegrationTests class contains integration tests for the StorageService class, verifying its behavior with different storage backends including local filesystem and S3.

These tests cover upload, download, delete, list, and space availability operations to ensure reliable backup and restore functionality across storage types.

```csharp
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Storage;

// Create a storage service instance for local storage
var localStorage = new LocalStorage("/var/backups");
var storageService = new StorageService(localStorage);

// Upload a backup file to local storage
var sourceFile = "/tmp/mydb-2024-01-01.db";
var destinationPath = "backups/mydb-2024-01-01.db";
await storageService.UploadBackupAsync(sourceFile, destinationPath);

// List available backups in local storage
var backups = await storageService.ListBackupsAsync("backups");
foreach (var backup in backups)
{
    Console.WriteLine($"Backup: {backup.Name}, Size: {backup.Size}, Modified: {backup.LastModified}");
}

// Download a backup from local storage to a temporary location
var tempFile = Path.GetTempFileName();
await storageService.DownloadBackupAsync("backups/mydb-2024-01-01.db", tempFile);

// Get available space in local storage
var availableSpace = await storageService.GetAvailableSpaceAsync();
Console.WriteLine($"Available space: {availableSpace} bytes");

// Delete a backup from local storage
await storageService.DeleteBackupAsync("backups/mydb-2024-01-01.db");
```

## BackupJobTests

The `BackupJobTests` class contains unit tests for the `BackupJob` domain model, verifying backup job lifecycle operations including status transitions, retry logic, and timing calculations. It tests scenarios like marking jobs as started/completed, retry eligibility, elapsed time calculations, and retry counter management.

```csharp
using DockerSqliteBackup.Domain;

// Create a backup job instance
var job = new BackupJob
{
    MaxRetries = 3,
    RetentionDays = 7,
    DatabasePath = "/data/mydb.db"
};

// Mark a pending job as started - updates status to InProgress, sets processing flag, and records start time
job.MarkStarted();
Console.WriteLine($"Job status: {job.Status}"); // (int)BackupStatus.InProgress
Console.WriteLine($"Is processing: {job.IsProcessing}"); // true
Console.WriteLine($"Started at: {job.StartedAt}"); // not null

// Mark an in-progress job as completed - updates status to Success and clears processing flag
job.MarkCompleted((int)BackupStatus.Success);
Console.WriteLine($"Job status: {job.Status}"); // (int)BackupStatus.Success
Console.WriteLine($"Is processing: {job.IsProcessing}"); // false
Console.WriteLine($"Completed at: {job.CompletedAt}"); // not null

// Check if a failed job with remaining retries can be retried
var failedJob = new BackupJob { MaxRetries = 3 };
failedJob.MarkStarted();
failedJob.MarkCompleted((int)BackupStatus.Failed);
failedJob.IncrementRetry();
Console.WriteLine($"Can retry: {failedJob.CanRetry}"); // true

// Check if a failed job with exhausted retries cannot be retried
var exhaustedJob = new BackupJob { MaxRetries = 2 };
exhaustedJob.MarkStarted();
exhaustedJob.MarkCompleted((int)BackupStatus.Failed);
exhaustedJob.IncrementRetry();
exhaustedJob.IncrementRetry();
Console.WriteLine($"Can retry: {exhaustedJob.CanRetry}"); // false

// Check elapsed time for a completed job
var timedJob = new BackupJob();
timedJob.MarkStarted();
Thread.Sleep(10);
timedJob.MarkCompleted((int)BackupStatus.Success);
var elapsed = timedJob.GetElapsedTime();
Console.WriteLine($"Elapsed time: {elapsed.TotalMilliseconds}ms"); // positive duration

// Increment retry counter multiple times
var retryJob = new BackupJob();
retryJob.IncrementRetry();
retryJob.IncrementRetry();
retryJob.IncrementRetry();
Console.WriteLine($"Retry count: {retryJob.RetryCount}"); // 3

## StorageAdapterTests

The `StorageAdapterTests` class provides comprehensive unit tests for the `StorageService` class, verifying storage adapter functionality for both local filesystem and Azure Blob Storage configurations. It implements `IAsyncLifetime` to manage temporary test directories that are created and cleaned up for each test run.




The test suite validates upload, download, delete, list, and space availability operations across different storage backends, including configuration validation for Azure storage with connection strings, SAS URIs, and local filesystem storage.

```csharp
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Create a temporary directory for test operations
var tempDir = Path.Combine(Path.GetTempPath(), $"storage-adapter-tests-{Guid.NewGuid()}");
Directory.CreateDirectory(tempDir);

try
{
    // Create the storage service instance
    var loggerMock = new Mock<ILogger<StorageService>>();
    var storageService = new StorageService(loggerMock.Object);
    
    // Initialize - creates temp directory
    await storageService.InitializeAsync();
    
    // Test local storage upload - copies file to destination
    var sourceFile = Path.Combine(tempDir, $"test-backup-{Guid.NewGuid()}.sqlite");
    File.WriteAllText(sourceFile, "backup-content");
    
    var localConfig = new LocalStorageConfiguration
    {
        Name = "local-test",
        BaseDirectory = Path.Combine(tempDir, "local-backups")
    };
    
    var uploadedPath = await storageService.UploadBackupAsync(sourceFile, localConfig);
    Console.WriteLine($"Uploaded to: {uploadedPath}");
    
    // Test local storage list - returns uploaded files
    var backups = await storageService.ListBackupsAsync(localConfig);
    Console.WriteLine($"Found {backups.Count()} backups");
    
    // Test local storage download - copies file to temp location
    var tempDownload = await storageService.DownloadBackupAsync(uploadedPath, localConfig);
    Console.WriteLine($"Downloaded to: {tempDownload}");
    
    // Test local storage delete - removes file
    await storageService.DeleteBackupAsync(uploadedPath, localConfig);
    Console.WriteLine("Backup deleted successfully");
    
    // Test Azure configuration validation
    var azureConfig = new AzureConfiguration
    {
        Name = "azure-test",
        ConnectionString = "DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;",
        ContainerName = "backups"
    };
    
    Console.WriteLine($"Azure config valid: {azureConfig.IsValid()}");
    
    // Test space availability
    var localSpace = await storageService.GetAvailableSpaceAsync(localConfig);
    Console.WriteLine($"Local storage available space: {localSpace} bytes");
    
    var azureSpace = await storageService.GetAvailableSpaceAsync(azureConfig);
    Console.WriteLine($"Azure storage available space: {azureSpace} bytes");
}
finally
{
    // Clean up - deletes temp directory
    if (Directory.Exists(tempDir))
        Directory.Delete(tempDir, recursive: true);
}
```
```

## RotationServiceTests

The `RotationServiceTests` class contains comprehensive unit tests for the `RotationService` class, which manages backup rotation policies and cleanup operations. These tests verify the service's behavior when executing rotation policies, calculating disk space that would be freed, and handling various rotation strategies including maximum file count, maximum age, and no rotation scenarios.

```csharp
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Data;
using Microsoft.Extensions.Logging;
using Moq;

// Create mock dependencies
var repositoryMock = new Mock<IBackupRepository>();
var loggerMock = new Mock<ILogger<RotationService>>();

// Create the rotation service instance
var rotationService = new RotationService(repositoryMock.Object, loggerMock.Object);

// Create a rotation policy with maximum file count strategy
var scheduleId = Guid.NewGuid();
var policy = new RotationPolicy
{
    ScheduleId = scheduleId,
    Strategy = (int)RotationStrategy.MaxFileCount,
    MaxBackupCount = 5,
    MinimumBackupCount = 2,
    MaxAgeDays = 30
};

// Save the rotation policy
var savedPolicy = await rotationService.SaveRotationPolicyAsync(policy);
Console.WriteLine($"Policy saved with LastModifiedAt: {savedPolicy.LastModifiedAt}");

// Execute rotation - this will delete excess backups based on the policy
var deletedCount = await rotationService.ExecuteRotationAsync(scheduleId);
Console.WriteLine($"Deleted {deletedCount} backups during rotation");

// Calculate disk space that would be freed by rotation
var diskSpaceFreed = await rotationService.CalculateDiskSpaceFreedAsync(scheduleId);
Console.WriteLine($"Disk space that would be freed: {diskSpaceFreed} bytes");

// Get backups eligible for rotation
var backupsForRotation = await rotationService.GetBackupsForRotationAsync(scheduleId);
Console.WriteLine($"Found {backupsForRotation.Count} backups eligible for rotation");

// Get the rotation policy
var retrievedPolicy = await rotationService.GetRotationPolicyAsync(scheduleId);
Console.WriteLine($"Retrieved policy with MaxBackupCount: {retrievedPolicy?.MaxBackupCount}");
```

## IntegrityCheckerServiceTests

The `IntegrityCheckerServiceTests` class provides comprehensive unit tests for the `IntegrityCheckerService` class, which performs integrity checks on SQLite database files. These tests verify database validation, metadata extraction, and various integrity check scenarios including quick checks, full database validation, foreign key validation, and backup file verification.

```csharp
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

// Create mock logger
var loggerMock = new Mock<ILogger<IntegrityCheckerService>>();

// Create the integrity checker service instance
var integrityChecker = new IntegrityCheckerService(loggerMock.Object);

// Initialize the test environment
await integrityChecker.InitializeAsync();

try
{
    // Create a valid SQLite database for testing
    var dbPath = Path.Combine(Path.GetTempPath(), $"test-db-{Guid.NewGuid()}.sqlite");
    using var connection = new SqliteConnection($"Data Source={dbPath}");
    connection.Open();
    
    using var cmd = connection.CreateCommand();
    cmd.CommandText = "CREATE TABLE test_table (id INTEGER PRIMARY KEY, name TEXT)";
    cmd.ExecuteNonQuery();
    
    using var insert = connection.CreateCommand();
    insert.CommandText = "INSERT INTO test_table (name) VALUES ('test-row')";
    insert.ExecuteNonQuery();

    // Perform a full integrity check on the database
    var fullReport = await integrityChecker.CheckDatabaseAsync(dbPath, fullCheck: true);
    Console.WriteLine($"Full check - Healthy: {fullReport.IsHealthy}");
    Console.WriteLine($"Quick check passed: {fullReport.PassedQuickCheck}");
    Console.WriteLine($"Full check passed: {fullReport.PassedFullCheck}");
    Console.WriteLine($"FK check passed: {fullReport.PassedForeignKeyCheck}");
    Console.WriteLine($"Tables found: {fullReport.TableCount}");
    
    // Perform a quick integrity check only
    var quickReport = await integrityChecker.CheckDatabaseAsync(dbPath, fullCheck: false);
    Console.WriteLine($"Quick check only - Healthy: {quickReport.IsHealthy}");
    
    // Quick check method
    var isValid = await integrityChecker.QuickCheckAsync(dbPath);
    Console.WriteLine($"Quick check result: {isValid}");
    
    // Check backup file integrity
    var backupReport = await integrityChecker.CheckBackupFileAsync(dbPath);
    Console.WriteLine($"Backup check - Healthy: {backupReport.IsHealthy}");
    
    // Test IntegrityReport validation
    var healthyReport = new IntegrityReport
    {
        PassedQuickCheck = true,
        PassedFullCheck = true,
        PassedForeignKeyCheck = true
    };
    Console.WriteLine($"Is healthy: {healthyReport.IsHealthy}"); // true
    Console.WriteLine($"Summary: {healthyReport.Summary}");
}
finally
{
    // Clean up
    await integrityChecker.DisposeAsync();
}
```

## ChecksumBenchmarks

The `ChecksumBenchmarks` class provides benchmark methods for measuring the performance of checksum calculations on a temporary file. It includes `Setup` and `Cleanup` helpers to create and delete a test file, and async methods to compute SHA‑256, CRC32, and a quick checksum.

```csharp
using DockerSqliteBackup.Benchmarks; // namespace where ChecksumBenchmarks resides
using DockerSqliteBackup.Utilities;

// Create an instance of the benchmark class
var benchmarks = new ChecksumBenchmarks();

// Prepare the temporary file
benchmarks.Setup();

try
{
    // Run the benchmark methods
    var sha256 = await benchmarks.CalculateSha256();
    Console.WriteLine($"SHA‑256: {sha256}");

    var crc32 = await benchmarks.CalculateCrc32();
    Console.WriteLine($"CRC32: {crc32}");

    var quick = await benchmarks.GenerateQuickChecksum();
    Console.WriteLine($"Quick checksum: {quick}");
}
finally
{
    // Clean up the temporary file
    benchmarks.Cleanup();
}
```

