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

## BackupServiceValidation

The `BackupServiceValidation` class provides validation extension methods for `BackupService`, `BackupResult`, and `BackupSchedule` types. It helps ensure that service instances and backup artifacts are properly configured before use by validating required fields, enumerations, paths, timestamps, and business rules. The validation methods return detailed error messages when issues are detected, enabling robust error handling in backup workflows.

Here's an example of how to use some of its public members:
```csharp
// Create a backup service instance (typically via DI)
var backupService = new BackupService(
    databasePath: "/var/lib/sqlite/production.db",
    backupDirectory: "/backups/sqlite",
    maxConcurrentBackups: 4,
    s3Configuration: null);

// Validate the service instance
var validationProblems = backupService.Validate();
if (validationProblems.Count > 0)
{
    Console.WriteLine("Backup service validation failed:");
    foreach (var problem in validationProblems)
    {
        Console.WriteLine($"- {problem}");
    }
    return;
}

// Perform a backup operation
var backupResult = await backupService.CreateBackupAsync(
    scheduleId: Guid.NewGuid(),
    backupMode: BackupMode.Full);

// Validate the backup result
if (!backupResult.IsValid())
{
    Console.WriteLine("Backup result is invalid:");
    var resultProblems = backupResult.Validate();
    foreach (var problem in resultProblems)
    {
        Console.WriteLine($"- {problem}");
    }
    backupService.EnsureValid(backupResult); // Throws if invalid
}

// Validate a backup schedule
var backupSchedule = new BackupSchedule
{
    Name = "Daily Production Backup",
    DatabasePath = "/var/lib/sqlite/production.db",
    CronExpression = "0 2 * * *", // Daily at 2 AM
    RetentionDays = 30,
    MaxBackupCount = 10,
    BackupMode = BackupMode.Full,
    NotificationEmails = "admin@example.com,devops@example.com"
};

if (!backupSchedule.IsValid())
{
    Console.WriteLine("Backup schedule is invalid:");
    var scheduleProblems = backupSchedule.Validate();
    foreach (var problem in scheduleProblems)
    {
        Console.WriteLine($"- {problem}");
    }
}
```

## FileSystemUtilityJsonExtensions

The `FileSystemUtilityJsonExtensions` class provides JSON serialization and deserialization utilities for working with `FileSystemUtilityConfig` objects. It simplifies configuration management by allowing easy conversion between configuration objects and JSON strings, with support for error handling through both throwing and non-throwing methods. The extension methods handle serialization of file system search patterns, retry configurations, and recursive search settings.

Here's an example of how to use its public members:
```csharp
using Docker.Sqlite.Backup.Utilities;

// Create a configuration object
var config = new FileSystemUtilityConfig
{
    MaxRetries = 3,
    RetryDelayMultiplier = 2,
    Recursive = true,
    DefaultSearchPattern = "*.db"
};

// Serialize to JSON
var json = config.ToJson();
Console.WriteLine(json);
// Output: {"MaxRetries":3,"RetryDelayMultiplier":2,"Recursive":true,"DefaultSearchPattern":"*.db"}

// Deserialize from JSON (throws on error)
var deserializedConfig = FileSystemUtilityConfig.FromJson(json);
Console.WriteLine(deserializedConfig.DefaultSearchPattern); // *.db

// Deserialize from JSON (returns null on error)
var parsedConfig = FileSystemUtilityConfig.TryFromJson(json);
if (parsedConfig != null)
{
    Console.WriteLine($"Loaded config: MaxRetries={parsedConfig.MaxRetries}");
}
```

// ... rest of the content ...