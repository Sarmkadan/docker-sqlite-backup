# PathUtility

`PathUtility` is a static utility class that provides filesystem path manipulation, validation, and I/O helpers for the backup workflow. It centralises common operations such as sanitising file names, resolving relative paths, verifying absolute paths, ensuring directory existence, and generating timestamped backup file names, reducing duplication and enforcing consistent path handling across the application.

## API

### SanitizeFileName

```csharp
public static string SanitizeFileName(string fileName)
```

Replaces characters in a file name that are invalid for the target filesystem with safe alternatives. Returns a sanitised string suitable for use as a file or directory name. Throws `ArgumentNullException` if `fileName` is `null`.

### Normalize

```csharp
public static string Normalize(string path)
```

Resolves a path string to a canonical form by collapsing redundant separators, trimming trailing slashes, and converting directory separators to the platform-native character. Returns the normalised absolute or relative path. Throws `ArgumentNullException` if `path` is `null`.

### IsAbsolute

```csharp
public static bool IsAbsolute(string path)
```

Determines whether the given path is an absolute path according to the operating system’s rules (e.g., rooted on Windows or starting with `/` on Unix). Returns `true` if the path is absolute; `false` if it is relative. Throws `ArgumentNullException` if `path` is `null`.

### GetRelativePath

```csharp
public static string GetRelativePath(string relativeTo, string path)
```

Computes a relative path from `relativeTo` to `path`. Both inputs must be absolute or relative consistently. Returns the relative path string. Throws `ArgumentException` if either argument is `null` or if the paths cannot be made relative (e.g., differing roots on Windows).

### EnsureDirectoryExists

```csharp
public static void EnsureDirectoryExists(string path)
```

Creates the directory specified by `path` (and any missing parent directories) if it does not already exist. Does nothing if the directory is already present. Throws `ArgumentNullException` if `path` is `null`, and may throw `IOException` or `UnauthorizedAccessException` if directory creation fails.

### CombinePath

```csharp
public static string CombinePath(string path1, string path2)
```

Combines two path segments into a single path, inserting a directory separator if necessary. Returns the combined path string. Throws `ArgumentNullException` if either argument is `null`.

### GetFileSize

```csharp
public static long GetFileSize(string filePath)
```

Returns the size, in bytes, of the file at the specified path. Throws `FileNotFoundException` if the file does not exist, `ArgumentNullException` if `filePath` is `null`, and `IOException` for general I/O failures.

### IsValidFilePath

```csharp
public static bool IsValidFilePath(string path)
```

Checks whether the given string represents a syntactically well-formed file path (not whether the file exists). Returns `true` if the path contains no invalid characters and has a structure acceptable to the operating system; `false` otherwise. Throws `ArgumentNullException` if `path` is `null`.

### GenerateBackupFileName

```csharp
public static string GenerateBackupFileName(string databaseName, DateTime timestamp)
```

Produces a standardised backup file name by combining a sanitised database name with a formatted timestamp and the `.sqlite` extension. Returns the generated file name string. Throws `ArgumentNullException` if `databaseName` is `null`.

## Usage

### Example 1: Creating a backup file path

```csharp
string dbName = "MyApp/Production.db";
string backupDir = "/var/backups/sqlite";

string safeName = PathUtility.SanitizeFileName(dbName);
string fileName = PathUtility.GenerateBackupFileName(safeName, DateTime.UtcNow);
string fullPath = PathUtility.CombinePath(backupDir, fileName);

PathUtility.EnsureDirectoryExists(backupDir);
Console.WriteLine($"Backup will be written to: {fullPath}");
```

### Example 2: Validating and inspecting an existing backup

```csharp
string backupPath = "/var/backups/sqlite/MyApp_Production_20250315_143022.sqlite";

if (!PathUtility.IsAbsolute(backupPath))
{
    throw new InvalidOperationException("Backup path must be absolute.");
}

if (!PathUtility.IsValidFilePath(backupPath))
{
    throw new InvalidOperationException("Backup path contains invalid characters.");
}

long size = PathUtility.GetFileSize(backupPath);
Console.WriteLine($"Backup file size: {size} bytes");

string relative = PathUtility.GetRelativePath("/var/backups", backupPath);
Console.WriteLine($"Relative to /var/backups: {relative}");
```

## Notes

- All methods that accept `string` arguments throw `ArgumentNullException` when passed `null`. Callers should guard inputs or allow exceptions to propagate.
- `SanitizeFileName` and `IsValidFilePath` operate on syntactic rules, not filesystem state. A path may be valid yet refer to a non-existent location.
- `Normalize` does not resolve symlinks or canonicalise case; it only adjusts separators and redundant segments.
- `GetRelativePath` requires both paths to share a common root; on Windows, paths on different volumes will cause an `ArgumentException`.
- `EnsureDirectoryExists` is safe to call concurrently on the same path from multiple threads, as the underlying directory creation is atomic. However, simultaneous calls with different paths may contend for filesystem resources.
- `GetFileSize` queries the filesystem at the moment of invocation; the result may be stale immediately after the call if the file is being written concurrently.
- This class is stateless and all members are static; no synchronisation is required when calling its methods from multiple threads.
