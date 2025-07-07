# Changelog

All notable changes to Docker SQLite Backup are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.2] - 2026-05-21

### Fixed
- Fix connection string parsing when path contains spaces or unicode characters
- Added regression test for the fix

---

## [2.0.0] - 2026-03-15

### Added
- Add async bulk import/export with streaming and progress reporting
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x

### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency

### Fixed
- Various edge cases found through testing

---

## [1.0.0] - 2025-12-15

### Added
- **NuGet Package**: Published as `Zaiets.docker.sqlite.backup` on NuGet.org
- **XML Documentation**: Full XML doc comments on all public APIs for IntelliSense support
- **Connection Pool**: `SqliteConnectionPool` for reuse of database connections across backup jobs
- **Compliance Audit Scripts**: Example scripts for regulatory audit trail generation
- **Docker Compose**: Reference `docker-compose.yml` for local and staging deployments
- **Kubernetes Manifests**: Ready-to-apply YAML for StatefulSet, RBAC, and PersistentVolumeClaims
- **Integration Tests**: End-to-end test scenarios covering backup → verify → rotate lifecycle

### Changed
- **API Stability**: All public interfaces and API contracts frozen for v1.0.0
- **Default Retention**: Increased default `RetentionDays` from 14 to 30
- **Logging**: Enriched structured log messages with schedule ID and job ID fields

### Fixed
- **Rotation Race**: Fixed edge case where concurrent rotation jobs could delete overlapping backups
- **Path Normalization**: Corrected path handling on Windows when `LocalStoragePath` contained trailing slash

### Security
- **Credentials**: AWS credentials are now redacted from all log output and exception messages
- **Input Validation**: Hardened `RequestValidator` against path traversal in backup IDs

---

## [0.9.0] - 2025-11-12

### Added
- **Audit Logger**: `AuditLogger` service writes tamper-evident operation records for every backup event
- **In-Memory Cache**: `MemoryCacheService` caches schedule state to reduce repeated configuration reads
- **Health Check Service**: `HealthCheckService` aggregates storage, schedule, and last-backup status
- **`/health` Endpoint**: HTTP endpoint suitable for Kubernetes liveness/readiness probes
- **`/metrics` Endpoint**: Prometheus-compatible counters for backup count, duration, and size

### Changed
- **Exception Hierarchy**: Introduced `BackupException`, `StorageException`, `VerificationException`, `ScheduleException` as typed exception classes
- **Error Responses**: API now returns structured error envelopes with `code`, `message`, and `details`

### Fixed
- **S3 Region**: Fixed incorrect default region when `Region` was omitted from S3 config
- **Health Endpoint**: Fixed health endpoint returning 200 when last backup had failed status

---

## [0.8.0] - 2025-10-01

### Added
- **Rate Limiting Middleware**: Configurable per-minute request cap to prevent runaway backup requests
- **Exception Handling Middleware**: Global handler converts unhandled exceptions to RFC 7807 problem responses
- **Request Context Middleware**: Assigns a `X-Request-Id` header and propagates it through logs
- **`CacheKeyBuilder`**: Centralized key builder for consistent cache key construction

### Changed
- **Middleware Pipeline**: Moved to explicit `PipelineBuilder` registration order (context → rate-limit → exception)
- **API Controllers**: Refactored to return `ApiResponse<T>` wrapper for all endpoints

### Fixed
- **Concurrent Backup Limit**: `SemaphoreSlim` was not being released on verification failure; fixed with `finally` block

---

## [0.7.0] - 2025-09-09

### Added
- **Event System**: `BackupEventPublisher` and `IBackupEventListener` interface for decoupled event handling
- **Metrics Listener**: `MetricsEventListener` records duration and size counters on each backup event
- **Notification Listener**: `NotificationEventListener` posts to webhook URL on backup completion or failure
- **Webhook Client**: `WebhookClient` with configurable timeout and JSON payload serialization
- **Notification Client**: `NotificationClient` wraps `HttpClientFactory` for typed HTTP calls

### Changed
- **Backup Service**: Replaced direct webhook call with event publish; listeners now handle side effects
- **DI Registration**: Listeners registered as `IEnumerable<IBackupEventListener>` for multi-listener support

### Fixed
- **Webhook Timeout**: Fixed webhook call blocking indefinitely when remote endpoint was unreachable

---

## [0.6.0] - 2025-08-19

### Added
- **AWS S3 Storage**: `StorageService` gained full S3 backend using `AWSSDK.S3`
- **S3 Configuration**: `S3Configuration` domain object with bucket, region, encryption, and versioning options
- **Multi-Part Upload**: Large database files use S3 multi-part upload to avoid single-request size limits
- **Storage Type Enum**: `StorageType` constant (`Local`, `S3`) used by schedule and storage service
- **`StorageConfiguration` Base Class**: Shared base for `LocalStorageConfiguration` and `S3Configuration`

### Changed
- **`IStorageService`**: Extended interface with `UploadAsync` / `DownloadAsync` / `ListAsync` / `DeleteAsync`
- **Configuration**: Schedule now carries `StorageType` and optional `S3Config` section

