// ... existing content ...

## CacheKeyBuilder

The `CacheKeyBuilder` class provides a fluent API for constructing consistent and collision-free cache keys. It ensures all keys follow a standardized naming convention with the `backup:` prefix and colon-separated parts. The builder supports adding string, Guid, integer, and long values, and includes a collection of predefined keys for common resources.


Here's an example usage of the `CacheKeyBuilder` class:

```csharp
using DockerSqliteBackup.Caching;

// Create a custom cache key
var customKey = new CacheKeyBuilder()
    .Add("schedule")
    .Add(Guid.NewGuid())
    .Add(DateTime.UtcNow.Year)
    .Build();

// Use predefined keys
var scheduleKey = CacheKeyBuilder.Keys.Schedule(Guid.NewGuid());
var allSchedulesKey = CacheKeyBuilder.Keys.AllSchedules();
var backupResultKey = CacheKeyBuilder.Keys.BackupResult(Guid.NewGuid());
var healthStatusKey = CacheKeyBuilder.Keys.HealthStatus();
```

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
