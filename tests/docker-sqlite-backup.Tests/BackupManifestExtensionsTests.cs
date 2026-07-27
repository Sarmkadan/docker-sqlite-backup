using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Tests
{
    public class BackupManifestExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var manifest = new BackupManifest();
            var expectedJson = "{\"key\":\"value\"}";

            // Act
            var actualJson = manifest.ToJson();

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new BackupManifest().ToJson());
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsBackupManifest()
        {
            // Arrange
            var json = "{\"key\":\"value\"}";
            var expectedManifest = new BackupManifest();

            // Act
            var actualManifest = BackupManifestExtensions.FromJson(json);

            // Assert
            Assert.Equal(expectedManifest, actualManifest);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => BackupManifestExtensions.FromJson(null));
        }

        [Fact]
        public void WriteToFile_HappyPath_WritesToFile()
        {
            // Arrange
            var manifest = new BackupManifest();
            var filePath = "test.json";

            // Act
            manifest.WriteToFile(filePath);

            // Assert
            Assert.True(File.Exists(filePath));
        }

        [Fact]
        public void WriteToFile_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new BackupManifest().WriteToFile("test.json"));
        }

        [Fact]
        public void ReadFromFile_HappyPath_ReturnsBackupManifest()
        {
            // Arrange
            var manifest = new BackupManifest();
            var filePath = "test.json";

            // Act
            manifest.WriteToFile(filePath);
            var actualManifest = BackupManifestExtensions.ReadFromFile(filePath);

            // Assert
            Assert.Equal(manifest, actualManifest);
        }

        [Fact]
        public void ReadFromFile_NullInput_ThrowsArgumentException()
        {
            // Act and Assert
            Assert.Throws<ArgumentException>(() => BackupManifestExtensions.ReadFromFile(null));
        }
    }
}
