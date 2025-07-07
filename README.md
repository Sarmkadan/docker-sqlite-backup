# Docker SQLite Backup

An automated SQLite backup tool for .NET - provides scheduled snapshots, S3/local storage, rotation policies, and restore verification.

## Features

- **Automated Scheduling**: Cron-based backup scheduling with full flexibility
- **Multiple Storage Backends**: Store backups locally or on AWS S3 (with redundancy support)
- **Backup Rotation**: Automatic cleanup of old backups based on age or count
- **Verification**: Automated backup verification with database integrity checks
- **Restore Testing**: Verify backups work by attempting restoration and record counting
- **Comprehensive Logging**: Full audit trail of all backup operations
- **Concurrency Control**: Rate limiting for concurrent backup operations
- **Timeout Protection**: Configurable timeouts for backup and verification operations

## Project Structure

```
docker-sqlite-backup/
├── src/
│   ├── Domain/              # Entity models
│   ├── Services/            # Business logic services
│   ├── Data/                # Repository and data access
│   ├── Configuration/       # DI and settings
│   ├── Exceptions/          # Custom exception types
│   └── Constants/           # Enums and constants
├── Program.cs               # Entry point
├── BackupWorker.cs          # Hosted background service
├── appsettings.json         # Configuration
├── docker-sqlite-backup.csproj
├── LICENSE
└── .gitignore
```

## Domain Models

- **BackupSchedule**: Defines a backup schedule with cron expression
- **BackupJob**: Represents a single backup execution
- **BackupResult**: Contains the result of a backup operation
- **RotationPolicy**: Configuration for backup cleanup and rotation
- **RestoreVerification**: Results of backup verification attempt
- **StorageConfiguration**: Base class for storage backends (S3, Local)

## Services

- **BackupService**: Executes backups and manages backup files
- **ScheduleService**: Creates and manages backup schedules
- **RotationService**: Manages backup cleanup and rotation
- **VerificationService**: Performs backup integrity checks
- **StorageService**: Handles upload/download to different storage backends

## Configuration

Edit `appsettings.json` to configure:

```json
{
  "AppSettings": {
    "DatabasePath": "backups.sqlite",
    "MaxConcurrentBackups": 3,
    "BackupTimeoutSeconds": 3600,
    "ScheduleCheckIntervalSeconds": 60,
    "EnableVerificationByDefault": true,
    "RetentionDays": 30,
    "MaxBackupCount": 10,
    "LocalStoragePath": "backups"
  }
}
```

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets
