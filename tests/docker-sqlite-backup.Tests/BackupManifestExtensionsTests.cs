using Xunit;
using System.Text.Json;
using System.IO;
using System;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Tests
{
    public class BackupManifestExtensionsTests : IDisposable
    {
        private readonly string _testDirectory;

        public BackupManifestExtensionsTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            try { Directory.Delete(_testDirectory, true); } catch { }
        }

        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var manifest = new BackupManifest();

            // Act
            var json = manifest.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Contains("version", json);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            BackupManifest manifest = null!;
            Assert.Throws<ArgumentNullException>(() => manifest.ToJson());
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsBackupManifest()
        {
            // Arrange
            var original = new BackupManifest();
            var json = original.ToJson();

            // Act
            var deserialized = BackupManifestExtensions.FromJson(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized!.Id);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => BackupManifestExtensions.FromJson(null!));
        }

        [Fact]
        public void WriteToFile_HappyPath_WritesToFile()
        {
            // Arrange
            var manifest = new BackupManifest();
            var filePath = Path.Combine(_testDirectory, "test.json");

            // Act
            manifest.WriteToFile(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void WriteToFile_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            BackupManifest manifest = null!;
            Assert.Throws<ArgumentNullException>(() => manifest.WriteToFile(Path.Combine(_testDirectory, "test.json")));
        }

        [Fact]
        public void ReadFromFile_HappyPath_ReturnsBackupManifest()
        {
            // Arrange
            var manifest = new BackupManifest();
            var filePath = Path.Combine(_testDirectory, "test.json");

            // Act
            manifest.WriteToFile(filePath);
            var actualManifest = BackupManifestExtensions.ReadFromFile(filePath);

            // Assert
            Assert.NotNull(actualManifest);
            Assert.Equal(manifest.Id, actualManifest!.Id);
        }

        [Fact]
        public void ReadFromFile_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => BackupManifestExtensions.ReadFromFile(null!));
        }
    }
}
