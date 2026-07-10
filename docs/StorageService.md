# StorageService
The `StorageService` class provides a set of methods for interacting with a storage system, allowing users to upload, download, delete, and list backups, as well as test connections and retrieve available space. This class is designed to be used in the context of the `docker-sqlite-backup` project, where it enables the management of SQLite database backups in a Docker environment.

## API
* `public StorageService`: The constructor for the `StorageService` class, which initializes a new instance of the service.
* `public async Task<string> UploadBackupAsync`: Uploads a backup to the storage system. The method returns a string representing the uploaded backup's identifier. This method may throw exceptions if the upload process fails or if the storage system is unavailable.
* `public async Task<string> DownloadBackupAsync`: Downloads a backup from the storage system. The method returns a string representing the downloaded backup's contents. This method may throw exceptions if the download process fails or if the storage system is unavailable.
* `public async Task DeleteBackupAsync`: Deletes a backup from the storage system. This method may throw exceptions if the deletion process fails or if the storage system is unavailable.
* `public async Task<IEnumerable<(string Path, long Size, DateTime Modified)>> ListBackupsAsync`: Lists the available backups in the storage system. The method returns an enumerable collection of tuples, where each tuple contains the path, size, and last modified date of a backup. This method may throw exceptions if the listing process fails or if the storage system is unavailable.
* `public async Task<bool> TestConnectionAsync`: Tests the connection to the storage system. The method returns a boolean indicating whether the connection is successful. This method may throw exceptions if the connection test fails.
* `public async Task<long> GetAvailableSpaceAsync`: Retrieves the available space in the storage system. The method returns the available space in bytes. This method may throw exceptions if the retrieval process fails or if the storage system is unavailable.

## Usage
The following examples demonstrate how to use the `StorageService` class:
```csharp
// Example 1: Upload and download a backup
var storageService = new StorageService();
var backupId = await storageService.UploadBackupAsync();
var backupContents = await storageService.DownloadBackupAsync();
Console.WriteLine($"Backup ID: {backupId}, Contents: {backupContents}");

// Example 2: List and delete backups
var storageService = new StorageService();
var backups = await storageService.ListBackupsAsync();
foreach (var backup in backups)
{
    Console.WriteLine($"Path: {backup.Path}, Size: {backup.Size}, Modified: {backup.Modified}");
    await storageService.DeleteBackupAsync();
}
```

## Notes
The `StorageService` class is designed to be used in a multithreaded environment, and its methods are thread-safe. However, it is essential to note that the storage system itself may have limitations or constraints that affect the behavior of the `StorageService` class. For example, if the storage system has a limited number of concurrent connections, the `StorageService` class may throw exceptions if the connection limit is exceeded. Additionally, the `StorageService` class may throw exceptions if the storage system is unavailable or if the backup files are corrupted. It is recommended to handle these exceptions and implement retry mechanisms to ensure reliable backup management.
