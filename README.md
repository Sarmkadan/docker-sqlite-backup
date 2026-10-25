# Docker SQLite Backup

.NET 10 worker service that automates SQLite backups inside Docker: cron scheduling, compression, AES encryption, checksum/integrity verification, rotation, and upload to local/S3/Azure storage.

## Quick Start

The easiest way to get started is using `docker-compose`:

```yaml
version: '3.8'
services:
  sqlite-backup:
    build:
      context: .
      dockerfile: Dockerfile
    image: sqlite-backup:latest
    container_name: sqlite-backup
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: "http://0.0.0.0:8080"
      AppSettings__DatabasePath: /data/app.db
      AppSettings__LocalStoragePath: /backups
      AppSettings__MaxConcurrentBackups: "3"
      AppSettings__BackupTimeoutSeconds: "3600"
      AppSettings__EnableVerificationByDefault: "true"
      AppSettings__RetentionDays: "30"
      AppSettings__MaxBackupCount: "10"
      Logging__LogLevel__Default: "Information"
    volumes:
      - ./data:/data
      - ./backups:/backups
      - ./appsettings.json:/app/appsettings.json:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
    networks:
      - backup-network

networks:
  backup-network:
    driver: bridge
```

### Required Environment Variables

The following environment variables are used (with defaults as shown in the docker-compose.yml):

- `AppSettings__DatabasePath`: Path to the SQLite database file (default: `/data/app.db`)
- `AppSettings__LocalStoragePath`: Path to store backups (default: `/backups`)
- `AppSettings__MaxConcurrentBackups`: Maximum number of concurrent backups (default: `3`)
- `AppSettings__BackupTimeoutSeconds`: Timeout for each backup in seconds (default: `3600`)
- `AppSettings__EnableVerificationByDefault`: Whether to enable verification by default (default: `true`)
- `AppSettings__RetentionDays`: Number of days to retain backups (default: `30`)
- `AppSettings__MaxBackupCount`: Maximum number of backups to retain (default: `10`)
- `AppSettings__DryRun`: Log the actions each schedule would take (backup, destination, rotation deletions) without writing, uploading, or deleting anything (default: `false`)
- `Logging__LogLevel__Default`: Default log level (default: `Information`)

### Verifying the First Backup

After starting the stack with `docker compose up -d`, you can verify the first backup by:

1. Checking the logs: `docker compose logs -f sqlite-backup`
2. Looking for a log entry indicating a backup has been started and completed.
3. Checking the backups directory: `ls -la ./backups` (on the host) should show a backup file.

Alternatively, you can check the health endpoint: `curl http://localhost:8080/health` should return a 200 OK when the service is healthy.

## Restore a backup

Backups are plain files in the local storage directory (`AppSettings__LocalStoragePath`, `/backups` in the container, `./backups` on the host). File names follow `backup_<schedule>_<yyyy-MM-dd_HH-mm-ss>.sqlite`, with a `.gz` suffix when compression is enabled. Encrypted backups have an additional `.enc` suffix.

There is no restore command in the CLI. Restoring is a manual file copy with the application stopped.

1. **List the backups** (newest first):

   ```bash
   ls -lt ./backups
   # or from inside the container
   docker compose exec sqlite-backup ls -lt /backups
   ```

2. **Prepare the file.** Decompress `.gz` backups and check the result:

   ```bash
   gunzip -k ./backups/backup_<schedule>_<timestamp>.sqlite.gz
   sqlite3 ./backups/backup_<schedule>_<timestamp>.sqlite "PRAGMA integrity_check;"   # expect: ok
   ```

   `.enc` backups must be decrypted with the application's encryption key (`BACKUP_ENCRYPTION_KEY`) before they can be used. See `docs/encryption.md`.

3. **Stop the application** that uses the database, so it is not writing while you copy:

   ```bash
   docker compose stop <app-service>
   ```

