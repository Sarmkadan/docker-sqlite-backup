# IEncryptionService

Interface that provides encryption-related operations for securing sensitive data in the `docker-sqlite-backup` project.

## API

### `bool IsEnabled`
Indicates whether encryption is currently enabled and available for use.

- **Return value**: `true` if encryption is enabled; otherwise, `false`.
- **Exceptions**: Does not throw exceptions.

### `bool HasValidKey`
Checks if a valid encryption key is present and usable for encryption operations.

- **Return value**: `true` if a valid key is available; otherwise, `false`.
- **Exceptions**: Does not throw exceptions.

### `string? KeyFingerprint`
Gets the fingerprint of the currently loaded encryption key, if available.

- **Return value**: A string representing the key fingerprint, or `null` if no key is loaded or the fingerprint cannot be determined.
- **Exceptions**: Does not throw exceptions.

### `string KeySource`
Gets the source identifier for the currently loaded encryption key.

- **Return value**: A string describing the key source (e.g., "environment variable", "file path", "secret store").
- **Exceptions**: Does not throw exceptions. Returns a non-null value only when encryption is enabled.

## Usage

### Example 1: Checking encryption status
