# IntegrityCheckerServiceTestsExtensions

Extension methods for testing scenarios involving `IntegrityCheckerService` instances. These utilities simplify the creation of test databases with varying states (healthy, corrupted, empty, or complex) and provide assertion helpers to verify database integrity checks.

## API

### `CreateTestDatabase`

Creates a new SQLite database file with a default schema suitable for testing integrity checks. The database contains a single table with sample data.

- **Returns**: `string` – The file path to the newly created database.
- **Throws**: `IOException` if the file cannot be created or written.

### `CreateComplexTestDatabase`

Creates a SQLite database with a more complex schema including multiple tables, indexes, and foreign key relationships. Designed to test integrity checks on non-trivial database structures.

- **Returns**: `string` – The file path to the newly created database.
- **Throws**: `IOException` if the file cannot be created or written.

### `CreateCorruptedDatabase`

Creates a SQLite database file and intentionally corrupts it by truncating or modifying critical internal structures. Useful for testing corruption detection logic.

- **Returns**: `string` – The file path to the corrupted database.
- **Throws**: `IOException` if the file cannot be created or corrupted.

### `CreateEmptyDatabase`

Creates an empty SQLite database file with no tables or data. Suitable for testing edge cases where the database is structurally valid but contains no user data.

- **Returns**: `string` – The file path to the empty database.
- **Throws**: `IOException` if the file cannot be created.

### `ShouldIndicateCorruption`

Asserts that the provided `IntegrityCheckerService` instance correctly identifies the given database as corrupted.

- **Parameters**:
  - `service` (`IntegrityCheckerService`) – The service instance to test.
  - `databasePath` (`string`) – The path to the database file expected to be corrupted.
- **Throws**: `XunitException` if the service does not report corruption.

### `ShouldIndicateHealthy`

Asserts that the provided `IntegrityCheckerService` instance correctly identifies the given database as healthy.

- **Parameters**:
  - `service` (`IntegrityCheckerService`) – The service instance to test.
  - `databasePath` (`string`) – The path to the database file expected to be healthy.
- **Throws**: `XunitException` if the service reports corruption or an error.

### `CreateMockService`

Creates a mock or stub implementation of `IntegrityCheckerService` for unit testing without requiring a real database connection. The mock may be preconfigured to simulate specific behaviors.

- **Returns**: `IntegrityCheckerService` – A new mock service instance.
- **Remarks**: The mock may not perform actual integrity checks unless explicitly configured.

## Usage
