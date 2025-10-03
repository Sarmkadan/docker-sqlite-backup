# EncryptionServiceExtensions

The `EncryptionServiceExtensions` class provides a set of asynchronous extension methods designed to facilitate secure data handling within the `docker-sqlite-backup` project. It encapsulates cryptographic operations for strings and streams, enabling seamless encryption of backup contents before storage and decryption during restoration processes without requiring direct interaction with lower-level cryptographic primitives.

## API

### EncryptStringAsync
```csharp
public static async Task<string> EncryptStringAsync(...)
```
Encrypts a plain text string into an encrypted representation. This method is typically used for securing small configuration values or metadata associated with the backup process. It accepts the input string and necessary cryptographic context (such as keys or initialization vectors) via its parameters. The method returns a `Task<string>` containing the encrypted payload, often encoded in Base64 or a similar format suitable for text storage. It throws an exception if the input is null, the cryptographic provider is uninitialized, or the encryption process fails due to invalid key material.

### DecryptStringAsync
```csharp
public static async Task<string> DecryptStringAsync(...)
```
Decrypts a previously encrypted string back to its original plain text form. This is the counterpart to `EncryptStringAsync` and is used when retrieving secured configuration data. It takes the encrypted string and required cryptographic context as parameters. The method returns a `Task<string>` representing the original plain text. It throws an exception if the input is null, the format is invalid, the key material is incorrect, or the integrity check of the encrypted data fails.

### EncryptStreamAsync
```csharp
public static async Task<string> EncryptStreamAsync(...)
```
Reads data from an input stream, encrypts the content, and returns the result as an encoded string. This method is optimized for handling SQLite database files or other binary assets where the output needs to be stored as a text blob or transmitted over text-based protocols. It accepts the source `Stream` and cryptographic parameters. The return value is a `Task<string>` containing the full encrypted content. It throws an exception if the input stream is null, not readable, or if the data size exceeds memory constraints during the buffering process.

### DecryptToStreamAsync
```csharp
public static async Task<Stream> DecryptToStreamAsync(...)
```
Decrypts an encrypted input (typically a string or encoded stream) and writes the resulting plain text data into a new readable `Stream`. This is primarily used to restore SQLite database files from their encrypted backups, allowing the decrypted content to be piped directly into a file writer or database connection. It accepts the encrypted source and cryptographic context. The method returns a `Task<Stream>` positioned at the beginning of the decrypted data. It throws an exception if the source data is corrupted, the decryption key is invalid, or if the underlying stream operations fail.

## Usage

### Encrypting a Database File Path Configuration
The following example demonstrates how to securely store a sensitive file path or connection string using `EncryptStringAsync` before saving it to an environment variable or configuration file.

```csharp
using System;
using System.Threading.Tasks;

public class ConfigManager
{
    public async Task SaveSecureConfigAsync(string sensitivePath)
    {
        // Encrypt the sensitive path before storage
        string encryptedPath = await EncryptionServiceExtensions.EncryptStringAsync(sensitivePath);
        
        // Store 'encryptedPath' in your configuration system
        Console.WriteLine($"Encrypted configuration saved: {encryptedPath}");
    }
}
```

### Restoring a Backup to a Stream
This example illustrates how to decrypt a backed-up SQLite database string representation back into a stream, which can then be written to disk for restoration.

```csharp
using System;
using System.IO;
using System.Threading.Tasks;

public class BackupRestorer
{
    public async Task RestoreDatabaseAsync(string encryptedBackupData, string destinationPath)
    {
        // Decrypt the backup data directly into a stream
        using (Stream decryptedStream = await EncryptionServiceExtensions.DecryptToStreamAsync(encryptedBackupData))
        {
            // Write the decrypted stream to the target file
            using (FileStream fileStream = File.Create(destinationPath))
            {
                await decryptedStream.CopyToAsync(fileStream);
            }
        }
        
        Console.WriteLine($"Database restored to {destinationPath}");
    }
}
```

## Notes

*   **Asynchronous Execution**: All methods are fully asynchronous and return `Task` objects. Callers should await these methods to prevent blocking the calling thread, which is critical in I/O-bound scenarios like file processing or network transmission.
*   **Stream Disposal**: For `DecryptToStreamAsync`, the returned `Stream` is owned by the caller and must be disposed of properly after use to release unmanaged resources. The input streams provided to encryption methods should also be managed by the caller unless explicitly documented otherwise.
*   **Thread Safety**: As these methods are static and rely on immutable parameters or thread-local cryptographic contexts, they are generally safe for concurrent calls from multiple threads. However, care must be taken if external mutable state (such as shared key containers) is injected into the service context.
*   **Error Handling**: These methods do not swallow exceptions. Failures related to cryptography (e.g., tampered data, wrong keys) or I/O (e.g., closed streams) will propagate as standard .NET exceptions. Callers should implement appropriate `try-catch` blocks to handle decryption failures gracefully.
*   **Memory Usage**: `EncryptStreamAsync` loads the stream content into memory to produce a string result. For very large database files, this may lead to high memory consumption; in such cases, streaming-to-streaming approaches (if available in specific implementations) or chunked processing should be considered.
