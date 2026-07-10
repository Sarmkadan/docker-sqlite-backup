# VerificationServiceIntegrationTests

`VerificationServiceIntegrationTests` is an integration test class for the `VerificationService` component in the `docker-sqlite-backup` project. It validates end-to-end behavior of backup verification operations—integrity checks, checksum verification, temporary file restoration, cleanup, and history retrieval—against real or simulated file system and database artifacts. The class implements `IAsyncLifetime` to manage shared test setup and teardown asynchronously.

## API

### `public VerificationServiceIntegrationTests`
The parameterless constructor. Initializes the test class instance. Any shared resources required across test methods are typically allocated in `InitializeAsync` rather than here.

### `public Task InitializeAsync`
Called by the test framework before any test method runs. Sets up the integration test environment, such as creating temporary directories, initializing the `VerificationService` with required dependencies, and preparing any shared fixture data. Returns a `Task` that completes when initialization is finished.

### `public Task DisposeAsync`
Called by the test framework after all test methods have executed. Cleans up resources acquired during `InitializeAsync` or accumulated during the test run—removing temporary directories, disposing of service instances, and releasing file handles. Returns a `Task` that completes when disposal is finished.

### `public async Task PerformIntegrityCheckAsync_ValidDatabase_ReturnsValidWithNoErrors`
Tests that performing an integrity check on a valid, uncorrupted SQLite database file returns a result indicating validity with zero reported errors. The method arranges a valid database file, invokes the integrity check, and asserts the outcome.

- **Parameters:** None (relies on test fixture state).
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the result is not valid or errors are present.

### `public async Task PerformIntegrityCheckAsync_NonExistentFile_ThrowsFileNotFoundException`
Tests that requesting an integrity check on a file path that does not exist causes the service to throw a `FileNotFoundException`. The method supplies a path known to be absent and verifies the exception type.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the expected exception is not thrown or a different exception type is raised.

### `public async Task VerifyChecksumAsync_CorrectChecksum_ReturnsTrue`
Tests that verifying a file’s computed checksum against a matching expected checksum returns `true`. The method provides a file and its precomputed correct checksum, then asserts the verification result.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the result is `false`.

### `public async Task VerifyChecksumAsync_WrongChecksum_ReturnsFalse`
Tests that verifying a file’s computed checksum against a deliberately incorrect expected checksum returns `false`. The method supplies a mismatched checksum value and asserts the negative result.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the result is `true`.

### `public async Task VerifyChecksumAsync_EmptyExpectedChecksum_ReturnsTrue`
Tests that when the expected checksum is an empty string, the verification returns `true`—treating an absent checksum constraint as a pass. The method supplies an empty string as the expected value and asserts the outcome.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the result is `false`.

### `public async Task RestoreToTemporaryAsync_UnencryptedBackup_CopiesFileToTempDir`
Tests that restoring an unencrypted backup file to a temporary directory successfully copies the file to the target location. The method arranges a source backup, invokes the restore operation, and verifies the file’s presence and integrity in the temporary directory.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the file is not copied or the copy is corrupted.

### `public async Task CleanupTemporaryFilesAsync_ExistingDirectory_DeletesIt`
Tests that cleaning up temporary files removes an existing temporary directory and its contents. The method creates a directory with representative files, calls the cleanup operation, and asserts the directory no longer exists.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the directory persists after cleanup.

### `public async Task VerifyBackupAsync_ValidSqliteFile_ReturnsSuccessfulVerification`
Tests the full verification workflow on a valid SQLite backup file with a correct checksum. The method expects a successful verification result, confirming that integrity and checksum checks both pass.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if verification indicates failure.

### `public async Task VerifyBackupAsync_WrongChecksum_ReturnsFailedVerification`
Tests the full verification workflow on a valid SQLite backup file paired with an incorrect expected checksum. The method expects a failed verification result due to the checksum mismatch.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if verification incorrectly reports success.

### `public async Task GetVerificationHistoryAsync_DelegatesToRepository`
Tests that retrieving verification history delegates to the underlying repository layer and returns the expected data. The method arranges a repository with known history entries, calls the service, and asserts the returned collection matches.

- **Parameters:** None.
- **Returns:** A `Task` representing the asynchronous test operation.
- **Throws:** Test assertion failures if the returned history differs from the repository data or delegation does not occur.

## Usage

### Example 1: Running a subset of integration tests with a shared fixture

```csharp
public class VerificationServiceIntegrationTestRunner
{
    private readonly VerificationServiceIntegrationTests _tests;

    public VerificationServiceIntegrationTestRunner()
    {
        _tests = new VerificationServiceIntegrationTests();
    }

    public async Task RunVerificationScenariosAsync()
    {
        await _tests.InitializeAsync();

        try
        {
            // Verify a known-good backup end-to-end
            await _tests.VerifyBackupAsync_ValidSqliteFile_ReturnsSuccessfulVerification();

            // Verify checksum mismatch is detected
            await _tests.VerifyBackupAsync_WrongChecksum_ReturnsFailedVerification();

            // Confirm history retrieval delegates correctly
            await _tests.GetVerificationHistoryAsync_DelegatesToRepository();
        }
        finally
        {
            await _tests.DisposeAsync();
        }
    }
}
```

### Example 2: Testing cleanup and restore operations in sequence

```csharp
public async Task ValidateRestoreAndCleanupLifecycleAsync()
{
    var tests = new VerificationServiceIntegrationTests();
    await tests.InitializeAsync();

    try
    {
        // Restore an unencrypted backup to a temporary directory
        await tests.RestoreToTemporaryAsync_UnencryptedBackup_CopiesFileToTempDir();

        // Perform integrity check on the restored file
        await tests.PerformIntegrityCheckAsync_ValidDatabase_ReturnsValidWithNoErrors();

        // Clean up the temporary directory
        await tests.CleanupTemporaryFilesAsync_ExistingDirectory_DeletesIt();
    }
    finally
    {
        await tests.DisposeAsync();
    }
}
```

## Notes

- **Test isolation:** Each test method is designed to run independently. `InitializeAsync` and `DisposeAsync` ensure a clean state before and after each test, preventing shared state leakage between scenarios.
- **File system dependencies:** Tests that interact with the file system (`PerformIntegrityCheckAsync_NonExistentFile_ThrowsFileNotFoundException`, `RestoreToTemporaryAsync_UnencryptedBackup_CopiesFileToTempDir`, `CleanupTemporaryFilesAsync_ExistingDirectory_DeletesIt`) assume the test process has sufficient permissions to read, write, and delete files and directories in the configured temporary paths.
- **Checksum edge cases:** `VerifyChecksumAsync_EmptyExpectedChecksum_ReturnsTrue` documents the design decision that an empty or unspecified expected checksum is treated as a passing condition, not a failure. Callers relying on mandatory checksum validation should ensure the expected value is non-empty.
- **Thread safety:** The class is intended for sequential execution within a test framework. No synchronization mechanisms are exposed, and concurrent invocation of test methods from multiple threads is not supported and may lead to race conditions on shared fixture resources.
- **Exception expectations:** Tests that assert exceptions (`PerformIntegrityCheckAsync_NonExistentFile_ThrowsFileNotFoundException`) will fail if the service implementation wraps the expected exception in an aggregate or different type. The test expects a direct `FileNotFoundException`.
- **Verification result model:** `VerifyBackupAsync_ValidSqliteFile_ReturnsSuccessfulVerification` and `VerifyBackupAsync_WrongChecksum_ReturnsFailedVerification` imply a result object that encapsulates success/failure status and possibly error details. The exact shape of that result is determined by the `VerificationService` implementation under test.
