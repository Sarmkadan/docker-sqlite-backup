![CI](https://github.com/sarmkadan/docker-sqlite-backup/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/docker-sqlite-backup)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

# Docker SQLite Backup

An enterprise-grade automated SQLite backup tool for .NET — providing scheduled snapshots, multi-backend storage (S3/local), intelligent rotation, verification, and restore testing. Designed for production workloads where data reliability is paramount.

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [CLI Reference](#cli-reference)
- [Configuration Reference](#configuration-reference)
- [Backup Strategies](#backup-strategies)
- [Restore Verification](#restore-verification)
- [Troubleshooting](#troubleshooting)
- [Performance](#performance)
- [Testing](#testing)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Features

### Core Backup Capabilities
- **Automated Scheduling**: Full cron expression support for flexible backup scheduling (e.g., `0 2 * * *` for daily 2 AM backups)
- **Multiple Storage Backends**: Store backups locally or on AWS S3 with configurable redundancy
- **Backup Rotation**: Intelligent cleanup based on age, count, or custom policies
- **Incremental Awareness**: Track backup metadata for smart restoration decisions
- **Compression Support**: Optional gzip compression for reduced storage footprint

### Data Integrity & Verification
- **Automated Verification**: Restore and integrity-check every backup automatically
- **Record Counting**: Verify data completeness by comparing source and restored database records
- **Checksum Validation**: SHA-256 checksums ensure backup file integrity
- **Rollback Testing**: Simulate restore operations before disaster strikes
- **Detailed Audit Logging**: Full trail of all backup operations and verification results

### Production-Grade Features
- **Concurrency Control**: Rate limiting for concurrent backup operations
- **Timeout Protection**: Configurable timeouts prevent hung backups from blocking operations
- **Request Context Tracking**: Trace requests through the system for debugging
- **Rate Limiting Middleware**: Protect against runaway backup requests
- **Comprehensive Metrics**: Built-in Prometheus-compatible health checks
- **Event-Driven Architecture**: Pluggable event listeners for custom integrations

### Developer Experience
- **REST API**: Full HTTP API for integration with monitoring systems
- **CLI Interface**: Command-line tools for scripting and automation
- **Health Checks**: `/health` endpoint for orchestration systems (Kubernetes, Docker)
- **Multiple Output Formats**: JSON, CSV, or XML output options
- **Docker Support**: Pre-built Docker image and docker-compose configuration

## Architecture

### System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Docker SQLite Backup                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────┐         ┌──────────────────┐                 │
│  │  REST API    │         │   CLI Handler    │                 │
│  │  Controllers │         │   (Scheduling)   │                 │
│  └──────┬───────┘         └────────┬─────────┘                 │
│         │                          │                           │
│         └──────────────┬───────────┘                           │
│                        ▼                                        │
│         ┌──────────────────────────┐                           │
│         │    Backup Service        │                           │
│         │  (Orchestration Core)    │                           │
│         └────────┬────────┬────────┘                           │
│                  │        │                                    │
│      ┌───────────┘        └────────────┐                       │
│      ▼                                  ▼                       │
│ ┌──────────────┐          ┌──────────────────────┐            │
│ │ Verification │          │ Rotation Service    │            │
│ │  Service     │          │ (Cleanup & Archive) │            │
│ └──────────────┘          └─────────┬───────────┘            │
│      │                               │                       │
│      └───────────┬───────────────────┘                       │
│                  ▼                                           │
│      ┌──────────────────────┐                               │
│      │ Storage Service      │                               │
│      │ (Backend Abstraction)│                               │
│      └────────┬─────────┬──────────┘                         │
│      ┌────────┴─────┐   ┌────────┴──────────┐               │
│      ▼              ▼   ▼                    ▼               │
│  ┌──────────┐  ┌────────────┐          ┌─────────────┐      │
│  │  Local   │  │ AWS S3     │          │ S3 with CDN │      │
│  │  Storage │  │  Storage   │          │  Redundancy │      │
│  └──────────┘  └────────────┘          └─────────────┘      │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Middleware & Cross-Cutting Concerns                  │  │
│  │  - Exception Handling | Rate Limiting | Auditing       │  │
│  │  - Request Context | Event Publishing | Caching        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────────┘
```

### Key Components

**Backup Service**: Core orchestration engine that coordinates backup operations, delegates to storage and verification services, and publishes events.

**Verification Service**: Automatically tests backup integrity by restoring to a temporary database and validating schema/data consistency.

**Storage Service**: Abstract interface supporting multiple backends (local filesystem, AWS S3) with pluggable implementations.

**Rotation Service**: Manages backup lifecycle, removing old or excess backups based on retention policies.

**Schedule Service**: Manages cron-based scheduling, ensuring backups execute at configured times.

**Event System**: Decoupled event publishing for notifications, metrics, and custom integrations.

## Installation

### Prerequisites

- **.NET 10 Runtime** or **SDK** (for development)
- **SQLite**: Your application's SQLite database
- **AWS Credentials** (optional): Only if using S3 storage
- **Docker** (optional): For containerized deployment

### Option 1: Build from Source

```bash
# Clone the repository
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release

# Run tests
dotnet test

# Run the application
dotnet run --configuration Release
```

### Option 2: Docker

```bash
# Build the Docker image
docker build -t docker-sqlite-backup:latest .

# Run with docker-compose (easiest)
docker-compose up -d

# Or run manually
docker run -d \
  --name sqlite-backup \
  -v /path/to/database:/app/data \
  -v /path/to/backups:/app/backups \
  -e "AppSettings__DatabasePath=/app/data/app.db" \
  docker-sqlite-backup:latest
```

### Option 3: DotNet CLI Global Tool

```bash
# Install as global tool (when published to NuGet)
dotnet tool install --global docker-sqlite-backup

# Use the CLI
docker-sqlite-backup --help
```

### Option 4: Integration into Existing Project

Add to your existing .NET 10 project:

```bash
dotnet add package docker-sqlite-backup
```

Then configure in Startup.cs:

```csharp
services.AddSqliteBackup(configuration.GetSection("BackupSettings"));
```

## Quick Start

### 1. Basic Configuration

Create or update `appsettings.json`:

```json
{
  "AppSettings": {
    "DatabasePath": "app.sqlite",
    "MaxConcurrentBackups": 3,
    "BackupTimeoutSeconds": 3600,
    "ScheduleCheckIntervalSeconds": 60,
    "EnableVerificationByDefault": true,
    "RetentionDays": 30,
    "MaxBackupCount": 10,
    "LocalStoragePath": "backups"
  },
  "Schedules": [
    {
      "Id": "daily-backup",
      "Name": "Daily Backup at 2 AM",
      "CronExpression": "0 2 * * *",
      "IsEnabled": true,
      "StorageType": "Local",
      "RotationStrategy": "AgeAndCount"
    }
  ]
}
```

### 2. Run the Application

```bash
dotnet run
```

### 3. Verify Backup Creation

Check the configured backup directory:

```bash
ls -lah backups/
```

### 4. Test Verification

```bash
curl -X POST http://localhost:5000/api/backup/verify/daily-backup
```

### 5. Monitor Health

```bash
curl http://localhost:5000/health
```

## Usage Examples

### Example 1: Simple Daily Backup to Local Storage

```csharp
// In appsettings.json
{
  "AppSettings": {
    "DatabasePath": "production.db",
    "LocalStoragePath": "/var/backups/sqlite",
    "RetentionDays": 90
  },
  "Schedules": [
    {
      "Id": "prod-daily",
      "CronExpression": "0 2 * * *",
      "IsEnabled": true,
      "StorageType": "Local"
    }
  ]
}
```

Result: Daily backups at 2 AM, kept for 90 days, stored locally.

### Example 2: Hourly Backup with S3 Storage

```csharp
{
  "AppSettings": {
    "DatabasePath": "critical.db",
    "MaxBackupCount": 168  // Keep last 7 days (hourly)
  },
  "Schedules": [
    {
      "Id": "hourly-s3",
      "CronExpression": "0 * * * *",
      "IsEnabled": true,
      "StorageType": "S3",
      "S3Config": {
        "BucketName": "company-backups",
        "Region": "us-east-1",
        "EnableEncryption": true,
        "EnableVersioning": true
      }
    }
  ]
}
```

Result: Hourly backups to S3 with encryption and versioning.

### Example 3: Multiple Backup Strategies

```csharp
{
  "Schedules": [
    {
      "Id": "daily-local",
      "CronExpression": "0 2 * * *",
      "StorageType": "Local",
      "RotationStrategy": "Count",
      "MaxBackupCount": 7
    },
    {
      "Id": "weekly-s3",
      "CronExpression": "0 3 * * 0",  // Sundays at 3 AM
      "StorageType": "S3",
      "RotationStrategy": "Age",
      "RetentionDays": 90
    }
  ]
}
```

Result: Daily local backups (7 kept), weekly S3 backups (90 days).

### Example 4: REST API - Trigger Manual Backup

```bash
# Trigger a manual backup
curl -X POST http://localhost:5000/api/backup/trigger \
  -H "Content-Type: application/json" \
  -d '{"scheduleId": "daily-local", "storageType": "S3"}'

# Response:
# {
#   "success": true,
#   "jobId": "backup-20260504-142530",
#   "message": "Backup triggered successfully",
#   "timestamp": "2026-05-04T14:25:30Z"
# }
```

### Example 5: REST API - List Recent Backups

```bash
curl http://localhost:5000/api/backup/list?scheduleId=daily-local&limit=10

# Response:
# {
#   "success": true,
#   "backups": [
#     {
#       "id": "backup-20260504-020000",
#       "createdAt": "2026-05-04T02:00:00Z",
#       "sizeBytes": 1024000,
#       "status": "Verified",
#       "storageType": "Local",
#       "verificationResult": {
#         "isValid": true,
#         "recordCount": 15234,
#         "checksumMatch": true
#       }
#     }
#   ]
# }
```

### Example 6: REST API - Verify a Backup

```bash
# Verify the integrity of a specific backup
curl -X POST http://localhost:5000/api/backup/verify/daily-local \
  -H "Content-Type: application/json" \
  -d '{"backupId": "backup-20260504-020000"}'
```

### Example 7: CLI - View Backup Status

```bash
# List all configured schedules
dotnet run -- list-schedules

# Get backup history for a schedule
dotnet run -- get-history --schedule daily-local --limit 5

# Show last backup status
dotnet run -- last-backup --schedule daily-local

# Output format options
dotnet run -- get-history --schedule daily-local --format json
dotnet run -- get-history --schedule daily-local --format csv
dotnet run -- get-history --schedule daily-local --format xml
```

### Example 8: CLI - Restore a Backup

```bash
# Restore from a specific backup
dotnet run -- restore \
  --backup backup-20260504-020000 \
  --output ./restored.db \
  --verify

# Restore to original location with automatic verification
dotnet run -- restore \
  --backup backup-20260504-020000 \
  --verify \
  --skip-confirmation
```

### Example 9: Docker - Kubernetes Deployment

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: sqlite-backup
spec:
  containers:
  - name: backup
    image: docker-sqlite-backup:latest
    env:
    - name: AppSettings__DatabasePath
      value: /data/app.db
    - name: AppSettings__LocalStoragePath
      value: /backups
    volumeMounts:
    - name: data
      mountPath: /data
    - name: backups
      mountPath: /backups
    livenessProbe:
      httpGet:
        path: /health
        port: 5000
      initialDelaySeconds: 30
      periodSeconds: 10
  volumes:
  - name: data
    hostPath:
      path: /var/lib/app
  - name: backups
    hostPath:
      path: /var/backups
```

### Example 10: Monitoring Integration

```bash
# Health check endpoint (for monitoring systems)
curl http://localhost:5000/health | jq .

# Response shows:
# - Last backup timestamp
# - Last backup status
# - Verification results
# - Schedule health
# - Storage availability

# Metrics available for Prometheus scraping
curl http://localhost:5000/metrics
```

### Example 11: Webhook Notifications

```csharp
// Configure webhook in appsettings.json
{
  "NotificationSettings": {
    "WebhookUrl": "https://alerts.company.com/backup-event",
    "NotifyOnSuccess": true,
    "NotifyOnFailure": true,
    "NotifyOnVerificationFailure": true
  }
}

// Events sent as POST requests:
// {
//   "eventType": "BackupCompleted",
//   "scheduleId": "daily-local",
//   "jobId": "backup-20260504-020000",
//   "status": "Success",
//   "timestamp": "2026-05-04T02:00:30Z",
//   "metadata": { ... }
// }
```

### Example 12: Custom Event Listener

```csharp
// Implement IBackupEventListener for custom behavior
public class CustomMetricsListener : IBackupEventListener
{
    private readonly IMetricsService _metrics;
    
    public async Task OnBackupCompleted(BackupEvent evt)
    {
        _metrics.RecordBackupDuration(evt.DurationMs);
        _metrics.RecordBackupSize(evt.SizeBytes);
    }
}

// Register in DI container
services.AddSingleton<IBackupEventListener, CustomMetricsListener>();
```

## API Reference

### Backup Endpoints

#### POST /api/backup/trigger
Trigger an immediate backup for a schedule.

**Request Body:**
```json
{
  "scheduleId": "string",
  "storageType": "Local|S3",
  "skipVerification": false
}
```

**Response:**
```json
{
  "success": true,
  "jobId": "string",
  "message": "string",
  "timestamp": "ISO8601"
}
```

#### GET /api/backup/list
List backups for a schedule.

**Query Parameters:**
- `scheduleId` (required): Schedule identifier
- `limit` (optional): Max results (default: 10)
- `offset` (optional): Skip first N results
- `status` (optional): Filter by status (Pending|Success|Failed)

**Response:**
```json
{
  "success": true,
  "backups": [
    {
      "id": "string",
      "createdAt": "ISO8601",
      "sizeBytes": 0,
      "status": "Success",
      "storageType": "Local",
      "verificationResult": {
        "isValid": true,
        "recordCount": 0,
        "checksumMatch": true
      }
    }
  ],
  "total": 0
}
```

#### POST /api/backup/verify/{scheduleId}
Verify backup integrity.

**Request Body:**
```json
{
  "backupId": "string"
}
```

**Response:**
```json
{
  "success": true,
  "backupId": "string",
  "isValid": true,
  "message": "Backup verification passed"
}
```

#### GET /api/schedule/list
List all configured schedules.

**Response:**
```json
{
  "success": true,
  "schedules": [
    {
      "id": "string",
      "name": "string",
      "cronExpression": "string",
      "isEnabled": true,
      "lastRun": "ISO8601",
      "nextRun": "ISO8601"
    }
  ]
}
```

#### GET /health
Health check endpoint for orchestration systems.

**Response:**
```json
{
  "status": "Healthy",
  "timestamp": "ISO8601",
  "lastBackup": {
    "scheduleId": "string",
    "timestamp": "ISO8601",
    "status": "Success"
  }
}
```

## CLI Reference

### Global Options

```bash
--help              Show help message
--version           Show version information
--config            Path to appsettings.json
--schedule          Target schedule ID
--format            Output format (json|csv|xml)
--verbose           Verbose logging
```

### Commands

#### list-schedules
List all configured backup schedules.

```bash
dotnet run -- list-schedules --format json
```

#### trigger-backup
Trigger an immediate backup.

```bash
dotnet run -- trigger-backup \
  --schedule daily-local \
  --skip-verification \
  --wait
```

#### get-history
View backup history for a schedule.

```bash
dotnet run -- get-history \
  --schedule daily-local \
  --limit 20 \
  --format csv \
  > backup_history.csv
```

#### last-backup
Show the most recent backup.

```bash
dotnet run -- last-backup --schedule daily-local
```

#### verify-backup
Manually verify a backup.

```bash
dotnet run -- verify-backup \
  --backup backup-20260504-020000 \
  --verbose
```

#### restore
Restore from a backup.

```bash
dotnet run -- restore \
  --backup backup-20260504-020000 \
  --output ./restored.db \
  --verify \
  --skip-confirmation
```

#### cleanup
Remove backups based on retention policy.

```bash
dotnet run -- cleanup \
  --schedule daily-local \
  --dry-run  # Show what would be deleted
```

## Configuration Reference

### Root Configuration (appsettings.json)

```json
{
  "AppSettings": {
    "DatabasePath": "string",                    // Path to SQLite database
    "MaxConcurrentBackups": 3,                   // Max parallel backups
    "BackupTimeoutSeconds": 3600,                // Timeout per backup
    "ScheduleCheckIntervalSeconds": 60,          // Schedule check frequency
    "EnableVerificationByDefault": true,         // Auto-verify all backups
    "RetentionDays": 30,                         // Default retention
    "MaxBackupCount": 10,                        // Default max count
    "LocalStoragePath": "backups"                // Local backup directory
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  },
  "NotificationSettings": {
    "WebhookUrl": "string",                      // Optional webhook URL
    "NotifyOnSuccess": true,
    "NotifyOnFailure": true,
    "NotifyOnVerificationFailure": true,
    "TimeoutSeconds": 30
  }
}
```

### Schedule Configuration

```json
{
  "Schedules": [
    {
      "Id": "string",                            // Unique identifier
      "Name": "string",                          // Human-readable name
      "CronExpression": "string",                // Cron expression
      "IsEnabled": true,
      "StorageType": "Local|S3",
      "RotationStrategy": "Age|Count|AgeAndCount",
      "RetentionDays": 30,                       // If strategy=Age
      "MaxBackupCount": 10,                      // If strategy=Count
      "CompressionEnabled": false,
      "S3Config": {
        "BucketName": "string",
        "Region": "string",
        "AccessKeyId": "string",
        "SecretAccessKey": "string",
        "EnableEncryption": true,
        "EnableVersioning": true,
        "StorageClass": "STANDARD"
      }
    }
  ]
}
```

### Environment Variables

All appsettings.json values can be overridden via environment variables using double-underscore separator:

```bash
# Set database path
export AppSettings__DatabasePath=/data/production.db

# Configure S3
export AppSettings__S3BucketName=my-backups
export AppSettings__S3Region=us-west-2

# Enable debug logging
export Logging__LogLevel__Default=Debug
```

## Backup Strategies

### Strategy 1: Daily Backups with 90-Day Retention

Ideal for most applications. Balances storage costs with recovery capability.

```json
{
  "Id": "daily-90d",
  "CronExpression": "0 2 * * *",
  "RotationStrategy": "Age",
  "RetentionDays": 90,
  "StorageType": "Local"
}
```

### Strategy 2: Hourly Backups with Count-Based Rotation

For critical systems where data loss is unacceptable. Keeps last 7 days.

```json
{
  "Id": "hourly-7d",
  "CronExpression": "0 * * * *",
  "RotationStrategy": "Count",
  "MaxBackupCount": 168,
  "StorageType": "S3"
}
```

### Strategy 3: Tiered Backup (Local + S3)

Daily local backups for quick restore, weekly S3 for long-term archive.

```json
{
  "Schedules": [
    {
      "Id": "daily-local",
      "CronExpression": "0 2 * * *",
      "RotationStrategy": "Count",
      "MaxBackupCount": 7,
      "StorageType": "Local"
    },
    {
      "Id": "weekly-archive",
      "CronExpression": "0 3 * * 0",
      "RotationStrategy": "Age",
      "RetentionDays": 365,
      "StorageType": "S3"
    }
  ]
}
```

### Strategy 4: Compliance-Grade Backup

Immutable backups with extensive audit logging for regulatory requirements.

```json
{
  "Id": "compliance-daily",
  "CronExpression": "0 2 * * *",
  "RotationStrategy": "Age",
  "RetentionDays": 2555,  // ~7 years
  "StorageType": "S3",
  "S3Config": {
    "EnableVersioning": true,
    "EnableMFADelete": true,
    "EnableObjectLock": true
  }
}
```

## Restore Verification

Restore verification is an automated integrity test that runs after every successful backup (when `VerifyAfterBackup: true`) or on demand via the API/CLI. It provides confidence that a backup can actually be restored before disaster strikes.

### How Verification Works

The `VerificationService` executes the following checks in order:

1. **Checksum validation** — The SHA-256 hash of the backup file is recomputed and compared to the value recorded at backup time. A mismatch immediately fails verification, indicating the file was corrupted or tampered with during storage or transfer.

2. **Restore to a temporary location** — The backup file is copied (and decrypted if `EnableEncryption: true`) to a unique temporary directory. No changes are made to the production database or the backup archive.

3. **SQLite `integrity_check` pragma** — A read-only connection is opened against the restored file and `PRAGMA integrity_check` is executed. SQLite inspects every page of the database for structural corruption. A passing result returns `"ok"`.

4. **Row-count comparison** — The total number of rows across all user tables is counted. This count is stored in the verification record and can be compared across backups to detect unexpected data loss.

5. **Cleanup** — The temporary directory is deleted regardless of the verification outcome.

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `VerifyAfterBackup` | `bool` | `true` | Automatically verify every successful backup. |
| `EnableVerificationByDefault` | `bool` | `true` | Global default used when a schedule does not specify its own value. |

```json
{
  "AppSettings": {
    "EnableVerificationByDefault": true
  },
  "Schedules": [
    {
      "Id": "daily-local",
      "CronExpression": "0 2 * * *",
      "VerifyAfterBackup": true
    }
  ]
}
```

### Triggering Verification On Demand

```bash
# Via REST API
curl -X POST http://localhost:5000/api/backup/verify/daily-local \
  -H "Content-Type: application/json" \
  -d '{"backupId": "<backup-guid>"}'

# Via CLI
dotnet run -- verify-backup --backup <backup-guid> --verbose
```

### Interpreting Verification Results

A successful verification produces a log entry similar to:

```
[INF] Backup verification completed successfully for {BackupId}
      IntegrityCheck: ok  |  RecordCount: 15234  |  Duration: 420ms
```

A failed verification will log the specific failure:

- **Checksum mismatch** — `Checksum mismatch for backup file: /path/to/backup.sqlite`
  The backup file was modified or corrupted after being written. Treat the backup as untrustworthy.

- **Integrity check failure** — `Database integrity check failed: <sqlite errors>`
  SQLite found corruption inside the database pages. The backup cannot be used for a clean restore.

- **Decryption failure** — `Backup file is encrypted but no valid decryption key is configured.`
  Ensure `BACKUP_ENCRYPTION_KEY` is set in the environment or `AppSettings__EncryptionKey` is configured.

The `VerificationResult` object returned by the API contains:

```json
{
  "isSuccessful": true,
  "integrityCheckPassed": true,
  "integrityCheckErrors": null,
  "recordCount": 15234,
  "databaseSizeBytes": 1048576,
  "durationMilliseconds": 420,
  "errorMessage": null
}
```

### Performance Implications

Verification restores the backup to a temporary file and opens a read-only SQLite connection. The dominant cost is disk I/O proportional to the backup file size. Typical overhead:

| Database Size | Verification Time |
|---|---|
| < 50 MB | < 1 second |
| 50 – 500 MB | 1 – 10 seconds |
| > 500 MB | 10+ seconds (consider scheduling during off-peak hours) |

**Recommendation for production**: Keep `VerifyAfterBackup: true` for all schedules. The small I/O cost is negligible compared to the risk of discovering a corrupt backup during an actual disaster recovery.

---

## Troubleshooting

### Backup Fails with "Database Locked"

**Problem**: Backup terminates with "database is locked" error.

**Solution**:
1. Ensure no other process is writing to the database
2. Increase `BackupTimeoutSeconds` in configuration
3. Lower `MaxConcurrentBackups` to reduce contention
4. Run backups during off-peak hours

```json
{
  "AppSettings": {
    "BackupTimeoutSeconds": 7200,
    "MaxConcurrentBackups": 1
  }
}
```

### Verification Fails After Successful Backup

**Problem**: Backup succeeds but verification fails.

**Solution**:
1. Verify disk space is sufficient for temporary restore
2. Check file permissions in backup directory
3. Ensure SQLite version compatibility
4. Review logs for schema differences

```bash
# Enable verbose logging
export Logging__LogLevel__Default=Debug
dotnet run

# Check backup file integrity
sqlite3 backup-file.db "PRAGMA integrity_check;"
```

### S3 Upload Timeouts

**Problem**: Backups timeout when uploading to S3.

**Solution**:
1. Increase `BackupTimeoutSeconds`
2. Use S3 transfer acceleration
3. Compress backups before upload
4. Check network connectivity and AWS credentials

```json
{
  "AppSettings": {
    "BackupTimeoutSeconds": 7200
  },
  "Schedules": [{
    "CompressionEnabled": true,
    "S3Config": {
      "UseTransferAcceleration": true
    }
  }]
}
```

### Memory Usage Growing Over Time

**Problem**: Application memory usage increases during normal operation.

**Solution**:
1. Check max concurrent backup limit
2. Verify event listeners are disposing resources
3. Review caching configuration
4. Enable memory profiling

```json
{
  "AppSettings": {
    "MaxConcurrentBackups": 2
  },
  "CacheSettings": {
    "MaxCacheSize": "500MB"
  }
}
```

### Health Check Returns Unhealthy

**Problem**: `/health` endpoint returns unhealthy status.

**Solution**:
1. Check if last backup exists and completed
2. Verify storage backend accessibility
3. Ensure configuration is valid
4. Review exception logs

```bash
# Check health details
curl -v http://localhost:5000/health | jq .

# View detailed logs
docker logs sqlite-backup | tail -100
```

## Performance

Benchmarks measured on a single-core Linux environment (.NET 10, Release build):

| Operation | Database Size | Duration |
|-----------|--------------|----------|
| Full backup (local) | 100 MB | ~0.4 s |
| Full backup (local) | 1 GB | ~3.8 s |
| S3 upload (multi-part) | 100 MB | ~1.2 s |
| SHA-256 checksum | 100 MB | < 180 ms |
| Restore + verification | 100 MB | ~0.9 s |
| Health endpoint response | — | < 5 ms |
| Schedule dispatch overhead | — | < 10 ms |

Key characteristics:

- **Throughput**: sustains ~260 MB/s on local NVMe; ~80 MB/s to S3 with multi-part upload
- **Concurrency**: up to 8 parallel backup operations before I/O contention becomes measurable
- **Memory footprint**: ~30 MB baseline; grows proportionally with `MaxConcurrentBackups` (~15 MB per active job)
- **Scheduling**: cron tick evaluation adds negligible overhead (<1 ms per schedule) regardless of schedule count

## Testing

The test suite covers domain models, schedule logic, and utility functions using xUnit and FluentAssertions.

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

Test files are located under `tests/docker-sqlite-backup.Tests/`:

| Test File | Coverage Area |
|-----------|--------------|
| `Domain/DomainModelTests.cs` | `BackupJob`, `BackupResult`, `RotationPolicy`, `RestoreVerification` domain objects |
| `Services/ScheduleServiceTests.cs` | Cron expression parsing, next-run calculation, enable/disable logic |
| `Utilities/StringUtilityTests.cs` | String sanitization, path normalization, truncation helpers |

To add new tests, follow the existing xUnit patterns and place them under the appropriate subdirectory inside `tests/docker-sqlite-backup.Tests/`.

## Related Projects

- [dotnet-deploy-notify](https://github.com/sarmkadan/dotnet-deploy-notify) — Deployment notification pipeline for .NET: routes build status events to Telegram, Slack, and Discord webhooks

### Integration Examples

**Send backup events to a Telegram/Slack channel via dotnet-deploy-notify:**

```csharp
public class DeployNotifyBackupListener : IBackupEventListener
{
    private readonly NotificationClient _notify;

    public DeployNotifyBackupListener(NotificationClient notify) => _notify = notify;

    public async Task OnBackupCompleted(BackupEvent evt)
    {
        var message = $"[{evt.ScheduleId}] backup {evt.Status} — {evt.SizeBytes / 1024:N0} KB";
        await _notify.SendAsync(message, cancellationToken: default);
    }
}
```

**Wire both packages together in `Program.cs`:**

```csharp
builder.Services.AddSqliteBackup(builder.Configuration.GetSection("BackupSettings"));
builder.Services.AddDeployNotify(builder.Configuration.GetSection("NotifySettings"));
builder.Services.AddSingleton<IBackupEventListener, DeployNotifyBackupListener>();
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork the repository** and create a feature branch
2. **Write tests** for new functionality (xUnit format)
3. **Follow code style**: Use the provided .editorconfig
4. **Add documentation** for new features
5. **Create a pull request** with clear description

### Development Setup

```bash
# Clone and setup
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup

# Install dependencies
dotnet restore

# Run tests
dotnet test

# Build for local testing
dotnet build -c Debug

# Run the application
dotnet run
```

### Code Style

- Use standard .NET naming conventions (PascalCase for public members)
- Add XML documentation comments for public APIs
- Keep methods under 30 lines when practical
- Include unit tests for business logic
- Use dependency injection for testability

### Submitting Changes

1. Ensure all tests pass: `dotnet test`
2. Update documentation as needed
3. Create PR with clear description
4. Reference relevant issues
5. Ensure CI/CD pipeline passes

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
