# ScheduleService

The `ScheduleService` class provides CRUD operations and lifecycle management for backup schedules in the docker-sqlite-backup system. It handles creation, retrieval, updating, deletion, activation, and deactivation of `BackupSchedule` instances, as well as cron expression validation and next-execution-time calculation. All data-modifying operations are asynchronous, while validation and time calculation are synchronous.

## API

### `public ScheduleService`

Constructor. Initializes a new instance of the service. No parameters.

---

### `public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)`

Creates a new backup schedule and persists it.

- **Parameters**  
  `schedule` – A `BackupSchedule` object containing the schedule configuration (e.g., cron expression, database path, retention policy).
- **Returns**  
  The created `BackupSchedule` with its generated identifier and any server-assigned defaults.
- **Throws**  
  `ArgumentNullException` if `schedule` is `null`.  
  `InvalidOperationException` if a schedule with the same identifier already exists.  
  `ArgumentException` if the schedule’s cron expression is invalid.

---

### `public Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)`

Non-async overload that returns a `Task<BackupSchedule>`. Behaves identically to the async version but does not use the `async` keyword internally. Callers should still `await` the returned task.

- **Parameters**  
  Same as the async overload.
- **Returns**  
  A `Task` that resolves to the created `BackupSchedule`.
- **Throws**  
  Same as the async overload.

---

### `public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)`

Updates an existing backup schedule.

- **Parameters**  
  `schedule` – A `BackupSchedule` object with the updated fields. The schedule’s identifier must match an existing schedule.
- **Returns**  
  The updated `BackupSchedule` after persistence.
- **Throws**  
  `ArgumentNullException` if `schedule` is `null`.  
  `KeyNotFoundException` if no schedule with the given identifier exists.  
  `ArgumentException` if the updated cron expression is invalid.

---

### `public async Task DeleteScheduleAsync(Guid scheduleId)`

Deletes a backup schedule by its identifier.

- **Parameters**  
  `scheduleId` – The unique identifier of the schedule to delete.
- **Returns**  
  `Task` (no value).
- **Throws**  
  `KeyNotFoundException` if no schedule with the given identifier exists.

---

### `public async Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId)`

Retrieves a single backup schedule by its identifier.

- **Parameters**  
  `scheduleId` – The unique identifier of the schedule.
- **Returns**  
  The matching `BackupSchedule`, or `null` if no schedule with that identifier exists.
- **Throws**  
  None.

---

### `public async Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync()`

Retrieves all schedules that are currently active (i.e., enabled and not past their end date).

- **Parameters**  
  None.
- **Returns**  
  An `IEnumerable<BackupSchedule>` containing the active schedules.
- **Throws**  
  None.

---

### `public async Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync()`

Retrieves all backup schedules, regardless of activation state.

- **Parameters**  
  None.
- **Returns**  
  An `IEnumerable<BackupSchedule>` containing every schedule.
- **Throws**  
  None.

---

### `public bool ValidateCronExpression(string expression)`

Validates a cron expression string.

- **Parameters**  
  `expression` – The cron expression to validate (e.g., `"0 2 * * *"`).
- **Returns**  
  `true` if the expression is syntactically correct; otherwise `false`.
- **Throws**  
  `ArgumentNullException` if `expression` is `null`.

---

### `public DateTime? GetNextExecutionTime(string cronExpression, DateTime? fromTime = null)`

Calculates the next scheduled execution time for a given cron expression.

- **Parameters**  
  `cronExpression` – A valid cron expression.  
  `fromTime` – Optional starting point for the calculation. If `null`, the current UTC time is used.
- **Returns**  
  A `DateTime?` representing the next execution time in UTC, or `null` if the expression does not yield a future time (e.g., a one-time expression in the past).
- **Throws**  
  `ArgumentNullException` if `cronExpression` is `null`.  
  `ArgumentException` if `cronExpression` is invalid.

---

### `public async Task DeactivateScheduleAsync(Guid scheduleId)`

Deactivates a backup schedule, preventing it from running.

- **Parameters**  
  `scheduleId` – The identifier of the schedule to deactivate.
- **Returns**  
  `Task` (no value).
- **Throws**  
  `KeyNotFoundException` if no schedule with the given identifier exists.

---

### `public async Task ActivateScheduleAsync(Guid scheduleId)`

Activates a backup schedule, allowing it to run according to its cron expression.

- **Parameters**  
  `scheduleId` – The identifier of the schedule to activate.
- **Returns**  
  `Task` (no value).
- **Throws**  
  `KeyNotFoundException` if no schedule with the given identifier exists.  
  `InvalidOperationException` if the schedule’s cron expression is invalid or its end date is in the past.

## Usage

### Example 1: Creating and activating a schedule

```csharp
var service = new ScheduleService();
var newSchedule = new BackupSchedule
{
    DatabasePath = "/data/mydb.sqlite",
    CronExpression = "0 3 * * *",   // daily at 3 AM
    RetentionDays = 7
};

// Create the schedule (async overload)
BackupSchedule created = await service.CreateScheduleAsync(newSchedule);
Console.WriteLine($"Created schedule with ID: {created.Id}");

// Activate it
await service.ActivateScheduleAsync(created.Id);
```

### Example 2: Validating a cron expression and retrieving active schedules

```csharp
var service = new ScheduleService();

string cron = "*/5 * * * *"; // every 5 minutes
if (!service.ValidateCronExpression(cron))
{
    Console.WriteLine("Invalid cron expression");
    return;
}

DateTime? nextRun = service.GetNextExecutionTime(cron);
Console.WriteLine($"Next execution: {nextRun}");

// Get all currently active schedules
IEnumerable<BackupSchedule> active = await service.GetActiveSchedulesAsync();
foreach (var s in active)
{
    Console.WriteLine($"Schedule {s.Id} – next run: {s.NextExecutionTime}");
}
```

## Notes

- **Thread safety**: `ScheduleService` is not thread-safe. Concurrent calls to mutation methods (`CreateScheduleAsync`, `UpdateScheduleAsync`, `DeleteScheduleAsync`, `ActivateScheduleAsync`, `DeactivateScheduleAsync`) from multiple threads may lead to inconsistent state or data corruption. External synchronization (e.g., a lock) is required when the same instance is used concurrently.
- **Cron validation**: `ValidateCronExpression` performs only syntactic validation. It does not check whether the expression will ever produce a valid future time; use `GetNextExecutionTime` for that purpose.
- **Null handling**: All methods that accept a `BackupSchedule` or a string parameter throw `ArgumentNullException` when the argument is `null`. Identifier-based methods throw `KeyNotFoundException` when the identifier does not match an existing schedule.
- **Time zone**: `GetNextExecutionTime` returns times in UTC. The cron expression is evaluated against UTC; no time zone conversion is applied.
- **Activation constraints**: `ActivateScheduleAsync` will throw `InvalidOperationException` if the schedule’s cron expression is invalid or if its end date (if set) is in the past. Deactivation does not perform these checks.
- **Non-async overload**: The non-async `CreateScheduleAsync` overload is provided for compatibility with synchronous call sites. It still returns a `Task` and should be awaited. Prefer the `async` overload in new code.
