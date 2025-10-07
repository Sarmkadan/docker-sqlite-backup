# FileSystemUtility

Utility class providing safe, asynchronous, and synchronous file‑system operations used throughout the `docker-sqlite-backup` project. The methods encapsulate common checks (e.g., ensuring a file is not locked, handling exceptions) to simplify backup and restore workflows.

## API

### `public static async Task SafeCopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)`
Copies a file from `sourcePath` to `destinationPath`.  
- **Parameters**  
  - `sourcePath`: Full path of the file to copy.  
  - `destinationPath`: Full path of the target file.  
  - `overwrite`: If `true`, an existing destination file is overwritten; otherwise the method throws if the file already exists.  
  - `cancellationToken`: Optional token to cancel the operation.  
- **Return value**: Completes when the copy finishes; returns no value.  
- **Exceptions**  
  - `ArgumentNullException` if either path is `null`.  
  - `ArgumentException` if a path is empty or contains invalid characters.  
  - `FileNotFoundException` if the source file does not exist.  
  - `IOException` if the destination file exists and `overwrite` is `false`, or if an I/O error occurs during copying.  
  - `UnauthorizedAccessException` if the caller lacks required permissions.  
  - `OperationCanceledException` if the cancellation token is triggered.

### `public static void SafeDeleteFile(string path)`
Deletes the file at `path` if it exists, swallowing `FileNotFoundException` and silently succeeding when the file is already missing.  
- **Parameters**  
  - `path`: Full path of the file to delete.  
- **Return value**: None.  
- **Exceptions**  
  - `ArgumentNullException` if `path` is `null`.  
  - `ArgumentException` if `path` is empty or invalid.  
  - `UnauthorizedAccessException` if the caller lacks delete permission.  
  - `IOException` for other I/O failures (e.g., file is locked by another process).

### `public static IEnumerable<string> GetFilesWithPattern(string directoryPath, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)`
Enumerates files matching `searchPattern` within `directoryPath`.  
- **Parameters**  
  - `directoryPath`: Directory to search.  
  - `searchPattern`: Standard wildcard pattern (e.g., `"*.bak"`).  
  - `searchOption`: Whether to search only the top directory or recursively include subdirectories.  
- **Return value**: An `IEnumerable<string>` yielding full paths of matching files. The enumeration is lazy; errors are thrown during iteration.  
- **Exceptions**  
  - `ArgumentNullException` if either path or pattern is `null`.  
  - `ArgumentException` if paths are empty or contain invalid characters.  
  - `DirectoryNotFoundException` if `directoryPath` does not exist.  
  - `UnauthorizedAccessException` if access to a directory is denied.

### `public static long CalculateDirectorySize(string directoryPath)`
Computes the total size, in bytes, of all files contained in `directoryPath` (including subdirectories).  
- **Parameters**  
  - `directoryPath`: Root directory to measure.  
- **Return value**: Total size in bytes as a signed 64‑bit integer.  
- **Exceptions**  
  - `ArgumentNullException` if `directoryPath` is `null`.  
  - `ArgumentException` if the path is empty or invalid.  
  - `DirectoryNotFoundException` if the directory does not exist.  
  - `UnauthorizedAccessException` if access to any sub‑directory or file is denied.  
  - `IOException` for other I/O errors encountered during traversal.

### `public static async Task DeleteDirectoryAsync(string directoryPath, bool recursive = true, CancellationToken cancellationToken = default)`
Asynchronously deletes the directory at `directoryPath`.  
- **Parameters**  
  - `directoryPath`: Directory to delete.  
  - `recursive`: If `true`, deletes all contents; if `false`, fails when the directory is not empty.  
  - `cancellationToken`: Optional token to cancel the operation.  
- **Return value**: Completes when the deletion finishes; returns no value.  
- **Exceptions**  
  - `ArgumentNullException` if `directoryPath` is `null`.  
  - `ArgumentException` if the path is empty or invalid.  
  - `DirectoryNotFoundException` if the directory does not exist (treated as success).  
  - `UnauthorizedAccessException` if insufficient permissions.  
  - `IOException` if the directory is not empty and `recursive` is `false`, or if an I/O error occurs.  
  - `OperationCanceledException` if the cancellation token is triggered.

### `public static long GetAvailableDiskSpace(string drivePath)`
Returns the amount of free space, in bytes, available on the drive containing `drivePath`.  
- **Parameters**  
  - `drivePath`: Any folder or drive root path (e.g., `"C:\"` or `"/mnt/backup"`).  
- **Return value**: Free space in bytes.  
- **Exceptions**  
  - `ArgumentNullException` if `drivePath` is `null`.  
  - `ArgumentException` if the path is empty or invalid.  
  - `DriveNotFoundException` if the drive does not exist.  
  - `UnauthorizedAccessException` if access to the drive information is denied.

