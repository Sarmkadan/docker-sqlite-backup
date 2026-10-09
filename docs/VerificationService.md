# VerificationService

Implements `IVerificationService`. Checks that a backup is usable: it verifies the file checksum, restores the backup to a temporary database, runs a SQLite integrity check, counts records, persists the result through `IBackupRepository`, and publishes a verification event.

Source: `src/Services/VerificationService.cs`
Interface: `src/Services/IVerificationService.cs`

## Constructor

```csharp
public VerificationService(
    IBackupRepository repository,
    AppSettings appSettings,
    ILogger<VerificationService> logger,
    IBackupEventPublisher? eventPublisher = null)
```

- `repository`: stores `RestoreVerification` records and reads verification history.
- `appSettings`: supplies `EncryptionKey` as a fallback for decrypting `.enc` backups.
- `logger`: receives progress and failure logs.
- `eventPublisher`: optional. When set, a `RestoreVerificationCompletedEvent` or `RestoreVerificationFailedEvent` is published after each run.

The class is `sealed`. It is registered as a singleton in `src/Configuration/ServiceCollectionExtensions.cs`.

## Methods

### `Task<RestoreVerification> VerifyBackupAsync(BackupResult backup, CancellationToken cancellationToken = default)`

Runs the full verification pipeline for one backup:

1. Verifies the checksum of `backup.BackupFilePath` against `backup.Checksum`. On mismatch it throws `BackupCorruptedException`.
2. Restores the backup to a temporary database with `RestoreToTemporaryAsync`.
3. Runs `PerformIntegrityCheckAsync` on the restored file. On failure it throws `IntegrityCheckFailedException`.
4. Records database size in bytes and the total record count.
5. Marks the verification as completed and publishes a completion event, if a publisher is configured.
6. In `finally`, deletes the temporary directory.
7. Saves the `RestoreVerification` through the repository and returns it.

Errors are not rethrown. Any exception is caught, logged, and stored on the result: `verification.ErrorMessage` is set and the status is marked as failed. A failure event is published with a failure stage:

| Exception | `RestoreVerificationFailureStage` |
|---|---|
| `IntegrityCheckFailedException` | `IntegrityCheckFailed` |
| `RestoreVerificationFailedException` | `RestoreFailed` |
| `BackupCorruptedException` | `ChecksumMismatch` |
| anything else | `Unknown` |

Callers must check the returned object's status. They will not see an exception for a failed verification.

The `cancellationToken` is passed only to the event publisher. It does not cancel the checksum, restore, or integrity steps.

### `Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId)`

Returns the stored verification records for the given backup. It delegates to `IBackupRepository.GetVerificationHistoryAsync`.

### `Task<(bool IsValid, string? Errors)> PerformIntegrityCheckAsync(string databasePath)`

Opens the file read-only with Microsoft.Data.Sqlite and runs `PRAGMA integrity_check`.

- Returns `(true, null)` when the result is `ok`.
- Returns `(false, result)` with SQLite's output otherwise.
- Throws `ArgumentNullException` if `databasePath` is null (the package's own `ArgumentNullException` type, not the BCL one).
- Throws `ArgumentException` if the path is empty or whitespace.
- Throws `FileNotFoundException` if the file does not exist.
- Wraps unexpected errors in `VerificationException`. `SqliteException`, `IOException`, and `UnauthorizedAccessException` are not wrapped and propagate as they are.

### `Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum)`

Computes the SHA256 hash of the file and compares it with `expectedChecksum`, ignoring case.

- If `expectedChecksum` is null or empty, it logs a warning and returns `true`. A missing checksum therefore passes verification.

### `Task<string> RestoreToTemporaryAsync(BackupResult backup)`

Creates `Path.GetTempPath()/sqlite-verify-<guid>/restore-check.sqlite` and fills it according to the file extension:

- `.enc`: decrypts with AES-256. The key is read from the `BACKUP_ENCRYPTION_KEY` environment variable, or from `AppSettings.EncryptionKey` if the variable is not set. If no valid key is available, it throws `VerificationException` with the backup ID.
- Compressed extension (`BackupConstants.CompressedBackupExtension`): decompresses with `GZipStream`.
- Any other extension: copies the file as-is.

Returns the path to the temporary database file. The caller is responsible for cleanup. `VerifyBackupAsync` does this automatically.

### `Task CleanupTemporaryFilesAsync(string tempDirectory)`

Recursively deletes `tempDirectory` if it exists. The work runs on the thread pool.

## Usage

```csharp
public sealed class BackupVerifier(IVerificationService verification)
{
    public async Task<bool> CheckAsync(BackupResult backup, CancellationToken ct)
    {
        var result = await verification.VerifyBackupAsync(backup, ct);
        return result.IntegrityCheckPassed;
    }
}
```

`RestoreVerification` is updated by `MarkCompleted(bool successful, string statusMessage)`, which sets the final status and message. `IntegrityCheckPassed` is a `bool`.

## Notes

- `CountRecordsAsync` (private) sums `COUNT(*)` across all tables in `sqlite_master`. If it fails, it logs a warning and returns `0`. A `0` therefore does not prove the database is empty.
- The integrity check and the record count open the restored database read-only.
- Temporary files are removed in `finally`, so a failed run does not leave a `sqlite-verify-*` directory behind.
- `BackupCorruptedException`, `IntegrityCheckFailedException`, and `RestoreVerificationFailedException` are domain exceptions. See `docs/VerificationException.md` for the base type.
