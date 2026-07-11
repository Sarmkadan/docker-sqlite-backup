# StorageAdapterTests

`StorageAdapterTests` is a test class that validates the functionality of storage adapters within the `docker-sqlite-backup` project. It ensures correct behavior for operations such as uploading, listing, deleting, and downloading backups, as well as validating configuration settings for both local and Azure-based storage. The tests cover edge cases, error conditions, and directory/file management to guarantee robustness in real-world scenarios.

## API

### `InitializeAsync`
**Purpose**: Initializes the test environment before each test execution. This may include setting up temporary directories or resources required for testing.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (implementation-specific exceptions may occur but are not documented as part of the public contract).

---

### `DisposeAsync`
**Purpose**: Cleans up resources after each test execution, such as deleting temporary files or directories.
**Parameters**: None.
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (implementation-specific exceptions may occur but are not documented as part of the public contract).

---

### `UploadBackupAsync_LocalStorage_CopiesFileToDestination`
**Purpose**: Verifies that `UploadBackupAsync` correctly copies a file from a source location to a specified destination in local storage.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `UploadBackupAsync_LocalStorage_CreatesDirectoryIfMissing`
**Purpose**: Ensures that `UploadBackupAsync` creates the destination directory if it does not already exist.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `UploadBackupAsync_FileNotFound_ThrowsLocalStorageException`
**Purpose**: Confirms that `UploadBackupAsync` throws a `LocalStorageException` when the source file does not exist.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: `LocalStorageException` (asserted as part of the test).

---

### `ListBackupsAsync_LocalStorage_ReturnsUploadedFiles`
**Purpose**: Validates that `ListBackupsAsync` returns the correct list of uploaded files in local storage.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `ListBackupsAsync_LocalStorage_MissingDirectory_ReturnsEmpty`
**Purpose**: Ensures that `ListBackupsAsync` returns an empty collection when the target directory does not exist.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `DeleteBackupAsync_LocalStorage_RemovesFile`
**Purpose**: Tests that `DeleteBackupAsync` successfully removes a specified backup file from local storage.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `DownloadBackupAsync_LocalStorage_CopiesFileToTempLocation`
**Purpose**: Verifies that `DownloadBackupAsync` correctly copies a backup file from local storage to a temporary location.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `AzureConfiguration_WithConnectionStringAndContainer_IsValid`
**Purpose**: Confirms that an Azure storage configuration with a valid connection string and container name is considered valid.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: `void`.
**Throws**: N/A (test failures are asserted internally).

---

### `AzureConfiguration_MissingContainerName_IsInvalid`
**Purpose**: Ensures that an Azure storage configuration without a container name is flagged as invalid.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: `void`.
**Throws**: N/A (test failures are asserted internally).

---

### `AzureConfiguration_MissingCredentials_IsInvalid`
**Purpose**: Validates that an Azure storage configuration missing credentials (connection string or SAS URI) is considered invalid.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: `void`.
**Throws**: N/A (test failures are asserted internally).

---

### `AzureConfiguration_WithSasUri_IsValid`
**Purpose**: Tests that an Azure storage configuration with a valid SAS URI is accepted as valid.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: `void`.
**Throws**: N/A (test failures are asserted internally).

---

### `GetAvailableSpaceAsync_AzureConfig_ReturnsLongMaxValue`
**Purpose**: Verifies that `GetAvailableSpaceAsync` returns `long.MaxValue` for Azure storage configurations, as cloud storage is assumed to have unlimited capacity.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

---

### `GetAvailableSpaceAsync_LocalConfig_ReturnsPositiveValue`
**Purpose**: Ensures that `GetAvailableSpaceAsync` returns a positive value for local storage configurations, reflecting the actual available space on disk.
**Parameters**: None (test-specific setup is handled internally).
**Return Value**: A `Task` representing the asynchronous operation.
**Throws**: N/A (test failures are asserted internally).

## Usage

### Example 1: Testing Local Storage Operations
