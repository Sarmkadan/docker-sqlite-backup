# PathUtilityTests

Unit tests for the `PathUtility` class, which provides helper methods for common filesystem operations such as path validation, sanitization, size checking, and backup file naming.

## API

### `PathUtilityTests`
Constructor that initializes test dependencies and prepares the test environment.

### `Dispose`
Disposes of any test resources, including temporary directories or files created during test execution.

### `SanitizeFileName_NormalName_ReturnsUnchanged`
Ensures that a valid filename remains unchanged when passed through sanitization.

- **Parameters**: `string input` – A valid filename without illegal characters.
- **Return value**: The same string, unchanged.
- **Throws**: Never.

### `SanitizeFileName_EmptyInput_ThrowsArgumentException`
Verifies that an empty string input causes an `ArgumentException`.

- **Parameters**: `string input` – An empty string.
- **Throws**: `ArgumentException` when input is empty.

### `SanitizeFileName_NullOrEmptyString_ThrowsArgumentException`
Ensures that a null or empty string input causes an `ArgumentException`.

- **Parameters**: `string input` – A null or empty string.
- **Throws**: `ArgumentException` when input is null or empty.

### `SanitizeFileName_AllInvalidChars_ReturnsFallback`
Confirms that a filename composed entirely of invalid characters returns a safe fallback name.

- **Parameters**: `string input` – A string containing only invalid filename characters.
- **Return value**: A safe fallback filename (e.g., `"file"`).
- **Throws**: Never.

### `IsAbsolute_AbsolutePath_ReturnsTrue`
Checks that an absolute filesystem path is correctly identified.

- **Parameters**: `string path` – An absolute path (e.g., `C:\data\file.txt` or `/home/user/file.txt`).
- **Return value**: `true`.
- **Throws**: Never.

### `IsAbsolute_RelativePath_ReturnsFalse`
Verifies that a relative path is not identified as absolute.

- **Parameters**: `string path` – A relative path (e.g., `data\file.txt` or `./subdir/file.txt`).
- **Return value**: `false`.
- **Throws**: Never.

### `GetFileSize_ExistingFile_ReturnsCorrectSize`
Ensures that the size of an existing file is returned accurately.

- **Parameters**: `string filePath` – Path to an existing file.
- **Return value**: The file size in bytes as a positive integer.
- **Throws**: Never.

### `GetFileSize_NonExistentFile_ReturnsNegativeOne`
Confirms that querying a non-existent file returns `-1`.

- **Parameters**: `string filePath` – Path to a non-existent file.
- **Return value**: `-1`.
- **Throws**: Never.

### `IsValidFilePath_ExistingNonEmptyFile_ReturnsTrue`
Validates that a path to an existing, non-empty file is considered valid.

- **Parameters**: `string filePath` – Path to an existing, non-empty file.
- **Return value**: `true`.
- **Throws**: Never.

### `IsValidFilePath_NonExistentFile_ReturnsFalse`
Ensures that a path to a non-existent file is not considered valid.

- **Parameters**: `string filePath` – Path to a non-existent file.
- **Return value**: `false`.
- **Throws**: Never.

### `IsValidFilePath_EmptyFile_ReturnsFalse`
Confirms that a path to an empty file is not considered valid.

- **Parameters**: `string filePath` – Path to an existing but empty file.
- **Return value**: `false`.
- **Throws**: Never.

### `GenerateBackupFileName_WithKnownTimestamp_ContainsExpectedParts`
Verifies that a backup filename generated with a known timestamp contains expected components.

- **Parameters**: `string baseName`, `DateTime timestamp` – Base name and timestamp used for generation.
- **Return value**: A string containing the base name, timestamp, and file extension.
- **Throws**: Never.

### `GenerateBackupFileName_WithoutTimestamp_ContainsCurrentDate`
Ensures that a backup filename generated without an explicit timestamp includes the current date.

- **Parameters**: `string baseName` – Base name for the backup file.
- **Return value**: A string containing the base name, current date, and file extension.
- **Throws**: Never.

### `CombinePath_TwoSegments_CombinesCorrectly`
Checks that two path segments are combined correctly into a valid path.

- **Parameters**: `params string[] segments` – Two non-empty path segments.
- **Return value**: A combined and normalized path string.
- **Throws**: Never.

### `CombinePath_SkipsEmptySegments_ProducesValidPath`
Ensures that empty segments are ignored during path combination.

- **Parameters**: `params string[] segments` – Array containing empty and non-empty segments.
- **Return value**: A valid path string with empty segments omitted.
- **Throws**: Never.

### `CombinePath_NoSegments_ThrowsArgumentException`
Confirms that passing no segments to `CombinePath` throws an `ArgumentException`.

- **Parameters**: `params string[] segments` – Empty array.
- **Throws**: `ArgumentException`.

### `CombinePath_AllSegmentsEmpty_ThrowsArgumentException`
Ensures that passing only empty segments to `CombinePath` throws an `ArgumentException`.

- **Parameters**: `params string[] segments` – Array containing only empty strings.
- **Throws**: `ArgumentException`.

### `EnsureDirectoryExists_DirectoryDoesNotExist_CreatesIt`
Verifies that a non-existent directory is created when requested.

- **Parameters**: `string directoryPath` – Path to a non-existent directory.
- **Return value**: `void`.
- **Throws**: Never.

## Usage
