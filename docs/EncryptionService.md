# EncryptionService

The `EncryptionService` class provides core cryptographic operations for securing SQLite database backup files within the `docker-sqlite-backup` project. It manages the lifecycle of encryption keys and facilitates asynchronous file-level encryption and decryption, ensuring that backup artifacts remain confidential both at rest and during transit. The service exposes state inspection capabilities to verify configuration status and key validity before performing sensitive operations.

## API

### `public EncryptionService`
Initializes a new instance of the `EncryptionService` class. This constructor sets up the internal cryptographic providers and loads configuration settings required for subsequent operations.

### `public async Task<string> EncryptFileAsync`
Encrypts a specified source file and writes the encrypted content to a new destination file.
*   **Parameters**: Accepts the path to the source file and the destination path (specific parameter names depend on implementation, but logically represent input/output paths).
*   **Return Value**: Returns a `Task<string>` representing the asynchronous operation. The result is the absolute path to the newly created encrypted file.
*   **Exceptions**: Throws an exception if the source file does not exist, if the destination path is invalid, if `IsEncryptionEnabled` is false, or if the active key is missing or invalid.

### `public async Task<string> DecryptFileAsync`
Decrypts a specified encrypted source file and writes the plaintext content to a new destination file.
*   **Parameters**: Accepts the path to the encrypted source file and the desired destination path.
*   **Return Value**: Returns a `Task<string>` representing the asynchronous operation. The result is the absolute path to the newly created decrypted file.
*   **Exceptions**: Throws an exception if the source file is not found, if the decryption key is incorrect, if the file format is invalid, or if `IsEncryptionEnabled` is false.

### `public string GenerateKey`
Generates a new, cryptographically secure encryption key.
*   **Return Value**: Returns a `string` containing the newly generated key, typically formatted as a base64-encoded string or hex string depending on internal configuration.
*   **Remarks**: This method is synchronous and should be called to provision keys before enabling encryption.

### `public bool ValidateKey`
Validates the format and usability of a provided encryption key string.
*   **Parameters**: Takes the key string to validate.
*   **Return Value**: Returns `true` if the key matches the expected format and length requirements; otherwise, returns `false`.
*   **Remarks**: This method does not verify if the key can successfully decrypt specific data, only that the key structure is valid for the service.

### `public bool IsEncryptionEnabled`
Gets a value indicating whether the encryption service is currently active and configured.
*   **Return Value**: Returns `true` if a valid key is loaded and encryption features are enabled in the configuration; otherwise, `false`.

### `public string? GetActiveKey`
Retrieves the currently loaded encryption key.
*   **Return Value**: Returns the active key as a `string`, or `null` if no key is currently loaded or if encryption is disabled.
*   **Security Note**: Care should be taken when handling the returned string to prevent leakage into logs or unsecured memory.

### `public EncryptionStatus GetStatus`
Retrieves the current operational status of the encryption subsystem.
*   **Return Value**: Returns an `EncryptionStatus` enum value indicating the state (e.g., `Ready`, `MissingKey`, `Disabled`, `Error`).

## Usage

### Example 1: Initializing and Encrypting a Backup
This example demonstrates generating a key, validating it, and encrypting a SQLite backup file.

```csharp
var encryptionService = new EncryptionService();

// Generate and validate a new key
string newKey = encryptionService.GenerateKey();
if (!encryptionService.ValidateKey(newKey))
{
    throw new InvalidOperationException("Generated key failed validation.");
}

// Ensure encryption is enabled before proceeding
if (!encryptionService.IsEncryptionEnabled)
{
    // Logic to load/configure the key would go here based on project specifics
    Console.WriteLine("Encryption is not currently enabled.");
    return;
}

// Perform asynchronous encryption
string sourcePath = "/backups/db.sqlite";
string destPath = "/backups/db.sqlite.enc";

try 
{
    string encryptedFilePath = await encryptionService.EncryptFileAsync(sourcePath, destPath);
    Console.WriteLine($"Backup encrypted successfully: {encryptedFilePath}");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Encryption failed: {ex.Message}");
}
```

### Example 2: Status Check and Decryption
This example checks the service status and decrypts a file only if the system is ready.

```csharp
var encryptionService = new EncryptionService();

// Check system status
var status = encryptionService.GetStatus();
if (status != EncryptionStatus.Ready)
{
    Console.WriteLine($"Cannot decrypt: Service status is {status}");
    return;
}

string? activeKey = encryptionService.GetActiveKey();
if (string.IsNullOrEmpty(activeKey))
{
    Console.WriteLine("No active key found.");
    return;
}

try 
{
    string encryptedPath = "/backups/db.sqlite.enc";
    string restoredPath = "/restored/db.sqlite";
    
    string decryptedFilePath = await encryptionService.DecryptFileAsync(encryptedPath, restoredPath);
    Console.WriteLine($"Database restored to: {decryptedFilePath}");
}
catch (CryptographicException)
{
    Console.Error.WriteLine("Decryption failed: Invalid key or corrupted file.");
}
```

## Notes

*   **Thread Safety**: The asynchronous methods (`EncryptFileAsync`, `DecryptFileAsync`) are designed to be non-blocking, but the internal state management regarding the active key may not be fully thread-safe for concurrent modification. It is recommended to initialize and configure the key during application startup before invoking concurrent encryption tasks.
*   **Key Management**: The `GetActiveKey` method returns the raw key material. Callers must ensure this value is not logged or exposed in exception messages.
*   **File Overwrites**: The encryption and decryption methods typically assume the destination path does not exist or is intended to be overwritten. Implementations should handle `IOException` if the destination file is locked or read-only.
*   **Validation vs. Decryption**: `ValidateKey` only checks syntactic correctness. A key can be valid according to this method but still fail to decrypt a specific file if it was not the key used for encryption.
*   **Disabled State**: If `IsEncryptionEnabled` returns `false`, calls to `EncryptFileAsync` or `DecryptFileAsync` will immediately throw an exception without attempting file I/O.
