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
