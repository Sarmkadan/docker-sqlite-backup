# Architecture

This document describes the project layout and the backup flow of `docker-sqlite-backup`, a .NET 10 worker service that runs SQLite backups inside Docker.

## Project structure

```
.
├── Program.cs                  Entry point (Generic Host); `healthcheck` subcommand for Docker HEALTHCHECK
├── BackupWorker.cs             BackgroundService: schedule loop, backup execution, verification, rotation
├── appsettings.json            Configuration, bound to AppSettings (env override prefix BACKUP_)
├── docker-sqlite-backup.csproj Project file (net10.0)
├── docker-sqlite-backup.slnx   Solution: app + tests + benchmarks
├── Dockerfile, docker-compose*.yml, Makefile, build.sh
├── src/
│   ├── Configuration/   AppSettings, AppSettingsValidation, ServiceCollectionExtensions (DI registration)
│   ├── Services/        Core logic: BackupService (SqliteBackupService), BackupScheduler, ScheduleService (Cronos),
│   │                    VerificationService, IntegrityCheckerService, RotationService, EncryptionService,
│   │                    StorageService and IStorageBackend implementations (LocalStorageBackend, S3StorageBackend, AzureStorageBackend)
│   ├── Domain/          Models: BackupJob, BackupSchedule, BackupResult, BackupManifest, RotationPolicy,
│   │                    IntegrityReport, RestoreVerification, storage configurations (Local, S3, Azure)
│   ├── Data/            BackupRepository / IBackupRepository: backup metadata in SQLite (Microsoft.Data.Sqlite)
│   ├── Events/          BackupEventPublisher, IBackupEventListener, BackupEvent types,
│   │                    MetricsEventListener, NotificationEventListener
│   ├── Health/          HealthStatusStore, HealthStatusSnapshot, DockerHealthCheckEvaluator, HealthCheckService
│   ├── Integration/     WebhookClient, NotificationClient
│   ├── Caching/         ICacheService, MemoryCacheService, CacheKeyBuilder
│   ├── Audit/           AuditLogger
│   ├── Exceptions/      Typed exceptions (Backup, Storage, Verification, Schedule, Rotation, Configuration, Validation)
│   ├── Constants/       BackupMode, BackupStatus, RotationStrategy, StorageType, BackupConstants
│   └── Utilities/       ChecksumUtility, EncryptionUtility, FileSystemUtility, PathUtility, DateTimeUtility, StringUtility
├── tests/
│   ├── docker-sqlite-backup.Tests/       xUnit + FluentAssertions + Moq; folders mirror src/
│   └── docker-sqlite-backup.Benchmarks/  BenchmarkDotNet
├── examples/            Usage samples, excluded from compilation
└── docs/                Per-type markdown documentation
```

Build-time notes:

- `Compile Remove` in the project file excludes `tests/**` and `examples/**` from the main assembly.
- `Directory.Build.props` enables Nullable and ImplicitUsings for all projects.
- The SDK version is pinned in `global.json`.

## Naming and namespace conventions

- Namespace: `DockerSqliteBackup.<Folder>`, one public type per file, file named after the type.
- Interfaces are prefixed with `I`. Services end with `Service`. Storage backends end with `StorageBackend`.
- Companion files sit next to the type they extend: `XExtensions.cs`, `XJsonExtensions.cs`, `XValidation.cs`.

## Runtime components

| Component | Role |
|-----------|------|
| `Program.cs` | Builds the Generic Host, registers services, runs `BackupWorker`. Handles the `healthcheck` subcommand. |
| `BackupWorker` | Long-running loop. Reads active schedules, decides which are due, runs the backup, then verification and rotation. |
| `ScheduleService` | Parses cron expressions with Cronos and computes next run times. |
| `BackupService` | Produces one backup file: copy, optional incremental capture, encryption, compression, checksum, manifest, remote upload, persistence. |
| `StorageService` | Selects an `IStorageBackend` by configuration type and uploads the file. |
| `VerificationService` | Verifies the backup's integrity and restorability (runs when `VerifyAfterBackup` is set). |
| `RotationService` | Deletes old backups according to `RotationPolicy`. Runs only after a successful verification (or when verification is disabled). |
| `BackupEventPublisher` | Fans out events to listeners (metrics, notifications, health status). |
| `HealthStatusStore` + `DockerHealthCheckEvaluator` | Hold the last backup status and answer the Docker HEALTHCHECK. |
| `BackupRepository` | Stores `BackupResult` rows (history, incremental base lookup, rotation input). |

## Backup flow

