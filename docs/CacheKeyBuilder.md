# CacheKeyBuilder

Fluent builder for constructing deterministic cache keys used throughout the docker‑sqlite‑backup application. Each call to `Add` appends a named component to the internal key parts; `Build` concatenates them with a colon separator to produce the final key string. Static string fields provide canonical prefixes for common key categories, ensuring consistency across the codebase.

## API

### Add (four overloads)

Appends a component to the cache key and returns the same instance to allow method chaining.

* **Purpose** – Extends the key with a new part; the exact overload determines whether a key‑only, key/value, or collection of parts is added.  
* **Parameters** – Vary by overload (e.g., `string key`, `string key, string value`, `string key, object value`, `IEnumerable<KeyValuePair<string,string>> parts`).  
* **Return value** – The `CacheKeyBuilder` instance, enabling fluent syntax.  
* **Exceptions** – Throws `ArgumentNullException` if any supplied argument is `null`. Overloads that receive a collection also throw if the collection itself is `null` or contains a `null` key or value.

### Build

Creates the final cache key string from the accumulated parts.

* **Purpose** – Produces a single, colon‑delimited key suitable for use with caching mechanisms.  
* **Parameters** – None.  
* **Return value** – A `string` representing the built key; returns an empty string if no parts have been added.  
* **Exceptions** – Throws `InvalidOperationException` if the internal state is corrupted (e.g., a part contains the separator character) – this guards against malformed keys.

### Clear

Resets the builder to its initial empty state.

* **Purpose** – Allows reuse of the same `CacheKeyBuilder` instance for constructing a different key.  
* **Parameters** – None.  
* **Return value** – `void`.  
* **Exceptions** – None.

### Schedule

Static constant representing the prefix for schedule‑related cache keys.

* **Purpose** – Provides a standardized segment (`"schedule"`) to prefix keys that store scheduling information.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### AllSchedules

Static constant representing the prefix for keys that aggregate all schedules.

* **Purpose** – Provides a standardized segment (`"all-schedules"`) for keys that hold a collection of schedule entries.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### BackupResult

Static constant representing the prefix for backup result cache keys.

* **Purpose** – Provides a standardized segment (`"backup-result"`) for keys that store the outcome of a backup operation.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### BackupHistory

Static constant representing the prefix for backup history cache keys.

* **Purpose** – Provides a standardized segment (`"backup-history"`) for keys that retain a history of backup runs.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### HealthStatus

Static constant representing the prefix for health‑status cache keys.

* **Purpose** – Provides a standardized segment (`"health-status"`) for keys that store the current health check result.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### Metrics

Static constant representing the prefix for metrics cache keys.

* **Purpose** – Provides a standardized segment (`"metrics"`) for keys that hold collected metrics data.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

### ConfigValue

Static constant representing the prefix for configuration‑value cache keys.

* **Purpose** – Provides a standardized segment (`"config-value"`) for keys that cache individual configuration values.  
* **Parameters** – None.  
* **Return value** – A `string` constant.  
* **Exceptions** – Never throws.

## Usage

```csharp
// Example 1: Building a key for a specific backup result.
var backupId = Guid.NewGuid();
var timestamp = DateTime.UtcNow.ToString("o");
string backupResultKey = new CacheKeyBuilder()
    .Add(CacheKeyBuilder.BackupResult)   // static prefix
    .Add("backup-id", backupId.ToString())
    .Add("timestamp", timestamp)
    .Build();
// backupResultKey => "backup-result:backup-id:<guid>:timestamp:<iso8601>"
```

```csharp
// Example 2: Building a key for the collection of all schedules.
string allSchedulesKey = new CacheKeyBuilder()
    .Add(CacheKeyBuilder.AllSchedules)   // static prefix
    .Build();
// allSchedulesKey => "all-schedules"
```

## Notes

* The builder is **not thread‑safe**; concurrent calls to `Add`, `Build`, or `Clear` on the same instance from multiple threads may produce undefined results. For thread‑safe usage, either confine the builder to a single thread or synchronize access externally.  
* Static string fields are immutable and therefore inherently thread‑safe; they can be safely referenced from any thread without synchronization.  
* Adding a `null` key or value (or a collection containing `null`) will cause an `ArgumentNullException`.  
* The separator used in `Build` is a colon (`:`). If any part itself contains a colon, the resulting key may become ambiguous; the implementation throws `InvalidOperationException` in such cases to prevent subtle bugs.  
* After calling `Clear`, the builder returns to an empty state; subsequent `Build` calls will yield an empty string until new parts are added.  
* The order in which parts are added matters and directly influences the final key string; callers should maintain a consistent ordering scheme when constructing keys for the same logical entity.
