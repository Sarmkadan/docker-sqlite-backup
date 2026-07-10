# StorageException

`StorageException` is a custom exception type used within the `docker-sqlite-backup` project to signal failures related to storage operations. It serves as a base class for more specific storage-related exceptions, allowing for consistent error handling across different storage backends (e.g., S3, Local, Azure). The exception includes contextual information about the storage type involved in the failure, enabling more precise error diagnosis and recovery.

## API

### `public string? StorageType`
- **Purpose**: Indicates the type of storage system associated with the exception (e.g., `"S3"`, `"Local"`, `"Azure"`). This property is `null` if the exception is instantiated without specifying a storage type.
- **Returns**: A string representing the storage type, or `null` if not set.

### `public StorageException(string message) : base(message)`
- **Purpose**: Constructs a `StorageException` with a custom error message.
- **Parameters**:
  - `message`: A string describing the error.
- **Throws**: Nothing.
- **Remarks**: The `StorageType` property will be `null` when using this constructor.

### `public StorageException(string message, string storageType) : base(message)`
- **Purpose**: Constructs a `StorageException` with a custom error message and a specified storage type.
- **Parameters**:
  - `message`: A string describing the error.
  - `storageType`: A string identifying the storage system (e.g., `"S3"`).
- **Throws**: Nothing.

### `public S3StorageException(string message) : base(message, "S3")`
- **Purpose**: Constructs a specialized `StorageException` for S3 storage failures, automatically setting the `StorageType` to `"S3"`.
- **Parameters**:
  - `message`: A string describing the error.
- **Throws**: Nothing.

### `public LocalStorageException(string message) : base(message, "Local")`
- **Purpose**: Constructs a specialized `StorageException` for local storage failures, automatically setting the `StorageType` to `"Local"`.
- **Parameters**:
  - `message`: A string describing the error.
- **Throws**: Nothing.

### `public AzureStorageException(string message) : base(message, "Azure")`
- **Purpose**: Constructs a specialized `StorageException` for Azure storage failures, automatically setting the `StorageType` to `"Azure"`.
- **Parameters**:
  - `message`: A string describing the error.
- **Throws**: Nothing.

### `public InsufficientStorageException`
- **Purpose**: A derived exception type (not further detailed in the provided signatures) intended to signal failures due to insufficient storage capacity or availability. Inherits from `StorageException`.
- **Remarks**: The exact constructor signatures and behavior are not specified in the provided members.

## Usage

### Example 1: Throwing a Generic Storage Exception
