## BackupManifestTests

The BackupManifestTests class contains tests for the BackupManifest class.

### Usage Example

```csharp
BackupManifestTests backupManifestTests = new BackupManifestTests();
backupManifestTests.BackupManifest_DefaultConstructor_SetsDefaultValues();
backupManifestTests.ToJson_SerializesAllProperties();
backupManifestTests.FromJson_DeserializesAllProperties();
backupManifestTests.WriteToFile_CreatesManifestFile();
backupManifestTests.ReadFromFile_NonExistentFile_ReturnsNull();
backupManifestTests.ToJson_HandlesNullValues();
backupManifestTests.ManifestFileNaming_MatchesBackupFile();
```

## DockerSqliteBackupExceptionTests

The DockerSqliteBackupExceptionTests class contains tests for the DockerSqliteBackupException class. It verifies that the various constructors set messages and inner exceptions correctly, including graceful handling of null or empty arguments. It also confirms that the exception inherits from Exception, exposes readable Message and InnerException properties, produces informative ToString output, and can be caught as its base exception type.

### Usage Example

```csharp
DockerSqliteBackupExceptionTests dockerSqliteBackupExceptionTests = new DockerSqliteBackupExceptionTests();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_DefaultConstructor_ShouldCreateException();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_Constructor_WithMessage_ShouldSetMessage();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_Constructor_WithEmptyOrNullMessage_ShouldHandleGracefully();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_Constructor_WithMessageAndInnerException_ShouldSetMessageAndInnerException();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_Constructor_WithNullOrEmptyMessageAndInnerException_ShouldHandleGracefully();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_Constructor_WithNullInnerException_ShouldSetMessageAndNullInnerException();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_InheritsFromException();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_MessageProperty_ShouldBeReadable();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_InnerExceptionProperty_ShouldBeReadable();
dockerSqliteBackupExceptionTests.DockerSqliteBackupException_ToString_ShouldIncludeAllInformation();

## BackupManifest

The BackupManifest class holds the metadata that describes a completed database backup. It records the backup's identity and timing, the size and location of both the source database and the produced backup file, integrity information such as the checksum, and processing flags for encryption and compression. A manifest is typically serialized to JSON and stored beside the backup so it can later be read back for verification, restore planning, and retention decisions.

### Usage Example

```csharp
using System;
using System.Text.Json;

var manifest = new BackupManifest
{
    Version = "1.0",
    Id = Guid.NewGuid(),
    ScheduleId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
    BackupJobId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"),
    CreatedAt = DateTime.UtcNow,
    CompletedAt = DateTime.UtcNow.AddMinutes(3),
    SourceDatabasePath = "/data/appdb.sqlite",
    SourceDatabaseSizeBytes = 52_428_800,
    BackupFilePath = "/backups/appdb-2026-08-26-full.bak",
    BackupFileSizeBytes = 18_874_368,
    OriginalFileSizeBytes = 52_428_800,
    CompressionRatio = 2.78,
    Checksum = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    IsEncrypted = true,
    IsCompressed = true,
    BackupMode = "Full",
    BaseBackupResultId = null,
    StorageType = "Local",
    RemoteStorageKey = null,
    Notes = "Nightly full backup; verified via checksum."
};

string json = JsonSerializer.Serialize(manifest);

BackupManifest restored = JsonSerializer.Deserialize<BackupManifest>(json)!;

Console.WriteLine($"Backup {restored.Id} achieved a compression ratio of {restored.CompressionRatio}");
```

## LocalStorageBackend

The `LocalStorageBackend` class provides a file-system-based implementation for storing and managing database backup files. It handles core storage operations such as uploading, downloading, and deleting backup files, while also offering utilities to list available backups, verify connectivity, and check disk space.

### Usage Example

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;

var backend = new LocalStorageBackend();

// Verify the storage path is accessible and check available disk space
bool isConnected = await backend.TestConnectionAsync();
long availableSpace = await backend.GetAvailableSpaceAsync();

// Upload a new backup file to the local storage
string uploadedPath = await backend.UploadBackupAsync("appdb-2024-05-20.bak");

// List all backups in the directory with their metadata
var backups = await backend.ListBackupsAsync();
foreach (var (path, size, modified) in backups)
{
    Console.WriteLine($"{path} - {size} bytes - {modified:yyyy-MM-dd}");
}

// Download a backup for local restoration
string localCopy = await backend.DownloadBackupAsync("appdb-2024-05-20.bak");

// Remove an outdated backup
await backend.DeleteBackupAsync("appdb-2024-05-19.bak");
```

