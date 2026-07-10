# VerificationException

Represents an exception that occurs when a verification operation on a SQLite backup fails. This base type is used to signal that a backup or restore process did not pass integrity or consistency checks. It carries optional identifiers and error details to help diagnose the failure.

## API

### `public Guid? BackupId`

Gets the unique identifier of the backup that failed verification, if one was provided. This property is `null` when the exception was created without a backup ID.

### Constructors

#### `public VerificationException(string message)`

Initializes a new instance of the exception with a specified error message.  
**Parameters:**  
- `message` – A human-readable description of the error.

#### `public VerificationException(string message, Guid backupId)`

Initializes a new instance with a message and the backup identifier that caused the failure.  
**Parameters:**  
- `message` – A human-readable description of the error.  
- `backupId` – The unique identifier of the backup that failed verification.

#### `public VerificationException()`

Initializes a new instance with no message or backup ID.

### `public string? Errors`

Gets a detailed error string, if available, that describes the specific verification failures. This property may be `null` if no detailed errors were captured.

### `public class IntegrityCheckFailedException : VerificationException`

A nested exception type thrown when an integrity check (e.g., `PRAGMA integrity_check`) on a backup file fails. This class inherits all members of `VerificationException` and can be caught separately to handle integrity-specific failures.

### `public class RestoreVerificationFailedException : VerificationException`

A nested exception type thrown when a restore operation fails verification after completion. This class inherits all members of `VerificationException` and allows callers to distinguish restore verification failures from other verification errors.

## Usage

### Example 1: Catching a verification exception with backup ID

```csharp
try
{
    var backupResult = BackupService.CreateBackup(databasePath, backupPath);
    if (!backupResult.IsVerified)
    {
        throw new VerificationException("Backup verification failed", backupResult.BackupId);
    }
}
catch (VerificationException ex)
{
    Console.WriteLine($"Verification failed for backup {ex.BackupId}: {ex.Message}");
    if (ex.Errors != null)
    {
        Console.WriteLine($"Details: {ex.Errors}");
    }
}
```

### Example 2: Handling specific derived exception types

```csharp
try
{
    RestoreService.RestoreFromBackup(backupPath, databasePath);
}
catch (VerificationException.RestoreVerificationFailedException ex)
{
    // Restore completed but verification of the restored database failed
    Log.Error($"Restore verification failed for backup {ex.BackupId}: {ex.Message}");
    // Attempt a fallback restore from a different backup
}
catch (VerificationException.IntegrityCheckFailedException ex)
{
    // The backup file itself is corrupt
    Log.Error($"Backup integrity check failed: {ex.Errors}");
    // Notify the user to obtain a valid backup
}
catch (VerificationException ex)
{
    // Generic verification error
    Log.Error($"Verification error: {ex.Message}");
}
```

## Notes

- The `BackupId` property is `null` when the exception is created using the parameterless constructor or the constructor that takes only a message. Always check for `null` before using the value.
- The `Errors` property may contain multiline text or be `null`. It is intended for diagnostic purposes and should not be parsed programmatically.
- `IntegrityCheckFailedException` and `RestoreVerificationFailedException` are nested types of `VerificationException`. They can be caught in any order, but more specific catches should appear before the base type.
- Thread safety: Instances of `VerificationException` are immutable after construction. Reading properties from the same instance across multiple threads is safe. Constructors are not thread-safe; each instance should be created and thrown on a single thread.
