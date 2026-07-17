# BackupServiceValidation

`BackupServiceValidation` provides static utility methods to validate configuration and runtime state required by the backup service in `docker-sqlite-backup`. It ensures that critical settings such as backup directory paths, retention policies, and connection strings are valid before operations proceed, preventing runtime failures due to misconfiguration.

## API

### `Validate()`
Returns a list of validation error messages. Each message describes a configuration or runtime issue that would prevent the backup service from functioning correctly. An empty list indicates that all validations passed.

- **Parameters**: None
- **Return value**: `IReadOnlyList<string>` – A read-only list of error messages. If empty, the configuration is valid.
- **Exceptions**: None

### `IsValid()`
Determines whether the current configuration and runtime state are valid for backup operations. Returns `true` if all validations pass; otherwise, returns `false`.

- **Parameters**: None
- **Return value**: `bool` – `true` if the configuration is valid; otherwise, `false`.
- **Exceptions**: None

### `EnsureValid()`
Validates the current configuration and runtime state. If any validation fails, throws an `InvalidOperationException` with a message describing the first encountered error. If all validations pass, the method returns without throwing.

- **Parameters**: None
- **Return value**: `void`
- **Exceptions**: Throws `InvalidOperationException` if any validation fails. The exception message describes the first encountered error.
