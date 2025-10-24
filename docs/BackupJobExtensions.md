# BackupJobExtensions

The `BackupJobExtensions` static class provides a set of extension methods that simplify querying the state, progress, and result of a `BackupJob` instance. These methods are designed to be used in a fluent or conditional manner, allowing callers to inspect job status without accessing internal fields directly.

## API

### `IsSuccessful(this BackupJob job)`

Returns `true` if the backup job has completed and its final result indicates success; otherwise `false`.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `bool` – `true` when the job is in a successful terminal state.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `IsFailed(this BackupJob job)`

Returns `true` if the backup job has completed and its final result indicates failure; otherwise `false`.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `bool` – `true` when the job is in a failed terminal state.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `IsPending(this BackupJob job)`

Returns `true` if the backup job has not yet started execution; otherwise `false`.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `bool` – `true` when the job is queued or waiting to begin.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `IsInProgress(this BackupJob job)`

Returns `true` if the backup job is currently executing; otherwise `false`.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `bool` – `true` when the job is actively running.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `GetFormattedDuration(this BackupJob job)`

Returns a human-readable string representing the elapsed or total duration of the backup job.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `string` – A formatted duration (e.g., `"00:02:34"`). If the job has not started, returns a zero-duration representation.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `GetRetryProgress(this BackupJob job)`

Returns a string describing the current retry attempt and the maximum number of retries configured for the job.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `string` – A progress indicator such as `"Attempt 2 of 5"`. If retries are not applicable, returns a default value.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `HasExceededRetries(this BackupJob job)`

Returns `true` if the backup job has exhausted all allowed retry attempts; otherwise `false`.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `bool` – `true` when the number of retries performed equals or exceeds the configured maximum.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

### `GetResult(this BackupJob job)`

Returns the final result of the backup job, or `null` if the job has not yet completed.

- **Parameters**  
  `job` – The `BackupJob` instance to evaluate. Must not be `null`.

- **Returns**  
  `BackupResult?` – A nullable `BackupResult` value. `null` indicates the job is still running or has not started.

- **Exceptions**  
  `ArgumentNullException` – if `job` is `null`.

## Usage

### Example 1: Checking job completion and duration

```csharp
BackupJob job = scheduler.GetJob("daily-backup");

if (job.IsSuccessful())
{
    Console.WriteLine($"Backup completed successfully in {job.GetFormattedDuration()}");
}
else if (job.IsFailed())
{
    Console.WriteLine($"Backup failed after {job.GetFormattedDuration()}");
}
```

### Example 2: Monitoring retry progress

```csharp
BackupJob job = scheduler.GetJob("nightly-backup");

if (job.IsInProgress())
{
    Console.WriteLine($"Backup in progress – {job.GetRetryProgress()}");
}

if (job.HasExceededRetries())
{
    Console.WriteLine("Backup has exceeded retry limit, manual intervention required.");
}
```

## Notes

- All extension methods throw `ArgumentNullException` if the `job` parameter is `null`. Always ensure the job instance is not null before calling these methods.
- The methods are read-only and do not modify the state of the `BackupJob`. They are safe to call from multiple threads concurrently, provided the underlying `BackupJob` instance is not being mutated by another thread at the same time. For best results, use these methods after the job’s state has settled (e.g., after a state change event).
- `GetFormattedDuration` and `GetRetryProgress` return meaningful strings even for jobs that have not started; they will not throw in those cases.
- `GetResult` returns `null` for jobs that are still pending or in progress. Check `IsPending`, `IsInProgress`, or `IsSuccessful`/`IsFailed` to determine the exact state before relying on the result value.
