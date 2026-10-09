# S3 Upload Integration

This document describes how backups are uploaded to Amazon S3 (or an S3-compatible service) by docker-sqlite-backup: the components involved, configuration, the upload flow, retry behavior, and known gaps between the configuration surface and the current implementation.

## Components

| Component | File | Role |
|---|---|---|
| `S3Configuration` | `src/Domain/S3Configuration.cs` | Settings and validation for the S3 backend. |
| `S3ConfigurationExtensions` | `src/Domain/S3ConfigurationExtensions.cs` | `GetS3ObjectKey` builds object keys from prefix and file name. |
| `S3StorageBackend` | `src/Services/S3StorageBackend.cs` | `IStorageBackend` implementation that talks to S3 via `AWSSDK.S3`. |
| `StorageService` | `src/Services/StorageService.cs` | Selects the backend for a given `StorageConfiguration` type. |
| `ServiceCollectionExtensions` | `src/Configuration/ServiceCollectionExtensions.cs` | Registers `S3StorageBackend` as a singleton in DI. |
| `BackupConstants` | `src/Constants/BackupConstants.cs` | S3 storage class names and `MaxS3BackupSizeBytes` (100 GB). |

`S3StorageBackend` is registered with `AddSingleton`. It depends on `ILogger<S3StorageBackend>` and `IBackupEventPublisher`.

## Configuration

`S3Configuration` inherits from `StorageConfiguration` and reports `StorageType.S3`.

| Property | Default | Description |
|---|---|---|
| `AccessKeyId` | `""` | AWS access key ID. Must be at least 16 characters to pass `IsValid()`. |
| `SecretAccessKey` | `""` | AWS secret access key. Must be at least 16 characters to pass `IsValid()`. |
| `BucketName` | `""` | Target bucket. Required. |
| `RegionName` | `us-east-1` | Region passed to `RegionEndpoint.GetBySystemName`. |
| `ObjectKeyPrefix` | `backups/` | Prefix for object keys. |
| `UseSSL` | `true` | Declared, but not read by the upload path (see Known gaps). |
| `EnableServerSideEncryption` | `true` | When `true`, sends `ServerSideEncryptionMethod = AES256` (SSE-S3). |
| `StorageClass` | `STANDARD` | Must be one of `STANDARD`, `REDUCED_REDUNDANCY`, `STANDARD_IA`, `ONEZONE_IA`, `INTELLIGENT_TIERING`, `GLACIER`, `DEEP_ARCHIVE` (case-insensitive). |
| `CustomEndpoint` | `null` | Sets `ServiceURL` for S3-compatible services such as MinIO or Wasabi. |
| `TransitionToGlacierDays` | `null` | Mapped in `StorageConfigurationExtensions`. Does not create a bucket lifecycle rule (see Known gaps). |
| `EnableStreamingUploads` | `true` | Declared, not read by the upload path (see Known gaps). |
| `MultipartPartSizeBytes` | 16 MB | Declared, not read by the upload path (see Known gaps). |

Application-level settings in `AppSettings`:

- `EnableStreamingS3Uploads` (default `true`)
- `S3MultipartPartSizeBytes` (default 16 MB, documented as max 5 GB per part)
- `EnableS3StorageByDefault` (default `false`)

Settings can be provided through `appsettings.json` or environment variables with the `BACKUP_` prefix, using `__` as the nesting separator (for example, `BACKUP_AppSettings__DatabasePath`).

### Validation

`S3Configuration.IsValid()` returns `false` when any of these hold:

- `Name` is empty
- `AccessKeyId` or `SecretAccessKey` is empty or shorter than 16 characters
- `BucketName` or `RegionName` is empty
- `StorageClass` is not a supported value

`IsValid()` does not check `TransitionToGlacierDays` or `CustomEndpoint`.

## Upload flow

`StorageService` resolves `S3Configuration` to `S3StorageBackend`. The call `UploadBackupAsync(filePath, config)` then runs:

