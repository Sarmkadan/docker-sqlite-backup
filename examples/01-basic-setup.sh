#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 1: Basic SQLite Backup Setup
# This script demonstrates the most basic setup for automated SQLite backups.

set -e

echo "=== Docker SQLite Backup - Basic Setup ==="

# Configuration
DATABASE_PATH="${1:-.data/app.db}"
BACKUP_PATH="${2:-.backups}"
BACKUP_INTERVAL="${3:-daily}"  # daily, hourly, weekly

# Create backup directory
mkdir -p "$BACKUP_PATH"
echo "✓ Created backup directory: $BACKUP_PATH"

# Create sample appsettings.json if it doesn't exist
if [ ! -f "appsettings.json" ]; then
    cat > appsettings.json <<EOF
{
  "AppSettings": {
    "DatabasePath": "$DATABASE_PATH",
    "MaxConcurrentBackups": 3,
    "BackupTimeoutSeconds": 3600,
    "ScheduleCheckIntervalSeconds": 60,
    "EnableVerificationByDefault": true,
    "RetentionDays": 30,
    "MaxBackupCount": 10,
    "LocalStoragePath": "$BACKUP_PATH"
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
      "Default": "Information",
      "System": "Warning",
      "Microsoft": "Warning"
    }
  }
}
EOF
    echo "✓ Created appsettings.json"
else
    echo "✓ Using existing appsettings.json"
fi

# Build the project
echo "Building application..."
dotnet restore > /dev/null 2>&1
dotnet build -c Release > /dev/null 2>&1
echo "✓ Build completed"

# Run the application
echo "Starting Docker SQLite Backup..."
echo ""
echo "Configuration:"
echo "  Database: $DATABASE_PATH"
echo "  Backups: $BACKUP_PATH"
echo "  Schedule: Daily at 2 AM"
echo ""
echo "API available at: http://localhost:5000"
echo "Check health: curl http://localhost:5000/health"
echo ""

dotnet run --configuration Release
