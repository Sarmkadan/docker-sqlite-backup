// ... existing content ...

## FileSystemUtilityTests
The `FileSystemUtilityTests` class provides a comprehensive set of unit tests for the `FileSystemUtility` class, verifying its behavior across various file system operations including file copying, deletion, directory operations, and disk space calculations. This class ensures the utility methods function correctly with different inputs and edge cases.
Here's an example of how to use some of its public members:
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

## ChecksumUtilityTests
The `ChecksumUtilityTests` class provides comprehensive unit tests for the `ChecksumUtility` class, verifying its checksum calculation and verification methods across various algorithms including SHA-256, MD5, CRC32, and quick checksums. This class ensures the utility methods generate deterministic and accurate checksums for different content types and handle edge cases like non-existent files appropriately.

Here's an example of how to use some of its public members:
```csharp
var checksumTests = new ChecksumUtilityTests();
await checksumTests.InitializeAsync();
try
{
 // Create a test file
 var testFilePath = Path.Combine(checksumTests.GetTempDirectory(), "test-file.txt");
 await File.WriteAllTextAsync(testFilePath, "Hello, World!");

 // Calculate SHA-256 hash
 var sha256Hash = await ChecksumUtility.CalculateFileSha256Async(testFilePath);
 Console.WriteLine($"SHA-256: {sha256Hash}");

 // Calculate MD5 hash  
 var md5Hash = await ChecksumUtility.CalculateFileMd5Async(testFilePath);
 Console.WriteLine($"MD5: {md5Hash}");

 // Calculate CRC32 checksum
 var crc32Checksum = await ChecksumUtility.CalculateFileCrc32Async(testFilePath);
 Console.WriteLine($"CRC32: {crc32Checksum}");

 // Generate quick checksum
 var quickChecksum = await ChecksumUtility.GenerateQuickChecksumAsync(testFilePath);
 Console.WriteLine($"Quick Checksum: {quickChecksum}");

 // Calculate string hash
 var stringHash = ChecksumUtility.CalculateStringSha256("test string");
 Console.WriteLine($"String Hash: {stringHash}");

 // Calculate collection checksum
 var collectionHash = ChecksumUtility.CalculateCollectionChecksum("item1", 42, true);
 Console.WriteLine($"Collection Hash: {collectionHash}");

 // Verify file hash
 var isValid = await ChecksumUtility.VerifyFileSha256Async(testFilePath, sha256Hash);
 Console.WriteLine($"Hash verification: {isValid}");
}
finally
{
 await checksumTests.DisposeAsync();
}
```

// ... rest of the content ...