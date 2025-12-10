# Getting Started with Docker SQLite Backup

This guide will help you set up and run Docker SQLite Backup in under 15 minutes.

## Prerequisites

Before starting, ensure you have:

- **.NET 10 SDK** installed ([download](https://dotnet.microsoft.com/download))
- **SQLite database** file you want to backup
- **Docker** (optional, for containerized setup)
- **AWS Account** (optional, for S3 storage)

### Verify Prerequisites

```bash
# Check .NET installation
dotnet --version  # Should be 10.x or higher

# Check SQLite installation
sqlite3 --version

# Optional: Check Docker installation
docker --version
```

## Step 1: Clone the Repository

```bash
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup
```

## Step 2: Configure Your Database

Create or update `appsettings.json` with your database path:

```json
{
  "AppSettings": {
    "DatabasePath": "/path/to/your/database.db",
    "LocalStoragePath": "./backups",
    "MaxConcurrentBackups": 3,
    "BackupTimeoutSeconds": 3600,
    "EnableVerificationByDefault": true,
    "RetentionDays": 30,
    "MaxBackupCount": 10
  },
  "Schedules": [
    {
      "Id": "daily-backup",
      "Name": "Daily Backup",
      "CronExpression": "0 2 * * *",
      "IsEnabled": true,
      "StorageType": "Local",
      "RotationStrategy": "AgeAndCount"
    }
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Key Configuration Fields

| Field | Purpose | Example |
|-------|---------|---------|
| `DatabasePath` | Path to your SQLite database | `/app/data.db` |
| `LocalStoragePath` | Directory for local backups | `./backups` |
| `CronExpression` | When to run backups | `0 2 * * *` = 2 AM daily |
| `RetentionDays` | Keep backups for N days | `30` |
| `MaxBackupCount` | Keep maximum N backups | `10` |

### Cron Expression Guide

Common cron expressions for backup schedules:

```
0 2 * * *       Daily at 2 AM
0 */4 * * *     Every 4 hours
0 0 * * 0       Weekly on Sunday at midnight
0 0 1 * *       Monthly on the 1st at midnight
*/30 * * * *    Every 30 minutes
*/15 9-17 * * * Every 15 minutes during 9 AM - 5 PM
```

Online tool: [crontab.guru](https://crontab.guru)

## Step 3: Run the Application

### Option A: Local Development

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release

# Run the application
dotnet run --configuration Release
```

The application starts and begins monitoring for scheduled backups.

### Option B: Docker

```bash
# Build the Docker image
docker build -t sqlite-backup:latest .

# Run with Docker
docker run -d \
  --name sqlite-backup \
  -v /path/to/database:/data \
  -v $(pwd)/backups:/app/backups \
  -e AppSettings__DatabasePath=/data/database.db \
  -p 5000:5000 \
  sqlite-backup:latest

# View logs
docker logs -f sqlite-backup
```

### Option C: Docker Compose

```bash
# Start using docker-compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

## Step 4: Verify Setup

### Check Health Status

```bash
# Health check endpoint
curl http://localhost:5000/health

# Expected response:
# {
#   "status": "Healthy",
#   "timestamp": "2026-05-04T12:00:00Z",
#   "lastBackup": { ... }
# }
```

### List Configured Schedules

```bash
curl http://localhost:5000/api/schedule/list

# Or via CLI
dotnet run -- list-schedules
```

### Trigger a Manual Backup

```bash
# Trigger immediately
curl -X POST http://localhost:5000/api/backup/trigger \
  -H "Content-Type: application/json" \
  -d '{"scheduleId": "daily-backup"}'

# Response should show success:
# {
#   "success": true,
#   "jobId": "backup-20260504-...",
#   "message": "Backup triggered successfully"
# }
```

## Step 5: Verify Backup Creation

### Check Local Backups

```bash
# List backups
ls -lah backups/

# View backup details
du -sh backups/*

# Example output:
# backup-20260504-020000.db    45M
# backup-20260503-020000.db    44M
```

### Verify Backup Integrity

```bash
# Check if backup is valid SQLite database
sqlite3 backups/backup-20260504-020000.db "SELECT COUNT(*) FROM sqlite_master WHERE type='table';"

# Expected: Shows number of tables in the backup
```

### View Backup Details via API

```bash
curl "http://localhost:5000/api/backup/list?scheduleId=daily-backup&limit=5"

# Shows backup metadata, verification results, storage location
```

## Step 6: Set Up Production Configuration

### For AWS S3 Storage

Update `appsettings.json`:

```json
{
  "Schedules": [
    {
      "Id": "s3-backup",
      "CronExpression": "0 3 * * *",
      "StorageType": "S3",
      "S3Config": {
        "BucketName": "my-company-backups",
        "Region": "us-east-1",
        "AccessKeyId": "${AWS_ACCESS_KEY_ID}",
        "SecretAccessKey": "${AWS_SECRET_ACCESS_KEY}",
        "EnableEncryption": true,
        "EnableVersioning": true,
        "StorageClass": "STANDARD_IA"
      }
    }
  ]
}
```

Or use environment variables:

```bash
export AWS_ACCESS_KEY_ID=your_key
export AWS_SECRET_ACCESS_KEY=your_secret
export AppSettings__S3BucketName=my-backups
export AppSettings__S3Region=us-east-1
```

### For Kubernetes Deployment

See `docs/deployment.md` for complete Kubernetes setup.

## Monitoring Your Backups

### Check Recent Backups

```bash
# CLI command
dotnet run -- get-history --schedule daily-backup --limit 10 --format json

# Or via API
curl "http://localhost:5000/api/backup/list?scheduleId=daily-backup"
```

### Enable Detailed Logging

For debugging, enable more verbose logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Set Up Webhooks for Alerts

```json
{
  "NotificationSettings": {
    "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
    "NotifyOnSuccess": false,
    "NotifyOnFailure": true,
    "NotifyOnVerificationFailure": true
  }
}
```

## Common Issues & Solutions

### "Database locked" error

This occurs when the database is being written to during backup.

**Solution**: Run backups during off-peak hours or increase timeout:

```json
{
  "AppSettings": {
    "BackupTimeoutSeconds": 7200,
    "MaxConcurrentBackups": 1
  },
  "Schedules": [{
    "CronExpression": "0 2 * * *"  // 2 AM when app is idle
  }]
}
```

### Backups not being created

Check that the schedule is enabled and cron expression is correct:

```bash
# View configured schedules
dotnet run -- list-schedules

# Check application logs
docker logs sqlite-backup 2>&1 | grep -i schedule

# Manually trigger to test
curl -X POST http://localhost:5000/api/backup/trigger \
  -H "Content-Type: application/json" \
  -d '{"scheduleId": "daily-backup"}'
```

### Storage permission errors

Ensure the application has write permissions:

```bash
# Check backup directory permissions
ls -ld backups/

# Grant write permissions if needed
chmod 755 backups/
```

## Next Steps

1. **Configure Retention**: Adjust `RetentionDays` and `MaxBackupCount` for your needs
2. **Set Up Verification**: Enable automated verification in `EnableVerificationByDefault`
3. **Enable Monitoring**: Configure webhooks or check health endpoint regularly
4. **Schedule Rotation**: Set up multiple schedules for different retention strategies
5. **Test Restore**: Periodically test restoration to ensure backups are usable

See `docs/deployment.md` for production deployment best practices.
