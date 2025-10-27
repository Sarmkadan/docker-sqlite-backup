# AppSettingsExtensions

Provides static accessors for backup‑related configuration values read from the application’s `appsettings.json` (or equivalent configuration source). The members expose boolean flags and helper methods that the backup workflow consumes to decide whether to verify, compress, encrypt, store to S3, or notify operators.

## API

### IsVerificationEnabled
- **Purpose**: Indicates whether backup verification should be performed after a snapshot is created.
- **Parameters**: None.
- **Return value**: `true` if verification is enabled; otherwise `false`.
- **Exceptions**: 
  - `InvalidOperationException` if the underlying configuration has not been initialized.
  - `FormatException` if the configuration value cannot be parsed as a Boolean.

### IsS3StorageEnabled
- **Purpose**: Indicates whether Amazon S3 storage is configured for storing backups.
- **Parameters**: None.
- **Return value**: `true` when valid S3 endpoint, bucket, and credentials are present; otherwise `false`.
- **Exceptions**: 
  - `InvalidOperationException` if configuration is missing.
  - `FormatException` if any required S3 setting is malformed.

### GetNotificationEmailsAsString
- **Purpose**: Retrieves a semicolon‑separated list of e‑mail addresses to notify on backup completion or failure.
- **Parameters**: None.
- **Return value**: A string containing the e‑mail list, or `null` when no notification e‑mails are configured.
- **Exceptions**: 
  - `InvalidOperationException` if configuration is not initialized.
  - `FormatException` if the e‑mail list contains invalid syntax.

### IsEncryptionConfigured
- **Purpose**: Indicates whether encryption settings (key, algorithm) are present for encrypting backup files.
- **Parameters**: None.
- **Return value**: `true` when encryption is properly configured; otherwise `false`.
- **Exceptions**: 
  - `InvalidOperationException` if configuration has not been set.
  - `FormatException` if the encryption key or algorithm values are invalid.

### ShouldCompressBackups
- **Purpose**: Determines whether backup files should be compressed before storage or transfer.
- **Parameters**: None.
- **Return value**: `true` when compression is enabled; otherwise `false`.
- **Exceptions**: 
  - `InvalidOperationException` if configuration is unavailable.
  - `FormatException` if the compression flag cannot be parsed.

### Validate
- **Purpose**: Performs a sanity check on all backup‑related configuration values.
- **Parameters**: None.
- **Return value**: `true` if every required setting is present and valid; `false` if any setting is missing or invalid.
- **Exceptions**: 
  - `InvalidOperationException` if the configuration source has not been supplied.
  - `ConfigurationException` (or derived) when a critical setting is malformed and cannot be recovered.

## Usage

```csharp
using DockerSqliteBackup.Configuration;

// Assume AppSettingsExtensions has been populated earlier (e.g., during startup).

if (!AppSettingsExtensions.Validate())
{
    throw new InvalidOperationException("Backup configuration is incomplete or invalid.");
}

if (AppSettingsExtensions.IsVerificationEnabled)
{
    // Run verification routine after snapshot.
    VerifyBackup(snapshotPath);
}

if (AppSettingsExtensions.ShouldCompressBackups)
{
    // Compress the snapshot before further processing.
    snapshotPath = CompressFile(snapshotPath);
}

if (AppSettingsExtensions.IsS3StorageEnabled)
{
    // Upload to S3.
    await UploadToS3Async(snapshotPath);
}
else
{
    // Store locally.
    File.Copy(snapshotPath, localBackupDestination, true);
}

var emailList = AppSettingsExtensions.GetNotificationEmailsAsString();
if (!string.IsNullOrEmpty(emailList))
{
    NotifyOperators(emailList, backupResult);
}
```

```csharp
using DockerSqliteBackup.Configuration;

// Quick health‑check at application start.
bool isEncryptionReady = AppSettingsExtensions.IsEncryptionConfigured;
bool isS3Ready       = AppSettingsExtensions.IsS3StorageEnabled;

Console.WriteLine($"Encryption configured: {isEncryptionReady}");
Console.WriteLine($"S3 storage configured: {isS3Ready}");

// Decide whether to enable optional features based on configuration.
bool verify = AppSettingsExtensions.IsVerificationEnabled;
bool compress = AppSettingsExtensions.ShouldCompressBackups;

if (verify || compress)
{
    Console.WriteLine("Optional backup features are active.");
}
else
{
    Console.WriteLine("Running with basic backup only.");
}
```

## Notes

- All members are **read‑only** after the configuration has been loaded; they do not modify any internal state.
- The class is **thread‑safe** for concurrent reads once the underlying configuration has been initialized. However, calling any member before configuration initialization results in an `InvalidOperationException`.
- `GetNotificationEmailsAsString` returns `null` when no notification e‑mails are present; callers should treat `null` as an empty list` equivalently to avoid sending notifications.
- The `Validate` method aggregates the validation logic of the individual getters; it may return `false` without throwing if the misconfiguration is recoverable (e.g., missing optional e‑mail list). Critical errors that prevent the backup from proceeding will cause an exception.
- Consumers should not rely on the order of evaluation between these members; each accesses the configuration independently but reflects the same snapshot of settings loaded at startup.
