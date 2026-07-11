# StorageServiceIntegrationTestsExtensions
The `StorageServiceIntegrationTestsExtensions` class provides a set of static methods for creating and verifying backup files, as well as interacting with the `StorageService` in the context of integration tests for the `docker-sqlite-backup` project. These methods simplify the process of setting up and testing backup scenarios, allowing developers to focus on the logic of their tests.

## API
* `public static string CreateTempFile`: Creates a temporary file. Returns the path to the created file.
* `public static LocalStorageConfiguration MakeLocalConfig`: Creates a local storage configuration. Returns the created configuration.
* `public static string GetTempDir`: Gets the temporary directory. Returns the path to the temporary directory.
* `public static StorageService GetStorageService`: Gets the storage service. Returns the storage service instance.
* `public static void VerifyBackupFile`: Verifies a backup file. Throws if the verification fails.
* `public static string CreateRandomBackupFile`: Creates a random backup file. Returns the path to the created file.
* `public static void ShouldMatchBackupPath(this StorageServiceIntegrationTests test, ...)`: Verifies that the backup path matches the expected path. Throws if the paths do not match.
* `public static List<string> CreateMultipleBackupFiles`: Creates multiple backup files. Returns a list of paths to the created files.
* `public static long GetFileSize`: Gets the size of a file. Returns the file size in bytes.
* `public static (string Path, long Size, DateTime Modified) CreateBackupTuple`: Creates a tuple representing a backup file. Returns the created tuple.

## Usage
```csharp
// Example 1: Creating and verifying a backup file
var backupFile = StorageServiceIntegrationTestsExtensions.CreateRandomBackupFile();
StorageServiceIntegrationTestsExtensions.VerifyBackupFile(backupFile);

// Example 2: Creating multiple backup files and checking their sizes
var backupFiles = StorageServiceIntegrationTestsExtensions.CreateMultipleBackupFiles();
foreach (var file in backupFiles)
{
    var fileSize = StorageServiceIntegrationTestsExtensions.GetFileSize(file);
    Console.WriteLine($"File {file} has size {fileSize} bytes");
}
```

## Notes
When using these methods, note that temporary files and directories are created in the system's temporary directory, which may have limited space and may be cleaned up periodically. Additionally, the `VerifyBackupFile` and `ShouldMatchBackupPath` methods will throw exceptions if the verification fails, so it is recommended to handle these exceptions accordingly. The `GetFileSize` method may throw an exception if the file does not exist or is inaccessible. The `CreateBackupTuple` method returns a tuple with the file path, size, and last modified date, which can be used to verify the backup file's properties. These methods are designed to be thread-safe, but it is still important to ensure that the tests are properly synchronized to avoid conflicts when accessing shared resources.
