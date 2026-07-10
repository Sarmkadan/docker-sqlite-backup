# IntegrityCheckerService

A service that performs integrity checks on SQLite databases and backup files to detect corruption, missing pages, or structural inconsistencies. It supports both quick validation scans and comprehensive integrity reports for databases and pre-existing backup files.

## API

### `IntegrityCheckerService`

The primary service class responsible for performing integrity checks on SQLite databases and backup files. This class is designed to be injected and used as a singleton or scoped service within dependency injection containers.

### `public async Task<IntegrityReport> CheckDatabaseAsync`

Performs a full integrity check on an active SQLite database file.

- **Parameters**
  - `databasePath`: The file system path to the SQLite database file to check.
- **Return value**
  - A `Task<IntegrityReport>` that resolves to an `IntegrityReport` object containing detailed results of the check, including any detected errors, warnings, or structural issues.
- **Exceptions**
  - Throws `ArgumentNullException` if `databasePath` is null.
  - Throws `FileNotFoundException` if the file at `databasePath` does not exist.
  - Throws `IOException` if the file cannot be opened or read.
  - Throws `SqliteException` if SQLite reports an internal error during integrity verification.

### `public async Task<bool> QuickCheckAsync`

Performs a lightweight integrity check on an active SQLite database file. This method is optimized for speed and is suitable for frequent or background checks where detailed reporting is not required.

- **Parameters**
  - `databasePath`: The file system path to the SQLite database file to check.
- **Return value**
  - A `Task<bool>` that resolves to `true` if the database passes integrity verification; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `databasePath` is null.
  - Throws `FileNotFoundException` if the file at `databasePath` does not exist.
  - Throws `IOException` if the file cannot be opened or read.
  - Throws `SqliteException` if SQLite reports an internal error during integrity verification.

### `public async Task<IntegrityReport> CheckBackupFileAsync`

Performs a full integrity check on a SQLite database backup file without requiring the file to be attached to a running database instance.

- **Parameters**
  - `backupFilePath`: The file system path to the SQLite backup file to check.
- **Return value**
  - A `Task<IntegrityReport>` that resolves to an `IntegrityReport` object containing detailed results of the check, including any detected errors, warnings, or structural issues.
- **Exceptions**
  - Throws `ArgumentNullException` if `backupFilePath` is null.
  - Throws `FileNotFoundException` if the file at `backupFilePath` does not exist.
  - Throws `IOException` if the file cannot be opened or read.
  - Throws `SqliteException` if SQLite reports an internal error during integrity verification.

## Usage