4. **Keep the current database and copy the backup into the data volume.** The example below uses the `./data:/data` bind mount from the Quick Start, where the database path is `/data/app.db`:

   ```bash
   cp ./data/app.db ./data/app.db.pre-restore 2>/dev/null || true
   cp ./backups/backup_<schedule>_<timestamp>.sqlite ./data/app.db
   rm -f ./data/app.db-wal ./data/app.db-shm   # stale WAL files from the old database, if present
   ```

   If the data is in a named volume rather than a bind mount, use `docker cp ./backups/<file> <app-container>:/data/app.db` while the container is stopped.

5. **Restart the application:**

   ```bash
   docker compose start <app-service>
   # or: docker compose up -d <app-service>
   ```

## Retention configuration

**Current behaviour:**

- Deletion happens during rotation. Rotation uses one `RotationPolicy` row per backup schedule, stored in the `RotationPolicies` table. If a schedule has no row, rotation does nothing. No code in this repository creates a row outside of tests, so by default no backups are deleted.
- `AppSettings__RetentionDays` and `AppSettings__MaxBackupCount` are bound from configuration, but no service reads them. Setting them does not change what gets deleted.
- `RetentionPolicy` (`src/Domain/RetentionPolicy.cs`) models daily, weekly and monthly tiers through `DailyRetentionDays`, `WeeklyRetentionWeeks`, `MonthlyRetentionMonths` and `MaxTotalStorageBytes`. It is bound from the `RetentionPolicy` configuration section and validated at startup (keep-counts and storage cap must not be negative; at least one tier must be greater than zero). Nothing enforces it at runtime, so setting these values does not change what gets deleted.

**`RotationPolicy` fields** (`src/Domain/RotationPolicy.cs`, saved with `IRotationService.SaveRotationPolicyAsync`):

| Field | Default | Meaning |
|---|---|---|
| `Strategy` | `2` (`Combined`) | `RotationStrategy` value: `0` MaxFileCount, `1` MaxAge, `2` Combined, `3` NoRotation |
| `MaxBackupCount` | `10` | Count limit for the count rule. `0` means unlimited |
| `MaxAgeDays` | `30` | Age limit in days for the age rule |
| `MinimumBackupCount` | `3` | The newest N backups are never deleted |
| `VerifyBeforeDeletion` | `false` | Only delete backups whose status is verified |
| `DeleteFailedBackups` | `true` | Delete failed backups during rotation |

**Example configurations** (C#, applied once per schedule; `schedule.Id` is the schedule's GUID):

Keep the newest 10 backups:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.MaxFileCount,
    MaxBackupCount = 10,
    MinimumBackupCount = 3
});
```

Keep 30 days of backups, always keeping at least 3:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.MaxAge,
    MaxAgeDays = 30,
    MinimumBackupCount = 3
});
```

Keep up to 14 backups and delete anything older than 30 days, deleting only verified backups:

```csharp
await rotationService.SaveRotationPolicyAsync(new RotationPolicy
{
    ScheduleId = schedule.Id,
    Strategy = (int)RotationStrategy.Combined,
    MaxBackupCount = 14,
    MaxAgeDays = 30,
    MinimumBackupCount = 7,
    VerifyBeforeDeletion = true
});
```

`RotationStrategy` is in `DockerSqliteBackup.Constants`, and `RotationPolicy` is in `DockerSqliteBackup.Domain`. A 7-daily / 4-weekly layout is not expressible with `RotationPolicy` alone. It is the shape `RetentionPolicy` describes, but that class is not wired up yet.

## ScheduleService

`src/Services/ScheduleService.cs` - manages CRUD operations and lifecycle for backup schedules. It validates schedule configuration and cron expressions (via the Cronos library) before persisting them through `IBackupRepository`.

### Scheduling model

Each `BackupSchedule` carries a cron expression (`CronExpression` field) that defines when a backup runs. The service validates the expression with `CronExpression.Parse` from Cronos. Next-execution time is computed relative to the schedule's `NextScheduledRunTime` (falling back to `DateTime.UtcNow`). Schedules have an `IsActive` flag toggled by activate/deactivate methods.

### Key public methods