1. Argument checks. `null` or whitespace paths and a null config throw. A config that is not `S3Configuration` throws `ArgumentException`.
2. If the local file does not exist, a `LocalStorageException` is thrown. This check runs before the config type check.
3. The file is uploaded through `UploadToS3Async`, which is wrapped in `ExecuteWithRetryAsync`.
4. Inside the retry operation:
   - A new `AmazonS3Client` is created via `CreateS3Client` (region, optional `ServiceURL`, access key and secret). The client is disposed after the operation.
   - The object key is `ObjectKeyPrefix + Path.GetFileName(filePath)`, with `\` replaced by `/`.
   - A single `PutObjectRequest` is sent with `FilePath`, the SSE header when enabled, and `StorageClass`.
5. The method returns the object key.

Any exception that is not a `StorageException` is wrapped in `S3StorageException("Failed to upload backup to S3", ex)` and logged.

Note: `GetS3ObjectKey` in `S3ConfigurationExtensions` is not used by the upload path. The upload path builds the key with `Path.Combine` and then normalizes separators. The two methods produce the same result for normal prefixes such as `backups/`, but they are separate code paths.

## Retry behavior

`ExecuteWithRetryAsync` retries up to 3 attempts with exponential backoff starting at 1 second (1 s, then 2 s).

An `AmazonS3Exception` is treated as transient when any of these hold:

- HTTP 500, 503 or 504
- Error code `SlowDown`, `Throttling` or `RequestTimeout`

On each transient failure before the last attempt, the method logs a warning and publishes a `BackupRetryEvent` with `AttemptNumber` and `PreviousError`.

On a non-transient `AmazonS3Exception`, or after the last attempt, it throws `S3StorageException(ex.Message, ex, isTransient)`. Non-S3 exceptions are not retried and are wrapped by the outer `UploadBackupAsync` handler.

## Other operations

| Method | Behavior |
|---|---|
| `DownloadBackupAsync` | Downloads `storagePath` to `Path.GetTempPath()` under the same file name. Not wrapped in retry. |
| `DeleteBackupAsync` | Deletes the object by key. Not wrapped in retry. |
| `ListBackupsAsync` | Lists objects under `ObjectKeyPrefix` with `ListObjectsV2`. Returns path, size and last-modified UTC. Not wrapped in retry. |
| `TestConnectionAsync` | Calls `ListBuckets` and returns `true` if `BucketName` is present. Returns `false` on any exception. |
| `GetAvailableSpaceAsync` | Returns `long.MaxValue`. S3 has no free-space limit in this model. |

`ListBackupsAsync` does not paginate. Only the first page of results from `ListObjectsV2` is returned, which is up to 1000 keys.

## Example configuration

```json
{
  "AppSettings": {
    "EnableS3StorageByDefault": true
  }
}
```

```bash
BACKUP_AppSettings__EnableS3StorageByDefault=true
```

Storage credentials are supplied through `S3Configuration` (see the Configuration section for property names). Do not commit real keys to source control. Prefer environment variables or a secrets mechanism.

## Known gaps

These items are verified against the current source. They are listed so that operators do not assume behavior the code does not implement.

1. **Streaming and multipart settings are unused.** `EnableStreamingUploads`, `MultipartPartSizeBytes`, `AppSettings.EnableStreamingS3Uploads` and `AppSettings.S3MultipartPartSizeBytes` are never read by `S3StorageBackend`. Every upload is a single `PutObject`. S3 rejects single PUT requests larger than 5 GB, and `MaxS3BackupSizeBytes` (100 GB) is not enforced anywhere, so backups above 5 GB will fail.
2. **`TransitionToGlacierDays` has no effect on the bucket.** It is copied between configuration types but no lifecycle rule is created. Objects keep the class set in `StorageClass`.
3. **`UseSSL` is ignored.** The SDK endpoint is chosen from `CustomEndpoint` and region. `ServiceURL` is set as provided, so an `http://` custom endpoint is used as written.
4. **Secrets appear in `ToString()`.** `S3Configuration.ToString()` includes `SecretAccessKey`. Avoid logging the configuration object.
5. **`DownloadBackupAsync` uses a fixed temp path.** Files with the same name overwrite each other.
6. **`TestConnectionAsync` swallows errors.** A failed call returns `false` with no logged reason.

## Tests

Existing tests are in `tests/docker-sqlite-backup.Tests`:

- `S3ConfigurationTests.cs`
- `S3StorageBackendTests.cs`

Run the S3 tests with:

```bash
dotnet test -c Release --filter "FullyQualifiedName~S3"
```
