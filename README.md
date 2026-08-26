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
