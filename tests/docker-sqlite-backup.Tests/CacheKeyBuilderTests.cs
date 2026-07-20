using System;
using DockerSqliteBackup.Caching;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class CacheKeyBuilderTests
    {
        [Fact]
        public void DeterministicKeys_ForSameInputs_ShouldBeEqual()
        {
            // Arrange
            var guid = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
            var builder1 = new CacheKeyBuilder()
                .Add("schedule")
                .Add(guid)
                .Add(42);

            var builder2 = new CacheKeyBuilder()
                .Add("schedule")
                .Add(guid)
                .Add(42);

            // Act
            var key1 = builder1.Build();
            var key2 = builder2.Build();

            // Assert
            Assert.Equal(key1, key2);
            Assert.StartsWith("backup:", key1);
        }

        [Fact]
        public void DistinctKeys_ForDifferentInputs_ShouldNotBeEqual()
        {
            // Arrange
            var guid = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
            var builder1 = new CacheKeyBuilder()
                .Add("schedule")
                .Add(guid)
                .Add(42);

            var builder2 = new CacheKeyBuilder()
                .Add("schedule")
                .Add(guid)
                .Add(43); // different integer part

            // Act
            var key1 = builder1.Build();
            var key2 = builder2.Build();

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void SeparatorCollision_ShouldProduceDistinctKeys()
        {
            // Arrange
            var builder1 = new CacheKeyBuilder()
                .Add("a")
                .Add("bc");

            var builder2 = new CacheKeyBuilder()
                .Add("ab")
                .Add("c");

            // Act
            var key1 = builder1.Build();
            var key2 = builder2.Build();

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void StaticKeys_ShouldReturnExpectedPatterns()
        {
            // Arrange
            var scheduleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var backupId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var configKey = "maxRetries";

            // Act
            var scheduleKey = CacheKeyBuilder.Keys.Schedule(scheduleId);
            var allSchedulesKey = CacheKeyBuilder.Keys.AllSchedules();
            var backupResultKey = CacheKeyBuilder.Keys.BackupResult(backupId);
            var backupHistoryKey = CacheKeyBuilder.Keys.BackupHistory(scheduleId, 5);
            var healthStatusKey = CacheKeyBuilder.Keys.HealthStatus();
            var metricsKey = CacheKeyBuilder.Keys.Metrics();
            var configValueKey = CacheKeyBuilder.Keys.ConfigValue(configKey);

            // Assert
            Assert.Equal($"backup:schedule:{scheduleId}", scheduleKey);
            Assert.Equal("backup:schedules:all", allSchedulesKey);
            Assert.Equal($"backup:backup:{backupId}", backupResultKey);
            Assert.Equal($"backup:backups:history:{scheduleId}:5", backupHistoryKey);
            Assert.Equal("backup:health:status", healthStatusKey);
            Assert.Equal("backup:metrics:summary", metricsKey);
            Assert.Equal($"backup:config:{configKey}", configValueKey);
        }
    }
}
