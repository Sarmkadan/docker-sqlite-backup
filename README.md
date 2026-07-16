// ... existing content ...

## FileSystemUtilityTests
The `FileSystemUtilityTests` class provides a comprehensive set of unit tests for the `FileSystemUtility` class, verifying its behavior across various file system operations including file copying, deletion, directory operations, and disk space calculations. This class ensures the utility methods function correctly with different inputs and edge cases. Here's an example of how to use some of its public members:
```csharp
var fileSystemUtilityTests = new FileSystemUtilityTests();
await fileSystemUtilityTests.InitializeAsync();
try
{
    var source = "source.txt";
    var dest = "dest.txt";
    await fileSystemUtilityTests.SafeCopyFileAsync_ExistingFile_CopiesSuccessfully();
    fileSystemUtilityTests.SafeDeleteFile_ExistingFile_DeletesSuccessfully();
    var files = fileSystemUtilityTests.GetFilesWithPattern_DirectoryWithMatchingFiles_ReturnsMatches();
    var size = fileSystemUtilityTests.CalculateDirectorySize_WithFiles_ReturnsSumOfFileSizes();
    fileSystemUtilityTests.CopyDirectory_ExistingDirectory_CopiesAllFiles();
    await fileSystemUtilityTests.DeleteDirectoryAsync_ExistingDirectory_DeletesItAndContents();
    var isFileInUse = fileSystemUtilityTests.IsFileInUse_ExistingUnlockedFile_ReturnsFalse();
    var availableSpace = fileSystemUtilityTests.GetAvailableDiskSpace_RootPath_ReturnsPositiveValue();
}
finally
{
    await fileSystemUtilityTests.DisposeAsync();
}
```
// ... rest of the content ...
```