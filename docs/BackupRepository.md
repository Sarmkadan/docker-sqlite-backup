# BackupRepository

`BackupRepository` serves as the central persistence layer for the docker-sqlite-backup system, encapsulating all database operations related to backup schedules, backup results, rotation policies, restore verifications, and backup jobs. It abstracts the underlying SQLite data store and provides asynchronous methods for creating, reading, updating, and deleting domain entities, along with initialization and health-check capabilities.

## API

### Constructor

```csharp
public BackupRepository(string connectionString)
```

Creates a new instance of the repository configured to use the specified SQLite database.

**Parameters:**
- `connectionString` — The connection string pointing to the target SQLite database file.

**Exceptions:**
- `ArgumentNullException` — Thrown when `connectionString` is null or empty.

---

### InitializeAsync

```csharp
public async Task InitializeAsync()
```

Ensures that all required database tables, indexes, and schema objects exist. This method is idempotent and safe to call multiple times; it will not drop or alter existing data.

**Exceptions:**
- `SqliteException` — Thrown when the underlying database file cannot be accessed or the schema cannot be applied.

---

### HealthCheckAsync

```csharp
public async Task<bool> HealthCheckAsync()
```

Performs a lightweight connectivity and integrity check against the database. Returns `true` if the database is reachable and responds to a simple query; otherwise returns `false`.

**Return value:**
- `bool` — `true` when healthy, `false` otherwise.

**Exceptions:**
- This method does not throw by design; all errors are caught and result in a `false` return.

---

### CreateScheduleAsync

```csharp
public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)
```

Persists a new backup schedule to the database. The `schedule` object must have its `Id` set to a default value (e.g., `null` or empty); the database assigns a unique identifier upon insertion.

**Parameters:**
- `schedule` — The `BackupSchedule` entity to persist.

**Return value:**
- `BackupSchedule` — The same entity with its `Id` populated.

**Exceptions:**
- `ArgumentNullException` — Thrown when `schedule` is null.
- `InvalidOperationException` — Thrown when `schedule.Id` is already set to a non-default value.

---

### UpdateScheduleAsync

```csharp
public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)
```

Updates an existing backup schedule. The entity must have a valid `Id` that corresponds to a row already in the database.

**Parameters:**
- `schedule` — The `BackupSchedule` entity with updated field values.

**Return value:**
- `BackupSchedule` — The updated entity as persisted.

**Exceptions:**
- `ArgumentNullException` — Thrown when `schedule` is null.
- `KeyNotFoundException` — Thrown when no schedule with the given `Id` exists.

---

### DeleteScheduleAsync

```csharp
public async Task DeleteScheduleAsync(string scheduleId)
```

Removes a backup schedule and all associated backup results from the database. This is a cascading delete.

**Parameters:**
- `scheduleId` — The unique identifier of the schedule to delete.

**Exceptions:**
- `ArgumentNullException` — Thrown when `scheduleId` is null or empty.
- `KeyNotFoundException` — Thrown when no schedule with the given `scheduleId` exists.

---

### GetScheduleAsync

```csharp
public async Task<BackupSchedule?> GetScheduleAsync(string scheduleId)
```

Retrieves a single backup schedule by its unique identifier. Returns `null` if no matching schedule exists.

**Parameters:**
- `scheduleId` — The unique identifier of the schedule.

**Return value:**
- `BackupSchedule?` — The matching schedule, or `null`.

---

### GetAllSchedulesAsync

```csharp
public async Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync()
```

Returns every backup schedule stored in the database, regardless of its active status.

**Return value:**
- `IEnumerable<BackupSchedule>` — All schedules, which may be an empty collection.

---

### GetActiveSchedulesAsync

```csharp
public async Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync()
```

Returns only those backup schedules whose `IsActive` property is set to `true`.

**Return value:**
- `IEnumerable<BackupSchedule>` — Active schedules, which may be an empty collection.

