# IntegrityReportExtensions

Provides a set of static extension methods for inspecting and summarizing the structural health, storage efficiency, and operational mode of a SQLite database. These methods operate on an integrity report object and expose key metrics such as file size, free page percentage, WAL mode status, and aggregated error details, enabling programmatic health checks and monitoring.

## API

### HasIntegrityIssues
`public static bool HasIntegrityIssues`

Returns `true` if the integrity report contains any detected structural problems; otherwise `false`. This is a simple boolean gate for determining whether further diagnostic action is necessary.

### GetDatabaseSizeInBytes
`public static long GetDatabaseSizeInBytes`

Returns the total size of the SQLite database file on disk, measured in bytes. The value reflects the current file size as reported by the integrity check, not the logical size of user data alone.

### GetFreePagePercentage
`public static double GetFreePagePercentage`

Calculates the percentage of database pages that are unallocated (free pages) relative to the total number of pages. Returns a `double` between `0.0` and `100.0`. A high value may indicate fragmentation or recent large deletions.

### GetAllErrors
`public static string GetAllErrors`

Aggregates all error messages from the integrity report into a single string. If multiple errors exist, they are concatenated. Returns an empty string when no errors are present. The format is implementation-defined and intended for logging or display.

### IsWalMode
`public static bool IsWalMode`

Indicates whether the database is operating in Write-Ahead Logging (WAL) journal mode. Returns `true` for WAL mode, `false` for other journal modes (delete, truncate, persist, memory, or off).

### GetHealthStatus
`public static string GetHealthStatus`

Produces a human-readable health classification based on the integrity report data. The returned string typically represents a status such as "Healthy", "Degraded", or "Critical", derived from the presence of integrity issues and free space thresholds. The exact set of possible values is determined by the internal logic of the method.

### HasLowFreeSpace
`public static bool HasLowFreeSpace`

Determines whether the database has critically low free page space. Returns `true` when the free page percentage falls below an internally defined threshold, signaling that the database may benefit from a `VACUUM` operation.

### GetMetadataSummary
`public static string GetMetadataSummary`

Builds and returns a formatted string containing key metadata fields from the integrity report. This typically includes database size, page count, free pages, journal mode, and any integrity flags. The output is intended for diagnostic summaries and structured logging.

## Usage

### Example 1: Basic Health Check with Conditional Vacuum
```csharp
var report = connection.ExecuteIntegrityCheck(); // hypothetical source
if (IntegrityReportExtensions.HasIntegrityIssues(report))
{
    Console.WriteLine($"Errors detected: {IntegrityReportExtensions.GetAllErrors(report)}");
    return;
}

if (IntegrityReportExtensions.HasLowFreeSpace(report))
{
    Console.WriteLine("Low free space — executing VACUUM.");
    connection.Execute("VACUUM;");
}

Console.WriteLine($"Health: {IntegrityReportExtensions.GetHealthStatus(report)}");
Console.WriteLine(IntegrityReportExtensions.GetMetadataSummary(report));
```

### Example 2: Monitoring and Logging in a Scheduled Task
```csharp
var report = connection.ExecuteIntegrityCheck();
var sizeBytes = IntegrityReportExtensions.GetDatabaseSizeInBytes(report);
var freePercent = IntegrityReportExtensions.GetFreePagePercentage(report);
var isWal = IntegrityReportExtensions.IsWalMode(report);

_logger.Information(
    "DB size: {Size} MB | Free pages: {Free:P1} | WAL: {Wal} | Status: {Status}",
    sizeBytes / (1024.0 * 1024.0),
    freePercent / 100.0,
    isWal,
    IntegrityReportExtensions.GetHealthStatus(report));

if (IntegrityReportExtensions.HasIntegrityIssues(report))
{
    _logger.Error("Integrity failure: {Errors}", IntegrityReportExtensions.GetAllErrors(report));
}
```

## Notes

- All methods are designed to be called on a valid integrity report object. Passing a null or uninitialized report will result in a `NullReferenceException` or undefined behavior depending on the underlying implementation.
- `GetFreePagePercentage` may return `0.0` for an empty database or one with no free pages. It does not throw on edge cases such as zero total pages, though the result should be treated as zero in that scenario.
- `GetAllErrors` returns an empty string when no errors exist; callers should not assume a non-empty result indicates success without also checking `HasIntegrityIssues`.
- `GetHealthStatus` and `HasLowFreeSpace` rely on internal thresholds that are not parameterized through these methods. The thresholds are fixed within the implementation and should be verified against current requirements.
- These methods perform synchronous CPU-bound calculations on in-memory data. They do not perform I/O and are safe to call from any thread, provided the integrity report object itself is not mutated concurrently.
