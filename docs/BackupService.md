# BackupService

The `BackupService` class provides the core functionality for managing SQLite database backups within the `docker-sqlite-backup` project. It handles the execution of backup operations, verification of data integrity via checksums, retrieval of historical backup records, and the lifecycle management of stored backup files through asynchronous methods designed for non-blocking I/O operations.

## API

### `public BackupService`
Initializes a new instance of the `BackupService` class. This constructor sets up the necessary internal dependencies required to interact with the storage backend and the SQLite database engine.

### `public async Task<BackupResult> ExecuteBackupAsync`
Initiates a new backup operation for the configured SQLite database.
*   **Purpose**: Creates a snapshot of the current database state and saves it to the designated storage location.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<BackupResult>` containing the metadata of the newly created backup, including the file path, timestamp, and size.
*   **Exceptions**: Throws an exception if the source database is locked, if there is insufficient storage space, or if write permissions are denied.

### `public async Task<string> CalculateBackupChecksumAsync`
Computes a cryptographic hash for a specific backup file to verify data integrity.
*   **Purpose**: Generates a checksum (typically SHA256) to ensure the backup file has not been corrupted or altered since creation.
*   **Parameters**: None (operates on the context of the most recent or specified backup depending on internal state configuration).
*   **Return Value**: Returns a `Task<string>` representing the hexadecimal checksum string.
*   **Exceptions**: Throws an exception if the backup file cannot be found or is inaccessible during the read operation.

### `public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync`
Retrieves a list of all previously executed backups.
*   **Purpose**: Enumerates stored backup records to allow for audit trails or restoration point selection.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<IEnumerable<BackupResult>>` containing a collection of `BackupResult` objects ordered by creation time.
*   **Exceptions**: Throws an exception if the metadata store is corrupted or inaccessible.

### `public async Task DeleteBackupAsync`
Permanently removes a specific backup from the storage system.
*   **Purpose**: Frees up storage space by deleting an unwanted or obsolete backup file and its associated metadata.
*   **Parameters**: None (targets the backup specified in the current operational context or requires implementation-specific identification).
*   **Return Value**: Returns a `Task` that completes when the deletion is finished.
*   **Exceptions**: Throws an exception if the specified backup does not exist or if the process lacks delete permissions for the target file.

### `public async Task<BackupResult?> GetBackupResultAsync`
Fetches the details of a specific backup operation.
*   **Purpose**: Retrieves metadata for a single backup entry, useful for verifying the status of a specific job.
*   **Parameters**: None.
*   **Return Value**: Returns a `Task<BackupResult?>`. If the backup is found, it returns the result object; otherwise, it returns `null`.
*   **Exceptions**: Generally does not throw for missing items (returns `null`), but may throw if the underlying storage system encounters a critical error.

## Usage

### Example 1: Executing a Backup and Verifying Integrity
This example demonstrates how to trigger a backup and immediately calculate its checksum to ensure the file was written correctly.

```csharp
var backupService = new BackupService();

try 
{
    // Execute the backup
    var result = await backupService.ExecuteBackupAsync();
    Console.WriteLine($"Backup created at: {result.FilePath}");

    // Calculate and verify checksum
    var checksum = await backupService.CalculateBackupChecksumAsync();
    Console.WriteLine($"Verification Hash: {checksum}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Backup operation failed: {ex.Message}");
}
```

### Example 2: Reviewing History and Cleaning Up Old Backups
This example retrieves the backup history and deletes the oldest entry if more than a certain number of backups exist.

```csharp
var backupService = new BackupService();

// Retrieve full history
var history = await backupService.GetBackupHistoryAsync();
var backupList = history.ToList();

if (backupList.Count > 5)
{
    // Identify the oldest backup (assuming list is sorted by date)
    var oldestBackup = backupList.First();
    
    Console.WriteLine($"Pruning old backup: {oldestBackup.Id}");
    
    // Delete the oldest backup
    await backupService.DeleteBackupAsync();
    
    // Confirm deletion by fetching specific result
    var verification = await backupService.GetBackupResultAsync();
    if (verification == null)
    {
        Console.WriteLine("Oldest backup successfully removed.");
    }
}
```

## Notes

*   **Thread Safety**: As the methods are asynchronous and likely share internal state regarding the database connection or file handles, concurrent calls to `ExecuteBackupAsync` should be serialized by the caller to prevent database locking errors (`SQLiteBusy`).
*   **Null Handling**: Consumers of `GetBackupResultAsync` must explicitly handle the `null` return case, indicating the requested backup metadata does not exist, rather than relying on exceptions for control flow.
*   **Resource Disposal**: While the service methods are asynchronous, the underlying file streams opened during `ExecuteBackupAsync` or `CalculateBackupChecksumAsync` are managed internally. However, long-running instances of `BackupService` should be monitored to ensure no file handles remain open if the application domain is unloaded unexpectedly.
*   **Idempotency**: `DeleteBackupAsync` is not idempotent; calling it multiple times for the same logical backup without re-validation may result in exceptions after the first successful deletion.
