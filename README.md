// ... existing content ...

## ChecksumUtility

The `ChecksumUtility` class provides a set of static methods for calculating various types of checksums and hashes. It includes methods for calculating SHA256, MD5, and CRC32 hashes, as well as generating a quick checksum based on file size and boundary bytes.

```csharp
using DockerSqliteBackup.Utilities;

// Calculate the SHA256 hash of a file
var sha256Hash = await ChecksumUtility.CalculateFileSha256Async("path/to/file.db");

// Calculate the SHA256 hash of a string
var stringHash = ChecksumUtility.CalculateStringSha256("Hello, world!");

// Verify that a file's SHA256 hash matches the expected value
var isValid = await ChecksumUtility.VerifyFileSha256Async("path/to/file.db", "expected_hash");

// Calculate the CRC32 checksum of a file
var crc32Checksum = await ChecksumUtility.CalculateFileCrc32Async("path/to/file.db");

// Generate a quick checksum based on file size and boundary bytes
var quickChecksum = ChecksumUtility.GenerateQuickChecksum("path/to/file.db");

// Calculate a checksum for a collection of values
var collectionChecksum = ChecksumUtility.CalculateCollectionChecksum("value1", "value2", "value3");
```

## PathUtility

`PathUtility` offers a collection of static helper methods for common file‑system path operations. It handles sanitising file names, normalising paths, checking whether a path is absolute, combining path segments, retrieving file sizes, validating file paths, and generating timestamped backup file names in a platform‑agnostic way.

```csharp
using DockerSqliteBackup.Utilities;

// Sanitize a file name
var safeName = PathUtility.SanitizeFileName("my*invalid?.db");

// Normalise a path
var normalized = PathUtility.Normalize(@"C:\temp\..\backup\file.db");

// Check if a path is absolute
bool isAbs = PathUtility.IsAbsolute(normalized);

// Get a relative path
var relative = PathUtility.GetRelativePath("/var/backups", "/var/backups/2023/file.db");

// Ensure a directory exists
PathUtility.EnsureDirectoryExists("/var/backups/2023");

// Combine multiple path segments
var combined = PathUtility.CombinePath("/var", "backups", "2023", "file.db");

// Get file size
long size = PathUtility.GetFileSize(combined);

// Validate file path
bool valid = PathUtility.IsValidFilePath(combined);

// Generate a backup file name
var backupName = PathUtility.GenerateBackupFileName("mydb");
```
