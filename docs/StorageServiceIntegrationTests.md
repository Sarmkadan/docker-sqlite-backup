# StorageServiceIntegrationTests

The `StorageServiceIntegrationTests` class serves as the primary integration test suite for validating the behavior of storage backends within the `docker-sqlite-backup` project. It verifies end-to-end functionality for local and S3 storage providers, ensuring that backup upload, download, deletion, listing, and space reporting operations adhere to expected contracts under various conditions, including missing directories, non-existent files, and unknown storage types.

## API

### Constructors

#### `public StorageServiceIntegrationTests()`
Initializes a new instance of the `StorageServiceIntegrationTests` class. This constructor typically sets up the necessary test context, temporary directories, or mock configurations required before individual test methods execute.

### Methods

#### `public void Dispose()`
Releases unmanaged resources and performs cleanup operations associated with the test instance. This includes deleting temporary files, removing test directories created during execution, and disposing of any disposable objects instantiated during the test lifecycle.

#### `public async Task UploadBackupAsync_LocalStorage_CopiesFileToDestination()`
Validates that uploading a backup to a local storage provider successfully copies the source file to the configured destination path.
*   **Parameters**: None (uses test context setup).
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the file copy fails, permissions are insufficient, or the destination path is invalid.

#### `public async Task UploadBackupAsync_FileDoesNotExist_ThrowsLocalStorageException()`
Ensures that attempting to upload a backup file that does not exist on the source filesystem results in a `LocalStorageException`.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Expects a `LocalStorageException`; fails the test if no exception or a different exception type is thrown.

#### `public async Task UploadBackupAsync_CreatesDestinationDirectoryIfMissing()`
Verifies that the upload operation automatically creates the destination directory structure if it does not already exist prior to the file copy.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the directory creation fails or the file is not copied after directory creation.

#### `public async Task DownloadBackupAsync_ExistingLocalFile_CopiesFileToTemp()`
Confirms that downloading an existing backup from local storage correctly copies the file to a temporary location.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the source file cannot be read or the write to the temporary location fails.

#### `public async Task DeleteBackupAsync_ExistingLocalFile_DeletesIt()`
Tests that deleting an existing backup file removes it from the local storage medium.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the file remains after the operation completes.

#### `public async Task DeleteBackupAsync_NonExistentLocalFile_DoesNotThrow()`
Ensures that attempting to delete a backup file that does not exist completes gracefully without throwing an exception.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: The test fails if any exception is thrown during execution.

#### `public async Task ListBackupsAsync_DirectoryWithSqliteFiles_ReturnsCorrectEntries()`
Validates that listing backups in a directory containing SQLite files returns a collection containing only the relevant `.sqlite` (or configured extension) files with correct metadata.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the returned list is empty, contains non-SQLite files, or has incorrect entry details.

#### `public async Task ListBackupsAsync_EmptyDirectory_ReturnsEmptyList()`
Confirms that listing backups in an existing but empty directory returns an empty collection.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the returned list is not empty.

#### `public async Task ListBackupsAsync_NonExistentDirectory_ReturnsEmptyList()`
Ensures that requesting a list of backups from a directory path that does not exist returns an empty collection rather than throwing an error.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: The test fails if an exception is thrown or if the result is not an empty list.

#### `public async Task GetAvailableSpaceAsync_LocalStorage_ReturnsPositiveValue()`
Checks that querying available space on local storage returns a positive integer value representing bytes.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the returned value is zero, negative, or if the operation fails.

#### `public async Task GetAvailableSpaceAsync_S3Storage_ReturnsMaxValue()`
Verifies that querying available space for an S3 storage provider returns the maximum possible value (indicating unlimited or undefined capacity typical of cloud object storage).
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if the returned value is not the expected maximum constant.

#### `public async Task UploadBackupAsync_UnknownStorageType_ThrowsStorageException()`
Ensures that attempting to upload a backup using an unrecognized or unsupported storage type identifier results in a generic `StorageException`.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Expects a `StorageException`; fails if no exception or a different type is thrown.

#### `public async Task FullWorkflow_UploadThenListThenDelete_WorksEndToEnd()`
Executes a comprehensive sequence of operations: uploading a file, listing the directory to confirm presence, and deleting the file, verifying the system state after each step.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous operation.
*   **Exceptions**: Throws if any step in the workflow fails or if the state assertions do not match expectations.

## Usage

### Example 1: Manual Execution of Specific Test Logic
While typically run via a test runner, the logic within these methods can be instantiated to verify storage behavior in a development environment.

```csharp
using System;
using System.Threading.Tasks;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Initialize the test suite
        var tests = new StorageServiceIntegrationTests();

        try 
        {
            // Execute specific integration check manually
            await tests.UploadBackupAsync_CreatesDestinationDirectoryIfMissing();
            Console.WriteLine("Directory creation test passed.");
            
            await tests.ListBackupsAsync_EmptyDirectory_ReturnsEmptyList();
            Console.WriteLine("Empty list test passed.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Integration check failed: {ex.Message}");
        }
        finally
        {
            // Ensure cleanup of temporary resources
            tests.Dispose();
        }
    }
}
```

### Example 2: Validating End-to-End Workflow
This example demonstrates running the full workflow test to ensure the storage service handles the complete lifecycle of a backup file.

```csharp
using System;
using System.Threading.Tasks;

public class WorkflowValidator
{
    public static async Task ValidateStoragePipeline()
    {
        var tests = new StorageServiceIntegrationTests();
        
        try 
        {
            // Run the comprehensive end-to-end scenario
            await tests.FullWorkflow_UploadThenListThenDelete_WorksEndToEnd();
            
            Console.WriteLine("Full storage pipeline validation successful.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Pipeline validation failed: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
        finally
        {
            tests.Dispose();
        }
    }
}
```

## Notes

*   **Side Effects**: Most methods in this class perform file system operations, including creating directories, writing files, and deleting data. The `Dispose` method must be called after execution to guarantee that temporary artifacts are removed, preventing disk space leakage or interference with subsequent test runs.
*   **Asynchronous Execution**: All functional tests return `Task` and must be awaited. Blocking on these tasks (e.g., using `.Result` or `.Wait()`) in UI or ASP.NET contexts may lead to deadlocks; always use `await`.
*   **Thread Safety**: This class is not thread-safe. Instances should not be shared across multiple threads concurrently. Each test method assumes exclusive access to the configured test directories and temporary paths during its execution.
*   **Exception Handling**: Several methods specifically validate that exceptions are thrown (e.g., `LocalStorageException`, `StorageException`) under error conditions. In a standard test runner, these methods pass only if the expected exception occurs; if run manually, the caller must anticipate these exceptions as part of the valid control flow for negative test cases.
*   **Storage Abstraction**: The tests cover both local file system constraints (e.g., directory creation, file existence) and cloud storage abstractions (e.g., `GetAvailableSpaceAsync` returning `long.MaxValue` for S3). Behavior may vary depending on the underlying configuration provided to the test constructor.