---

### CreateBackupResultAsync

```csharp
public async Task<BackupResult> CreateBackupResultAsync(BackupResult result)
```

Records a new backup result associated with a schedule. The `result` must reference a valid `ScheduleId` and have a default `Id`.

**Parameters:**
- `result` — The `BackupResult` entity to persist.

**Return value:**
- `BackupResult` — The entity with its `Id` assigned.

**Exceptions:**
- `ArgumentNullException` — Thrown when `result` is null.
- `InvalidOperationException` — Thrown when `result.Id` is already set or `ScheduleId` does not reference an existing schedule.

---

### UpdateBackupResultAsync

```csharp
public async Task<BackupResult> UpdateBackupResultAsync(BackupResult result)
```

Updates an existing backup result (e.g., to record completion status, file size, or error messages).

**Parameters:**
- `result` — The `BackupResult` entity with updated fields.

**Return value:**
- `BackupResult` — The updated entity.

**Exceptions:**
- `ArgumentNullException` — Thrown when `result` is null.
- `KeyNotFoundException` — Thrown when no result with the given `Id` exists.

---

### DeleteBackupResultAsync

```csharp
public async Task DeleteBackupResultAsync(string resultId)
```

Removes a single backup result record. Does not affect the parent schedule.

**Parameters:**
- `resultId` — The unique identifier of the result to delete.

**Exceptions:**
- `ArgumentNullException` — Thrown when `resultId` is null or empty.
- `KeyNotFoundException` — Thrown when no result with the given `resultId` exists.

---

### GetBackupResultAsync

```csharp
public async Task<BackupResult?> GetBackupResultAsync(string resultId)
```

Retrieves a single backup result by its identifier.

**Parameters:**
- `resultId` — The unique identifier of the result.

**Return value:**
- `BackupResult?` — The matching result, or `null`.

---

### GetBackupHistoryAsync

```csharp
public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(string scheduleId, int limit = 50)
```

Returns the most recent backup results for a given schedule, ordered by completion time descending. The `limit` parameter caps the number of returned rows.

**Parameters:**
- `scheduleId` — The schedule whose history to retrieve.
- `limit` — Maximum number of results to return (default 50).

**Return value:**
- `IEnumerable<BackupResult>` — The history entries, which may be empty.

**Exceptions:**
- `ArgumentNullException` — Thrown when `scheduleId` is null or empty.

---

### GetRotationPolicyAsync

```csharp
public async Task<RotationPolicy?> GetRotationPolicyAsync()
```

Retrieves the global backup rotation policy. There is at most one policy; returns `null` if none has been saved.

**Return value:**
- `RotationPolicy?` — The current policy, or `null`.

---

### SaveRotationPolicyAsync

```csharp
public async Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy)
```

Persists the global rotation policy, overwriting any previously saved policy. This is an upsert operation.

**Parameters:**
- `policy` — The `RotationPolicy` to save.

**Return value:**
- `RotationPolicy` — The saved policy.

**Exceptions:**
- `ArgumentNullException` — Thrown when `policy` is null.

---

### SaveRestoreVerificationAsync

```csharp
public async Task<RestoreVerification> SaveRestoreVerificationAsync(RestoreVerification verification)
```

Records a restore verification attempt. Each call inserts a new row; historical verifications are retained.

**Parameters:**
- `verification` — The `RestoreVerification` entity to persist.

**Return value:**
- `RestoreVerification` — The entity with its `Id` assigned.

**Exceptions:**
- `ArgumentNullException` — Thrown when `verification` is null.

---

### GetVerificationHistoryAsync

```csharp
public async Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(int limit = 20)
```

Returns the most recent restore verification records, ordered by timestamp descending.

**Parameters:**
- `limit` — Maximum number of records to return (default 20).

**Return value:**
- `IEnumerable<RestoreVerification>` — The verification history, which may be empty.

---

### CreateBackupJobAsync

