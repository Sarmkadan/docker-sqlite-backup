# BackupRepository

`BackupRepository` (`src/Data/BackupRepository.cs`) is the SQLite persistence layer for backup metadata. It stores backup schedules, backup results, backup jobs, rotation policies, and restore verifications in a single SQLite database file. It implements `IBackupRepository`, and the services in `src/Services` (`BackupService`, `RotationService`, `VerificationService`, `ScheduleService`, and others) depend on that interface. Code should depend on `IBackupRepository` and only construct `BackupRepository` directly at the composition root or in tests.

The class stores metadata only. Backup files themselves are handled by the storage backends.

## Responsibilities

- Create the metadata schema on demand (`InitializeAsync`).
- Report whether the database is reachable (`HealthCheckAsync`).
- Provide CRUD for schedules, backup results, and backup jobs.
- Provide an upsert for the rotation policy of a schedule.
- Record and read restore verification history.
- Map between SQLite rows and the domain types in `DockerSqliteBackup.Domain`.

## Storage model

`InitializeAsync` creates these tables with `CREATE TABLE IF NOT EXISTS`:

| Table | Primary key | Notes |
|-------|-------------|-------|
| `BackupSchedules` | `Id` | `Name` is `UNIQUE`. |
| `BackupResults` | `Id` | `ScheduleId` and `BackupJobId` are `NOT NULL`. |
| `RotationPolicies` | `Id` | `ScheduleId` is `UNIQUE`, so each schedule has at most one policy. |
| `RestoreVerifications` | `Id` | `BackupResultId` is `NOT NULL`. |
| `BackupJobs` | `Id` | `ScheduleId` is `NOT NULL`. |

