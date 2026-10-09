# StorageService

`StorageService` (`src/Services/StorageService.cs`) is the single entry point for storage operations in docker-sqlite-backup. It implements `IStorageService` and uses the strategy pattern: it picks an `IStorageBackend` based on the runtime type of the `StorageConfiguration` and forwards the call to it. It also normalizes errors, so callers only deal with `StorageException`.

Registration: `src/Configuration/ServiceCollectionExtensions.cs` registers it as a singleton, `IStorageService -> StorageService`.

## Dependencies

| Dependency | Purpose |
|---|---|
| `ILogger<StorageService>` | Logs upload start and failures. |
| `IServiceProvider` | Resolves the concrete backend on each call. |

Backends resolved from `IServiceProvider` (must be registered in DI):

| Configuration type | Backend |
|---|---|
| `LocalStorageConfiguration` | `LocalStorageBackend` |
| `S3Configuration` | `S3StorageBackend` |
| `AzureConfiguration` | `AzureStorageBackend` |

Any other `StorageConfiguration` subtype throws `StorageException("Unknown storage configuration type: ...")`.

## Public API

### `Task<string> UploadBackupAsync(string filePath, StorageConfiguration config)`

Uploads a local backup file to the backend.

- Returns the remote key or path of the uploaded file.
- Checks, in order: `filePath` is not null, `config` is not null, `filePath` is not empty or whitespace, and the file exists.
- If the file does not exist, throws `LocalStorageException` (not wrapped). This check happens before any backend is resolved.
- Any non-`StorageException` error from the backend is logged at Error level and rethrown as `StorageException("Failed to upload backup to storage", inner)`.

### `Task<string> DownloadBackupAsync(string storagePath, StorageConfiguration config)`

Downloads a backup from storage to a local temporary location.

- Returns the local path of the downloaded file.
- Checks that `storagePath` and `config` are not null, and that `storagePath` is not empty or whitespace.
- Backend errors are wrapped in `StorageException("Failed to download backup from storage", inner)`.

### `Task DeleteBackupAsync(string storagePath, StorageConfiguration config)`

Deletes a backup from storage.

- No upfront argument validation. Null or invalid arguments fail inside the backend and are wrapped as `StorageException("Failed to delete backup from storage", inner)`.

### `Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync(StorageConfiguration config)`

Lists backup files in the backend.

- Returns path, size in bytes, and last-modified time for each file.
- No upfront argument validation. Backend errors are wrapped as `StorageException("Failed to list backups from storage", inner)`.

### `Task<bool> TestConnectionAsync(StorageConfiguration config)`

Checks whether the backend is reachable.

- Returns `true` on success and `false` on any exception. It never throws.

### `Task<long> GetAvailableSpaceAsync(StorageConfiguration config)`

Returns free space in bytes on the backend.

- Returns `0` on any exception. It never throws.
- A `0` result therefore means either "no space" or "could not determine". Callers cannot tell these apart.

## Error handling summary

| Method | Invalid input | Backend error | Missing file |
|---|---|---|---|
| `UploadBackupAsync` | `ArgumentNullException` / `ArgumentException` | `StorageException` | `LocalStorageException` |
| `DownloadBackupAsync` | `ArgumentNullException` / `ArgumentException` | `StorageException` | n/a |
| `DeleteBackupAsync` | Wrapped in `StorageException` | `StorageException` | n/a |
| `ListBackupsAsync` | Wrapped in `StorageException` | `StorageException` | n/a |
| `TestConnectionAsync` | `false` | `false` | n/a |
| `GetAvailableSpaceAsync` | `0` | `0` | n/a |

Exceptions that are already `StorageException` pass through unchanged in every method.

## Known issues

1. **Swapped arguments in `ArgumentException`.** In `UploadBackupAsync` and `DownloadBackupAsync`, the code calls `new ArgumentException(nameof(filePath), "...")`. The constructor signature is `(message, paramName)`, so the parameter name ends up as the message and the real message becomes the parameter name. Correct form: `new ArgumentException("File path cannot be null or whitespace.", nameof(filePath))`. The same applies to `storagePath`.
2. **Silent failures.** `TestConnectionAsync` and `GetAvailableSpaceAsync` catch every exception without logging. A misconfigured backend looks the same as an unreachable one, and no diagnostic is recorded.
3. **Inconsistent validation.** Only `UploadBackupAsync` and `DownloadBackupAsync` validate arguments up front. Delete and list rely on the backend to fail, which produces a wrapped `StorageException` with a less useful message.
4. **Null config in `GetBackendForConfig`.** A null `config` reaches the switch and `config.GetType()` throws `NullReferenceException`. Delete and list wrap this in `StorageException`, and the two "never throws" methods return `false` or `0`.

## Usage example

```csharp
var config = new LocalStorageConfiguration { /* ... */ };

try
{
    string remotePath = await storageService.UploadBackupAsync("/data/backups/app-2026-10-09.db.gz", config);
    logger.LogInformation("Uploaded to {RemotePath}", remotePath);
}
catch (StorageException ex)
{
    logger.LogError(ex, "Backup upload failed");
}
```

## Related

- `src/Services/IStorageService.cs` - public contract
- `src/Services/IStorageBackend.cs` - backend strategy interface
- `docs/LocalStorageBackend.md`, `docs/S3StorageBackend.md`, `docs/AzureStorageBackend.md` - backend implementations
- `tests/docker-sqlite-backup.Tests/StorageServiceTests.cs` - unit tests
