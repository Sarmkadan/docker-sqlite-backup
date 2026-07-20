#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="BackupManifest"/> domain model and its extension methods.
/// Tests serialization, deserialization, and manifest file operations.
/// </summary>
public class BackupManifestTests
{
    [Fact]
    public void BackupManifest_DefaultConstructor_SetsDefaultValues()
    {
        var manifest = new BackupManifest();

        manifest.Version.Should().Be("1.0");
        manifest.Id.Should().NotBe(Guid.Empty);
        manifest.ScheduleId.Should().Be(Guid.Empty);
        manifest.BackupJobId.Should().Be(Guid.Empty);
        manifest.CreatedAt.Should().Be(default);
        manifest.CompletedAt.Should().Be(default);
        manifest.SourceDatabasePath.Should().BeEmpty();
        manifest.SourceDatabaseSizeBytes.Should().Be(0);
        manifest.BackupFilePath.Should().BeEmpty();
        manifest.BackupFileSizeBytes.Should().Be(0);
        manifest.OriginalFileSizeBytes.Should().BeNull();
        manifest.CompressionRatio.Should().BeNull();
        manifest.Checksum.Should().BeEmpty();
        manifest.IsEncrypted.Should().BeFalse();
        manifest.IsCompressed.Should().BeFalse();
        manifest.BackupMode.Should().BeEmpty();
        manifest.BaseBackupResultId.Should().BeNull();
        manifest.StorageType.Should().Be("Local");
        manifest.RemoteStorageKey.Should().BeNull();
        manifest.Notes.Should().BeEmpty();
    }

