# FileSystemUtilityTests
The `FileSystemUtilityTests` class provides a comprehensive set of tests for the file system utility functions, ensuring their correctness and reliability in various scenarios. These tests cover a wide range of file system operations, including file copying, deletion, directory creation, and disk space calculation, as well as error handling for non-existent files and directories.

## API
* `public Task InitializeAsync`: Initializes the test environment asynchronously.
* `public async Task DisposeAsync`: Disposes of the test environment asynchronously.
* `public async Task SafeCopyFileAsync_ExistingFile_CopiesSuccessfully`: Tests the successful copying of an existing file.
* `public async Task SafeCopyFileAsync_NonExistentSource_ThrowsFileNotFoundException`: Tests that a `FileNotFoundException` is thrown when attempting to copy a non-existent file.
* `public async Task SafeCopyFileAsync_CreatesDestinationDirectory`: Tests that the destination directory is created if it does not exist.
* `public void SafeDeleteFile_ExistingFile_DeletesSuccessfully`: Tests the successful deletion of an existing file.
* `public void SafeDeleteFile_NonExistentFile_DoesNotThrow`: Tests that no exception is thrown when attempting to delete a non-existent file.
* `public void GetFilesWithPattern_DirectoryWithMatchingFiles_ReturnsMatches`: Tests that files matching a given pattern are returned.
* `public void GetFilesWithPattern_NoMatchingFiles_ReturnsEmpty`: Tests that an empty list is returned when no files match the given pattern.
* `public void GetFilesWithPattern_NonExistentDirectory_ReturnsEmpty`: Tests that an empty list is returned when the directory does not exist.
* `public void CalculateDirectorySize_WithFiles_ReturnsSumOfFileSizes`: Tests that the total size of files in a directory is calculated correctly.
* `public void CalculateDirectorySize_NonExistentDirectory_ReturnsZero`: Tests that a size of zero is returned for a non-existent directory.
* `public void CalculateDirectorySize_EmptyDirectory_ReturnsZero`: Tests that a size of zero is returned for an empty directory.
* `public void CopyDirectory_ExistingDirectory_CopiesAllFiles`: Tests the successful copying of all files in an existing directory.
* `public void CopyDirectory_RecursiveCopy_CopiesSubdirectories`: Tests that subdirectories are copied recursively.
* `public void CopyDirectory_NonExistentSource_ThrowsDirectoryNotFoundException`: Tests that a `DirectoryNotFoundException` is thrown when attempting to copy a non-existent directory.
* `public async Task DeleteDirectoryAsync_ExistingDirectory_DeletesItAndContents`: Tests the successful deletion of an existing directory and its contents asynchronously.
* `public async Task DeleteDirectoryAsync_NonExistentDirectory_DoesNotThrow`: Tests that no exception is thrown when attempting to delete a non-existent directory asynchronously.
* `public void IsFileInUse_ExistingUnlockedFile_ReturnsFalse`: Tests that a file is reported as not in use when it is not locked.
* `public void GetAvailableDiskSpace_RootPath_ReturnsPositiveValue`: Tests that the available disk space for the root path is reported as a positive value.

## Usage
The following examples demonstrate how to use the `FileSystemUtilityTests` class:
```csharp
// Example 1: Copying a file
var fileSystemUtilityTests = new FileSystemUtilityTests();
await fileSystemUtilityTests.InitializeAsync();
await fileSystemUtilityTests.SafeCopyFileAsync("source.txt", "destination.txt");
await fileSystemUtilityTests.DisposeAsync();
```

```csharp
// Example 2: Calculating directory size
var fileSystemUtilityTests = new FileSystemUtilityTests();
await fileSystemUtilityTests.InitializeAsync();
var directorySize = fileSystemUtilityTests.CalculateDirectorySize("/path/to/directory");
Console.WriteLine($"Directory size: {directorySize} bytes");
await fileSystemUtilityTests.DisposeAsync();
```

## Notes
When using the `FileSystemUtilityTests` class, note that:
* The `InitializeAsync` and `DisposeAsync` methods must be called to ensure proper setup and teardown of the test environment.
* The `SafeCopyFileAsync` and `DeleteDirectoryAsync` methods are asynchronous and may throw exceptions if the source file or directory does not exist.
* The `GetFilesWithPattern` method returns an empty list if the directory does not exist or no files match the given pattern.
* The `CalculateDirectorySize` method returns a size of zero for non-existent or empty directories.
* The `IsFileInUse` method reports a file as not in use if it is not locked.
* The `GetAvailableDiskSpace` method reports the available disk space for the root path as a positive value.
* The `FileSystemUtilityTests` class is designed to be thread-safe, but concurrent access to the same file system resources may still cause issues.
