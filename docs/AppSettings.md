# AppSettings

Configuration container for Docker-SQLite-Backup default behaviors and global settings. These values control default behavior for backup jobs unless overridden by job-specific configuration.

## API

### `EnableVerificationByDefault`
- **Purpose**: Determines whether backup verification (checksum validation) is enabled by default for newly created backup jobs.
- **Type**: `bool`
- **Default**: `false`
- **Usage**: When `true`, every backup will perform a verification step after completion unless explicitly disabled per job.

### `EnableS3StorageByDefault`
- **Purpose**: Controls whether newly created backup jobs default to storing backups in S3-compatible storage instead of local filesystem.
- **Type**: `bool`
- **Default**: `false`
- **Usage**: When `true`, backup jobs will use S3 storage unless overridden by job-specific configuration.

### `CompressBackups`
- **Purpose**: Specifies whether backup archives should be compressed (e.g., gzip) by default.
- **Type**: `bool`
- **Default**: `true`
- **Usage**: When `false`, backups will be stored as uncompressed SQLite files unless overridden per job.

### `NotificationEmails`
- **Purpose**: List of email addresses to notify when backup operations complete or fail.
- **Type**: `string[]`
- **Default**: Empty array (`[]`)
- **Usage**: Emails are sent only if notifications are enabled at job level; this array provides the default recipient list.

### `EnableEncryption`
- **Purpose**: Enables transparent encryption of backup files by default.
- **Type**: `bool`
- **Default**: `false`
- **Usage**: When `true`, backup files are encrypted using the provided key unless explicitly disabled per job.

### `EncryptionKey`
- **Purpose**: Cryptographic key used to encrypt backup files when encryption is enabled.
- **Type**: `string?`
- **Default**: `null`
- **Usage**: Must be a valid encryption key when `EnableEncryption` is `true`; otherwise ignored.
- **Validation**: Throws `InvalidOperationException` if `EnableEncryption` is `true` and `EncryptionKey` is `null` or empty.

## Usage
