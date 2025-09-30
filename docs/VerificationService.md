# VerificationService

Provides functionality to verify the integrity and correctness of SQLite database backups, including checksum validation, restore verification, and temporary file management.

## API

### `VerificationService`

Initializes a new instance of the `VerificationService` class.

### `async Task<RestoreVerification> VerifyBackupAsync`

Verifies a backup archive by extracting it to a temporary location and validating its contents.

- **Parameters**:
  - `backupPath`: Path to the backup archive file to verify.
  - `expectedChecksum`: Optional expected SHA256 checksum of the backup archive.
- **Return value**: A `RestoreVerification` object containing verification results.
- **Throws**: `ArgumentNullException` if `backupPath` is null.
- **Throws**: `FileNotFoundException` if the backup file does not exist.

### `async Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync`

Retrieves the history of all backup verifications performed by this service.

- **Return value**: An enumerable collection of `RestoreVerification` objects representing past verifications.
- **Throws**: `InvalidOperationException` if the verification history cannot be read.

### `async Task<(bool IsValid, string? Errors)> PerformIntegrityCheckAsync`

Performs an integrity check on a SQLite database file to ensure it is not corrupted.

- **Parameters**:
  - `databasePath`: Path to the SQLite database file to check.
- **Return value**: A tuple where `IsValid` indicates whether the database is valid, and `Errors` contains any error messages (null if no errors).
- **Throws**: `ArgumentNullException` if `databasePath` is null.
- **Throws**: `FileNotFoundException` if the database file does not exist.

### `async Task<bool> VerifyChecksumAsync`

Computes and compares the SHA256 checksum of a file against an expected value.

- **Parameters**:
  - `filePath`: Path to the file to verify.
  - `expectedChecksum`: Expected SHA256 checksum of the file.
- **Return value**: `true` if the computed checksum matches the expected value; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `filePath` or `expectedChecksum` is null.
- **Throws**: `FileNotFoundException` if the file does not exist.

### `async Task<string> RestoreToTemporaryAsync`

Extracts a backup archive to a temporary directory and returns the path to the extracted database.

- **Parameters**:
  - `backupPath`: Path to the backup archive to restore.
- **Return value**: Path to the extracted SQLite database file within the temporary directory.
- **Throws**: `ArgumentNullException` if `backupPath` is null.
- **Throws**: `FileNotFoundException` if the backup file does not exist.
- **Throws**: `InvalidOperationException` if extraction fails.

### `async Task CleanupTemporaryFilesAsync`

Deletes all temporary files created by the service during verification or restoration.

- **Return value**: A `Task` representing the asynchronous cleanup operation.
- **Throws**: `InvalidOperationException` if cleanup cannot be completed.

## Usage

### Example 1: Verify a Backup Archive
