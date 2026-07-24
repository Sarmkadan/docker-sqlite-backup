# MemoryCacheService

Provides an in-memory caching layer implementing `ICacheService`, backed by a `ConcurrentDictionary<string, CacheEntry>` with a background `Timer` that periodically sweeps expired entries. It is intended for single-instance applications and development scenarios where temporary data must be shared across components within a single process, such as configuration lookup results, expensive computation outputs, or intermediate backup metadata.

Namespace: `DockerSqliteBackup.Caching`

## API

### MemoryCacheService(TimeSpan? cleanupInterval = null)

**Purpose**
Initializes a new instance of the cache service and starts the background cleanup timer.

**Parameters**
- `cleanupInterval` – Optional interval between expired-entry sweeps. Defaults to 5 minutes.

**Return**
A new `MemoryCacheService` instance.

**Throws**
None.

### Get<T>(string key)

**Purpose**
Retrieves and deserializes a cached value of type `T` associated with `key`.

**Parameters**
- `key` – The cache key.

**Return**
The cached value if present and not expired; otherwise `default(T)`.

**Throws**
None directly; deserialization failures are swallowed and `default(T)` is returned.

### GetAsync<T>(string key, CancellationToken cancellationToken = default)

**Purpose**
Asynchronous wrapper around `Get<T>`.

**Return**
A `Task<T?>` that completes synchronously with the cached value or `default(T)`.

### Set<T>(string key, T value, TimeSpan? expiration = null)

**Purpose**
Serializes `value` to JSON and inserts or overwrites the cache entry for `key`.

**Parameters**
- `key` – The cache key.
- `value` – The object to cache. May be `null`.
- `expiration` – Optional relative expiration; if omitted, the entry never expires on its own (it can still be removed via `Remove`/`Clear`).

**Return**
`void`.

**Throws**
None.

### SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)

**Purpose**
Asynchronous wrapper around `Set<T>`.

**Return**
A completed `Task`.

### Remove(string key)

**Purpose**
Removes the entry associated with `key`, if any.

**Return**
`void`.

### RemoveAsync(string key, CancellationToken cancellationToken = default)

**Purpose**
Asynchronous wrapper around `Remove`.

**Return**
A completed `Task`.

### Clear()

**Purpose**
Removes all entries from the cache.

**Return**
`void`.

### Exists(string key)

**Purpose**
Determines whether a non-expired entry exists for `key`. Lazily removes the entry if it has expired.

**Return**
`true` if a live entry is present; otherwise `false`.

### GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)

**Purpose**
Returns the cached value if present and not expired; otherwise invokes `factory`, caches its result, and returns it.

**Return**
The cached or newly created value of type `T`.

**Throws**
Any exception thrown by `factory` is propagated unchanged.

### GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default)

**Purpose**
Asynchronous counterpart of `GetOrSet`.

**Return**
A `Task<T>` that completes with the cached or newly created value.

**Throws**
Any exception thrown by `factory` is propagated through the returned task.

### Dispose()

**Purpose**
Stops the background cleanup timer and releases its resources. Safe to call multiple times.

**Return**
`void`.

## Usage

### Synchronous get-or-set pattern

```csharp
using var cache = new MemoryCacheService();

var config = cache.GetOrSet(
    key: "AppConfig",
    factory: () => LoadConfigurationFromFile(),
    expiration: TimeSpan.FromMinutes(10));

Process(config);
```

### Asynchronous caching with fallback

```csharp
private readonly MemoryCacheService _cache = new();

public async Task<BackupMetadata> GetLatestMetadataAsync(CancellationToken ct)
{
    return await _cache.GetOrSetAsync(
        key: "LatestBackupMetadata",
        factory: cancellationToken => BackupRepository.FetchLatestMetadataAsync(cancellationToken),
        expiration: TimeSpan.FromHours(1),
        cancellationToken: ct);
}
```

## Notes

- **Serialization** – Values are stored as JSON via `System.Text.Json`. Types must be JSON-serializable; failed deserialization is treated as a cache miss (`default(T)` is returned) rather than throwing.
- **Expiration** – Only a single relative `expiration` `TimeSpan` is supported per entry. Entries without an expiration are never swept by the background timer, but can still be removed explicitly.
- **Lazy + background eviction** – Expired entries are removed both lazily (on `Get`/`Exists`) and periodically by the background cleanup timer.
- **Disposal** – `MemoryCacheService` implements `IDisposable`; dispose it (or wrap it in a `using` statement) to stop the cleanup timer when the cache is no longer needed.
- **Thread safety** – Backed by `ConcurrentDictionary`, so all public members are safe to call concurrently from multiple threads.
