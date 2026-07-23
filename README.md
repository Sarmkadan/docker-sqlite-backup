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

## Docker HEALTHCHECK

The image exposes a `healthcheck` CLI subcommand instead of an HTTP endpoint, since the
process is a background worker with no web server. It evaluates a small status file
(`HealthCheckStatusFilePath` in `appsettings.json`, default `health-status.json`) that is
kept up to date by the running process whenever it publishes a `BackupCompletedEvent`,
`BackupFailedEvent`, or `RestoreVerificationCompletedEvent`. Because the state lives on
disk, the check works correctly even though each `HEALTHCHECK` probe runs in a brand new
`dotnet` process.

The container is reported unhealthy when:

- the most recent restore verification failed, or
- the most recent backup attempt failed (and no successful backup happened after it), or
- the most recent successful backup is older than the schedule's expected interval
  (derived from its cron expression) multiplied by `HealthCheckGraceFactor` (default `1.5`).

Add this to the `Dockerfile`:

```dockerfile
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD ["dotnet", "docker-sqlite-backup.dll", "healthcheck"]
```

You can also run it manually to inspect the current state:

```bash
dotnet docker-sqlite-backup.dll healthcheck
echo $?   # 0 = healthy, 1 = unhealthy
```

// ... rest of the content ...
