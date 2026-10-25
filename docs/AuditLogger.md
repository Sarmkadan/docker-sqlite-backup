# AuditLogger

`AuditLogger` (`src/Audit/AuditLogger.cs`) records audit events for backup operations, schedule changes, configuration changes, and data access. Each event is an `AuditEntry` serialized to JSON. Every entry is written to two places: the `ILogger<AuditLogger>` sink (as `AUDIT: {json}`) and an append-only audit file.

Convenience methods live in `AuditLoggerExtensions` (`src/Audit/AuditLoggerExtensions.cs`): `LogSuccess` and `LogBackupResult`. See [Extensions](#extensions).

Tests: `tests/docker-sqlite-backup.Tests/AuditLoggerTests.cs`.

## Constructor

### `public AuditLogger(ILogger<AuditLogger> logger, string? auditLogPath = null)`

| Parameter | Behavior |
|---|---|
| `logger` | Required. `null` throws `ArgumentNullException`. |
| `auditLogPath` | Audit file path. `null` uses `audit.log` in `AppContext.BaseDirectory`. An empty string disables file output; entries go only to the logger. |

The repository does not register `AuditLogger` in its DI container. Construct it directly or register it in your own host.

## Methods

### `public void LogBackupOperation(Guid scheduleId, string operation, bool success, string? details = null)`

Writes an entry with `Category = "BACKUP"`, `Action = operation`, `TargetId = scheduleId.ToString()`, plus `Success` and `Details`. `UserId` is not set.

- **Throws** `ArgumentNullException` if `operation` is `null`, or `ArgumentException` if it is empty.

### `public void LogScheduleChange(Guid scheduleId, string action, Dictionary<string, string> changes)`

Writes an entry with `Category = "SCHEDULE"`, `Action = action`, `TargetId = scheduleId.ToString()`, and `Success = true`. `Details` is the changes joined as `Key=Value` pairs separated by `"; "`. An empty dictionary produces an empty `Details` string.

- **Note:** `Success` is always `true` here. Use `LogEntry` to record a failed schedule change.
- **Throws** `ArgumentNullException` if `action` or `changes` is `null`, or `ArgumentException` if `action` is empty.

### `public void LogConfigChange(string setting, string oldValue, string newValue, string? reason = null)`

Writes an entry with `Category = "CONFIG"`, `Action = "CHANGED"`, `TargetId = setting`, and `Success = true`. `Details` is `"{oldValue} -> {newValue}"`, with ` ({reason})` appended when `reason` is not `null`.

- **Throws** `ArgumentNullException` or `ArgumentException` if `setting`, `oldValue`, or `newValue` is `null` or empty.

### `public void LogDataAccess(string resource, string action, string? userId = null, bool success = true)`

Writes an entry with `Category = "DATA_ACCESS"`, `Action = action`, `TargetId = resource`, `UserId`, and `Success`. `Details` is not set.

- **Throws** `ArgumentNullException` or `ArgumentException` if `resource` or `action` is `null` or empty.

### `public void LogEntry(AuditEntry entry)`

Writes `entry` as given. The caller controls every field, including `Timestamp` and `CorrelationId`. No field is validated.

- If `entry` is `null`, the method returns without logging or writing anything.
- `Timestamp` is not defaulted. If it is not set, it stays at `default(DateTime)`.

## Output and error handling

- Each public method logs an `Information` line when it starts and another when it finishes (for example, `LogBackupOperation called for ...`). `LogEntry` logs only its category.
- The entry is serialized with `JsonSerializer.Serialize(entry)` using default options, so JSON property names are PascalCase.
- The file write is `File.AppendAllText(path, json + Environment.NewLine)`. There is no rotation, buffering, or locking.
- Serialization and file errors are caught and logged at `Error` as `Failed to write audit log entry`. They are not rethrown, so auditing never fails the caller.
- Argument validation runs after the start log line and before any entry is built. An invalid argument throws and writes nothing.

## `AuditEntry`

`AuditEntry` (same file as `AuditLogger`) is a plain class with settable properties.

| Property | Type | Default | Notes |
|---|---|---|---|
| `Timestamp` | `DateTime` | `default` | Set to `DateTime.UtcNow` by the `Log*` methods. |
| `Category` | `string` | `""` | Free-form category. |
| `Action` | `string` | `""` | Free-form action. |
| `TargetId` | `string` | `""` | Identifier of the affected resource. |
| `UserId` | `string?` | `null` | Actor identifier, if known. |
| `Success` | `bool` | `false` | Defaults to `false`, unlike the `Log*` methods, which set it explicitly. |
| `Details` | `string?` | `null` | Free-form context. |
| `CorrelationId` | `Guid` | `Guid.NewGuid()` | A new value per instance. The `Log*` methods do not accept one. |

### `public override string ToString()`

Returns:

```
[{Timestamp:O}] {Category}/{Action} on {TargetId} - SUCCESS|FAILED[: {Details}][ (User: {UserId})]
```

`Details` and `UserId` are appended only when non-empty. `CorrelationId` is not included.

## Extensions

`AuditLoggerExtensions` are extension methods on `AuditLogger`. Both call `LogEntry`.

### `public static void LogSuccess(this AuditLogger auditLogger, string category, string action, bool success)`

Writes an entry with the given `Category`, `Action`, and `Success`, and `Timestamp = DateTime.UtcNow`. `TargetId` is not set.

- **Throws** `ArgumentNullException` if `auditLogger` is `null`, or `ArgumentException` if `category` or `action` is `null` or empty.

### `public static void LogBackupResult(this AuditLogger auditLogger, string targetId, string? userId, bool success)`

Writes an entry with `Category = "Backup"`, `Action = "Operation"`, `TargetId = targetId`, `UserId = userId`, and `Success`.

- **Note:** the category is `"Backup"`, while `LogBackupOperation` writes `"BACKUP"`. Filters on the category must match the method that produced the entry.
- **Throws** `ArgumentNullException` if `auditLogger` is `null`, or `ArgumentException` if `targetId` is `null` or empty.

## Usage

```csharp
using DockerSqliteBackup.Audit;
using Microsoft.Extensions.Logging.Abstractions;

// Replace NullLogger with the host's ILogger<AuditLogger> in production.
var auditLogger = new AuditLogger(
    NullLogger<AuditLogger>.Instance,
    auditLogPath: Path.Combine(Path.GetTempPath(), "audit.log"));

var scheduleId = Guid.NewGuid();

// Backup outcome
auditLogger.LogBackupOperation(scheduleId, "BackupCreated", success: true, details: "Size=1048576 bytes");

// Schedule change
auditLogger.LogScheduleChange(scheduleId, "ScheduleUpdated", new Dictionary<string, string>
{
    ["CronExpression"] = "0 2 * * *",
    ["Enabled"] = "true",
});

// Configuration change with a reason
auditLogger.LogConfigChange("Compression", "None", "Gzip", reason: "Reduce storage usage");

// Data access by a known user
auditLogger.LogDataAccess("backups/app.db.gz", "Download", userId: "ops-user");

// Extension method: failed backup with no user
auditLogger.LogBackupResult(scheduleId.ToString(), userId: null, success: false);

// Custom entry: failed schedule change, which LogScheduleChange cannot record
auditLogger.LogEntry(new AuditEntry
{
    Timestamp = DateTime.UtcNow,
    Category = "SCHEDULE",
    Action = "ScheduleUpdateFailed",
    TargetId = scheduleId.ToString(),
    Success = false,
    Details = "Invalid cron expression",
});
```

Each call appends one JSON line to the audit file and logs it as `AUDIT: {json}`.
