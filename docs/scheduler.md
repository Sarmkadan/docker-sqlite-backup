# BackupScheduler

`BackupScheduler` (`src/Services/BackupScheduler.cs`, namespace `DockerSqliteBackup.Services`) handles the timing side of backup schedules. It parses cron expressions, decides which active schedules are due, detects backup windows missed while the container was down, and computes upcoming execution times. It does not run backups itself and does not persist anything except next-run timestamps.

## Dependencies

| Parameter | Purpose |
|---|---|
| `IScheduleService` | Loads active schedules and persists updates. |
| `AppSettings` | Global application settings. Stored but not read by the current methods. |
| `ILogger<BackupScheduler>` | Warnings for missed windows, errors for invalid cron, info after refresh. |

Null arguments throw `ArgumentNullException` (the project's own type from `DockerSqliteBackup.Exceptions`, aliased in the file).

## Cron support

- Expressions are parsed with `Cronos.CronExpression.Parse(expr)` (Cronos 0.13.0). Cronos defaults to `CronFormat.Standard`, so the expected format is the classic 5-field form: `minute hour day-of-month month day-of-week`. Six-field expressions with seconds are not accepted by this code path.
- Evaluation is in UTC. All comparisons use `DateTime.UtcNow`.
- Parsed expressions are cached per schedule ID in an in-memory `Dictionary<Guid, CronExpression>`. Call `InvalidateCronCache(scheduleId)` after a schedule's cron string changes, otherwise the old expression keeps being used.
- `TryParseCron(string)` returns `null` on a parse failure instead of throwing. Use it to validate input before saving.
- An invalid expression stored on a schedule is logged as an error (`Invalid cron expression for schedule ...`) and the schedule is skipped. It does not stop evaluation of other schedules.

## Methods

### `GetDueSchedulesAsync(CancellationToken)`

Returns `IReadOnlyList<BackupSchedule>` with the active schedules that should run now.

1. Loads active schedules from `IScheduleService.GetActiveSchedulesAsync()`.
2. For each schedule, takes the anchor `LastBackupAt ?? CreatedAt`.
3. Computes the next occurrence after the anchor. If that occurrence is `<= now`, the schedule is due.
4. Counts all occurrences between the anchor and now. If more than one was missed, logs a warning with the count.

The schedule is returned once per call regardless of how many windows were missed. The method does not catch up multiple runs. The `CancellationToken` parameter is accepted but not passed to the service call.

### `GetUpcomingExecutions(BackupSchedule, int count = 5)`

Returns the next `count` UTC execution times, starting from `DateTime.UtcNow`. The list can be shorter than `count` if the expression has no further occurrences. Throws `InvalidCronExpressionException` if the expression cannot be parsed.

### `RefreshNextRunTimesAsync(CancellationToken)`

Recomputes the next run from `DateTime.UtcNow` for every active schedule. If the value differs from the stored one, it sets both `NextScheduledRunTime` and `NextRunTime`, saves through `UpdateScheduleAsync`, and counts the update. Returns the number of updated schedules. Logs at info level.

This keeps the stored next-run value current, which supports missed-schedule detection after a restart. The `CancellationToken` parameter is accepted but not used.

### `TryParseCron(string)`

Returns the parsed `CronExpression`, or `null` if parsing fails. Does not log and does not use the cache.

### `InvalidateCronCache(Guid)`

Removes the cached expression for one schedule. The next evaluation re-parses the stored string.

## Scheduling logic summary

```
anchor = schedule.LastBackupAt ?? schedule.CreatedAt
next   = cron.GetNextOccurrence(anchor)
due    = next <= UtcNow
missed = count of occurrences in (anchor, UtcNow]
```

A schedule with no backup yet is anchored to its creation time. A schedule that has run is anchored to its last backup, so a due window is not reported again until the following occurrence passes.

## Integration status

- `BackupScheduler` is not registered in `src/Configuration/ServiceCollectionExtensions.cs`. Only `IScheduleService` is. Consumers must register it before injecting it.
- `BackupWorker.cs` parses cron expressions directly with `CronExpression.Parse` and does not call `BackupScheduler`. The due-check logic in `BackupWorker` therefore may not match `GetDueSchedulesAsync`. Check both before changing scheduling behavior.
- `BackupScheduleExtensions`, `BackupServiceValidation`, `ScheduleService`, and `DockerHealthCheckEvaluator` also parse cron strings. The first two use `CronFormat.Standard` explicitly.

## Known limitations

- Missed windows are logged, not replayed. A schedule runs at most once per due check.
- The cache is never evicted automatically. Changes to a schedule's cron string need an explicit `InvalidateCronCache` call.
- Both `CancellationToken` parameters are unused.
- `AppSettings` is injected but not used.
