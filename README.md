![CI](https://github.com/sarmkadan/docker-sqlite-backup/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/docker-sqlite-backup)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

# docker-sqlite-backup

Scheduled SQLite backup service for .NET 10. Runs as a background worker,
snapshots your database on a cron schedule, and stores backups locally or in
S3/Azure. Supports AES-256 encryption, rotation, and restore verification.

## Features

- Cron-based scheduling
- Storage: local filesystem, AWS S3, Azure Blob Storage
- Full and incremental backup modes (SQLite Online Backup API + WAL checkpoint)
- AES-256-CBC encryption at rest
- SHA-256 checksums + restore verification (integrity check + row count)
- Rotation by age, count, or both
- Webhook notifications on backup events

## Quick start

```bash
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup
dotnet run --configuration Release
```

Or with Docker:

```bash
docker compose up -d
```

## Configuration

`appsettings.json`:

```json
{
  "AppSettings": {
    "DatabasePath": "/data/app.db",
    "MaxConcurrentBackups": 2,
    "BackupTimeoutSeconds": 3600,
    "ScheduleCheckIntervalSeconds": 60,
    "EnableVerificationByDefault": true,
    "RetentionDays": 30,
    "MaxBackupCount": 10,
    "LocalStoragePath": "/backups"
  }
}
```

All keys can be overridden via environment variables using double-underscore:

```bash
AppSettings__DatabasePath=/data/prod.db
AppSettings__LocalStoragePath=/var/backups/sqlite
BACKUP_ENCRYPTION_KEY=<base64-encoded-32-byte-key>
```

### S3 storage

```json
{
  "S3Config": {
    "BucketName": "my-backups",
    "RegionName": "us-east-1",
    "AccessKeyId": "...",
    "SecretAccessKey": "...",
    "ObjectKeyPrefix": "sqlite/",
    "EnableServerSideEncryption": true
  }
}
```

### Azure Blob Storage

```json
{
  "AzureConfig": {
    "ConnectionString": "DefaultEndpointsProtocol=https;...",
    "ContainerName": "sqlite-backups",
    "BlobPrefix": "production/",
    "AccessTier": "Cool"
  }
}
```

## Encryption

Set `AppSettings__EnableEncryption=true` and provide a 32-byte Base64 key.
The key is read from `BACKUP_ENCRYPTION_KEY` env var first, then `AppSettings.EncryptionKey`.

```bash
# Generate a key
openssl rand -base64 32
export BACKUP_ENCRYPTION_KEY=<output>
```

## Tests

```bash
dotnet test
```

## License

MIT - Copyright (c) 2026 Vladyslav Zaiets



