# docker-sqlite-backup

Reliable, scheduled SQLite backup solution for .NET.

![Build](https://github.com/sarmkadan/docker-sqlite-backup/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/docker-sqlite-backup)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

## Overview

docker-sqlite-backup is a background worker for .NET that performs scheduled SQLite backups.
It snapshots your database on a cron schedule, and stores backups locally, or in S3/Azure.
Supports AES-256 encryption, rotation, and restore verification.

## Installation

```bash
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup
dotnet build
```

## Usage

See the [examples/](examples/) directory for practical usage scenarios:

- [BasicUsage.cs](examples/BasicUsage.cs) - Getting started
- [AdvancedUsage.cs](examples/AdvancedUsage.cs) - Configuration and error handling
- [IntegrationExample.cs](examples/IntegrationExample.cs) - ASP.NET DI integration


## Docker

You can run the application using Docker Compose:

```bash
docker-compose up -d
```

The application will be available at `http://localhost:8080`.

For development with hot-reload enabled:

```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
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

## BackupEventPublisherExtensions

The `BackupEventPublisherExtensions` class provides extension methods for `BackupEventPublisher` that simplify event publishing and subscription management. It includes convenience methods for publishing events with caller information, type-safe event publishing, and temporary subscription handling.

### Usage Examples

```csharp
// Get an event publisher instance from DI
var publisher = services.GetRequiredService<BackupEventPublisher>();

// Publish an event with automatic caller information
var backupStartedEvent = new BackupStartedEvent("production.db", DateTime.UtcNow);
await publisher.PublishWithCallerInfoAsync(backupStartedEvent);

// Publish a typed event
var backupCompletedEvent = new BackupCompletedEvent("production.db", DateTime.UtcNow, 1024);
await publisher.PublishAsync(backupCompletedEvent);

// Create a temporary subscription that automatically unsubscribes when disposed
using var subscription = publisher.SubscribeTemporarily(new MyEventListener());

// The subscription will be automatically unsubscribed when exiting the using block
```

## Benchmarks

This project includes a BenchmarkDotNet suite to monitor performance of critical operations like encryption and checksum generation.

To run the benchmarks:

```bash
cd tests/docker-sqlite-backup.Benchmarks
dotnet run -c Release
```

### Results

| Method | Mean | Error | StdDev | Allocated |
| :--- | :--- | :--- | :--- | :--- |
| CalculateSha256 | 9.073 ms | 0.176 ms | 0.164 ms | 1.44 KB |
| CalculateCrc32 | 53.566 ms | 1.059 ms | 1.177 ms | 109.99 KB |
| GenerateQuickChecksum | 8.441 us | 0.166 us | 0.237 us | 5.76 KB |
| Encrypt | 1.856 ms | 0.050 ms | 0.063 ms | 9.31 KB |
| Decrypt | 2.518 ms | 0.132 ms | 0.132 ms | 23.93 KB |

## License

MIT - Copyright (c) 2026 Vladyslav Zaiets






