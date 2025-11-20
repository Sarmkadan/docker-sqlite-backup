// entire file content ...
// ... goes in between

## NotificationEventListener

The `NotificationEventListener` class is responsible for sending notifications on important backup events. It supports multiple notification channels and can be extended with custom notification clients.

### Usage Example

```csharp
using DockerSqliteBackup.Events;

// Create a notification event listener
var listener = new NotificationEventListener(
    logger: new Logger<NotificationEventListener>(),
    webhookClient: new WebhookClient(),
    webhookUrl: "https://example.com/webhook");

// Add a custom notification client
listener.AddNotificationClient(new CustomNotificationClient());

// Handle a backup completed event
var completedEvent = new BackupCompletedEvent
{
    Result = new BackupResult
    {
        Id = Guid.NewGuid(),
        ScheduleId = Guid.NewGuid(),
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
        Status = BackupStatus.Success,
        SizeBytes = 1024 * 1024,
        FilePath = "/backups/app.db.20260714"
    },
    Duration = TimeSpan.FromMinutes(5)
};

await listener.HandleAsync(completedEvent);
```

## StorageConfiguration

The `StorageConfiguration` class represents configuration for different storage backends used by the backup system. It defines common properties shared across all storage types and provides validation capabilities to ensure the configuration is valid before use.


### Usage Example

```csharp
using DockerSqliteBackup.Domain;
using System;

// Create a local filesystem storage configuration
var localStorageConfig = new LocalStorageConfiguration
{
    Id = Guid.NewGuid(),
    Name = "Local Backup Storage",
    IsDefault = true,
    StoragePath = @"/backups/docker-sqlite-backup",
    CreatedAt = DateTime.UtcNow,
    LastModifiedAt = DateTime.UtcNow
};

// Create an S3 storage configuration
var s3StorageConfig = new S3StorageConfiguration
{
    Id = Guid.NewGuid(),
    Name = "AWS S3 Backup Storage",
    IsDefault = false,
    BucketName = "my-backups-bucket",
    Region = "us-east-1",
    AccessKey = "AKIAIOSFODNN7EXAMPLE",
    SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
    CreatedAt = DateTime.UtcNow,
    LastModifiedAt = DateTime.UtcNow
};

// Validate configuration
bool isValid = localStorageConfig.IsValid();
Console.WriteLine($"Local storage is valid: {isValid}");

bool s3IsValid = await s3StorageConfig.TestConnectionAsync();
Console.WriteLine($"S3 storage connection test: {s3IsValid}");
```

## BackupSchedule

The `BackupSchedule` class represents a scheduled backup configuration for a SQLite database. It defines all parameters needed to schedule, execute, and manage database backups including scheduling rules, retention policies, storage options, and notification settings.

### Usage Example

```csharp
using DockerSqliteBackup.Domain;
using System;

// Create a new backup schedule
var schedule = new BackupSchedule
{
    Id = Guid.NewGuid(),
    Name = "Daily Application Database Backup",
    Description = "Daily full backup of the main application database at 2 AM",
    DatabasePath = @"/app/data/application.db",
    CronExpression = "0 2 * * *", // Run daily at 2:00 AM
    IsActive = true,
    RetentionDays = 30, // Keep backups for 30 days
    MaxBackupCount = 10, // Keep up to 10 backup files
    NotificationEmails = "admin@example.com,devops@example.com",
    VerifyAfterBackup = true,
    StorageType = 0, // Local storage
    BackupMode = 0, // Full backup mode
    StorageConfiguration = null, // No additional storage configuration
    CreatedAt = DateTime.UtcNow,
    LastModifiedAt = DateTime.UtcNow
};

// Validate the schedule configuration
bool isValid = schedule.IsValid();
Console.WriteLine($"Schedule is valid: {isValid}");

// Validate database file exists
bool dbExists = schedule.ValidateDatabasePath();
Console.WriteLine($"Database exists: {dbExists}");

// Update next run time (typically handled by the scheduler)
schedule.NextRunTime = DateTime.Parse("2026-07-16T02:00:00Z");
```

## MetricsEventListener

The `MetricsEventListener` aggregates statistics from backup events, such as total backups, success/failure counts, data transferred, and average duration. It exposes a `BackupMetrics` snapshot that can be queried at any time and can be reset for a fresh start.

### Usage Example

```csharp
using DockerSqliteBackup.Events;
using Microsoft.Extensions.Logging;

// Create a logger factory (e.g., console logger)
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<MetricsEventListener>();

// Instantiate the metrics listener
var metricsListener = new MetricsEventListener(logger);

// Simulate handling a completed backup event
var completedEvent = new BackupCompletedEvent
{
    Result = new BackupResult
    {
        Id = Guid.NewGuid(),
        ScheduleId = Guid.NewGuid(),
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
        Status = BackupStatus.Success,
        SizeBytes = 10 * 1024 * 1024, // 10 MB
        FilePath = "/backups/app.db.20260714"
    },
    Duration = TimeSpan.FromSeconds(120)
};

await metricsListener.HandleAsync(completedEvent);

// Retrieve current metrics
BackupMetrics metrics = metricsListener.GetMetrics();
Console.WriteLine(metrics);

// Reset metrics if needed
metricsListener.ResetMetrics();
```