### `public static bool IsFileInUse(string filePath)`
Determines whether the file at `filePath` is currently locked by another process.  
- **Parameters**  
  - `filePath`: Path to the file to test.  
- **Return value**: `true` if the file cannot be opened for exclusive access; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if `filePath` is `null`.  
  - `ArgumentException` if the path is empty or invalid.  
  - `FileNotFoundException` if the file does not exist (returns `false`).  
  - `UnauthorizedAccessException` if the caller lacks permission to open the file.

### `public static void CopyDirectory(string sourceDir, string destDir, bool overwrite = false)`
Copies the entire directory tree from `sourceDir` to `destDir`.  
- **Parameters**  
  - `sourceDir`: Directory to copy.  
  - `destDir`: Target directory that will receive the copy.  
  - `overwrite`: If `true`, existing files in the destination are overwritten; otherwise the method throws when encountering an existing file.  
- **Return value**: None.  
- **Exceptions**  
  - `ArgumentNullException` if either directory path is `null`.  
  - `ArgumentException` if a path is empty or invalid.  
  - `DirectoryNotFoundException` if `sourceDir` does not exist.  
  - `IOException` if `destDir` already exists and `overwrite` is `false`, or if any file copy fails.  
  - `UnauthorizedAccessException` if access to source or destination is denied.

## Usage

### Example 1: Safely backing up a SQLite file
```csharp
using System;
using System.Threading.Tasks;
using DockerSqliteBackup.Utilities; // namespace containing FileSystemUtility

class BackupJob
{
    public async Task PerformBackupAsync(string sourceDb, string backupFolder)
    {
        // Ensure the backup folder exists
        if (!Directory.Exists(backupFolder))
            Directory.CreateDirectory(backupFolder);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var backupPath = Path.Combine(backupFolder, $"backup-{timestamp}.db");

        // Wait until the database file is not locked
        while (FileSystemUtility.IsFileInUse(sourceDb))
        {
            await Task.Delay(200);
        }

        // Copy the file safely, overwriting any previous backup with same name
        await FileSystemUtility.SafeCopyFileAsync(sourceDb, backupPath, overwrite: true);

        Console.WriteLine($"Backup completed: {backupPath}");
    }
}
```

### Example 2: Cleaning up old backup directories based on size
```csharp
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DockerSqliteBackup.Utilities;

class CleanupService
{
    private const long MaxBackupSizeBytes = 10L * 1024 * 1024 * 1024; // 10 GB

    public async Task EnforceSizeLimitAsync(string backupRoot)
    {
        // Get all backup directories sorted by creation time (oldest first)
        var directories = new DirectoryInfo(backupRoot)
            .GetDirectories()
            .OrderBy(d => d.CreationTimeUtc);

        long used = 0;
        foreach (var dir in directories)
        {
            used += FileSystemUtility.CalculateDirectorySize(dir.FullName);
            if (used <= MaxBackupSizeBytes)
                continue; // still within limit

            // Exceeded limit – delete this directory and all older ones
            await FileSystemUtility.DeleteDirectoryAsync(dir.FullName, recursive: true);
            Console.WriteLine($"Deleted backup directory: {dir.FullName}");
        }
    }
}
```

## Notes

- All methods are **static** and contain no mutable state; therefore they are thread‑safe with respect to the class itself. Concurrent calls from multiple threads will not corrupt internal data, but the underlying file‑system operations are not atomic. Simultaneous modifications of the same file or directory by different threads (or processes) may lead to race conditions, exceptions, or unexpected results.
- Path arguments are validated for `null` and empty strings; invalid characters trigger `ArgumentException`. Callers should ensure paths are normalized (e.g., using `Path.GetFullPath`) before invoking these methods when dealing with user input.
- `SafeCopyFileAsync` and `CopyDirectory` respect the `overwrite` flag; when `false` they throw `IOException` if the target already exists, preventing accidental data loss.
- `GetFilesWithPattern` returns a lazy enumeration; exceptions such as `UnauthorizedAccessException` are only thrown when the enumerator is accessed. Consumers should wrap iteration in a `try/catch` if they need to handle access‑denied scenarios gracefully.
- `IsFileInUse` attempts to open the file with exclusive read/write access. A brief window exists where the file could become locked after the check but before subsequent operations; callers that require guaranteed exclusivity should combine this check with a retry loop or use file‑stream locking mechanisms.
- `CalculateDirectorySize` and `GetAvailableDiskSpace` return signed 64‑bit integers (`long`). On systems with very large volumes (> 8 EB) the value may overflow; however, such sizes exceed the practical limits of the file systems supported by the project.
- Cancellation tokens are honored where applicable (`SafeCopyFileAsync`, `DeleteDirectoryAsync`). If cancellation is requested mid‑operation, partially copied or deleted files may remain; callers should verify completion or clean up as needed.