| Method | Description |
|---|---|
| `CreateScheduleAsync(BackupSchedule)` | Validates the schedule and cron expression, sets timestamps, persists via repository. Throws `InvalidScheduleException` or `InvalidCronExpressionException` on bad input. |
| `CreateScheduleAsync(string name, string databasePath)` | Convenience overload - builds a `BackupSchedule` from name and path, delegates to the full create. |
| `UpdateScheduleAsync(BackupSchedule)` | Validates and persists changes. Throws `ScheduleException` on repository failure. |
| `DeleteScheduleAsync(Guid)` | Removes a schedule by ID. |
| `GetScheduleAsync(Guid)` | Returns a single schedule or `null`. |
| `GetActiveSchedulesAsync()` | Returns all schedules where `IsActive` is true. |
| `GetAllSchedulesAsync()` | Returns every schedule. |
| `ActivateScheduleAsync(Guid)` | Sets `IsActive = true` and updates the timestamp. |
| `DeactivateScheduleAsync(Guid)` | Sets `IsActive = false` and updates the timestamp. |
| `ValidateCronExpression(string)` | Returns `true` if the expression is parseable by Cronos. |
| `GetNextExecutionTime(BackupSchedule)` | Computes the next occurrence from the schedule's cron expression, or `null` on parse failure. |

## BackupService

`src/Services/BackupService.cs` - implements `IBackupService`. It runs one backup for a `BackupSchedule` and returns a `BackupResult`. It is registered as a singleton by `AddBackupServices`, and `BackupWorker` calls it for each due schedule.

### Purpose and pipeline position

`BackupService` is the file-level step of the backup pipeline. `ScheduleService` decides when a backup is due, and `BackupWorker` calls `ExecuteBackupAsync`. On success, the worker runs verification and rotation, updates `LastBackupAt`, and publishes `BackupCompletedEvent`. On failure, it publishes a failure event. `BackupService` itself does not verify backups, rotate old files, or send notifications.

`ExecuteBackupAsync` runs these steps in order:

1. **Validate** the schedule. A null schedule throws `ArgumentNullException`. An invalid schedule throws `InvalidScheduleException`. A missing database file throws `DatabaseAccessException`.
2. **Choose a path** of `<AppContext.BaseDirectory>/backups/<schedule name>/backup_<timestamp>.sqlite`. The schedule name is sanitized for use as a directory name. The directory is created if needed.
3. **Snapshot** the database. Full mode copies the database with the SQLite Online Backup API from a read-only connection. Incremental mode (see below) first checkpoints the WAL.
4. **Encrypt** the file if `EnableEncryption` is set and a valid key is found (AES-256, `.enc` suffix). The key is read from `BACKUP_ENCRYPTION_KEY`, then from `AppSettings.EncryptionKey`. A missing or invalid key logs a warning or error and the backup continues unencrypted.
5. **Compress** the file with gzip (`.gz` suffix) if `CompressBackups` is set. If compression fails, the uncompressed file is kept and a warning is logged.
6. **Checksum** the final file with SHA-256 and record its size.
7. **Write a manifest** next to the backup as `<backup file>.meta.json`. This is best-effort: a failure is logged and the backup still succeeds.
8. **Upload** to remote storage if the schedule has a storage configuration that is not `LocalStorageConfiguration`. This goes through `IStorageService.UploadBackupAsync`. Upload errors are not caught, so the backup is marked failed. For S3, `IsStoredInS3` and `S3ObjectKey` are set on the result.
9. **Persist** the `BackupResult` in a `finally` block through `IBackupRepository.CreateBackupResultAsync`. A persistence failure is logged and not thrown.

If a step throws, the result's status is set to `Failed` with the error message and stack trace, and the exception is rethrown.

### Full and incremental modes

- **Full** (`BackupMode.Full`, the default): every run makes a complete snapshot.
- **Incremental** (`BackupMode.Incremental`): the service finds the most recent successful full result for the schedule, runs `PRAGMA wal_checkpoint(TRUNCATE)`, and copies the database again. The result's `BaseBackupResultId` is set to that full result. The file is still a complete copy of the current state, not a page-level diff. If no successful full backup exists, the run falls back to a full snapshot and the result's `BackupMode` is set to `Full`.

