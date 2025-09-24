# RestoreVerification

Represents the outcome of a database restore verification process. This type captures whether the restored database passed integrity checks, how many records were recovered, the size of the restored database, and timing information. It is created after a backup has been restored to a temporary directory and validated.

## API

### Properties

#### `public Guid Id`
Unique identifier for this verification record.

#### `public Guid BackupResultId`
The identifier of the associated backup result that this verification was performed against.

#### `public bool IsSuccessful`
Indicates whether the restore and verification process completed without errors and passed integrity checks.

#### `public string StatusMessage`
A human-readable summary of the verification outcome, such as success confirmation or a description of what failed.

#### `public DateTime StartedAt`
The UTC timestamp when the restore verification process began.

#### `public DateTime? CompletedAt`
The UTC timestamp when the restore verification process finished, or `null` if the process has not yet completed.

#### `public long DurationMilliseconds`
The total wall-clock time consumed by the restore and verification process, in milliseconds. Populated after completion.

#### `public long RecordCount`
The number of records found in the restored database. Used to confirm that the expected data volume was recovered.

#### `public long DatabaseSizeBytes`
The size of the restored database file on disk, in bytes.

#### `public bool IntegrityCheckPassed`
Indicates whether the database integrity check (e.g., SQLite `PRAGMA integrity_check`) returned a clean result.

#### `public string? IntegrityCheckErrors`
If `IntegrityCheckPassed` is `false`, contains the error output from the integrity check. `null` when the check passed or was not executed.

#### `public string TemporaryDirectory`
The path to the temporary directory where the backup was restored for verification purposes.

#### `public string? ErrorMessage`
If the verification process encountered an exception or fatal error, contains the error details. `null` when no errors occurred.

### Methods

#### `public void MarkCompleted()`
Marks the verification as finished by setting `CompletedAt` to the current UTC time and calculating `DurationMilliseconds` from `StartedAt`. Call this once when the restore and all checks have concluded.

| Aspect | Detail |
|---|---|
| **Parameters** | None |
| **Returns** | Nothing |
| **Throws** | Does not throw |

#### `public TimeSpan GetElapsedDuration()`
Returns the elapsed time between `StartedAt` and either `CompletedAt` (if set) or the current UTC time (if still in progress).

| Aspect | Detail |
|---|---|
| **Parameters** | None |
| **Returns** | `TimeSpan` representing the elapsed duration |
| **Throws** | Does not throw |

## Usage

### Example 1: Recording a successful verification

```csharp
var verification = new RestoreVerification
{
    Id = Guid.NewGuid(),
    BackupResultId = backupResult.Id,
    IsSuccessful = true,
    StatusMessage = "Restore and integrity check passed",
    StartedAt = DateTime.UtcNow,
    TemporaryDirectory = "/tmp/restore-abc123",
    RecordCount = 154_320,
    DatabaseSizeBytes = 8_912_896,
    IntegrityCheckPassed = true
};

// Simulate restore and verification work
PerformRestore(verification.TemporaryDirectory);
verification.IntegrityCheckPassed = RunIntegrityCheck(verification.TemporaryDirectory);

verification.MarkCompleted();

Console.WriteLine($"Verification took {verification.GetElapsedDuration().TotalSeconds:F1}s");
```

### Example 2: Handling a failed integrity check

```csharp
var verification = new RestoreVerification
{
    Id = Guid.NewGuid(),
    BackupResultId = backupResult.Id,
    StartedAt = DateTime.UtcNow,
    TemporaryDirectory = "/tmp/restore-def456"
};

try
{
    RestoreBackup(backupResult.FilePath, verification.TemporaryDirectory);
    var (passed, errors) = CheckIntegrity(verification.TemporaryDirectory);

    verification.IntegrityCheckPassed = passed;
    verification.IntegrityCheckErrors = passed ? null : errors;
    verification.IsSuccessful = passed;
    verification.StatusMessage = passed
        ? "Verification succeeded"
        : $"Integrity check failed: {errors}";
    verification.RecordCount = CountRecords(verification.TemporaryDirectory);
    verification.DatabaseSizeBytes = new FileInfo(
        Path.Combine(verification.TemporaryDirectory, "restored.db")).Length;
}
catch (Exception ex)
{
    verification.IsSuccessful = false;
    verification.StatusMessage = "Verification threw an exception";
    verification.ErrorMessage = ex.ToString();
}
finally
{
    verification.MarkCompleted();
}

if (!verification.IsSuccessful)
{
    LogWarning($"Restore verification {verification.Id} failed: {verification.ErrorMessage ?? verification.IntegrityCheckErrors}");
}
```

## Notes

- **Incomplete state**: Before `MarkCompleted()` is called, `CompletedAt` is `null` and `DurationMilliseconds` is zero. `GetElapsedDuration()` will return the running duration from `StartedAt` to the current moment.
- **Integrity check semantics**: `IntegrityCheckPassed` being `true` does not automatically imply `IsSuccessful` is `true`—the caller is responsible for setting `IsSuccessful` based on the overall outcome, which may include factors beyond the integrity check (e.g., record count mismatches).
- **Thread safety**: This type is not inherently thread-safe. If multiple threads access a `RestoreVerification` instance—particularly calling `MarkCompleted()` while another reads `CompletedAt` or `DurationMilliseconds`—external synchronization is required.
- **Temporary directory lifetime**: The `TemporaryDirectory` path is recorded for reference. Cleanup of that directory is the caller's responsibility and is not performed by this type.
- **Error message vs. integrity errors**: `ErrorMessage` captures infrastructure or process-level failures (exceptions during restore), while `IntegrityCheckErrors` captures database-level corruption detected by the integrity check. Both can be non-null simultaneously if an integrity check reported errors and a subsequent cleanup step threw an exception.
