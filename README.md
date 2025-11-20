## ScheduleException

The `ScheduleException` class represents a custom exception for schedule-related errors. It provides information about the schedule ID that caused the error and a detailed error message.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to validate a schedule
    var scheduleService = new MyScheduleService();
    scheduleService.ValidateSchedule();
}
catch (ScheduleException ex)
{
    Console.WriteLine($"Schedule error for schedule ID '{ex.ScheduleId}': {ex.Message}");
}
```

## IntegrityCheckerServiceTestsExtensions

The `IntegrityCheckerServiceTestsExtensions` class provides a set of extension methods for testing the `IntegrityCheckerService`. It offers methods for creating test databases with various characteristics and for verifying the service's behavior.

### Usage Example

```csharp
using docker_sqlite_backup.Services;

// Create a test database with complex data
var complexTestDatabase = IntegrityCheckerServiceTestsExtensions.CreateComplexTestDatabase();

// Check that the service indicates the database is healthy
var mockService = IntegrityCheckerServiceTestsExtensions.CreateMockService();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateHealthy(mockService, complexTestDatabase);

// Check that the service indicates corruption in a corrupted database
var corruptedDatabase = IntegrityCheckerServiceTestsExtensions.CreateCorruptedDatabase();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateCorruption(mockService, corruptedDatabase);
```

## StringUtilityTestsExtensions

The `StringUtilityTestsExtensions` class provides a set of utility methods for testing string manipulation operations. It includes methods for formatting, transforming, and validating strings in test scenarios.

### Usage Example

```csharp
using docker_sqlite_backup.Tests.Utilities;

// Test string truncation and case conversion
var longText = "ThisIsAVeryLongStringThatNeedsTruncating";
var truncated = StringUtilityTestsExtensions.TruncateForTest(longText, 10);
var camelCase = StringUtilityTestsExtensions.ToCamelCaseForTest("example_string");

// Validate and format strings
var validEmail = "test@example.com";
var invalidEmail = "bad-email";
var isValid = StringUtilityTestsExtensions.IsValidEmailForTest(validEmail);

// Join strings with readable formatting
var parts = new[] { "apple", "banana", "cherry" };
var joined = StringUtilityTestsExtensions.JoinReadableForTest(parts);
```

## ValidationException

The `ValidationException` class represents a custom exception for validation errors. It provides information about the parameter that caused the validation failure and a collection of error messages.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to validate a value
    var validator = new MyValidator();
    validator.Validate("invalid-value");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed for parameter '{ex.ParameterName}': {string.Join(", ", ex.Errors?.Select(e => e.Key + ": " + e.Value))}");
}
```

## ConfigurationException

The `ConfigurationException` class represents a custom exception for configuration errors. It provides information about the configuration key that caused the error and a detailed error message.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to load a configuration value
    var config = new MyConfig();
    var value = config.Load("invalid-key");
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration error for key '{ex.ConfigurationKey}': {ex.Message}");
}
```

## BackupException

The `BackupException` class represents a custom exception for backup-related errors. It provides information about the backup ID, schedule ID, and the underlying exception that caused the error.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to perform a backup
    var backupService = new MyBackupService();
    backupService.Backup();
}
catch (BackupException ex)
{
    Console.WriteLine($"Backup error for backup ID '{ex.BackupId}': {ex.Message}");
}
```

## BackupEvent

The `BackupEvent` class is the base class for all domain events in the backup system. It provides common event tracking properties such as `EventType`, `EventId`, `OccurredAt`, and `CorrelationId`. All backup-related events inherit from this class, including events for backup lifecycle management (started, completed, failed, retry) and schedule management (created, updated, deleted).


### Usage Example

```csharp
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Domain;

// Create a backup started event
var startedEvent = new BackupStartedEvent
{
    Schedule = new BackupSchedule
    {
        Id = Guid.NewGuid(),
        Name = "daily-backup",
        SourcePath = "/data/app.db",
        DestinationPath = "/backups/app.db",
        ScheduleType = ScheduleType.Daily,
        ScheduleExpression = "0 2 * * *"
    },
    StartTime = DateTime.UtcNow
};

Console.WriteLine($"Event Type: {startedEvent.EventType}");
Console.WriteLine($"Event ID: {startedEvent.EventId}");
Console.WriteLine($"Occurred At: {startedEvent.OccurredAt}");
Console.WriteLine($"Correlation ID: {startedEvent.CorrelationId}");

// Create a backup completed event
var completedEvent = new BackupCompletedEvent
{
    Result = new BackupResult
    {
        Id = Guid.NewGuid(),
        ScheduleId = startedEvent.Schedule.Id,
        StartedAt = startedEvent.StartTime,
        CompletedAt = DateTime.UtcNow,
        Status = BackupStatus.Success,
        SizeBytes = 1024 * 1024,
        FilePath = "/backups/app.db.20260714"
    },
    Duration = TimeSpan.FromMinutes(5)
};

// Create a backup failed event
var failedEvent = new BackupFailedEvent
{
    ScheduleId = startedEvent.Schedule.Id,
    ErrorMessage = "Failed to connect to database",
    StackTrace = "System.Data.Sqlite.SqliteException: Connection timeout"
};

// Create a schedule created event
var scheduleCreatedEvent = new ScheduleCreatedEvent
{
    Schedule = new BackupSchedule
    {
        Id = Guid.NewGuid(),
        Name = "weekly-archive",
        SourcePath = "/data/archive.db",
        DestinationPath = "/backups/archive.db",
        ScheduleType = ScheduleType.Weekly,
        ScheduleExpression = "0 3 * * 0"
    }
};
```