### Snapshot errors

When the Online Backup API fails, the behavior depends on the SQLite error code:

- `BUSY`, `LOCKED`, `IOERR`, or `NOTFOUND`: the copy is retried with bounded exponential backoff and jitter. After the retries run out, `DatabaseAccessException` is thrown.
- `CORRUPT`: `DatabaseAccessException` is thrown immediately.
- Other SQLite errors: the service falls back to `File.Copy`.

### Key public methods

| Method | Description |
|---|---|
| `ExecuteBackupAsync(BackupSchedule, CancellationToken)` | Runs the pipeline above and returns the populated `BackupResult`. The token is accepted but not checked or forwarded inside the method, so the worker's timeout does not interrupt a copy that is already running. |
| `GetBackupHistoryAsync(Guid scheduleId, int limit = 10)` | Returns the schedule's results from the repository, as the repository orders them. |
| `GetBackupResultAsync(Guid backupResultId)` | Returns one result, or `null` if it does not exist. |
| `DeleteBackupAsync(Guid backupResultId)` | Deletes the local backup file if it exists and removes the repository record. It does not delete objects in remote storage. Throws `BackupException` if the result does not exist. |
| `CalculateBackupChecksumAsync(string filePath)` | Returns the lowercase hex SHA-256 of a file. Throws `FileNotFoundException` if the file is missing, and `BackupException` on other read failures. |

## RotationService

