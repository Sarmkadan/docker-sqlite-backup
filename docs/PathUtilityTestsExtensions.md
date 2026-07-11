# PathUtilityTestsExtensions

Provides a set of fluent assertion extension methods for validating file‑system paths in unit tests. The methods are intended to be used with testing frameworks that treat thrown exceptions as test failures (e.g., xUnit, NUnit, MSTest).

## API

### ShouldBeValidFilePathWithContent
```csharp
public static void ShouldBeValidFilePathWithContent(this string path, string expectedContent);
```
**Purpose**  
Asserts that `path` points to an existing file whose contents exactly match `expectedContent`.

**Parameters**  
- `path`: The file path to validate. Must not be `null`.  
- `expectedContent`: The exact text expected inside the file. May be `null` or empty to assert an empty file.

**Return value**  
`void`. The method returns normally when the assertion succeeds.

**Exceptions**  
- `ArgumentNullException` if `path` is `null`.  
- `IOException` or `UnauthorizedAccessException` if the file cannot be read.  
- `Xunit.Sdk.EqualException` (or the framework‑specific assertion exception) if the file does not exist, is not a file, or its content differs from `expectedContent`.

### ShouldHaveFileSize
```csharp
public static void ShouldHaveFileSize(this string path, long expectedSizeBytes);
```
**Purpose**  
Asserts that the file at `path` exists and its size in bytes equals `expectedSizeBytes`.

**Parameters**  
- `path`: The file path to validate. Must not be `null`.  
- `expectedSizeBytes`: The expected file size. Must be non‑negative.

**Return value**  
`void`. Normal return indicates success.

**Exceptions**  
- `ArgumentNullException` if `path` is `null`.  
- `ArgumentOutOfRangeException` if `expectedSizeBytes` is negative.  
- `IOException`/`UnauthorizedAccessException` if the file cannot be accessed.  
- Assertion exception if the file does not exist, is not a file, or its size differs from `expectedSizeBytes`.

### ShouldBeNormalized
```csharp
public static void ShouldBeNormalized(this string path);
```
**Purpose**  
Asserts that `path` is in a normalized form: no trailing directory separator, no `.` or `..` components, and redundant separators are collapsed.

**Parameters**  
- `path`: The path string to validate. Must not be `null`.

**Return value**  
`void`. Normal return indicates the path is already normalized.

**Exceptions**  
- `ArgumentNullException` if `path` is `null`.  
- Assertion exception if the path contains any non‑normalized elements (e.g., `C:\temp\..\file.txt` or `C:\\temp\\file.txt`).

### ShouldHaveRelativePath
```csharp
public static void ShouldHaveRelativePath(this string path, string basePath);
```
**Purpose**  
Asserts that `path` is expressed as a relative path with respect to `basePath`. The method does not require the files to exist; it works purely on string representation.

**Parameters**  
- `path`: The candidate relative path. Must not be `null`.  
- `basePath`: The base directory against which relativity is evaluated. Must not be `null` and should be an absolute path without trailing separator (though the method tolerates one).

**Return value**  
`void`. Normal return indicates `path` is relative to `basePath`.

**Exceptions**  
- `ArgumentNullException` if either argument is `null`.  
- Assertion exception if `path` is absolute, contains invalid characters for a relative path, or does not resolve to a location under `basePath` when interpreted relatively.

## Usage

```csharp
[Fact]
public void BackupCreatesCorrectFile()
{
    // Arrange
    var backupPath = Path.Combine(tempDir, "backup.sqlite");
    var expectedContent = File.ReadAllText(sourceDbPath);

    // Act
    BackupUtility.CreateBackup(sourceDbPath, backupPath);

    // Assert
    backupPath.ShouldBeValidFilePathWithContent(expectedContent);
    backupPath.ShouldHaveFileSize(new FileInfo(sourceDbPath).Length);
    backupPath.ShouldBeNormalized();
}
```

```csharp
[Fact]
public void RelativePathIsCorrect()
{
    // Arrange
    var baseDir = @"C:\projects\docker-sqlite-backup";
    var relative = @"scripts\run-backup.sh";

    // Act
    // (some code that produces a relative path)
    var result = PathHelper.MakeRelative(baseDir, absolutePath);

    // Assert
    result.ShouldHaveRelativePath(baseDir);
    result.ShouldBeNormalized(); // ensures no stray separators or up‑levels
}
```

## Notes

- All methods are pure extensions; they retain no internal state and are therefore thread‑safe as long as the supplied `string` arguments are not mutated concurrently by other threads.  
- Passing `null` for any string argument will always result in an `ArgumentNullException`.  
- The file‑system‑based assertions (`ShouldBeValidFilePathWithContent`, `ShouldHaveFileSize`) perform actual I/O; they will throw IOException‑derived exceptions if the underlying file is locked, missing, or inaccessible.  
- `ShouldBeNormalized` does not verify that the path refers to an existing item; it only checks the string format.  
- `ShouldHaveRelativePath` treats paths in a platform‑agnostic manner regarding separator characters but expects the base path to be an absolute path on the current operating system. Mixing separators (e.g., `/` on Windows) may lead to false negatives.  
- These methods are intended for test scenarios; using them in production code may hide legitimate error conditions because they throw on failure rather than returning a boolean result.