    [Fact]
    public void ToJson_SerializesAllProperties()
    {
        var manifest = new BackupManifest
        {
            ScheduleId = Guid.NewGuid(),
            BackupJobId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            CompletedAt = DateTime.UtcNow,
            SourceDatabasePath = "/var/lib/sqlite/app.db",
            SourceDatabaseSizeBytes = 1024 * 1024,
            BackupFilePath = "/backups/app/backup_2024-01-01_12-00-00.sqlite",
            BackupFileSizeBytes = 512 * 1024,
            OriginalFileSizeBytes = 1024 * 1024,
            CompressionRatio = 2.0,
            Checksum = "a1b2c3d4e5f67890abcdef1234567890abcdef1234567890abcdef1234567890",
            IsEncrypted = true,
            IsCompressed = true,
            BackupMode = "Full",
            BaseBackupResultId = Guid.NewGuid(),
            StorageType = "S3",
            RemoteStorageKey = "backups/app/backup_2024-01-01_12-00-00.sqlite.gz",
            Notes = "Test backup"
        };

        var json = manifest.ToJson();

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"version\":\"1.0\"");
        json.Should().Contain("\"scheduleId\"");
        json.Should().Contain("\"backupJobId\"");
        json.Should().Contain("\"createdAt\"");
        json.Should().Contain("\"completedAt\"");
        json.Should().Contain("\"sourceDatabasePath\"");
        json.Should().Contain("\"sourceDatabaseSizeBytes\"");
        json.Should().Contain("\"backupFilePath\"");
        json.Should().Contain("\"backupFileSizeBytes\"");
        json.Should().Contain("\"originalFileSizeBytes\"");
        json.Should().Contain("\"compressionRatio\"");
        json.Should().Contain("\"checksum\"");
        json.Should().Contain("\"isEncrypted\":true");
        json.Should().Contain("\"isCompressed\":true");
        json.Should().Contain("\"backupMode\":\"Full\"");
        json.Should().Contain("\"baseBackupResultId\"");
        json.Should().Contain("\"storageType\":\"S3\"");
        json.Should().Contain("\"remoteStorageKey\"");
        json.Should().Contain("\"notes\"");
    }

    [Fact]
    public void FromJson_DeserializesAllProperties()
    {
        string json = "{\r\n" +
            "  \"version\": \"1.0\",\r\n" +
            "  \"id\": \"550e8400-e29b-41d4-a716-446655440000\",\r\n" +
            "  \"scheduleId\": \"6ba7b810-9dad-11d1-80b4-00c04fd430c8\",\r\n" +
            "  \"backupJobId\": \"6ba7b811-9dad-11d1-80b4-00c04fd430c8\",\r\n" +
            "  \"createdAt\": \"2024-01-01T12:00:00Z\",\r\n" +
            "  \"completedAt\": \"2024-01-01T12:05:00Z\",\r\n" +
            "  \"sourceDatabasePath\": \"/var/lib/sqlite/app.db\",\r\n" +
            "  \"sourceDatabaseSizeBytes\": 1048576,\r\n" +
            "  \"backupFilePath\": \"/backups/app/backup_2024-01-01_12-00-00.sqlite\",\r\n" +
            "  \"backupFileSizeBytes\": 524288,\r\n" +
            "  \"originalFileSizeBytes\": 1048576,\r\n" +
            "  \"compressionRatio\": 2.0,\r\n" +
            "  \"checksum\": \"a1b2c3d4e5f67890abcdef1234567890abcdef1234567890abcdef1234567890\",\r\n" +
            "  \"isEncrypted\": true,\r\n" +
            "  \"isCompressed\": true,\r\n" +
            "  \"backupMode\": \"Full\",\r\n" +
            "  \"baseBackupResultId\": \"6ba7b812-9dad-11d1-80b4-00c04fd430c8\",\r\n" +
            "  \"storageType\": \"S3\",\r\n" +
            "  \"remoteStorageKey\": \"backups/app/backup_2024-01-01_12-00-00.sqlite.gz\",\r\n" +
            "  \"notes\": \"Test backup\"\r\n" +
            "}";

        var manifest = BackupManifestExtensions.FromJson(json);

        manifest.Should().NotBeNull();
        manifest!.Version.Should().Be("1.0");
        manifest.Id.Should().Be(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
        manifest.ScheduleId.Should().Be(Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8"));
        manifest.BackupJobId.Should().Be(Guid.Parse("6ba7b811-9dad-11d1-80b4-00c04fd430c8"));
        manifest.CreatedAt.Should().Be(DateTime.Parse("2024-01-01T12:00:00Z"));
        manifest.CompletedAt.Should().Be(DateTime.Parse("2024-01-01T12:05:00Z"));
        manifest.SourceDatabasePath.Should().Be("/var/lib/sqlite/app.db");
        manifest.SourceDatabaseSizeBytes.Should().Be(1048576);
        manifest.BackupFilePath.Should().Be("/backups/app/backup_2024-01-01_12-00-00.sqlite");
        manifest.BackupFileSizeBytes.Should().Be(524288);
        manifest.OriginalFileSizeBytes.Should().Be(1048576);
        manifest.CompressionRatio.Should().Be(2.0);
        manifest.Checksum.Should().Be("a1b2c3d4e5f67890abcdef1234567890abcdef1234567890abcdef1234567890");
        manifest.IsEncrypted.Should().BeTrue();
        manifest.IsCompressed.Should().BeTrue();
        manifest.BackupMode.Should().Be("Full");
        manifest.BaseBackupResultId.Should().Be(Guid.Parse("6ba7b812-9dad-11d1-80b4-00c04fd430c8"));
        manifest.StorageType.Should().Be("S3");
        manifest.RemoteStorageKey.Should().Be("backups/app/backup_2024-01-01_12-00-00.sqlite.gz");
        manifest.Notes.Should().Be("Test backup");
    }

    [Fact]
    public void WriteToFile_CreatesManifestFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var manifest = new BackupManifest
            {
                ScheduleId = Guid.NewGuid(),
                BackupJobId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                CompletedAt = DateTime.UtcNow,
                SourceDatabasePath = "/data/db.sqlite",
                SourceDatabaseSizeBytes = 2048,
                BackupFilePath = Path.Combine(tempDir, "backup.sqlite"),
                BackupFileSizeBytes = 1024,
                Checksum = "abc123def4567890abcdef1234567890abcdef1234567890abcdef123456789",
                IsEncrypted = false,
                IsCompressed = false,
                BackupMode = "Full",
                StorageType = "Local",
                Notes = "Test manifest"
            };

            var manifestPath = Path.Combine(tempDir, "backup.sqlite.meta.json");
            manifest.WriteToFile(manifestPath);

            File.Exists(manifestPath).Should().BeTrue();
            var json = File.ReadAllText(manifestPath);
            json.Should().NotBeNullOrEmpty();

            var deserialized = BackupManifestExtensions.FromJson(json);
            deserialized.Should().NotBeNull();
            deserialized!.ScheduleId.Should().Be(manifest.ScheduleId);
            deserialized.BackupFilePath.Should().Be(manifest.BackupFilePath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ReadFromFile_NonExistentFile_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var manifestPath = Path.Combine(tempDir, "nonexistent.meta.json");
            var manifest = BackupManifestExtensions.ReadFromFile(manifestPath);

            manifest.Should().BeNull();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void ToJson_HandlesNullValues()
    {
        var manifest = new BackupManifest
        {
            ScheduleId = Guid.NewGuid(),
            SourceDatabasePath = "/data/db.sqlite",
            BackupFilePath = "/backups/backup.sqlite",
            Checksum = "abc123",
            BaseBackupResultId = null,
            OriginalFileSizeBytes = null,
            CompressionRatio = null,
            RemoteStorageKey = null
        };

        var json = manifest.ToJson();

        json.Should().NotContain("\"baseBackupResultId\":null");
        json.Should().NotContain("\"originalFileSizeBytes\":null");
        json.Should().NotContain("\"compressionRatio\":null");
        json.Should().NotContain("\"remoteStorageKey\":null");
    }

    [Fact]
    public void ManifestFileNaming_MatchesBackupFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var backupPath = Path.Combine(tempDir, "backup_2024-01-01_12-00-00.sqlite.gz");
            var manifestPath = Path.Combine(tempDir, "backup_2024-01-01_12-00-00.sqlite.gz.meta.json");

            File.WriteAllText(backupPath, "dummy backup content");

            var manifest = new BackupManifest
            {
                ScheduleId = Guid.NewGuid(),
                BackupFilePath = backupPath,
                Checksum = "abc123"
            };

            manifest.WriteToFile(manifestPath);

            File.Exists(manifestPath).Should().BeTrue();
            File.Exists(backupPath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
