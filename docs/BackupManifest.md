# BackupManifest

Represents metadata and status information for a SQLite database backup operation within the docker-sqlite-backup system. This class encapsulates details such as backup identifiers, source and destination paths, file sizes, compression/encryption states, and storage configuration to enable tracking, verification, and management of backup artifacts.

## API

### `Version`
- **Purpose**: Specifies the schema version of the backup manifest format.
- **Return Value**: `string` representing the version identifier.
- **Exceptions**: None; always accessible.

### `Id`
- **Purpose**: Unique identifier for the backup manifest instance.
- **Return Value**: `Guid` value uniquely identifying this backup.
- **Exceptions**: None; always accessible.

### `ScheduleId`
- **Purpose**: References the backup schedule configuration that triggered this backup.
- **Return Value**: `Guid` linking to the originating schedule.
- **Exceptions**: None; always accessible.

### `BackupJobId`
- **Purpose**: Identifies the specific backup job execution associated with this manifest.
- **Return Value**: `Guid` linking to the backup job instance.
- **Exceptions**: None; always accessible.

### `CreatedAt`
- **Purpose**: Timestamp indicating when the backup operation was initiated.
- **Return Value**: `DateTime` representing the start time.
- **Exceptions**: None; always accessible.

### `CompletedAt`
- **Purpose**: Timestamp indicating when the backup operation finished.
- **Return Value**: `DateTime` representing the completion time.
- **Exceptions**: None; always accessible.

### `SourceDatabasePath`
- **Purpose**: File system path to the original SQLite database being backed up.
- **Return Value**: `string` path to the source database.
- **Exceptions**: None; always accessible.

### `SourceDatabaseSizeBytes`
- **Purpose**: Size of the source database file in bytes at the time of backup.
- **Return Value**: `long` representing the source file size.
- **Exceptions**: None; always accessible.

### `BackupFilePath`
- **Purpose**: File system path where the backup file is stored.
- **Return Value**: `string` path to the backup file.
- **Exceptions**: None; always accessible.

### `BackupFileSizeBytes`
- **Purpose**: Size of the generated backup file in bytes.
- **Return Value**: `long` representing the backup file size.
- **Exceptions**: None; always accessible.

### `OriginalFileSizeBytes`
- **Purpose**: Original size of the database file before any compression or encryption was applied. Nullable if unavailable.
- **Return Value**: `long?` original size in bytes, or `null` if not applicable.
- **Exceptions**: None; always accessible.

### `CompressionRatio`
- **Purpose**: Ratio of original size to compressed size (e.g., 2.5 indicates 2.5x compression). Nullable if compression was not used or original size is unknown.
- **Return Value**: `double?` compression ratio, or `null` if not applicable.
- **Exceptions**: None; always accessible.

### `Checksum`
- **Purpose**: Cryptographic checksum (e.g., SHA-256) of the backup file for integrity verification.
- **Return Value**: `string` containing the checksum value.
- **Exceptions**: None; always accessible.

### `IsEncrypted`
- **Purpose**: Indicates whether the backup file is encrypted.
- **Return Value**: `bool` flag (`true` if encrypted).
- **Exceptions**: None; always accessible.

### `IsCompressed`
- **Purpose**: Indicates whether the backup file is compressed.
- **Return Value**: `bool` flag (`true` if compressed).
- **Exceptions**: None; always accessible.

### `BackupMode`
- **Purpose**: Specifies the backup mode (e.g., "full", "incremental", "differential").
- **Return Value**: `string` describing the backup type.
- **Exceptions**: None; always accessible.

### `BaseBackupResultId`
- **Purpose**: References the manifest ID of a base backup for incremental/differential backups. Nullable if not applicable.
- **Return Value**: `Guid?` linking to the base backup, or `null` if not used.
- **Exceptions**: None; always accessible.

### `StorageType`
- **Purpose**: Defines the storage backend used (e.g., "local", "s3", "azure_blob").
- **Return Value**: `string` indicating the storage type.
- **Exceptions**: None; always accessible.

### `RemoteStorageKey`
- **Purpose**: Identifier for the backup file in remote storage systems (e.g., S3 object key). Nullable if not stored remotely.
- **Return Value**: `string?` remote storage key, or `null` if not applicable.
- **Exceptions**: None; always accessible.

### `Notes`
- **Purpose**: Optional free-form text for additional metadata or operational comments.
- **Return Value**: `string` containing notes, or `null` if empty.
- **Exceptions**: None; always accessible.

## Usage

### Example 1: Creating a BackupManifest After a Backup Operation
```csharp
var manifest = new BackupManifest
{
    Version = "1.0",
    Id = Guid.NewGuid(),
    ScheduleId = schedule.Id,
    BackupJobId = job.Id,
    CreatedAt = DateTime.UtcNow,
    CompletedAt = DateTime.UtcNow.AddSeconds(30),
    SourceDatabasePath = "/data/db.sqlite",
    SourceDatabaseSizeBytes = 1024000,
    BackupFilePath = "/backups/db_20231001.sqlite.bak",
    BackupFileSizeBytes = 512000,
    OriginalFileSizeBytes = 1024000,
    CompressionRatio = 2.0,
    Checksum = "a1b2c3d4e5f6...",
    IsEncrypted = true,
    IsCompressed = true,
    BackupMode = "full",
    StorageType = "local",
    Notes = "Initial backup after schema migration"
};
```

### Example 2: Verifying Backup Integrity and Compression
```csharp
if (manifest.IsCompressed == true && manifest.CompressionRatio.HasValue)
{
    Console.WriteLine($"Compressed backup achieved {manifest.CompressionRatio.Value:0.0}x ratio.");
}

if (!string.IsNullOrEmpty(manifest.Checksum))
{
    bool isValid = ChecksumUtility.VerifyChecksum(manifest.BackupFilePath, manifest.Checksum);
    Console.WriteLine($"Backup integrity check: {(isValid ? "passed" : "failed")}");
}
```

## Notes

- `OriginalFileSizeBytes` and `CompressionRatio` are nullable and may be `null` if compression is disabled or the original size is unavailable. Always check for `null` before performing calculations.
- `RemoteStorageKey` is only populated when using remote storage backends (e.g., S3). Local backups will have this field as `null`.
- Thread safety is not guaranteed for instances of `BackupManifest`. If shared across threads, external synchronization is required for property modifications.
- The `Checksum` field assumes a standard hashing algorithm (e.g., SHA-256) but does not enforce format or length. Consumers should validate checksum compatibility with their verification logic.
- `BackupMode` values are not restricted to a predefined set. Implementations should define valid modes (e.g., "full", "incremental") and handle unknown values gracefully.
