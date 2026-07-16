// ... existing content ...

## AppSettings

The `AppSettings` class represents the application settings configuration model. It provides a set of properties that can be used to customize the behavior of the backup service.

Here's an example usage of the `AppSettings` class:

```csharp
using DockerSqliteBackup.Configuration;

// Create a new instance of AppSettings
var appSettings = new AppSettings
{
    EnableVerificationByDefault = true,
    EnableS3StorageByDefault = false,
    CompressBackups = false,
    NotificationEmails = new[] { "user1@example.com", "user2@example.com" },
    EnableEncryption = true,
    EncryptionKey = "Base64-encoded 32-byte AES-256 key"
};

// Use the settings to configure the backup service
var backupService = new BackupService(appSettings);
```

// ... existing content ...
