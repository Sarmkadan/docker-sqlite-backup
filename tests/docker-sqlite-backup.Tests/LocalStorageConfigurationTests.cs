using Xunit;
using DockerSqliteBackup.Domain;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DockerSqliteBackup.Tests
{
    public class LocalStorageConfigurationTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            // Arrange & Act
            var config = new LocalStorageConfiguration();

            // Assert
            Assert.Equal(string.Empty, config.BaseDirectory);
            Assert.True(config.CreateSubdirectoriesBySchedule);
            Assert.Equal("0640", config.FilePermissions);
            Assert.False(config.CompressBackups);
            Assert.Equal(1073741824, config.MinimumFreeSpaceBytes); // 1 GB
            Assert.True(config.PreserveFileTimestamp);
            Assert.Equal((int)Constants.StorageType.Local, config.StorageType);
        }

        [Fact]
        public void BaseDirectory_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var config = new LocalStorageConfiguration();
            var testPath = @"/tmp/test-backups";

            // Act
            config.BaseDirectory = testPath;

            // Assert
            Assert.Equal(testPath, config.BaseDirectory);
        }

        [Fact]
        public void FilePermissions_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var config = new LocalStorageConfiguration();
            var permissions = "0755";

            // Act
            config.FilePermissions = permissions;

            // Assert
            Assert.Equal(permissions, config.FilePermissions);
        }

        [Fact]
        public void CompressBackups_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var config = new LocalStorageConfiguration();

            // Act
            config.CompressBackups = true;

            // Assert
            Assert.True(config.CompressBackups);
        }

        [Fact]
        public void MinimumFreeSpaceBytes_SetAndGet_ReturnsCorrectValue()
        {
            // Arrange
            var config = new LocalStorageConfiguration();
            var space = 2147483648L; // 2 GB

            // Act
            config.MinimumFreeSpaceBytes = space;

            // Assert
            Assert.Equal(space, config.MinimumFreeSpaceBytes);
        }

        [Fact]
        public void IsValid_WithValidConfiguration_ReturnsTrue()
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                Name = "TestConfig",
                BaseDirectory = @"/tmp/valid-backups"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_WithNullOrEmptyName_ReturnsFalse(string? name)
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                Name = name,
                BaseDirectory = @"/tmp/backups"
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValid_WithNullOrEmptyBaseDirectory_ReturnsFalse(string? baseDirectory)
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                Name = "TestConfig",
                BaseDirectory = baseDirectory
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidDirectory_ReturnsTrue()
        {
            // Arrange
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                var config = new LocalStorageConfiguration
                {
                    Name = "TestConfig",
                    BaseDirectory = tempDir,
                    MinimumFreeSpaceBytes = 100 // Small value for test
                };

                // Act
                var result = await config.TestConnectionAsync();

                // Assert
                Assert.True(result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public void GetBackupPath_WithDefaultSettings_ReturnsCorrectPath()
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                BaseDirectory = @"/backups",
                CreateSubdirectoriesBySchedule = false
            };
            var scheduleName = "daily-backup";
            var timestamp = new DateTime(2024, 12, 25, 15, 30, 45);

            // Act
            var backupPath = config.GetBackupPath(scheduleName, timestamp);

            // Assert
            Assert.Equal(@"/backups/backup_daily-backup_2024-12-25_15-30-45.sqlite", backupPath);
        }

        [Fact]
        public void GetBackupPath_WithSubdirectories_ReturnsNestedPath()
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                BaseDirectory = @"/backups",
                CreateSubdirectoriesBySchedule = true
            };
            var scheduleName = "weekly-full";
            var timestamp = new DateTime(2024, 12, 25, 15, 30, 45);

            // Act
            var backupPath = config.GetBackupPath(scheduleName, timestamp);

            // Assert
            Assert.Equal(@"/backups/weekly-full/backup_weekly-full_2024-12-25_15-30-45.sqlite", backupPath);
        }

        [Fact]
        public void GetBackupPath_WithCompression_ReturnsGzPath()
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                BaseDirectory = @"/backups",
                CreateSubdirectoriesBySchedule = false,
                CompressBackups = true
            };
            var scheduleName = "hourly";
            var timestamp = new DateTime(2024, 12, 25, 15, 30, 45);

            // Act
            var backupPath = config.GetBackupPath(scheduleName, timestamp);

            // Assert
            Assert.Equal(@"/backups/backup_hourly_2024-12-25_15-30-45.sqlite.gz", backupPath);
        }

        [Fact]
        public void GetBackupPath_WithAllFeatures_ReturnsFullPath()
        {
            // Arrange
            var config = new LocalStorageConfiguration
            {
                BaseDirectory = @"/var/backups/mysql",
                CreateSubdirectoriesBySchedule = true,
                CompressBackups = true
            };
            var scheduleName = "daily-snapshots";
            var timestamp = new DateTime(2024, 12, 25, 15, 30, 45);

            // Act
            var backupPath = config.GetBackupPath(scheduleName, timestamp);

            // Assert
            Assert.Equal(@"/var/backups/mysql/daily-snapshots/backup_daily-snapshots_2024-12-25_15-30-45.sqlite.gz", backupPath);
        }
    }
}
