# LocalStorageBackend

Provides a local filesystem-based implementation of a backup storage backend. It stores backup files in a designated directory on disk and exposes asynchronous operations for uploading, downloading, deleting, listing, and verifying connectivity to the storage location. The class is intended for use in the `docker-sqlite-backup` project where backups of SQLite databases are managed locally.

## API

### `public LocalStorageBackend(string basePath)`

Initializes a new instance of the `LocalStorageBackend` class.

- **Parameters**  
  `basePath` – The root directory where backup files will be stored. The directory is created if it does not exist.

- **Throws**  
  `ArgumentException` if `basePath` is `null` or empty.  
  `UnauthorizedAccessException` if the process does not have read/write permission for the specified path.

---

### `public async Task<string> UploadBackupAsync(string backupName, Stream data)`

Uploads a backup file to the local storage.

- **Parameters**  
  `backupName` – The name (including optional relative path) of the backup file.  
  `data` – A `Stream` containing the backup content to be written.

- **Returns**  
  The full absolute path of the stored backup file.

- **Throws**  
  `ArgumentNullException` if `backupName` or `data` is `null`.  
  `IOException` if the file cannot be created or written (e.g., disk full, permission denied).  
  `StorageException` (or a derived exception) if the operation fails due to a storage-specific error.

---

### `public async Task<string> DownloadBackupAsync(string backupName)`

Downloads (copies) a backup file from local storage to a temporary location and returns its path.

- **Parameters**  
  `backupName` – The name of the backup file to retrieve.

- **Returns**  
  The absolute path of a temporary file containing the backup content. The caller is responsible for deleting this file after use.

- **Throws**  
  `ArgumentNullException` if `backupName` is `null`.  
  `FileNotFoundException` if the backup file does not exist.  
  `IOException` if the file cannot be read or the temporary copy fails.

---

### `public async Task DeleteBackupAsync(string backupName)`

Deletes a backup file from local storage.

- **Parameters**  
  `backupName` – The name of the backup file to delete.

- **Returns**  
  A `Task` representing the asynchronous operation.

- **Throws**  
  `ArgumentNullException` if `backupName` is `null`.  
  `FileNotFoundException` if the backup file does not exist.  
  `IOException` if the file cannot be deleted (e.g., locked by another process).

---

### `public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync()`

Lists all backup files currently stored in the base directory.

- **Parameters**  
  None.

- **Returns**  
  A collection of tuples, each containing:  
  - `Path` – The relative path of the backup file (relative to the base directory).  
  - `Size` – The file size in bytes.  
  - `Modified` – The last write time (UTC) of the file.

- **Throws**  
  `DirectoryNotFoundException` if the base directory has been deleted externally.  
  `UnauthorizedAccessException` if the process cannot enumerate the directory.

---

### `public async Task<bool> TestConnectionAsync()`

Tests whether the local storage backend is accessible and writable.

- **Parameters**  
  None.

- **Returns**  
  `true` if the base directory exists and a temporary file can be created and deleted; otherwise `false`.

- **Throws**  
  This method does not throw exceptions; it returns `false` on failure.

---

### `public async Task<long> GetAvailableSpaceAsync()`

Returns the amount of free disk space available on the drive containing the base directory.

- **Parameters**  
  None.

- **Returns**  
  The number of free bytes on the volume.

- **Throws**  
  `IOException` if the disk information cannot be retrieved.  
  `PlatformNotSupportedException` on non-Windows/non-Linux platforms where the underlying API is unavailable.

## Usage

### Example 1: Upload a backup and list all backups

```csharp
using var backend = new LocalStorageBackend("/var/backups/sqlite");
using var stream = File.OpenRead("/tmp/mydb.sqlite");

string uploadedPath = await backend.UploadBackupAsync("daily/mydb-2025-03-15.bak", stream);
Console.WriteLine($"Uploaded to: {uploadedPath}");

var backups = await backend.ListBackupsAsync();
foreach (var (path, size, modified) in backups)
{
    Console.WriteLine($"{path} - {size} bytes - {modified:u}");
}
```

### Example 2: Download a backup, verify connectivity, and delete

```csharp
var backend = new LocalStorageBackend("/var/backups/sqlite");

bool connected = await backend.TestConnectionAsync();
if (!connected)
{
    Console.Error.WriteLine("Storage backend is not accessible.");
    return;
}

string tempPath = await backend.DownloadBackupAsync("weekly/mydb-2025-03-10.bak");
try
{
    // Process the backup file at tempPath
    Console.WriteLine($"Backup downloaded to {tempPath}");
}
finally
{
    File.Delete(tempPath);
}

await backend.DeleteBackupAsync("weekly/mydb-2025-03-10.bak");
Console.WriteLine("Backup deleted.");
```

## Notes

- **Thread safety**: This class is not thread-safe. Concurrent calls to `UploadBackupAsync`, `DeleteBackupAsync`, or `ListBackupsAsync` from multiple threads may lead to race conditions or file system errors. External synchronization (e.g., a lock or a dedicated scheduler) is recommended when sharing a single instance.
- **File name collisions**: `UploadBackupAsync` overwrites existing files with the same `backupName`. To avoid accidental data loss, callers should ensure unique names or check existence via `ListBackupsAsync` before uploading.
- **Temporary files from `DownloadBackupAsync`**: The returned temporary file is not automatically cleaned up. The caller must delete it after use to avoid disk space leaks.
- **Base directory removal**: If the base directory is deleted externally after the `LocalStorageBackend` instance is created, subsequent operations will throw `DirectoryNotFoundException` (except `TestConnectionAsync`, which returns `false`).
- **Disk space**: `GetAvailableSpaceAsync` reflects the free space at the time of the call. It does not reserve space; a subsequent upload may still fail with `IOException` if the disk becomes full in the meantime.
- **Path traversal**: The `backupName` parameter is used directly as a relative path. Callers should sanitize input to prevent directory traversal attacks (e.g., `../../etc/passwd`). The backend does not perform validation beyond basic null checks.
