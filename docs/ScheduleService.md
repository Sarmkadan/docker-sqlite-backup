# ScheduleService

`ScheduleService` (`src/Services/ScheduleService.cs`) implements `IScheduleService` in namespace `DockerSqliteBackup.Services`. It manages `BackupSchedule` records: create, update, read, delete, activate/deactivate, cron validation and next-run calculation. Persistence is delegated to `IBackupRepository`. The class is `sealed` and is registered as a singleton in `Configuration/ServiceCollectionExtensions.cs`.

## Constructor

```csharp
public ScheduleService(IBackupRepository repository, ILogger<ScheduleService> logger)
```

| Parameter | Description |
|---|---|
| `repository` | Storage for schedules (`IBackupRepository`, SQLite-backed in `src/Data`). |
| `logger` | Logger used for informational and warning messages. |

## Methods

### `CreateScheduleAsync(BackupSchedule schedule)`

Validates and persists a new schedule.

1. Throws `ArgumentNullException` (the project's own type in `DockerSqliteBackup.Exceptions`) if `schedule` is `null`.
2. Throws `InvalidScheduleException` if `schedule.IsValid()` returns `false`.
3. Throws `InvalidCronExpressionException` if the cron expression does not parse.
4. If `schedule.ValidateDatabasePath()` fails (the file does not exist), logs a warning and **continues**. The schedule is still created.
5. Sets `CreatedAt` and `LastModifiedAt` to `DateTime.UtcNow`.
6. Calls `_repository.CreateScheduleAsync`. Any exception from the repository is wrapped in `ScheduleException`.

Returns the schedule as returned by the repository.

### `CreateScheduleAsync(string name, string databasePath, CancellationToken cancellationToken = default)`

Convenience overload. Builds a `BackupSchedule` with `Name` and `DatabasePath` set, all other fields at their model defaults (cron `0 2 * * *`, active, 30-day retention), and delegates to the overload above. The `cancellationToken` parameter is **not** used.

### `UpdateScheduleAsync(BackupSchedule schedule)`

Validates and updates an existing schedule.

1. `ArgumentNullException` if `schedule` is `null`.
2. `InvalidScheduleException` if `schedule.IsValid()` is `false`.
3. `InvalidCronExpressionException` if the cron expression is invalid.
4. Sets `LastModifiedAt` to `DateTime.UtcNow`.
5. Calls `_repository.UpdateScheduleAsync`. Repository exceptions are wrapped in `ScheduleException`.

Logs the update and returns the repository result.

### `DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)`

Calls `_repository.DeleteScheduleAsync(scheduleId)` and logs the deletion. The `cancellationToken` is not forwarded. There is no existence check in the service. Any error comes from the repository unwrapped.

### `GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)`

Returns the schedule from the repository, or `null` if not found. The `cancellationToken` is not used.

### `GetAllSchedulesAsync(CancellationToken cancellationToken = default)`

Returns all schedules from the repository, regardless of `IsActive`. The `cancellationToken` is not used.

### `GetActiveSchedulesAsync()`

Returns the schedules the repository reports as active. The service does not filter them itself, so the definition of "active" is the repository's.

### `DeactivateScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)`

Loads the schedule. If found, sets `IsActive = false`, updates `LastModifiedAt`, saves via `UpdateScheduleAsync`, and logs. If not found, does nothing and does not throw.

### `ActivateScheduleAsync(Guid scheduleId)`

Same as deactivate, but sets `IsActive = true`. If not found, does nothing and does not throw. It does **not** re-validate the cron expression or any other field.

### `bool ValidateCronExpression(string cronExpression)`

Returns `true` if `Cronos.CronExpression.Parse` accepts the string. Any exception (including for `null`) returns `false`. The check is syntactic only.

### `DateTime? GetNextExecutionTime(BackupSchedule schedule)`

Computes the next occurrence of `schedule.CronExpression` (Cronos).

- Anchor: `schedule.NextScheduledRunTime`, or `DateTime.UtcNow` if that is `null`.
- Returns the next occurrence in UTC (no time zone conversion).
- On a parse or calculation error, logs a warning and returns `null`. It does not throw.

## Exceptions

| Exception | When |
|---|---|
| `ArgumentNullException` (project type) | `schedule` is `null` in Create/Update. |
| `InvalidScheduleException` | `BackupSchedule.IsValid()` fails in Create/Update. |
| `InvalidCronExpressionException` | Cron expression does not parse in Create/Update. |
| `ScheduleException` | Repository call fails in Create/Update. The original exception is the inner exception. |

## Usage

```csharp
// Injected via DI as IScheduleService
var schedule = await scheduleService.CreateScheduleAsync("Nightly", "/data/app.sqlite");

schedule.CronExpression = "0 3 * * *";   // 03:00 UTC daily
schedule = await scheduleService.UpdateScheduleAsync(schedule);

if (scheduleService.ValidateCronExpression(schedule.CronExpression))
{
    DateTime? next = scheduleService.GetNextExecutionTime(schedule);
}

await scheduleService.DeactivateScheduleAsync(schedule.Id);
```

## Notes

- **Thread safety**: the class holds no mutable state of its own. Concurrency safety therefore depends on `IBackupRepository`. Read-modify-write sequences in Activate/Deactivate are not atomic, so concurrent changes to the same schedule can overwrite each other.
- **Missing database file**: Create does not fail when `DatabasePath` does not exist. Only a warning is logged.
- **Silent no-ops**: Activate/Deactivate ignore unknown IDs. Delete does not check existence.
- **Time zone**: all times are UTC. Cron expressions are evaluated against UTC.
- **Interface members with unused tokens**: several methods accept a `CancellationToken` that is never passed on.
