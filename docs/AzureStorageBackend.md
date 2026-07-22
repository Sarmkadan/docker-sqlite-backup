# AzureStorageBackend

The `AzureStorageBackend` class provides an implementation of a backup storage backend using Azure Blob Storage. It enables uploading, downloading, deleting, and listing backups, as well as testing connectivity and checking available space. This class is intended for use in the `docker-sqlite-backup` project to manage SQLite database backups stored in Azure.

## API

### `AzureStorageBackend()`

Initializes a new instance of the `AzureStorageBackend` class.

### `public async Task<string> UploadBackupAsync(...)`

Uploads a backup file to Azure Blob Storage.

- **Purpose**: Stores a backup in the configured Azure Blob Storage container.
- **Parameters**: (not documented – see implementation for details)
- **Return value**: A `Task<string>` that resolves to the URI of the uploaded blob.
- **Throws**: Throws an exception if the upload fails (e.g., network error, authentication failure, container not found).

### `public async Task<string> DownloadBackupAsync(...)`

Downloads a backup from Azure Blob Storage.

- **Purpose**: Retrieves a backup from the configured container and saves it locally.
- **Parameters**: (not documented – see implementation for details)
- **Return value**: A `Task<string>` that resolves to the local file path of the downloaded backup.
- **Throws**: Throws an exception if the download fails (e.g., blob not found, network error, insufficient local disk space).

### `public async Task DeleteBackupAsync(...)`

Deletes a backup from Azure Blob Storage.

- **Purpose**: Removes a specific backup blob from the container.
- **Parameters**: (not documented – see implementation for details)
- **Return value**: A `Task` representing the asynchronous operation.
- **Throws**: Throws an exception if the deletion fails (e.g., blob does not exist, permission denied).

### `public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync()`

Lists all backups stored in the Azure Blob Storage container.

- **Purpose**: Returns metadata for each backup blob.
- **Parameters**: None.
- **Return value**: A `Task` that resolves to a collection of tuples. Each tuple contains:
  - `Path` (string): The blob name or path.
  - `Size` (long): The size of the blob in bytes.
  - `Modified` (DateTime): The last modified timestamp of the blob.
- **Throws**: Throws an exception if listing fails (e.g., container does not exist, network error).

### `public async Task<bool> TestConnectionAsync()`

Tests connectivity to Azure Blob Storage.

- **Purpose**: Verifies that the storage account is reachable and the credentials are valid.
- **Parameters**: None.
- **Return value**: A `Task<bool>` that resolves to `true` if the connection is successful, `false` otherwise.
- **Throws**: Does not throw; returns `false` on failure.

### `public async Task<long> GetAvailableSpaceAsync()`

Retrieves the available space in the Azure Blob Storage account or container.

- **Purpose**: Returns the remaining storage capacity, typically in bytes.
- **Parameters**: None.
- **Return value**: A `Task<long>` that resolves to the available space in bytes.
- **Throws**: Throws an exception if the space cannot be determined (e.g., API error, insufficient permissions).

## Usage

### Example 1: Upload and Download a Backup

```csharp
using var backend = new AzureStorageBackend(); // Assumes configuration is set elsewhere

// Upload a local SQLite backup file
string backupPath = "/backups/daily_2025-03-15.db";
string blobUri = await backend.UploadBackupAsync(backupPath);
Console.WriteLine($"Uploaded to: {blobUri}");

// Download the same backup to a different location
string localCopy = await backend.DownloadBackupAsync("daily_2025-03-15.db");
Console.WriteLine($"Downloaded to: {localCopy}");
```

### Example 2: List Backups and Delete Old Ones

```csharp
using var backend = new AzureStorageBackend();

// List all backups
var backups = await backend.ListBackupsAsync();
foreach (var (path, size, modified) in backups)
{
    Console.WriteLine($"{path} - {size} bytes - {modified}");
}

// Delete backups older than 30 days
var cutoff = DateTime.UtcNow.AddDays(-30);
foreach (var (path, _, modified) in backups)
{
    if (modified < cutoff)
    {
        await backend.DeleteBackupAsync(path);
        Console.WriteLine($"Deleted old backup: {path}");
    }
}
```

## Notes

- **Thread safety**: All public methods are asynchronous and can be called concurrently from multiple threads. The class does not maintain mutable shared state across calls, so it is safe to reuse a single instance.
- **Edge cases**:
  - If the Azure Blob Storage container does not exist, `UploadBackupAsync`, `DownloadBackupAsync`, `DeleteBackupAsync`, and `ListBackupsAsync` will throw an exception. `TestConnectionAsync` will return `false`.
  - `DownloadBackupAsync` expects the blob to exist; if it does not, an exception is thrown.
  - `DeleteBackupAsync` throws if the blob does not exist; consider checking existence via `ListBackupsAsync` before deletion.
  - Network interruptions or transient Azure service errors may cause any method to throw. Retry logic should be implemented by the caller.
  - `GetAvailableSpaceAsync` may return a value that is not perfectly up-to-date due to caching in Azure Storage; it is suitable for approximate capacity planning.
- **Configuration**: The constructor likely requires connection string and container name. Ensure these are provided before calling any methods.
