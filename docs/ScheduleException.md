# ScheduleException

The `ScheduleException` class is the base exception type used within the `docker-sqlite-backup` application to encapsulate errors occurring during the processing or execution of scheduled tasks. By providing a structured mechanism for error reporting, it allows callers to distinguish scheduling-related failures from other system exceptions and optionally associate the error with a specific `ScheduleId`.

## API

### Members
* **`public Guid? ScheduleId`**
  Gets the unique identifier of the schedule associated with this exception. Returns `null` if the exception is not related to a specific schedule.

* **`public ScheduleException()`**
  Initializes a new instance of the `ScheduleException` class with default values.

* **`public ScheduleException(string message)`**
  Initializes a new instance of the `ScheduleException` class with a specified error message.
  * `message`: The message that describes the error.

* **`public ScheduleException(string message, Guid scheduleId)`**
  Initializes a new instance of the `ScheduleException` class with a specified error message and the ID of the related schedule.
  * `message`: The message that describes the error.
  * `scheduleId`: The `Guid` of the schedule that triggered the exception.

### Associated Exception Types
* **`public InvalidCronExpressionException`**
  An associated exception type derived from `ScheduleException`, thrown when a provided cron expression does not conform to the expected format.

* **`public InvalidScheduleException`**
  An associated exception type derived from `ScheduleException`, thrown when a schedule configuration is invalid or inconsistent.

## Usage

### Basic Exception Handling
```csharp
try
{
    // Logic that might fail during schedule processing
    throw new ScheduleException("An unexpected error occurred during the backup schedule execution.");
}
catch (ScheduleException ex)
{
    Console.WriteLine($"Scheduling error: {ex.Message}");
}
```

### Throwing with Schedule Context
```csharp
Guid currentScheduleId = Guid.NewGuid();

try
{
    // Validate or execute specific schedule
    throw new ScheduleException("The specified schedule is currently disabled.", currentScheduleId);
}
catch (ScheduleException ex) when (ex.ScheduleId.HasValue)
{
    Console.WriteLine($"Error in schedule {ex.ScheduleId}: {ex.Message}");
}
```

## Notes

* **Thread Safety:** Like other exception types, `ScheduleException` is safe for use in multi-threaded applications; however, the exception instances themselves are typically immutable once thrown.
* **Guid.Empty:** If `ScheduleId` is initialized with `Guid.Empty`, it should be treated as an unset or invalid identifier, even though `Guid?` allows `null` to represent the absence of an ID.
* **Base Classes:** This exception inherits from the standard `System.Exception` class and supports all standard exception features, including inner exceptions and stack trace capture.
