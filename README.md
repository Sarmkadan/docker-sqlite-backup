// ... existing content ...

## CacheKeyBuilder

The `CacheKeyBuilder` class provides a fluent API for constructing consistent and collision-free cache keys. It ensures all keys follow a standardized naming convention with the `backup:` prefix and colon‑separated parts. The builder supports adding string, Guid, integer, and long values, and includes a collection of predefined keys for common resources.


Here's an example usage of the `CacheKeyBuilder` class:

```csharp
using DockerSqliteBackup.Caching;

// Create a custom cache key
var customKey = new CacheKeyBuilder()
    .Add("schedule")
    .Add(Guid.NewGuid())
    .Add(DateTime.UtcNow.Year)
    .Build();

// Use predefined keys
var scheduleKey = CacheKeyBuilder.Keys.Schedule(Guid.NewGuid());
var allSchedulesKey = CacheKeyBuilder.Keys.AllSchedules();
var backupResultKey = CacheKeyBuilder.Keys.BackupResult(Guid.NewGuid());
var healthStatusKey = CacheKeyBuilder.Keys.HealthStatus();
```

## MemoryCacheService

`MemoryCacheService` is an in‑memory implementation of `ICacheService` that stores values in a thread‑safe `ConcurrentDictionary`. It supports synchronous and asynchronous get/set operations, automatic expiration handling, and helper methods for retrieving or creating cached values.

```csharp
using DockerSqliteBackup.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

// Create the cache service (optional cleanup interval)
var cache = new MemoryCacheService(TimeSpan.FromMinutes(10));

// Store a value for 5 minutes
cache.Set("greeting", "Hello, world!", TimeSpan.FromMinutes(5));

// Retrieve it synchronously
var greeting = cache.Get<string>("greeting");

// Asynchronously retrieve a value
var asyncGreeting = await cache.GetAsync<string>("greeting");

// Remove a key
cache.Remove("greeting");

// Use GetOrSet to lazily create a value if missing
var count = cache.GetOrSet("requestCount", () => 0);

// Asynchronously get or set a value
var asyncCount = await cache.GetOrSetAsync(
    "asyncRequestCount",
    async ct => {
        await Task.Delay(100, ct);
        return 42;
    },
    expiration: TimeSpan.FromHours(1));
```

The service also provides `Exists`, `Clear`, and asynchronous `RemoveAsync` methods for full cache management.

## FileSystemUtility

`FileSystemUtility` provides a set of static helper methods for common file‑system tasks such as safe copying, deleting, enumerating files, calculating directory size, and recursive directory management. The methods handle errors internally and are suitable for backup, cleanup, and verification scenarios.

```csharp
using DockerSqliteBackup.Utilities;
using System;
using System.Threading.Tasks;

public static class FileSystemUtilityExample
{
    public static async Task RunAsync()
    {
        // Safely copy a file
        await FileSystemUtility.SafeCopyFileAsync("source.db", "backup/source.db");

        // Delete a file if it exists
        FileSystemUtility.SafeDeleteFile("temp.txt");

        // Enumerate files matching a pattern
        var logFiles = FileSystemUtility.GetFilesWithPattern("/var/logs", "*.log");
        foreach (var file in logFiles)
            Console.WriteLine(file);

        // Calculate total size of a directory
        long size = FileSystemUtility.CalculateDirectorySize("/var/backups");
        Console.WriteLine($"Backup size: {size} bytes");

        // Recursively delete a directory
        await FileSystemUtility.DeleteDirectoryAsync("/tmp/old-backups");

        // Get available disk space for a path
        long freeSpace = FileSystemUtility.GetAvailableDiskSpace("/var/backups");
        Console.WriteLine($"Free space: {freeSpace} bytes");

        // Check if a file is currently in use
        bool inUse = FileSystemUtility.IsFileInUse("backup/source.db");
        Console.WriteLine($"File in use: {inUse}");

        // Copy an entire directory
        FileSystemUtility.CopyDirectory("/var/backups", "/mnt/backup-copy");
    }
}
```

```
// ... existing content ...
