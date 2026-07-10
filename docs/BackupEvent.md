# BackupEvent

The `BackupEvent` base class provides a standard structure for representing system events within the `docker-sqlite-backup` workflow, enabling consistent logging, tracking, and auditing of backup operations. Each event encapsulates metadata required for tracing, including a unique event identifier and a correlation ID to link related operations across the backup lifecycle.

## API

### Base Class: BackupEvent
*   `public string EventType`: The identifier for the type of event (e.g., "backup.started", "backup.failed").
*   `public Guid EventId`: A unique identifier for the specific event instance.
*   `public DateTime OccurredAt`: The timestamp when the event was recorded.
*   `public string CorrelationId`: A shared identifier used to group related events across different operations.

### Subclasses

#### BackupStartedEvent
*   `public BackupStartedEvent()`: Constructor initializing the event with type "backup.started".
*   `public BackupSchedule Schedule`: The schedule configuration triggering the backup.
*   `public DateTime StartTime`: The specific timestamp the backup process began.

#### BackupCompletedEvent
*   `public BackupCompletedEvent()`: Constructor initializing the event with type "backup.completed".
*   `public BackupResult Result`: The final outcome of the backup operation.
*   `public TimeSpan Duration`: The total time elapsed during the backup process.

#### BackupFailedEvent
*   `public BackupFailedEvent()`: Constructor initializing the event with type "backup.failed".
*   `public Guid ScheduleId`: The identifier of the schedule associated with the failed backup.
*   `public string ErrorMessage`: A descriptive message detailing the cause of the failure.
*   `public string? StackTrace`: The stack trace associated with the failure, if available.

#### BackupRetryEvent
*   `public BackupRetryEvent()`: Constructor initializing the event with type "backup.retry".
*   `public Guid ScheduleId`: The identifier of the schedule being retried.
*   `public int AttemptNumber`: The current attempt sequence number.
*   `public string? PreviousError`: A summary or description of the error from the immediately preceding failed attempt.

#### ScheduleCreatedEvent
*   `public ScheduleCreatedEvent()`: Constructor initializing the event with type "schedule.created".
*   `public BackupSchedule Schedule`: The newly created backup schedule definition.

## Usage

### Example 1: Handling a Completed Backup
```csharp
public void OnBackupCompleted(BackupCompletedEvent e)
{
    Console.WriteLine($"Backup completed for correlation {e.CorrelationId}.");
    Console.WriteLine($"Duration: {e.Duration.TotalSeconds} seconds, Status: {e.Result}");
}
```

### Example 2: Responding to a Backup Failure
```csharp
public void HandleFailure(BackupFailedEvent e)
{
    logger.LogError($"Backup failed for schedule {e.ScheduleId}: {e.ErrorMessage}");
    if (e.StackTrace != null)
    {
        logger.LogDebug(e.StackTrace);
    }
}
```

## Notes

*   **Immutability:** Event objects should be treated as immutable once constructed. Properties should be considered read-only after the object has been dispatched through the event system.
*   **Thread Safety:** While instances are generally thread-safe if implemented as immutable objects, the consuming code must ensure that it handles events in a thread-safe manner, particularly when updating shared state or logging to shared sinks.
*   **Correlation ID:** The `CorrelationId` is critical for tracing in scenarios where the backup process spans asynchronous tasks or multiple services. It should be propagated through the entire lifecycle of a backup operation to maintain traceability.
