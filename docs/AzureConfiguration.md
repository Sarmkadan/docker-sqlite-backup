# AzureConfiguration

Configuration class for Azure Blob Storage interactions in the docker-sqlite-backup project. Encapsulates connection settings, storage policies, and validation logic required to interact with Azure Blob Storage containers.

## API

### `ConnectionString`
- **Purpose**: Gets or sets the Azure Storage account connection string.
- **Type**: `string?`
- **Remarks**: Null or empty indicates the connection is not configured. Used for authentication with Azure Blob Storage.

### `SasUri`
- **Purpose**: Gets or sets the Shared Access Signature (SAS) URI for blob access.
- **Type**: `string?`
- **Remarks**: When provided, overrides `ConnectionString` for authentication. Must be a valid SAS token with appropriate permissions.

### `ContainerName`
- **Purpose**: Gets or sets the name of the Azure Blob Storage container.
- **Type**: `string`
- **Remarks**: Required for all operations. Must comply with Azure naming conventions (3–63 characters, lowercase, alphanumeric and hyphens only).

### `BlobPrefix`
- **Purpose**: Gets or sets the prefix used to filter blobs within the container.
- **Type**: `string`
- **Default**: Empty string (no prefix filtering).
- **Remarks**: Used to scope operations to a subset of blobs. Not validated for correctness.

### `AccessTier`
- **Purpose**: Gets or sets the default access tier for blobs.
- **Type**: `string`
- **Remarks**: Valid values include `"Hot"`, `"Cool"`, and `"Archive"`. Affects storage costs and retrieval latency.

### `EnableImmutability`
- **Purpose**: Gets or sets whether blob immutability policies are enforced.
- **Type**: `bool`
- **Default**: `false`
- **Remarks**: When `true`, prevents modification or deletion of blobs for the specified retention period.

### `SoftDeleteRetentionDays`
- **Purpose**: Gets or sets the number of days for soft-delete retention.
- **Type**: `int`
- **Default**: `0` (soft-delete disabled)
- **Remarks**: Must be between `1` and `365` when `EnableImmutability` is `true`. Affects recoverability of deleted blobs.

### `IsValid`
- **Purpose**: Determines whether the current configuration is valid for Azure operations.
- **Type**: `bool`
- **Returns**: `true` if `ContainerName` is non-empty and either `ConnectionString` or `SasUri` is non-empty; otherwise `false`.
- **Remarks**: Does not validate credentials or connectivity.

### `TestConnectionAsync()`
- **Purpose**: Asynchronously tests the connectivity and authentication to Azure Blob Storage.
- **Returns**: `Task<bool>` indicating success (`true`) or failure (`false`).
- **Exceptions**: May throw `ArgumentException` if configuration is invalid (`IsValid` is `false`).
- **Remarks**: Validates both credentials and container existence. Does not modify state.

## Usage

### Example 1: Basic Configuration with Connection String
