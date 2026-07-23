using Xunit;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;

namespace DockerSqliteBackup.Tests
{
    public class BackupEventTests
    {
        [Fact]
        public void BackupStartedEvent_HasCorrectEventType()
        {
            // Arrange and Act
            var backupStartedEvent = new BackupStartedEvent();

            // Assert
            Assert.Equal("backup.started", backupStartedEvent.EventType);
        }

        [Fact]
        public void BackupCompletedEvent_HasCorrectEventType()
        {
            // Arrange and Act
            var backupCompletedEvent = new BackupCompletedEvent();

            // Assert
            Assert.Equal("backup.completed", backupCompletedEvent.EventType);
        }

        [Fact]
        public void BackupEvent_HasUniqueEventId()
        {
            // Arrange and Act
            var backupEvent1 = new BackupStartedEvent();
            var backupEvent2 = new BackupStartedEvent();

            // Assert
            Assert.NotEqual(backupEvent1.EventId, backupEvent2.EventId);
        }

        [Fact]
        public void BackupEvent_HasOccurredAtDateTime()
        {
            // Arrange and Act
            var backupEvent = new BackupStartedEvent();

            // Assert
            Assert.True(backupEvent.OccurredAt <= DateTime.UtcNow);
        }

        [Fact]
        public void BackupStartedEvent_HasScheduleAndStartTime()
        {
            // Arrange
            var schedule = new BackupSchedule();
            var startTime = DateTime.UtcNow;

            // Act
            var backupStartedEvent = new BackupStartedEvent { Schedule = schedule, StartTime = startTime };

            // Assert
            Assert.Equal(schedule, backupStartedEvent.Schedule);
            Assert.Equal(startTime, backupStartedEvent.StartTime);
        }

        [Fact]
        public void BackupCompletedEvent_HasResultAndDuration()
        {
            // Arrange
            var result = new BackupResult();
            var duration = TimeSpan.FromSeconds(10);

            // Act
            var backupCompletedEvent = new BackupCompletedEvent { Result = result, Duration = duration };

            // Assert
            Assert.Equal(result, backupCompletedEvent.Result);
            Assert.Equal(duration, backupCompletedEvent.Duration);
        }
    }
}
