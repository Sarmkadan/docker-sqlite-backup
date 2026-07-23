// ... existing content ...

## MemoryCacheService
The `MemoryCacheService` class is an in-memory implementation of `ICacheService`, backed by a `ConcurrentDictionary` with automatic background cleanup of expired entries. It is suitable for single-instance applications and development scenarios.

Here's an example of how to use its public API:
```csharp
using DockerSqliteBackup.Caching;

// Arrange
using var cache = new MemoryCacheService();

// Act & Assert
cache.Set("key", "value");
var value = cache.Get<string>("key");

await cache.SetAsync("key", "value");
var asyncValue = await cache.GetAsync<string>("key");

var exists = cache.Exists("key");

var cached = cache.GetOrSet("key", () => "computed-value");
var cachedAsync = await cache.GetOrSetAsync("key", ct => Task.FromResult("computed-value"));

cache.Remove("key");
cache.Clear();
```

// ... rest of the content ...
