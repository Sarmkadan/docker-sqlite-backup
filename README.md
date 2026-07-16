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

```
// ... existing content ...
