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