`src/Services/RotationService.cs` - implements `IRotationService`. It stores the rotation policy for a schedule, decides which backups the policy would delete, deletes them, and previews the space that would be freed. Field-level policy details are in [Retention configuration](#retention-configuration). The full method-by-method reference is in [`docs/RotationService.md`](docs/RotationService.md).

### Purpose and candidate selection

Candidates come from the backup history in `IBackupRepository`, not from a directory scan. The history is sorted by `StartedAt`, newest first. The newest `MinimumBackupCount` backups are never candidates. Each remaining backup is checked against the policy's strategy. `remainingCount` drops by one for each candidate, so the count rule applies to what is left.

### Rotation strategies

The `Strategy` field of `RotationPolicy` maps to `DockerSqliteBackup.Constants.RotationStrategy`:

| Value | Strategy | Rule for a candidate backup |
|---|---|---|
| `0` | `MaxFileCount` | Deleted when `MaxBackupCount > 0` and the remaining count is above `MaxBackupCount` (and above `MinimumBackupCount`). |
| `1` | `MaxAge` | Deleted when the backup is older than `MaxAgeDays` (and the remaining count is above `MinimumBackupCount`). |
| `2` | `Combined` | Deleted when either the count rule or the age rule matches. |
| `3` | `NoRotation` | Nothing is deleted by `ExecuteRotationAsync` or `PreviewRotationAsync`. |

Two options change which candidates are removed:

- `DeleteFailedBackups` deletes failed backups beyond `MinimumBackupCount` directly, without checking the strategy.
- `VerifyBeforeDeletion` keeps non-failed candidates whose `Status` is not `VerifiedSuccess`.

### Key public methods

| Method | Description |
|---|---|
| `ExecuteRotationAsync(Guid scheduleId)` | Deletes each candidate's file (if it exists) and its repository record. Returns the number of backups deleted. A failure on one backup is logged and skipped. Updates `LastRotatedAt`. Throws `ArgumentException` for an empty GUID. |
| `PreviewRotationAsync(Guid scheduleId)` | Dry run. Returns the candidate backups and their total size in bytes without deleting anything. |
| `GetBackupsForRotationAsync(Guid scheduleId)` | Returns the backups the current policy would delete. This is the shared candidate logic for execution, preview, and disk-space calculation. |
| `CalculateDiskSpaceFreedAsync(Guid scheduleId)` | Returns the sum of `BackupFileSizeBytes` over the candidates. |
| `GetRotationPolicyAsync(Guid scheduleId)` | Returns the stored policy, or `null` if none exists. |
| `SaveRotationPolicyAsync(RotationPolicy policy)` | Validates the policy with `IsValid()`, sets `LastModifiedAt`, and saves it. Throws `ValidationException` for an invalid policy and `RotationException` if the save fails. |

`NoRotation` is not handled the same way in every method. `GetBackupsForRotationAsync` and `CalculateDiskSpaceFreedAsync` do not check it, so with `DeleteFailedBackups` enabled they can report candidates that `ExecuteRotationAsync` will not delete. See [`docs/RotationService.md`](docs/RotationService.md#notes) for this and the other known limitations.

## VerificationService

`src/Services/VerificationService.cs` - implements `IVerificationService` as a sealed class. It checks that a backup file can be restored and that the restored SQLite database passes `PRAGMA integrity_check`, then counts its records. It is registered as a singleton by `AddBackupServices`. Nothing in `src/` calls `VerifyBackupAsync` at the moment, so verification runs only when a caller invokes it directly.

### Purpose and verification steps

`VerifyBackupAsync` runs these steps in order:

1. **Checksum**: `VerifyChecksumAsync` compares the file's SHA-256 with `BackupResult.Checksum`. A mismatch throws `BackupCorruptedException`. An empty expected checksum is skipped and passes.
2. **Restore** the file to a temporary location with `RestoreToTemporaryAsync` (see below).
3. **Integrity check** the restored file with `PerformIntegrityCheckAsync`. A failed result throws `IntegrityCheckFailedException`.
4. **Measure** the restored file. `DatabaseSizeBytes` is the file length. `RecordCount` is the sum of `COUNT(*)` over all user tables.
5. **Complete** the `RestoreVerification` with success and the message `Backup verification successful`. If an `IBackupEventPublisher` was supplied, publish `RestoreVerificationCompletedEvent`.

Any exception from these steps is caught and does not propagate. The verification is marked failed with `Verification failed: <message>`, `ErrorMessage` is set, and `RestoreVerificationFailedEvent` is published if a publisher was supplied. The failure stage is `ChecksumMismatch` for `BackupCorruptedException`, `IntegrityCheckFailed` for `IntegrityCheckFailedException`, `RestoreFailed` for `RestoreVerificationFailedException`, and `Unknown` for anything else.

The `finally` block deletes the temporary directory and logs a warning if deletion fails. The `RestoreVerification` is then saved through `IBackupRepository.SaveRestoreVerificationAsync` and returned, whether the verification passed or failed.

### Restore formats

`RestoreToTemporaryAsync` chooses the restore method from the backup file's extension. It writes to `<temp>/sqlite-verify-<guid>/restore-check.sqlite`.

- `.enc`: decrypted with AES-256. The key is read from `BACKUP_ENCRYPTION_KEY`, then from `AppSettings.EncryptionKey`. A missing or invalid key throws `VerificationException`.
- `.gz`: decompressed with `GZipStream`.
- Any other extension: copied with `File.Copy`.

### Key public methods

| Method | Description |
|---|---|
| `VerifyBackupAsync(BackupResult, CancellationToken = default)` | Runs the steps above and returns the `RestoreVerification`. Verification failures are reported in the returned object and are not thrown. Throws `ArgumentNullException` for a null backup. |
| `GetVerificationHistoryAsync(Guid backupResultId)` | Returns the verification records for a backup from the repository. |
| `PerformIntegrityCheckAsync(string databasePath)` | Opens the file read-only and runs `PRAGMA integrity_check`. Returns `(true, null)` when the result is `ok`, and `(false, result)` otherwise. Throws `ArgumentNullException` or `ArgumentException` for a null or whitespace path, and `FileNotFoundException` if the file is missing. Other errors are wrapped in `VerificationException`, except `SqliteException`, `IOException`, and `UnauthorizedAccessException`, which are rethrown as they are. |
| `VerifyChecksumAsync(string filePath, string expectedChecksum)` | Computes the file's SHA-256 and compares it case-insensitively with the expected lowercase hex value. An empty expected value logs a warning and returns `true`. |
| `RestoreToTemporaryAsync(BackupResult)` | Restores the backup to a temporary database file as described in [Restore formats](#restore-formats) and returns the path to that file. |
| `CleanupTemporaryFilesAsync(string tempDirectory)` | Deletes the directory and its contents recursively if it exists. Throws `ArgumentNullException` for a null path. |

### Known limitations

- **Encrypted and compressed backups fail verification.** `BackupService` writes encrypted files first and then compresses them, so the name ends in `.enc.gz`. `RestoreToTemporaryAsync` checks for `.enc` at the end of the name, so it only decompresses. The result is still ciphertext, and the integrity check fails.
- **Temporary directories are not removed.** `VerifyBackupAsync` stores the restored file path, not the directory, in `RestoreVerification.TemporaryDirectory`. The `finally` block checks that value with `Directory.Exists`, so the check is always false and the directory is left behind. If `RestoreToTemporaryAsync` throws, the directory is also left behind, because the property is never set in that case.
- **Record counts can be silently zero.** If listing or counting tables fails, `CountRecordsAsync` logs a warning and returns `0`. A `RecordCount` of `0` does not prove the database is empty.

## AuditLogger

`src/Audit/AuditLogger.cs` - records audit trail entries for backup operations, schedule changes, configuration changes, and data access. Each entry is an `AuditEntry` serialized as one JSON object per line. Entries are also emitted through `ILogger<AuditLogger>` as `AUDIT: {json}`.

### Output

- The file defaults to `audit.log` in `AppContext.BaseDirectory`. Pass `auditLogPath` to the constructor to override it.
- Each entry is appended with `File.AppendAllText`, so the file grows across runs.
- Write failures are caught and logged as errors; they never propagate to the caller.
- `AuditEntry` has `Timestamp` (UTC), `Category`, `Action`, `TargetId`, `UserId`, `Success`, `Details`, and a `CorrelationId` that is generated per entry. `ToString()` returns a one-line human-readable summary.

### Key public methods

| Method | Description |
|---|---|
| `AuditLogger(ILogger<AuditLogger>, string? auditLogPath = null)` | Constructor. Throws `ArgumentNullException` if the logger is null. |
| `LogBackupOperation(Guid scheduleId, string operation, bool success, string? details = null)` | Records a `BACKUP` entry for a schedule. Throws `ArgumentException` if `operation` is empty. |
| `LogScheduleChange(Guid scheduleId, string action, Dictionary<string, string> changes)` | Records a `SCHEDULE` entry. `changes` is flattened to `key=value` pairs joined by `; `. Throws on empty `action` or null `changes`. |
| `LogConfigChange(string setting, string oldValue, string newValue, string? reason = null)` | Records a `CONFIG` entry with action `CHANGED`, with details `old -> new (reason)`. Throws if any required argument is empty. |
| `LogDataAccess(string resource, string action, string? userId = null, bool success = true)` | Records a `DATA_ACCESS` entry. Throws if `resource` or `action` is empty. |
| `LogEntry(AuditEntry)` | Writes a caller-built entry as-is. A null entry is silently ignored. |

Argument checks run before an entry is built, so invalid input throws and no audit line is written.

### Extension methods

`src/Audit/AuditLoggerExtensions.cs` - convenience wrappers over `LogEntry`, defined on `AuditLogger`:

| Method | Description |
|---|---|
| `LogSuccess(category, action, success)` | Records an entry with no target. |
| `LogBackupResult(targetId, userId, success)` | Records a `Backup` / `Operation` entry for a target. |
| `LogFailure(category, action, targetId, exception)` | Records a failed entry. `Details` is `ExceptionType: message`; stack traces are omitted. |

## EncryptionService

`src/Services/EncryptionService.cs` - implements `IEncryptionService` and provides AES-256-CBC encryption of backup files. It is registered as a singleton in `ServiceCollectionExtensions`. The cipher work and the file format are in `src/Utilities/EncryptionUtility.cs`. The full method-by-method reference is in [`docs/EncryptionService.md`](docs/EncryptionService.md).

### Purpose and key resolution

Encryption applies only when `AppSettings.EnableEncryption` is `true`. The key is resolved on each call:

1. The `BACKUP_ENCRYPTION_KEY` environment variable, if it is set.
2. Otherwise `AppSettings.EncryptionKey` (bound from `AppSettings__EncryptionKey`).

The key must be Base64 that decodes to exactly 32 bytes. The lookup does not fall through on bad input. If the environment variable is set but empty, or set to an invalid value, the configuration key is not used and the resolved key is `null`. `GetStatus()` checks for an empty value differently and may report a configuration key as valid even when encryption will not use it, so compare `GetStatus()` with the actual behavior when debugging a key problem.

The encrypted file format is `[16-byte IV][ciphertext]`. Each encrypt call generates a new random IV and writes it first. Padding is PKCS#7. Decryption reads the first 16 bytes as the IV. A file shorter than 16 bytes throws `InvalidDataException`.

### Key public methods

| Method | Description |
|---|---|
| `EncryptionService(AppSettings, ILogger<EncryptionService>)` | Constructor. Throws `ArgumentNullException` if either argument is null. |
| `EncryptFileAsync(string sourcePath, string destinationPath, CancellationToken)` | Encrypts the source into the destination, overwriting it, and returns the destination path. Throws `ArgumentException` for empty paths, `FileNotFoundException` if the source is missing, and `InvalidOperationException` if no usable key is resolved. Errors during encryption are logged and rethrown. |
| `DecryptFileAsync(string sourcePath, string destinationPath, CancellationToken)` | Decrypts the source into the destination and returns the destination path. Same argument and key checks as encryption, but the source must be an encrypted file. A wrong key or damaged data usually throws `CryptographicException`. |
| `GenerateKey()` | Returns a new random 256-bit key as a Base64 string. It does not store the key. |
| `ValidateKey(string?)` | Returns `true` if the value is Base64 that decodes to 32 bytes. Returns `false` for `null` or any other value. |
| `IsEncryptionEnabled()` | Returns `AppSettings.EnableEncryption`. |
| `GetActiveKey()` | Returns the resolved key, or `null` if encryption is disabled, no key is set, or the key is invalid. Logs a warning or error in those cases. |
| `GetStatus()` | Returns an `EncryptionStatus` with `IsEnabled`, `HasValidKey`, `KeyFingerprint`, and `KeySource` (`Environment`, `Configuration`, or `None`). Does not log. |

`KeyFingerprint` is the first 8 hex characters of the SHA-256 of the key bytes. It identifies the key without exposing it.

### Security notes

- **No authentication.** The mode is CBC with no HMAC or AEAD tag, so decryption does not detect changes to the ciphertext. A wrong key or a modified file usually fails with a padding error, but it can also return garbage. The checksum that `BackupService` records for the final file is the integrity check for `.enc` backups. That checksum is not a MAC: anyone who can change both the file and the stored checksum can hide the change.
- **Keys are not logged or stored by this class.** Log messages contain file paths and the key source, never key material. `GetActiveKey()` returns the raw key to the caller, so do not log its return value.
- **Keep the key out of config files.** Prefer the `BACKUP_ENCRYPTION_KEY` environment variable so the key is not in `appsettings.json` or source control. If the key is lost, encrypted backups cannot be recovered; the application does not escrow the key.
- **Partial output on failure.** Neither encrypt nor decrypt removes the destination file when a write fails. A failed decrypt can leave partial plaintext on disk, so remove the destination path after a failure.
- **Cancellation is checked once.** `ct` is checked before the work starts and is not passed to `EncryptionUtility`, so a running encrypt or decrypt cannot be cancelled.
- **Plaintext temp files in the extension methods.** `src/Services/EncryptionServiceExtensions.cs` writes plaintext to `Path.GetTempFileName()` files. The string and stream encrypt methods and `DecryptStringAsync` delete their temp files in `finally`. `DecryptToStreamAsync` deletes them only on failure, so on success the decrypted temp file remains until the caller or the system removes it.
