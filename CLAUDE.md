# CLAUDE.md

## Overview
.NET 10 worker service that automates SQLite backups inside Docker: cron scheduling, compression, AES encryption, checksum/integrity verification, rotation, and upload to local/S3/Azure storage.

## Build
- `dotnet restore && dotnet build -c Release` (CI adds `--warnaserror`)
- `make build` / `make build-debug`
- Docker: `make docker-local` (Dockerfile in root), `docker compose up` (`docker-compose.yml`, `docker-compose.dev.yml`)
- SDK pinned in `global.json` (10.0.100, rollForward latestMinor); shared props in `Directory.Build.props` (Nullable + ImplicitUsings enabled)

## Test
- `dotnet test -c Release` (xunit + FluentAssertions + Moq)
- `make test` (with cobertura coverage), `make test-watch`, `make test-failed`
- Single test: `dotnet test --filter "FullyQualifiedName~ScheduleServiceTests"`
- Benchmarks: `dotnet run -c Release --project tests/docker-sqlite-backup.Benchmarks` (BenchmarkDotNet)

## Lint / Format
- `make lint` = `dotnet build /p:EnforceCodeStyleInBuild=true`
- `make format` / `make format-check` = `dotnet format` (rules in `.editorconfig`)

## Layout
- `Program.cs` - entry point (Generic Host); `healthcheck` subcommand used by Docker HEALTHCHECK
- `BackupWorker.cs` - BackgroundService running the backup loop
- `appsettings.json` - config, bound to `AppSettings`; env override prefix `BACKUP_`
- `src/Configuration` - AppSettings, DI registration (`ServiceCollectionExtensions`)
- `src/Services` - core logic: BackupService, ScheduleService (Cronos), RotationService, EncryptionService, VerificationService, IntegrityCheckerService, StorageService + `IStorageBackend` impls (Local/S3/Azure)
- `src/Domain` - models: BackupJob, BackupSchedule, BackupResult, BackupManifest, RotationPolicy, storage configs
- `src/Data` - `BackupRepository` (SQLite metadata via Microsoft.Data.Sqlite)
- `src/Events` - `BackupEventPublisher` + `IBackupEventListener` (metrics, notifications, health)
- `src/Health` - health status store/evaluator for Docker
- `src/Exceptions`, `src/Constants`, `src/Utilities`, `src/Caching`, `src/Audit`, `src/Integration` (webhook/notification clients)
- `tests/docker-sqlite-backup.Tests` - unit tests mirroring `src/` folders; `tests/docker-sqlite-backup.Benchmarks`
- `docs/` - per-class markdown docs; `examples/` - usage samples (excluded from compile)
- `.github/workflows` - ci, codeql, docker, publish, release

## Conventions
- Namespace `DockerSqliteBackup.<Folder>`; one public type per file, file named after the type
- Interfaces prefixed `I`, services suffixed `Service`, storage backends suffixed `StorageBackend`
- Companion files: `XExtensions.cs`, `XJsonExtensions.cs`, `XValidation.cs` next to `X.cs`
- Tests: `XTests.cs`, method names `Method_Scenario_Expected`; xunit `[Fact]`/`[Theory]`, FluentAssertions, Moq
- 4-space indent, LF, `var` only when type is apparent, nullable enabled, `// Author: Vladyslav Zaiets` header in source files
- Config via env vars with `BACKUP_` prefix (e.g. `BACKUP_AppSettings__DatabasePath`)
