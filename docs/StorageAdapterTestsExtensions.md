# StorageAdapterTestsExtensions

A static utility class providing helper methods for writing unit and integration tests against storage adapters in the docker-sqlite-backup project. These extensions simplify common test scenarios such as file system manipulation, directory validation, and storage configuration setup for Azure and local storage backends.

## API

### `CreateTempFile`
Creates a temporary file with optional content in the system's temporary directory.

- **Parameters**
  - `content` (optional): The string content to write to the file. If `null`, an empty file is created.
- **Returns**
  - `string`: The full path to the created temporary file.
- **Throws**
  - `IOException`: If the file cannot be created or written.

### `CreateTempDirectory`
Creates a temporary directory with a unique name in the system's temporary directory.

- **Returns**
  - `string`: The full path to the created temporary directory.
- **Throws**
  - `IOException`: If the directory cannot be created.

### `WithLocalStorage`
Configures a `LocalStorageConfiguration` instance with the specified directory path.

- **Parameters**
  - `directoryPath`: The absolute path to the directory to use for local storage.
- **Returns**
  - `LocalStorageConfiguration`: A configured `LocalStorageConfiguration` instance.
- **Throws**
  - `ArgumentNullException`: If `directoryPath` is `null`.
  - `DirectoryNotFoundException`: If the specified directory does not exist.

### `WithAzureStorage`
Configures an `AzureConfiguration` instance with the specified connection string and container name.

- **Parameters**
  - `connectionString`: The Azure Storage connection string.
  - `containerName`: The name of the container to use.
- **Returns**
  - `AzureConfiguration`: A configured `AzureConfiguration` instance.
- **Throws**
  - `ArgumentNullException`: If either `connectionString` or `containerName` is `null`.

### `ShouldThrowLocalStorageExceptionAsync`
Asserts that the provided asynchronous action throws a `LocalStorageException`.

- **Parameters**
  - `action`: The asynchronous action to test.
- **Throws**
  - `XunitException`: If the action does not throw a `LocalStorageException`.

### `ShouldExistWithContentAsync`
Asserts that a file at the specified path exists and contains the expected content.

- **Parameters**
  - `filePath`: The path to the file to check.
  - `expectedContent`: The expected content of the file.
- **Throws**
  - `XunitException`: If the file does not exist or its content does not match `expectedContent`.

### `ShouldNotExist`
Asserts that a file at the specified path does not exist.

- **Parameters**
  - `filePath`: The path to the file to check.
- **Throws**
  - `XunitException`: If the file exists.

### `DirectoryShouldExist`
Asserts that a directory at the specified path exists.

- **Parameters**
  - `directoryPath`: The path to the directory to check.
- **Throws**
  - `XunitException`: If the directory does not exist.

### `DirectoryShouldNotExist`
Asserts that a directory at the specified path does not exist.

- **Parameters**
  - `directoryPath`: The path to the directory to check.
- **Throws**
  - `XunitException`: If the directory exists.

## Usage

### Example 1: Testing local file storage
