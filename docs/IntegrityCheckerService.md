# IntegrityCheckerService

`IntegrityCheckerService` (`src/Services/IntegrityCheckerService.cs`, namespace `DockerSqliteBackup.Services`) runs SQLite integrity checks on live databases and on backup files. It implements `IIntegrityCheckerService`.

Every check opens the file through a read-only connection (`Mode=ReadOnly`), so the checked file is never modified.

## Check levels

| PRAGMA | Purpose | Run by |
|---|---|---|
| `quick_check` | Fast structural scan (B-tree pointers, page sizes) | Always |
| `integrity_check` | Full record-level validation of every B-tree cell | `fullCheck = true` |
| `foreign_key_check` | Verifies all foreign key constraints | `fullCheck = true` |

## Constructor

```csharp
public IntegrityCheckerService(ILogger<IntegrityCheckerService> logger)
```

Takes a logger for progress and failure messages. No other dependencies.

## Methods

### `CheckDatabaseAsync`

```csharp
public async Task<IntegrityReport> CheckDatabaseAsync(
    string databasePath,
    bool fullCheck = true,
    CancellationToken ct = default)
```

Runs the full check sequence and returns an `IntegrityReport`.

- **Parameters**
  - `databasePath`: path to the SQLite file.
  - `fullCheck`: when `true`, runs `integrity_check` and `foreign_key_check` after `quick_check`. When `false`, only `quick_check` runs.
  - `ct`: cancellation token passed to all database calls.
- **Sequence**
  1. Verify the file exists.
  2. Open a read-only connection.
  3. Collect metadata (see below).
  4. Run `quick_check`.
  5. If `fullCheck`, run `integrity_check` and `foreign_key_check`.
  6. Set `Duration` and log the result.
- **Quick-only mode**: `PassedFullCheck` is copied from `PassedQuickCheck`, and `PassedForeignKeyCheck` is set to `true`. Foreign key violations are not checked in this mode.
- **Return value**: the populated report. `IsHealthy` on the report is derived from the individual pass flags.
- **Exceptions**
  - `ArgumentException` (from `ThrowIfNullOrWhiteSpace`) if `databasePath` is null, empty, or whitespace.
  - `FileNotFoundException` if the file does not exist. A warning is logged first.
  - Any exception from opening or querying the database (for example `SqliteException`). Each stage logs an error and rethrows.
  - `OperationCanceledException` if `ct` is cancelled.

### `QuickCheckAsync`

```csharp
public async Task<bool> QuickCheckAsync(string databasePath, CancellationToken ct = default)
```

Opens a read-only connection and runs only `PRAGMA quick_check`. Returns `true` if the result is `ok`.

- **Exceptions**: `ArgumentException` for a null or blank path, `FileNotFoundException` if the file is missing, and SQLite or cancellation exceptions as above.
- **Difference from `CheckDatabaseAsync`**: no metadata is collected and no logging is done.

### `CheckBackupFileAsync`

```csharp
public Task<IntegrityReport> CheckBackupFileAsync(string backupFilePath, CancellationToken ct = default)
```

Calls `CheckDatabaseAsync(backupFilePath, fullCheck: true, ct)`. Always runs the full check, so it is the right call for validating a backup before trusting it for restore.

## Metadata collected

Filled in by `CheckDatabaseAsync` before the checks run:

- `PageSize`: `PRAGMA page_size`
- `PageCount`: `PRAGMA page_count`
- `FreePageCount`: `PRAGMA freelist_count`
- `JournalMode`: `PRAGMA journal_mode`
- `TableCount`: number of rows in `sqlite_master` with `type='table'`
- `HasUncheckpointedWal`: only set when the journal mode is `wal`. Runs `PRAGMA wal_checkpoint(PASSIVE)` and sets the flag when the log frame count is greater than the checkpointed frame count. This is a read of the WAL state. Because the check runs on a read-only connection, the checkpoint does not write to the main file.

## Result format

Each check stores a pass flag and an error text in the report:

- `PRAGMA` results are read row by row. Any row that is not `ok` is appended to the error text, one per line. The pass flag is `true` when no such rows exist, and the error text is `null` in that case.
- `foreign_key_check` rows are formatted as `table=<name> rowid=<id> parent=<table>`. The `rowid` field is `?` when the row has no rowid.

## Logging

- Information: start of a check (path and `fullCheck`), and completion with elapsed milliseconds and `Healthy`.
- Warning: file not found.
- Error: any failure during connection, metadata collection, or a specific check. The exception is attached.

## Usage

```csharp
// Injected via DI
var report = await checker.CheckDatabaseAsync("/data/app.db");
if (!report.IsHealthy)
{
    logger.LogWarning("Integrity problems: {Quick} {Full} {Fk}",
        report.QuickCheckErrors, report.FullCheckErrors, report.ForeignKeyErrors);
}

// Fast liveness check
bool ok = await checker.QuickCheckAsync("/data/app.db");

// Verify a backup before restoring
var backupReport = await checker.CheckBackupFileAsync("/backups/app-2026-10-01.db");
```

## Notes

- Quick-only mode does not check foreign keys. Use `fullCheck: true` when FK consistency matters.
- `integrity_check` reads every page and can be slow on large databases. Pass a cancellation token with a timeout for scheduled runs.
- The check opens the database read-only. A database that is locked by an exclusive writer may fail to open, and that failure is rethrown rather than reported in the `IntegrityReport`.
