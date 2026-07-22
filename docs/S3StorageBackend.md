# S3StorageBackend

Provides an implementation of `IStorageBackend` that stores SQLite backups in an S3-compatible object storage service. Supports asynchronous upload, download, deletion, and listing of backup files, with connection testing and space availability checks.

## API

### `UploadBackupAsync`

Uploads a backup file to the configured S3 bucket.

**Parameters:**
- `sourcePath` (string): Local filesystem path of the backup file to upload.
- `targetPath` (string): Target object key in the S3 bucket (e.g., `backups/db-2024-06-05T12-30-00Z.sqlite`).
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task<string>`: The S3 object URL of the uploaded file.

**Exceptions:**
- Throws `ArgumentNullException` if `sourcePath` or `targetPath` is null.
- Throws `FileNotFoundException` if `sourcePath` does not exist.
- Throws `AmazonS3Exception` on S3-specific errors (e.g., bucket not found, permission denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

---

### `DownloadBackupAsync`

Downloads a backup file from the configured S3 bucket to a local path.

**Parameters:**
- `sourcePath` (string): S3 object key of the backup file to download.
- `targetPath` (string): Local filesystem path to save the downloaded file.
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task<string>`: The local filesystem path where the file was saved.

**Exceptions:**
- Throws `ArgumentNullException` if `sourcePath` or `targetPath` is null.
- Throws `AmazonS3Exception` on S3-specific errors (e.g., object not found, access denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

---

### `DeleteBackupAsync`

Deletes a backup file from the configured S3 bucket.

**Parameters:**
- `path` (string): S3 object key of the backup file to delete.
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task`: A task that completes when deletion is finished.

**Exceptions:**
- Throws `ArgumentNullException` if `path` is null.
- Throws `AmazonS3Exception` on S3-specific errors (e.g., object not found, permission denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

---
### `ListBackupsAsync`

Lists all backup files in the configured S3 bucket, returning metadata for each.

**Parameters:**
- `prefix` (string, optional): S3 object key prefix to filter results (e.g., `backups/`). Defaults to `null`.
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task<IEnumerable<(string Path, long Size, DateTime Modified)>>`: An enumerable of tuples containing:
  - `Path` (string): S3 object key.
  - `Size` (long): File size in bytes.
  - `Modified` (DateTime): Last modified timestamp in UTC.

**Exceptions:**
- Throws `AmazonS3Exception` on S3-specific errors (e.g., bucket not found, access denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

---
### `TestConnectionAsync`

Tests connectivity to the S3-compatible storage service and validates bucket accessibility.

**Parameters:**
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task<bool>`: `true` if the bucket exists and is accessible; otherwise `false`.

**Exceptions:**
- Throws `AmazonS3Exception` on S3-specific errors (e.g., bucket not found, permission denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

---
### `GetAvailableSpaceAsync`

Gets the available space in bytes for the configured S3 bucket. Note: S3 does not provide a true "available space" metric; this returns the bucket's quota minus current usage if supported by the provider, or `long.MaxValue` if not available.

**Parameters:**
- `cancellationToken` (CancellationToken, optional): Token to monitor for cancellation requests.

**Return value:**
- `Task<long>`: Available space in bytes, or `long.MaxValue` if not determinable.

**Exceptions:**
- Throws `AmazonS3Exception` on S3-specific errors (e.g., bucket not found, access denied).
- Throws `OperationCanceledException` if `cancellationToken` is triggered.

## Usage

### Example 1: Upload and list backups

```csharp
using var backend = new S3StorageBackend(
    bucketName: "my-backups",
    accessKey: "AKIA...",
    secretKey: "SECRET...",
    endpoint: "https://s3.example.com",
    region: "us-east-1");

var backupPath = "/var/lib/sqlite/backup.sqlite";
var targetKey = $"backups/db-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ssZ}.sqlite";

var objectUrl = await backend.UploadBackupAsync(backupPath, targetKey);
Console.WriteLine($"Uploaded to: {objectUrl}");

var backups = await backend.ListBackupsAsync(prefix: "backups/");
foreach (var (path, size, modified) in backups)
{
    Console.WriteLine($"{path} ({size} bytes, {modified:u})");
}
```

### Example 2: Download and delete a backup

```csharp
using var backend = new S3StorageBackend(
    bucketName: "my-backups",
    accessKey: "AKIA...",
    secretKey: "SECRET...",
    endpoint: "https://s3.example.com",
    region: "us-east-1");

var sourceKey = "backups/db-2024-06-05T12-30-00Z.sqlite";
var localPath = "/tmp/restore/db.sqlite";

await backend.DownloadBackupAsync(sourceKey, localPath);
Console.WriteLine($"Downloaded to: {localPath}");

await backend.DeleteBackupAsync(sourceKey);
Console.WriteLine("Backup deleted from S3.");
```

## Notes

- **Thread safety**: The class is thread-safe for concurrent calls to any public method. Internally, it uses a single `AmazonS3Client` instance shared across all operations.
- **Cancellation**: All async methods respect the provided `CancellationToken` and will throw `OperationCanceledException` if the token is triggered.
- **S3-compatible endpoints**: The class works with any S3-compatible service (e.g., MinIO, Wasabi) by configuring the correct `endpoint` and `region`.
- **Large files**: Uploads and downloads stream data in chunks; memory usage remains bounded regardless of file size.
- **Object keys**: Ensure `targetPath` and `sourcePath` use consistent delimiters (e.g., `/`) to avoid cross-platform issues.
- **Quotas**: `GetAvailableSpaceAsync` may return `long.MaxValue` if the S3 provider does not expose quota information.