The schema declares foreign keys from results, policies, verifications, and jobs to their parent rows. This class never executes `PRAGMA foreign_keys = ON`, so SQLite does not enforce them on connections it opens unless the environment enables enforcement. See [Notes](#notes).

## API

### Constructor

```csharp
public BackupRepository(string connectionString, ILogger<BackupRepository> logger)
```

Creates a repository that opens a new SQLite connection for each operation.

**Parameters:**
- `connectionString` — Microsoft.Data.Sqlite connection string, for example `Data Source=/data/backups.db`.
- `logger` — Logger used for schema, health-check, and error messages.

**Exceptions:**
- `ArgumentNullException` — `connectionString` or `logger` is `null`.
- `ArgumentException` — `connectionString` is an empty string.

---

### InitializeAsync

```csharp
public async Task InitializeAsync()
```

Creates the five metadata tables if they do not already exist and logs `Database schema initialized successfully` on success. Existing tables and rows are not modified. The method does not create indexes or apply schema migrations, so changes to an existing table's definition are not applied to an existing database.

**Exceptions:**
- `SqliteException` — The database file cannot be opened or a `CREATE TABLE` statement fails. Not caught.

---

### HealthCheckAsync

```csharp
public async Task<bool> HealthCheckAsync()
```

Opens a connection and runs `SELECT 1`. Returns `true` if both succeed.

**Return value:**
- `bool` — `true` if the database is reachable. `false` otherwise.

**Exceptions:**
- None. Any exception is logged at `Error` level and converted to `false`.

This check verifies connectivity only. It does not run `PRAGMA integrity_check`; use `IIntegrityCheckerService` for that.

---

### Schedule operations

#### CreateScheduleAsync

```csharp
public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)
```

Inserts `schedule` as a new row and returns the same instance. `BackupSchedule.Id` is a `Guid` that defaults to `Guid.NewGuid()`, so callers normally do not set it.

**Exceptions:**
- `ArgumentNullException` — `schedule` is `null`.
- `SqliteException` — The `Id` already exists, or `Name` duplicates an existing schedule (`UNIQUE` constraint).

#### UpdateScheduleAsync

```csharp
public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)
```

Overwrites every mutable column of the row whose `Id` matches `schedule.Id`, then returns `schedule`. The method does not check whether a row was matched. If no row has that `Id`, nothing is written and the input is still returned.

**Exceptions:**
- `ArgumentNullException` — `schedule` is `null`.

#### DeleteScheduleAsync

```csharp
public async Task DeleteScheduleAsync(Guid scheduleId)
```

Deletes the `BackupSchedules` row with the given `Id`. It does not delete related `BackupResults`, `BackupJobs`, or `RotationPolicies` rows. Deleting a missing schedule is a no-op.

#### GetScheduleAsync

```csharp
public async Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId)
```

Returns the schedule with the given `Id`, or `null` if none exists.

#### GetAllSchedulesAsync

```csharp
public async Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync()
```

Returns every schedule, active or not, ordered by `CreatedAt` descending. Returns an empty collection if there are none.

#### GetActiveSchedulesAsync

```csharp
public async Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync()
```

Returns schedules with `IsActive = 1`, ordered by `Name` ascending. Returns an empty collection if there are none.

---

### Backup result operations

#### CreateBackupResultAsync

```csharp
public async Task<BackupResult> CreateBackupResultAsync(BackupResult result)
```

Inserts `result` as a new row and returns the same instance. `BackupResult.Id` defaults to `Guid.NewGuid()`.

**Exceptions:**
- `ArgumentNullException` — `result` is `null`.
- `SqliteException` — The `Id` already exists.

#### UpdateBackupResultAsync

```csharp
public async Task<BackupResult> UpdateBackupResultAsync(BackupResult result)
```

Overwrites the mutable columns of the row whose `Id` matches `result.Id` and returns `result`. Like `UpdateScheduleAsync`, it does not report whether a row was matched. `ScheduleId` and `BackupJobId` are not included in the `UPDATE` statement, so they cannot be changed here.

**Exceptions:**
- `ArgumentNullException` — `result` is `null`.

#### DeleteBackupResultAsync

```csharp
public async Task DeleteBackupResultAsync(Guid resultId)
```

Deletes the `BackupResults` row with the given `Id`. Deleting a missing result is a no-op. Verification rows that reference the result are not deleted by this method.

#### GetBackupResultAsync

```csharp
public async Task<BackupResult?> GetBackupResultAsync(Guid resultId)
```

Returns the result with the given `Id`, or `null` if none exists.

#### GetBackupHistoryAsync

```csharp
public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit)
```

Returns up to `limit` results for `scheduleId`, ordered by `StartedAt` descending (newest first). `limit` has no default and is passed to SQLite as `LIMIT`.

**Parameters:**
- `scheduleId` — The schedule whose results to return.
- `limit` — Maximum number of rows to return.

---

### Rotation policy operations

#### GetRotationPolicyAsync

```csharp
public async Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId)
```

Returns the rotation policy for `scheduleId`, or `null` if the schedule has none.

#### SaveRotationPolicyAsync

```csharp
public async Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy)
```

Upserts the policy keyed by `policy.ScheduleId`:

1. Looks up the existing policy for `policy.ScheduleId`.
2. If one exists, sets `policy.Id` to the existing row's `Id` (this mutates the argument), then updates the row.
3. Otherwise inserts `policy` using `policy.Id`.

Returns `policy`. The lookup and the write are two separate statements without a transaction, so two concurrent saves for the same schedule can both try to insert and one will fail with a `UNIQUE` violation on `ScheduleId`.

**Exceptions:**
- `ArgumentNullException` — `policy` is `null`.
- `SqliteException` — A concurrent insert won the race described above.

---

### Restore verification operations

#### SaveRestoreVerificationAsync

```csharp
public async Task<RestoreVerification> SaveRestoreVerificationAsync(RestoreVerification verification)
```

Inserts a verification record and returns the same instance. Each call adds a row; existing verifications are never updated.

**Exceptions:**
- `ArgumentNullException` — `verification` is `null`.
- `SqliteException` — The `Id` already exists.

#### GetVerificationHistoryAsync

```csharp
public async Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId)
```

Returns every verification for `backupResultId`, ordered by `StartedAt` descending. The method has no row limit.

---

### Backup job operations

#### CreateBackupJobAsync

```csharp
public async Task<BackupJob> CreateBackupJobAsync(BackupJob job)
```

Inserts `job` and returns the same instance. `BackupJob.Id` defaults to `Guid.NewGuid()`.

**Exceptions:**
- `ArgumentNullException` — `job` is `null`.
- `SqliteException` — The `Id` already exists.

#### UpdateBackupJobAsync

```csharp
public async Task<BackupJob> UpdateBackupJobAsync(BackupJob job)
```

Overwrites `Status`, `StartedAt`, `CompletedAt`, `RetryCount`, and `IsProcessing` for the row whose `Id` matches `job.Id`, then returns `job`. Like the other `Update*` methods, it does not report whether a row was matched.

**Exceptions:**
- `ArgumentNullException` — `job` is `null`.

#### GetBackupJobAsync

```csharp
public async Task<BackupJob?> GetBackupJobAsync(Guid jobId)
```

Returns the job with the given `Id`, or `null` if none exists.

---

## Usage

### Example 1: Initialize, create a schedule, and record a backup

```csharp
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

ILogger<BackupRepository> logger = loggerFactory.CreateLogger<BackupRepository>();
var repository = new BackupRepository("Data Source=/data/backups.db", logger);
await repository.InitializeAsync();

var schedule = await repository.CreateScheduleAsync(new BackupSchedule
{
    Name = "nightly-app-db",
    DatabasePath = "/data/app.db",
    CronExpression = "0 2 * * *",
    RetentionDays = 14
});

var job = await repository.CreateBackupJobAsync(new BackupJob
{
    ScheduleId = schedule.Id
});

var startedAt = DateTime.UtcNow;

// ... perform the backup ...

await repository.CreateBackupResultAsync(new BackupResult
{
    ScheduleId = schedule.Id,
    BackupJobId = job.Id,
    BackupFilePath = "/backups/app-20261025-020000.db.gz",
    BackupFileSizeBytes = 15_728_640,
    Checksum = "<sha256 of the backup file>",
    StartedAt = startedAt,
    CompletedAt = DateTime.UtcNow,
    DurationMilliseconds = 120_000,
    Status = (int)BackupStatus.Success
});

job.Status = (int)BackupStatus.Success;
job.CompletedAt = DateTime.UtcNow;
await repository.UpdateBackupJobAsync(job);
```

### Example 2: Check health and read recent history

```csharp
var repository = new BackupRepository("Data Source=/data/backups.db", logger);

if (!await repository.HealthCheckAsync())
{
    Console.WriteLine("Database is not reachable.");
    return;
}

foreach (var schedule in await repository.GetActiveSchedulesAsync())
{
    var history = (await repository.GetBackupHistoryAsync(schedule.Id, limit: 5)).ToList();
    Console.WriteLine($"Schedule '{schedule.Name}' has {history.Count} recent results.");
}
```

### Example 3: Upsert a rotation policy

```csharp
var policy = await repository.GetRotationPolicyAsync(schedule.Id)
    ?? new RotationPolicy { ScheduleId = schedule.Id };

policy.MaxBackupCount = 7;

// Inserts on first call, updates on later calls. Sets policy.Id to the stored Id.
await repository.SaveRotationPolicyAsync(policy);
```

---

## Notes

- **Connections:** Every method opens and disposes its own `SqliteConnection`. The repository holds no connection state, so one instance can be shared, but concurrent writers still contend for SQLite's write lock and can receive `SqliteException` (for example, "database is locked").
- **Foreign keys:** Because `PRAGMA foreign_keys` is never enabled here, the declared foreign keys are not enforced by this class. Deleting a schedule leaves its results, jobs, and policy in place. Callers that need cascading cleanup must delete those rows explicitly, or enable enforcement on the connection, before relying on it.
- **Exceptions:** Only `HealthCheckAsync` swallows errors. Every other method lets `SqliteException` propagate to the caller.
- **Update semantics:** `Update*` methods do not check the affected row count. A stale or wrong `Id` results in a silent no-op that still returns the input object.
- **Identifiers:** IDs are `Guid` values stored as text. Callers supply them on creation (the default is `Guid.NewGuid()`).
- **Timestamps:** `DateTime` values are converted with `ToUniversalTime()` before being written. Values read back are always `DateTimeKind.Utc`. Nullable timestamps are stored as `NULL` and read back as `null`.
- **Empty strings for nulls:** Several nullable text properties (for example `Description`, `Checksum`, `ErrorMessage`, `StackTrace`, `Notes`, `S3ObjectKey`, `NotificationEmails`) are written as `""` when `null`. They are read back as `""`, not `null`.
- **Null returns:** `GetScheduleAsync`, `GetBackupResultAsync`, `GetRotationPolicyAsync`, and `GetBackupJobAsync` return `null` when no row matches. Check for `null` before accessing members.
- **Collections:** Methods returning `IEnumerable<T>` materialize the full result into a `List<T>`. `GetBackupHistoryAsync` is the only query with a caller-controlled limit. Use it for large tables.

## Related

- `src/Data/IBackupRepository.cs` — the interface this class implements.
- `src/Domain/BackupSchedule.cs`, `BackupResult.cs`, `BackupJob.cs`, `RotationPolicy.cs`, `RestoreVerification.cs` — the entities persisted here.
- `src/Constants/BackupStatus.cs` — values for the `Status` columns.