### 1. Scheduling (`BackupWorker.ExecuteAsync`)

1. The worker loads active schedules through `IScheduleService.GetActiveSchedulesAsync()`.
2. For each schedule, `ShouldExecuteSchedule` compares the cron next-run time with the last run recorded in `_scheduleLastRun`.
3. Due schedules are started as tasks. `_activeBackups` tracks concurrency.
4. The worker sleeps until the next due time. On error it waits `ErrorRetryDelayMilliseconds` (10 s) and retries.
5. On shutdown, the worker waits for in-flight backups for a grace period before returning.

### 2. Execution (`ExecuteScheduleAsync` -> `BackupService.ExecuteBackupAsync`)

`ExecuteBackupAsync` runs these steps in order:

1. **Validate** the schedule (`ValidateSchedule`).
2. **Create the output path** from the schedule and a UTC timestamp, then prepare the directory.
3. **Create the snapshot**:
   - *Full mode*: `SafeCopyDatabaseAsync` copies the SQLite database using the SQLite backup API with retries on busy/locked errors.
   - *Incremental mode*: the newest successful full backup in the history is used as the base (`BaseBackupResultId`). `CaptureIncrementalAsync` checkpoints the WAL, then copies the database. If there is no full base yet, the run falls back to a full copy and the result is recorded as Full.
4. **Encrypt** if configured (`EncryptBackupIfNeededAsync`, AES via `EncryptionUtility`).
5. **Compress** if configured (`CompressBackupIfNeededAsync`, GZip at the configured level). The original size and compression ratio are recorded.
6. **Checksum**: SHA-256 of the final file (`CalculateBackupChecksumAsync`). Status is set to `Success`.
7. **Manifest**: `CreateBackupManifestAsync` writes a manifest next to the backup file.
8. **Upload** when the schedule has a non-local storage configuration. The upload goes through `StorageService` to S3 or Azure. Errors are not caught here, so they fail the job. For S3 the result stores `IsStoredInS3` and `S3ObjectKey`.
9. **Persist** (`finally`): the `BackupResult` is saved through `BackupRepository`. A persistence error is logged and does not replace the backup outcome.

On any exception, the result is marked `Failed` with the error message and stack trace, and the exception is rethrown.

### 3. Post-backup (`BackupWorker.HandleSuccessfulBackupAsync`)

1. `VerifyAndRotateAsync` decides what happens next:
   - If `VerifyAfterBackup` is false, rotation runs immediately.
   - If it is true, `VerificationService.VerifyBackupAsync` runs.
     - On success: status becomes `VerifiedSuccess`, `IsVerified` and `VerifiedAt` are set, and rotation runs.
     - On failure: status becomes `VerificationFailed` and rotation is skipped, so existing backups are kept.
   - A `RestoreVerificationCompletedEvent` is published after verification.
2. The schedule is updated (`UpdateScheduleAsync`) so the next run time moves forward.
3. A `BackupCompletedEvent` is published.

On failure, `PublishFailureAsync` publishes a failure event for the schedule.

### 4. Events and health

- `BackupEventPublisher` sends each event to every `IBackupEventListener`.
- `MetricsEventListener` records metrics.
- `NotificationEventListener` sends notifications through `NotificationClient` and `WebhookClient`. Webhook payloads are signed (see `docs/webhook-signature.md`).
- `HealthStatusEventListener` updates `HealthStatusStore`. The Docker HEALTHCHECK uses `DockerHealthCheckEvaluator` to decide whether the service is healthy.
- `AuditLogger` records audit entries.

```
BackupWorker
  -> ScheduleService (is due?)
  -> BackupService.ExecuteBackupAsync
       copy (full | incremental) -> encrypt -> compress -> checksum -> manifest -> upload -> persist
  -> VerificationService (if VerifyAfterBackup)
  -> RotationService (only on success)
  -> UpdateSchedule -> BackupEventPublisher -> listeners (metrics, notifications, health, audit)
```

## Configuration

- `appsettings.json` is bound to `AppSettings`. Validation lives in `AppSettingsValidation`.
- Environment variables with the `BACKUP_` prefix override settings, using `__` as the section separator (for example `BACKUP_AppSettings__DatabasePath`).
- Storage targets (Local, S3, Azure) are chosen per schedule through their storage configuration types.

## Testing

- `dotnet test -c Release` runs the unit and integration tests.
- Test classes mirror the `src/` folders. Method names follow `Method_Scenario_Expected`.
- Benchmarks: `dotnet run -c Release --project tests/docker-sqlite-backup.Benchmarks`.
