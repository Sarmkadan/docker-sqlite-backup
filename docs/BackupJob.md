# BackupJob

Represents a scheduled backup operation for SQLite databases running in Docker containers. Tracks the job lifecycle from creation through completion or failure, including retry attempts and timing metrics.

## API

### `public Guid Id`
Unique identifier for the backup job. Assigned at creation and immutable for the lifetime of the job.

### `public Guid ScheduleId`
Identifier of the parent backup schedule that created this job. Used to correlate jobs with their originating schedules.

### `public int Status`
Current execution status of the job. Follows a state machine where transitions are managed by the methods `MarkStarted`, `MarkCompleted`, and `IncrementRetry`.

### `public DateTime CreatedAt`
Timestamp when the job was created. Set once during initialization and never modified.

### `public DateTime? StartedAt`
Timestamp when the job began execution. `null` if the job has not started. Set by `MarkStarted`.

### `public DateTime? CompletedAt`
Timestamp when the job finished execution. `null` if the job is incomplete. Set by `MarkCompleted`.

### `public int RetryCount`
Number of retry attempts already performed for this job. Incremented by `IncrementRetry`.

### `public int MaxRetries`
Maximum number of retry attempts allowed before the job is considered failed. Configured at job creation and immutable.

### `public bool IsProcessing`
Indicates whether the job is currently being processed. Derived from the presence of `StartedAt` and absence of `CompletedAt`.

### `public BackupResult? Result`
Outcome of the backup operation if completed. `null` if the job is incomplete or failed without producing a result. Set by `MarkCompleted`.

### `public TimeSpan GetElapsedTime()`
Returns the total elapsed time of the job.

- **Returns**: `TimeSpan` representing the duration from `CreatedAt` to `CompletedAt` if completed; otherwise, duration from `CreatedAt` to `StartedAt` if started; otherwise, `TimeSpan.Zero`.
- **Throws**: `InvalidOperationException` if `StartedAt` is `null` and `CompletedAt` is `null`.

### `public void MarkStarted()`
Transitions the job from pending to in-progress.

- **Throws**: `InvalidOperationException` if the job has already started (`StartedAt` is not `null`) or is completed (`CompletedAt` is not `null`).

### `public void MarkCompleted(BackupResult result)`
Finalizes the job as successfully completed.

- **Parameters**:
  - `result`: Non-null outcome of the backup operation.
- **Throws**:
  - `ArgumentNullException` if `result` is `null`.
  - `InvalidOperationException` if the job has already been completed or not yet started.

### `public void IncrementRetry()`
Increments the retry counter and updates internal state to reflect a retry attempt.

- **Throws**: `InvalidOperationException` if the job has already been completed or the maximum retry count has been reached (`RetryCount >= MaxRetries`).

## Usage
