# MemoryCacheService

Provides an in‑memory caching layer backed by `Microsoft.Extensions.Caching.Memory.IMemoryCache`. The service wraps common cache operations (get, set, remove, clear) and adds helper methods for atomic “get‑or‑set” patterns, both synchronously and asynchronously. It is intended for scenarios where temporary data must be shared across components within a single process, such as configuration lookup results, expensive computation outputs, or intermediate backup metadata.

## API

### MemoryCacheService()

**Purpose**  
Initializes a new instance of the cache service with default options.

**Parameters**  
None.

**Return**  
A new `MemoryCacheService` instance.

**Throws**  
None.

### Get<T>(string key)

**Purpose**  
Retrieves a cached value of type `T` associated with `key`.

**Parameters**  
- `key` – The cache key. Must not be `null`.

**Return**  
The cached value if present and compatible with `T`; otherwise `null`.

**Throws**  
- `ArgumentNullException` if `key` is `null`.

### GetAsync<T>(string key)

**Purpose**  
Asynchronously retrieves a cached value of type `T` associated with `key`.

**Parameters**  
- `key` – The cache key. Must not be `null`.

**Return**  
A `Task<T?>` that completes with the cached value or `null`.

**Throws**  
- `ArgumentNullException` if `key` is `null` (exception thrown synchronously before the task is returned).

### Set<T>(string key, T value, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)

**Purpose**  
Inserts or updates a cache entry.

**Parameters**  
- `key` – The cache key. Must not be `null` or empty.  
- `value` – The object to cache. May be `null`.  
- `slidingExpiration` – Optional sliding expiration interval. If specified, must be non‑negative.  
- `absoluteExpiration` – Optional absolute expiration point. If specified, must be in the future.

**Return**  
`void`.

**Throws**  
- `ArgumentNullException` if `key` is `null`.  
- `ArgumentException` if `key` is empty.  
- `ArgumentOutOfRangeException` if `slidingExpiration` is negative.  
- `InvalidOperationException` if both `slidingExpiration` and `absoluteExpiration` are supplied (the implementation chooses to require exactly one).

### SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)

**Purpose**  
Asynchronously inserts or updates a cache entry.

**Parameters**  
Same as `Set<T>`.

**Return**  
A `Task` representing the operation.

**Throws**  
Same exceptions as `Set<T>`; they are thrown synchronously before the task is returned.

### Remove(string key)

**Purpose**  
Removes the entry associated with `key` from the cache.

**Parameters**  
- `key` – The cache key. Must not be `null`.

**Return**  
`void`.

**Throws**  
- `ArgumentNullException` if `key` is `null`.

### RemoveAsync(string key)

**Purpose**  
Asynchronously removes the entry associated with `key`.

**Parameters**  
- `key` – The cache key. Must not be `null`.

**Return**  
A `Task` representing the operation.

**Throws**  
- `ArgumentNullException` if `key` is `null` (exception thrown synchronously).

### Clear()

**Purpose**  
Removes all entries from the cache.

**Parameters**  
None.

**Return**  
`void`.

**Throws**  
None.

### Exists(string key)

**Purpose**  
Determines whether a non‑expired entry exists for `key`.

**Parameters**  
- `key` – The cache key. Must not be `null`.

**Return**  
`true` if an entry is present and has not expired; otherwise `false`.

**Throws**  
- `ArgumentNullException` if `key` is `null`.

### GetOrSet<T>(string key, Func<T> factory, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)

**Purpose**  
Returns the cached value if present; otherwise invokes `factory` to create a value, caches it, and returns the result. The operation is atomic with respect to other threads.

**Parameters**  
- `key` – The cache key. Must not be `null` or empty.  
- `factory` – A delegate that creates the value when the key is missing. Must not be `null`.  
- `slidingExpiration` – Optional sliding expiration for the newly cached value.  
- `absoluteExpiration` – Optional absolute expiration for the newly cached value.

**Return**  
The cached or newly created value of type `T`.

**Throws**  
- `ArgumentNullException` if `key` or `factory` is `null`.  
- `ArgumentException` if `key` is empty.  
- `ArgumentOutOfRangeException` if `slidingExpiration` is negative.  
- `InvalidOperationException` if both `slidingExpiration` and `absoluteExpiration` are supplied.  
- Any exception thrown by `factory` is propagated unchanged.

### GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)

**Purpose**  
Asynchronously returns the cached value if present; otherwise awaits `factory` to produce a value, caches it, and returns the result. The operation is atomic with respect to other threads.

**Parameters**  
- `key` – The cache key. Must not be `null` or empty.  
- `factory` – An async delegate that creates the value when the key is missing. Must not be `null`.  
- `slidingExpiration` – Optional sliding expiration for the newly cached value.  
- `absoluteExpiration` – Optional absolute expiration for the newly cached value.

**Return**  
A `Task<T>` that completes with the cached or newly created value.

**Throws**  
- `ArgumentNullException` if `key` or `factory` is `null` (exception thrown synchronously).  
- `ArgumentException` if `key` is empty.  
- `ArgumentOutOfRangeException` if `slidingExpiration` is negative.  
- `InvalidOperationException` if both `slidingExpiration` and `absoluteExpiration` are supplied.  
- Any exception thrown by `factory` is propagated through the returned task.

### Value

**Purpose**  
Gets the most recently accessed cache entry as a string. If the entry is not a string or no entry was accessed, returns `null`.

**Parameters**  
None.

**Return**  
`string` representation of the last accessed cached value, or `null`.

**Throws**  
None.

### ExpiresAt

**Purpose**  
Gets the absolute expiration timestamp of the most recently accessed cache entry, if the entry uses an absolute expiration policy; otherwise returns `null`.

**Parameters**  
None.

**Return**  
`DateTime?` representing the absolute expiration moment, or `null` for sliding‑expired, non‑expiring, or absent entries.

**Throws**  
None.

### IsExpired

**Purpose**  
Indicates whether the most recently accessed cache entry has expired.

**Parameters**  
None.

**Return**  
`true` if the entry does not exist or is expired; `false` if the entry exists and has not expired.

**Throws**  
None.

## Usage

### Synchronous get‑or‑set pattern

```csharp
var cache = new MemoryCacheService();

// Retrieve a configuration object, creating it on first miss.
var config = cache.GetOrSet(
    key: "AppConfig",
    factory: () =>
    {
        // Expensive operation – e.g., read from file or remote service.
        return LoadConfigurationFromFile();
    },
    slidingExpiration: TimeSpan.FromMinutes(10));

// Use the config object...
Process(config);
```

### Asynchronous caching with fallback

```csharp
private readonly MemoryCacheService _cache = new MemoryCacheService();

public async Task<BackupMetadata> GetLatestMetadataAsync()
{
    return await _cache.GetOrSetAsync(
        key: "LatestBackupMetadata",
        factory: async () =>
        {
            // Simulate an I/O bound operation.
            return await BackupRepository.FetchLatestMetadataAsync();
        },
        absoluteExpiration: DateTimeOffset.UtcNow.AddHours(1));
}
```

## Notes

- **Thread safety** – All public members are safe to call concurrently from multiple threads. Internal synchronization ensures that `GetOrSet`/`GetOrSetAsync` invoke the factory only once even when several threads race on the same missing key.
- **Null values** – The cache permits storing `null` values. `Get<T>` will return `null` for a deliberately cached null, making it indistinguishable from a missing entry; use `Exists` to differentiate.
- **Expiration policies** – Only one of `slidingExpiration` or `absoluteExpiration` may be supplied; supplying both results in an `InvalidOperationException`. If neither is supplied, the entry lives until the cache is cleared or evicted under memory pressure.
- **Property semantics** – `Value`, `ExpiresAt`, and `IsExpired` reflect the state of the *last* key accessed via any of the get‑or‑set methods. They are primarily intended for debugging or diagnostic scenarios and should not be relied upon for programmatic logic that depends on a specific key.
- **Memory pressure** – The underlying `IMemoryCache` may evict entries when the system runs low on memory, regardless of expiration settings. Callers should be prepared to handle a missing entry after a `Set` operation if the cache aggressively purges data.
- **Exception propagation** – Exceptions thrown by user‑supplied factories in `GetOrSet`/`GetOrSetAsync` are not caught by the cache; they bubble up to the caller, leaving the cache unchanged for the requested key.