## WebhookClient

The `WebhookClient` class sends HTTP POST notifications about backup events with HMAC-SHA256 payload signing and exponential-backoff retry logic. It supports secure webhook verification through shared secrets and handles both backup completion and schedule-related events.

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Logging;

// Create logger and settings (typically from dependency injection)
ILogger<WebhookClient> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<WebhookClient>();
AppSettings settings = new AppSettings { WebhookSecret = "my-shared-secret" };

// Initialize webhook client
var webhookClient = new WebhookClient(logger, settings);

// Send backup completion notification
var backupResult = new BackupResult
{
    Id = Guid.NewGuid(),
    ScheduleId = Guid.NewGuid(),
    Status = 1, // Success status code
    BackupFilePath = "/backups/db-2026-08-28.bak",
    BackupFileSizeBytes = 1024000,
    Checksum = "abc123",
    StartedAt = DateTime.UtcNow.AddMinutes(-5),
    CompletedAt = DateTime.UtcNow
};

await webhookClient.SendBackupNotificationAsync(
    "https://example.com/webhooks/backup",
    backupResult
);

// Send schedule notification (e.g., for schedule start)
var schedule = new BackupSchedule
{
    Id = Guid.NewGuid(),
    Name = "daily-backup",
    DatabasePath = "/data/app.db",
    CronExpression = "0 2 * * *",
    IsActive = true
};

await webhookClient.SendScheduleNotificationAsync(
    "https://example.com/webhooks/backup",
    schedule,
    "schedule.started"
);
```

## S3StorageBackend

The `S3StorageBackend` class provides an AWS S3-based implementation for storing and managing database backup files. It handles core storage operations such as uploading, downloading, and deleting backup files, while also offering utilities to list available backups, verify connectivity, and check available space (which is considered unlimited for S3).

### Usage Example

```csharp
using System;
using System.Threading.Tasks;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Domain; // For S3Configuration
using Microsoft.Extensions.Logging;

// For demonstration, we create a simple logger and event publisher.
// In a real application, these would be provided by dependency injection.
ILogger<S3StorageBackend> logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<S3StorageBackend>();
IBackupEventPublisher eventPublisher = new DemoEventPublisher();

var backend = new S3StorageBackend(logger, eventPublisher);

// Configure S3 settings (typically from configuration or environment variables)
var s3Config = new S3Configuration
{
    BucketName = "my-backup-bucket",
    AccessKeyId = "my-access-key",
    SecretAccessKey = "my-secret-key",
    RegionName = "us-west-2"
};

// Verify the storage is accessible
bool isConnected = await backend.TestConnectionAsync(s3Config);
if (!isConnected)
{
    Console.WriteLine("Unable to connect to S3.");
    return;
}

// Upload a new backup file to S3
string uploadedKey = await backend.UploadBackupAsync("/path/to/local/backup.bak", s3Config);
Console.WriteLine($"Uploaded backup to S3: {uploadedKey}");

// List all backups in the bucket with their metadata
var backups = await backend.ListBackupsAsync(s3Config);
foreach (var (key, size, modified) in backups)
{
    Console.WriteLine($"{key} - {size} bytes - {modified:u}");
}

// Download a backup for local restoration
string localCopy = await backend.DownloadBackupAsync(uploadedKey, s3Config);
Console.WriteLine($"Downloaded backup to: {localCopy}");

// Remove an outdated backup
await backend.DeleteBackupAsync(uploadedKey, s3Config);
Console.WriteLine($"Deleted backup from S3: {uploadedKey}");

// Helper class for demonstration purposes
class DemoEventPublisher : IBackupEventPublisher
{
    public Task PublishAsync(BackupEvent @event)
    {
        // In a real app, this would publish the event to a message broker or similar
        return Task.CompletedTask;
    }
}
```