```csharp
public async Task<BackupJob> CreateBackupJobAsync(BackupJob job)
```

Persists a new backup job record representing a discrete execution of a backup schedule. The `job` must reference a valid `ScheduleId`.

**Parameters:**
- `job` — The `BackupJob` entity to create.

**Return value:**
- `BackupJob` — The entity with its `Id` assigned.

**Exceptions:**
- `ArgumentNullException` — Thrown when `job` is null.
- `InvalidOperationException` — Thrown when `ScheduleId` does not reference an existing schedule.

---

### UpdateBackupJobAsync

```csharp
public async Task<BackupJob> UpdateBackupJobAsync(BackupJob job)
```

Updates an existing backup job (e.g., to mark it as completed or record progress).

**Parameters:**
- `job` — The `BackupJob` entity with updated fields.

**Return value:**
- `BackupJob` — The updated entity.

**Exceptions:**
- `ArgumentNullException` — Thrown when `job` is null.
- `KeyNotFoundException` — Thrown when no job with the given `Id` exists.

---

## Usage

### Example 1: Creating a schedule and recording a backup result

```csharp
var repo = new BackupRepository("Data Source=backups.db");
await repo.InitializeAsync();

var schedule = new BackupSchedule
{
    Name = "Daily PostgreSQL",
    CronExpression = "0 2 * * *",
    DatabasePath = "/data/pg.sqlite",
    IsActive = true
};

schedule = await repo.CreateScheduleAsync(schedule);

var result = new BackupResult
{
    ScheduleId = schedule.Id,
    StartTime = DateTime.UtcNow.AddHours(-1),
    EndTime = DateTime.UtcNow,
    FileSizeBytes = 15_728_640,
    Status = BackupStatus.Success
};

await repo.CreateBackupResultAsync(result);
```

### Example 2: Retrieving active schedules and checking health

```csharp
var repo = new BackupRepository("Data Source=backups.db");

bool isHealthy = await repo.HealthCheckAsync();
if (!isHealthy)
{
    Console.WriteLine("Database is not reachable.");
    return;
}

IEnumerable<BackupSchedule> activeSchedules = await repo.GetActiveSchedulesAsync();

foreach (var s in activeSchedules)
{
    var history = await repo.GetBackupHistoryAsync(s.Id, limit: 5);
    Console.WriteLine($"Schedule '{s.Name}' has {history.Count()} recent results.");
}
```

---

## Notes

- **Thread safety:** The repository itself does not manage connection pooling or enforce mutual exclusion. SQLite’s serialized mode or external synchronization (e.g., a semaphore) should be employed when multiple threads or processes access the same database file concurrently.
- **Idempotency of `InitializeAsync`:** Repeated calls are safe and do not alter existing data. This method is suitable for use in application startup routines.
- **Cascading deletes:** Deleting a schedule via `DeleteScheduleAsync` removes all associated backup results. Backup jobs tied to that schedule are also removed.
- **Null returns:** `GetScheduleAsync`, `GetBackupResultAsync`, and `GetRotationPolicyAsync` return `null` when no matching record exists. Callers must guard against null references before accessing members of the returned objects.
- **Default limits:** `GetBackupHistoryAsync` and `GetVerificationHistoryAsync` apply default row limits (50 and 20, respectively) to prevent unbounded result sets. Pass an explicit `limit` to override.
- **Upsert semantics:** `SaveRotationPolicyAsync` overwrites any existing policy; there is no separate update method. The same upsert behavior applies to `SaveRestoreVerificationAsync` in the sense that each call inserts a new row rather than modifying an existing one.
- **Entity identity:** Methods that create entities (`CreateScheduleAsync`, `CreateBackupResultAsync`, `CreateBackupJobAsync`, `SaveRestoreVerificationAsync`) expect the entity’s `Id` to be unset. Passing an entity with a pre-populated `Id` will result in an `InvalidOperationException`.