### Fixed
- **Local Storage**: Fixed `FileSystemUtility` not creating nested backup directories on first run

---

## [0.5.0] - 2025-07-29

### Added
- **CLI Interface**: `CliCommandHandler` and `CliCommandParser` for command-line backup operations
- **CLI Commands**: `list-schedules`, `trigger-backup`, `get-history`, `last-backup`, `verify-backup`, `restore`, `cleanup`
- **Output Formatters**: `JsonOutputFormatter`, `CsvOutputFormatter`, `XmlOutputFormatter` with shared `IOutputFormatter`
- **`OutputFormatterFactory`**: Selects formatter based on `--format` flag value

### Changed
- **`Program.cs`**: Detects CLI args at startup and routes to `CliCommandHandler` instead of hosted service
- **`CliOptions`**: Typed options record for all command-line arguments

### Fixed
- **Restore Command**: Fixed `--output` path not being respected when restoring to a custom location

---

## [0.4.0] - 2025-07-03

### Added
- **REST API**: ASP.NET Core minimal API with `BackupController`, `HealthController`, `ScheduleController`
- **Backup Endpoints**: `POST /api/backup/trigger`, `GET /api/backup/list`, `POST /api/backup/verify/{scheduleId}`
- **Schedule Endpoints**: `GET /api/schedule/list`, `GET /api/schedule/{id}`, enable/disable endpoints
- **`ApiResponse<T>`**: Unified response envelope with `success`, `data`, `message`, and `timestamp` fields
- **`BackupRepository`**: `IBackupRepository` and in-memory implementation for backup job persistence

### Changed
- **`Program.cs`**: Switched from console app to `WebApplication` host with API routing
- **`AppSettings`**: Added `MaxConcurrentBackups` and `BackupTimeoutSeconds` fields

### Fixed
- **Schedule Endpoint**: Fixed `/api/schedule/list` returning empty array when schedules were defined in config

---

## [0.3.0] - 2025-06-11

### Added
- **Rotation Service**: `RotationService` implementing `IRotationService` with three strategies
- **Rotation Strategies**: `Age` (delete older than N days), `Count` (keep last N), `AgeAndCount` (both)
- **`RotationPolicy` Domain Object**: Encapsulates strategy, retention days, and max count
- **`RotationStrategy` Enum**: Typed constant for strategy selection in configuration
- **`BackupConstants`**: Shared constant values for default retention and count limits

### Changed
- **`BackupService`**: Calls `RotationService.ApplyPolicyAsync` after each successful backup
- **`BackupSchedule`**: Added `RotationStrategy`, `RetentionDays`, and `MaxBackupCount` fields

### Fixed
- **Age Calculation**: Fixed off-by-one in retention age check (was using `>=` instead of `>`)

---

## [0.2.0] - 2025-05-22

### Added
- **Verification Service**: `VerificationService` implementing `IVerificationService`
- **Restore Testing**: Each backup is restored to a temporary file and validated before the temp file is deleted
- **Record Counting**: Source and restored database row counts compared across all tables
- **Checksum Utility**: `ChecksumUtility` computes SHA-256 hashes for backup file integrity
- **`RestoreVerification` Domain Object**: Carries `IsValid`, `RecordCount`, `ChecksumMatch`, and error detail
- **`BackupStatus` Constants**: `Pending`, `Running`, `Success`, `Failed`, `Verified` status values

### Changed
- **`BackupResult`**: Extended with `VerificationResult` and `ChecksumHash` fields
- **`BackupService`**: Calls verification after local copy completes when `EnableVerificationByDefault` is true

### Fixed
- **Temp File Cleanup**: Temporary restore file was not deleted when verification threw an exception

---

## [0.1.0] - 2025-04-28

### Added
- **Core Backup Engine**: `BackupService` copies SQLite database using `VACUUM INTO` for a clean, unlocked snapshot
- **Scheduled Backups**: `ScheduleService` evaluates cron expressions via `Cronos` library on a configurable interval
- **Local Storage**: `StorageService` writes backup files to a configurable local directory with timestamped filenames
- **Domain Models**: `BackupJob`, `BackupResult`, `BackupSchedule`, `LocalStorageConfiguration`
- **Configuration**: `AppSettings` bound from `appsettings.json` with environment variable override support
- **Dependency Injection**: All services registered via `ServiceCollectionExtensions`
- **Background Worker**: `BackupWorker` hosted service drives the schedule check loop
- **Structured Logging**: Microsoft.Extensions.Logging throughout; log level configurable per namespace
- **`DateTimeUtility`**: UTC-safe date helpers used by schedule and rotation logic
- **`PathUtility`**: Cross-platform path construction for backup file names
- **`StringUtility`**: Sanitization helpers for schedule IDs and file name components
- **`EnumerableExtensions`**: `Batch` and `ChunkBy` extensions used in rotation logic
- **Unit Tests**: Initial test suite for domain models, schedule parsing, and string utilities
