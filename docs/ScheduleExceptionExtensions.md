# ScheduleExceptionExtensions

The `ScheduleExceptionExtensions` class provides a set of static extension methods designed to simplify the handling and categorization of schedule-related exceptions within the `docker-sqlite-backup` library. These utilities enable consistent wrapping of generic errors into domain-specific exception types and provide helper methods for identifying schedule-related failures during execution.

## API

### WithCronExpression
Converts a generic exception into a specialized `InvalidCronExpressionException`, augmenting it with the associated cron expression string.

*   **Parameters:**
    *   `exception` (Exception): The original exception to be wrapped.
    *   `cronExpression` (string): The invalid cron expression that triggered the failure.
*   **Returns:** `InvalidCronExpressionException`
*   **Throws:** `ArgumentNullException` if `exception` is null.

### WithScheduleContext
Converts a generic exception into a specialized `InvalidScheduleException`, augmenting it with relevant scheduling context.

*   **Parameters:**
    *   `exception` (Exception): The original exception to be wrapped.
    *   `context` (string): Contextual information describing the schedule state at the time of failure.
*   **Returns:** `InvalidScheduleException`
*   **Throws:** `ArgumentNullException` if `exception` is null.

### IsScheduleException
Determines whether the provided exception is a recognized schedule-related exception type handled by the library.

*   **Parameters:**
    *   `exception` (Exception): The exception to evaluate.
*   **Returns:** `bool` (`true` if the exception is recognized as a schedule exception; otherwise, `false`).

### IsInvalidCronExpression
Checks if the provided exception specifically indicates an invalid cron expression.

*   **Parameters:**
    *   `exception` (Exception): The exception to evaluate.
*   **Returns:** `bool` (`true` if the exception is an `InvalidCronExpressionException`; otherwise, `false`).

## Usage

### Example 1: Categorizing and Logging Exceptions
```csharp
try
{
    // Schedule processing logic
}
catch (Exception ex)
{
    if (ex.IsScheduleException())
    {
        logger.LogError("A schedule-related error occurred: {Message}", ex.Message);
    }
    else
    {
        throw;
    }
}
```

### Example 2: Wrapping Exceptions with Context
```csharp
try
{
    ValidateCron(cronExpression);
}
catch (Exception ex) when (!ex.IsInvalidCronExpression())
{
    // Wrap generic validation errors into the specific domain exception
    throw ex.WithCronExpression(cronExpression);
}
```

## Notes

*   **Null Handling:** All extension methods check for a null `exception` argument and will throw an `ArgumentNullException` if one is provided.
*   **Thread Safety:** As these are static extension methods that do not modify shared state, they are inherently thread-safe for use in concurrent environments.
*   **Performance:** These methods are lightweight wrappers intended for exception handling blocks, where the performance impact of object allocation during exception throwing is generally acceptable.
