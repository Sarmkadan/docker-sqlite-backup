using System;
using System.IO;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class BackupScheduleTests
    {
        [Fact]
        public void DefaultConstructor_ShouldInitializeWithDefaults()
        {
            var schedule = new BackupSchedule();

            Assert.NotEqual(Guid.Empty, schedule.Id);
            Assert.Equal(string.Empty, schedule.Name);
            Assert.Equal(string.Empty, schedule.Description);
            Assert.Equal(string.Empty, schedule.DatabasePath);
            Assert.Equal("0 2 * * *", schedule.CronExpression);
            Assert.True(schedule.IsActive);
            Assert.True(schedule.IsEnabled);
            Assert.Null(schedule.NextRunTime);
            Assert.Null(schedule.NextScheduledRunTime);
            Assert.NotEqual(default, schedule.CreatedAt);
            Assert.NotEqual(default, schedule.LastModifiedAt);
            Assert.Null(schedule.LastBackupAt);
            Assert.Equal(30, schedule.RetentionDays);
            Assert.Equal(10, schedule.MaxBackupCount);
        }

        [Fact]
        public void IsEnabled_Alias_ShouldReflectIsActive()
        {
            var schedule = new BackupSchedule();

            schedule.IsActive = false;
            Assert.False(schedule.IsEnabled);

            schedule.IsEnabled = true;
            Assert.True(schedule.IsActive);
        }

        [Fact]
        public void IsValid_ReturnsTrue_WhenAllRequiredPropertiesAreSet()
        {
            var schedule = new BackupSchedule
            {
                Name = "Daily backup",
                DatabasePath = "/tmp/db.sqlite",
                CronExpression = "0 2 * * *",
                RetentionDays = 7,
                MaxBackupCount = 5
            };

            Assert.True(schedule.IsValid());
        }

        [Theory]
        [InlineData(null, "/tmp/db.sqlite", "0 2 * * *", 7, 5, false)] // Name null
        [InlineData("", "/tmp/db.sqlite", "0 2 * * *", 7, 5, false)]   // Name empty
        [InlineData("Backup", null, "0 2 * * *", 7, 5, false)]       // DatabasePath null
        [InlineData("Backup", "", "0 2 * * *", 7, 5, false)]         // DatabasePath empty
        [InlineData("Backup", "/tmp/db.sqlite", null, 7, 5, false)] // CronExpression null
        [InlineData("Backup", "/tmp/db.sqlite", "", 7, 5, false)]   // CronExpression empty
        [InlineData("Backup", "/tmp/db.sqlite", "0 2 * * *", 0, 5, false)] // RetentionDays <1
        [InlineData("Backup", "/tmp/db.sqlite", "0 2 * * *", 7, 0, false)] // MaxBackupCount <1
        public void IsValid_ReturnsFalse_WhenRequiredPropertiesInvalid(
            string name,
            string dbPath,
            string cron,
            int retention,
            int maxCount,
            bool expected)
        {
            var schedule = new BackupSchedule
            {
                Name = name ?? string.Empty,
                DatabasePath = dbPath ?? string.Empty,
                CronExpression = cron ?? string.Empty,
                RetentionDays = retention,
                MaxBackupCount = maxCount
            };

            Assert.Equal(expected, schedule.IsValid());
        }

        [Fact]
        public void ValidateDatabasePath_ReturnsTrue_WhenFileExists()
        {
            // Arrange: create a temporary file
            var tempFile = Path.GetTempFileName();
            try
            {
                var schedule = new BackupSchedule { DatabasePath = tempFile };
                Assert.True(schedule.ValidateDatabasePath());
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void ValidateDatabasePath_ReturnsFalse_WhenFileDoesNotExist()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".sqlite");
            var schedule = new BackupSchedule { DatabasePath = nonExistentPath };
            Assert.False(schedule.ValidateDatabasePath());
        }
    }
}
