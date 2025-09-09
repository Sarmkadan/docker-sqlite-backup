# BackupException

`BackupException` is the base exception class for errors encountered during SQLite database backup operations within the `docker-sqlite-backup` system. It provides a structured way to report failure details while associating the error with specific identifiers for the backup task or the associated schedule, allowing for precise diagnostics in automated backup workflows.

## API

### Constructors

*   `BackupException()`
    Initializes a new instance of the `BackupException` class with a default message.
*   `BackupException(string message)`
    Initializes a new instance of the `BackupException` class with a specified error message.
*   `BackupException(string message, Guid backupId)`
    Initializes a new instance of the `BackupException` class with a specified error message and the unique identifier of the failed backup task.
*   `BackupException(string message, Guid scheduleId, Guid backupId)`
    Initializes a new instance of the `BackupException` class with a specified error message, the schedule identifier, and the backup identifier involved in the failure.

### Properties

*   `public Guid? BackupId { get; }`
    Gets the unique identifier of the backup task associated with this exception, if provided.
*   `public Guid? ScheduleId { get; }`
    Gets the unique identifier of the backup schedule associated with this exception, if provided.

### Related Exception Types

These exceptions inherit from or are functionally related to `BackupException` within the namespace:

*   `DatabaseAccessException`
*   `BackupTimeoutException`
*   `BackupCorruptedException`

## Usage

### Catching and Logging a Backup Failure

```csharp
try 
{
    backupService.ExecuteBackup(backupId);
}
catch (BackupException ex) when (ex.BackupId.HasValue)
{
    logger.LogError(ex, "Backup operation {BackupId} failed: {Message}", ex.BackupId, ex.Message);
}
catch (BackupException ex)
{
    logger.LogError(ex, "General backup error: {Message}", ex.Message);
}
```

### Throwing a Specialized Exception from a Service

```csharp
if (isDatabaseLocked)
{
    throw new BackupException("Unable to acquire database lock for backup.", scheduleId, backupId);
}
```

## Notes

*   **Thread Safety**: `BackupException` instances are immutable after instantiation and are thread-safe.
*   **Nullability**: The `BackupId` and `ScheduleId` properties are nullable. Consumers must check for the presence of these identifiers (`HasValue`) before accessing them if they were not provided to the constructor.
*   **Inheritance**: `BackupException` is designed to be the base class for specific error scenarios. When defining new exception types for the backup system, it is recommended to inherit from `BackupException` to maintain consistent error handling patterns.
