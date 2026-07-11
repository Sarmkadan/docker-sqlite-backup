# IntegrityCheckerServiceTests

The `IntegrityCheckerServiceTests` class contains unit tests for verifying the functionality of an integrity checker service that validates SQLite database files. These tests ensure the service correctly identifies healthy databases, detects corruption or missing files, and accurately reports metadata about database structure and content.

## API

### `InitializeAsync`
Initializes the test class before test execution. This method is called automatically by the test framework.
- **Purpose**: Sets up test dependencies or state.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `DisposeAsync`
Cleans up test resources after test execution. This method is called automatically by the test framework.
- **Purpose**: Releases resources or resets state.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `CheckDatabaseAsync_ValidDatabase_ReturnsHealthyReport`
Tests that `CheckDatabaseAsync` returns a healthy integrity report for a valid database file.
- **Purpose**: Verifies the service correctly identifies a healthy database.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `CheckDatabaseAsync_ValidDatabase_PopulatesMetadata`
Tests that `CheckDatabaseAsync` populates metadata (e.g., table counts, row counts) in the integrity report for a valid database.
- **Purpose**: Ensures metadata collection works as expected.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `CheckDatabaseAsync_NonExistentFile_ThrowsFileNotFoundException`
Tests that `CheckDatabaseAsync` throws a `FileNotFoundException` when the database file does not exist.
- **Purpose**: Verifies error handling for missing files.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `FileNotFoundException` if the file is missing.

### `CheckDatabaseAsync_QuickCheckOnly_SkipsFullAndFkChecks`
Tests that `CheckDatabaseAsync` skips full integrity and foreign key checks when `quickCheckOnly` is enabled.
- **Purpose**: Ensures the service respects the quick-check flag.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `CheckDatabaseAsync_MultipleTablesWithData_CountsTablesCorrectly`
Tests that `CheckDatabaseAsync` accurately counts tables and rows in a database with multiple tables and data.
- **Purpose**: Validates metadata accuracy for complex databases.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `QuickCheckAsync_ValidDatabase_ReturnsTrue`
Tests that `QuickCheckAsync` returns `true` for a valid database.
- **Purpose**: Verifies the quick-check functionality for healthy databases.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `QuickCheckAsync_NonExistentFile_ThrowsFileNotFoundException`
Tests that `QuickCheckAsync` throws a `FileNotFoundException` when the database file does not exist.
- **Purpose**: Verifies error handling for missing files in quick-check mode.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: `FileNotFoundException` if the file is missing.

### `CheckBackupFileAsync_ValidDatabase_ReturnsHealthyReport`
Tests that `CheckBackupFileAsync` returns a healthy integrity report for a valid backup file.
- **Purpose**: Ensures backup file validation works as expected.
- **Parameters**: None.
- **Return Value**: `Task` representing the asynchronous operation.
- **Throws**: None.

### `IntegrityReport_IsHealthy_RequiresAllThreeChecksPassed`
Tests that the `IsHealthy` property of `IntegrityReport` returns `true` only if all three checks (quick, full, foreign key) pass.
- **Purpose**: Validates the logic for determining database health.
- **Parameters**: None.
- **Return Value**: Void.
- **Throws**: None.

### `IntegrityReport_Summary_ContainsHealthyMessageWhenAllPassed`
Tests that the `Summary` property of `IntegrityReport` contains a "healthy" message when all checks pass.
- **Purpose**: Ensures the report summary is correctly formatted.
- **Parameters**: None.
- **Return Value**: Void.
- **Throws**: None.

## Usage

### Example 1: Testing a Valid Database
