# HealthCheckService

`HealthCheckService` (in `src/Health/HealthCheckService.cs`) runs an in-process health check of the backup worker's environment. It evaluates three components, `storage`, `disk` and `database`, and combines them into a single `HealthCheckResult` with an overall status of `"healthy"` or `"degraded"`. Each component is checked independently, so one failing check does not stop the others.

The file also defines the supporting types `ComponentHealth` and `HealthCheckResult`.

## API

### Constructor

```csharp
public HealthCheckService(
    ILogger<HealthCheckService> logger,
    IBackupEventPublisher? eventPublisher = null,
    AppSettings? appSettings = null)
```

| Parameter | Description |
|-----------|-------------|
| `logger` | Required. Receives progress and outcome messages for each check. Throws `ArgumentNullException` when `null`. |
| `eventPublisher` | Optional. When provided, a `HealthCheckEvent` is published after every check. When `null`, no event is published. |
| `appSettings` | Optional. Supplies `DatabasePath` for the database check. When `null` or unset, the database check uses `backups.sqlite`. |

### PerformHealthCheckAsync

```csharp
public async Task<HealthCheckResult> PerformHealthCheckAsync(
    CancellationToken cancellationToken = default)
```

Runs the three component checks in order and returns the aggregated result.

- **Components**: each check is wrapped in its own `try`/`catch`. An exception is logged and recorded as an unhealthy component instead of being thrown.
- **Overall status**: `"healthy"` when every component is healthy, otherwise `"degraded"`.
- **Event**: when an `IBackupEventPublisher` was supplied, a `HealthCheckEvent` with `ComponentName = "system"`, the overall `Status`, and the message `Health check: <status>` is published.
- **Cancellation**: `cancellationToken` is passed only to the event publish step. The component checks themselves are synchronous and do not observe it.
- **Exceptions**: exceptions thrown by the event publisher are not caught and propagate to the caller.

### Component checks

These checks are `private` and run as part of `PerformHealthCheckAsync`. They are listed here because they determine the result.

| Key | What is checked | Healthy when |
|-----|-----------------|--------------|
| `storage` | Creates and deletes a `.health-check` file in `backups` under `AppContext.BaseDirectory`. | The write and delete succeed. |
| `disk` | Free space from `FileSystemUtility.GetAvailableDiskSpace()`. | Free space is strictly greater than 1 GB. |
| `database` | Resolves `DatabasePath` (relative paths are resolved against `AppContext.BaseDirectory`), then opens it read-only and runs `PRAGMA quick_check`. | The file exists and `quick_check` returns `ok`. |

When `DatabasePath` is missing from the settings, the default `backups.sqlite` is used.

## Result types

### ComponentHealth

```csharp
public class ComponentHealth
{
    public string Name { get; set; }
    public bool IsHealthy { get; set; }
    public string Message { get; set; }
    public DateTime CheckedAt { get; set; }   // defaults to DateTime.UtcNow
}
```

### HealthCheckResult

```csharp
public class HealthCheckResult
{
    public string Status { get; set; }                              // "unknown" until a check runs
    public DateTime CheckedAt { get; set; }                         // set when the result is created
    public Dictionary<string, ComponentHealth> Components { get; set; }
    public override string ToString();
}
```

`ToString()` returns `System is <status>`. When one or more components are unhealthy, it appends `. Issues: <name>: <message>, ...`.

### Validation helpers

`HealthCheckServiceValidation` (in `src/Health/HealthCheckServiceValidation.cs`) provides the extension methods `Validate`, `IsValid` and `EnsureValid` for `HealthCheckService`. The service exposes no public properties, so these methods report no problems for a non-null instance.

## Usage

### Example 1: Run a check and inspect the result

```csharp
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Health;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

var settings = new AppSettings
{
    DatabasePath = "/data/backups.sqlite"
};

var publisher = new BackupEventPublisher(
    loggerFactory.CreateLogger<BackupEventPublisher>());

var healthCheck = new HealthCheckService(
    loggerFactory.CreateLogger<HealthCheckService>(),
    publisher,
    settings);

HealthCheckResult result = await healthCheck.PerformHealthCheckAsync();

Console.WriteLine(result);   // e.g. "System is degraded. Issues: disk: Free disk space: 512 MB (Below minimum threshold)"

foreach (var (name, component) in result.Components)
{
    Console.WriteLine($"{name}: healthy={component.IsHealthy}, {component.Message}");
}
```

### Example 2: Cancel a check that is taking too long

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

var result = await healthCheck.PerformHealthCheckAsync(cts.Token);
```

The token affects only the event-publish step. The component checks run to completion regardless, so this does not bound how long the checks take.

## Notes

- **Not registered in DI**: nothing in this repository registers or constructs `HealthCheckService` outside the `Health` folder and its tests. The Docker `healthcheck` subcommand reads `HealthStatusStore` through `DockerHealthCheckEvaluator` instead.
- **Storage side effect**: each check creates and deletes `backups/.health-check`. The directory is created if it does not exist.
- **Disk threshold**: the 1 GB minimum is hard-coded and uses a strict `>` comparison, so exactly 1 GB of free space is reported as unhealthy.
- **Read-only database access**: the database is opened with `Mode=ReadOnly`, so the check does not modify it. The check fails if the file is missing; it does not create one.
- **Fallback component name**: if an unexpected exception escapes a component check, the fallback `ComponentHealth` is created without a `Name`, so its `Name` is an empty string. The entry is still keyed by `storage`, `disk` or `database` in `Components`.
- **Result is not cached**: each call creates a new `HealthCheckResult`. The service keeps no state between calls.
