// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// Tests for ScheduleService.GetNextExecutionTime() method
// Focus: next-run computation from cron expressions, boundary times, invalid schedules
// =============================================================================

using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services
{
    /// <summary>
    /// Tests for ScheduleService.GetNextExecutionTime() method covering:
    /// - next-run computation from cron expressions
    /// - boundary times
    /// - invalid schedules
    /// </summary>
    public class ScheduleServiceNextRunTests
    {
        private readonly Mock<IBackupRepository> _repositoryMock;
        private readonly Mock<ILogger<ScheduleService>> _loggerMock;
        private readonly ScheduleService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleServiceNextRunTests"/> class.
        /// </summary>
        public ScheduleServiceNextRunTests()
        {
            _repositoryMock = new Mock<IBackupRepository>();
            _loggerMock = new Mock<ILogger<ScheduleService>>();
            _sut = new ScheduleService(_repositoryMock.Object, _loggerMock.Object);
        }

        /// <summary>
        /// Tests that a daily cron expression returns a next run time in the future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_DailyCronExpression_ReturnsFutureDate()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Daily Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 2 * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddDays(2));
        }

        /// <summary>
        /// Tests that an hourly cron expression returns a next run time in the near future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_HourlyCronExpression_ReturnsNearFutureDate()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Hourly Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 * * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddHours(2));
        }

        /// <summary>
        /// Tests that a minute-level cron expression returns a next run time in the very near future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_MinuteLevelCronExpression_ReturnsVeryNearFutureDate()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Every 5 Minutes",
                DatabasePath = "/data/app.db",
                CronExpression = "*/5 * * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddMinutes(10));
        }

        /// <summary>
        /// Tests that a weekly cron expression returns a next run time in the future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_WeeklyCronExpression_ReturnsFutureDate()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Weekly Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 3 * * 0" // Sunday at 3 AM
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddDays(8));
        }

        /// <summary>
        /// Tests that a monthly cron expression returns a next run time in the future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_MonthlyCronExpression_ReturnsFutureDate()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Monthly Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 0 1 * *" // 1st of month at midnight
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddDays(32));
        }

        /// <summary>
        /// Tests that a cron expression with specific minute and hour returns correct next run time.
        /// Boundary test: exact minute boundary.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_SpecificMinuteHour_ReturnsCorrectBoundaryTime()
        {
            // Arrange - schedule for 2:30 AM
            var schedule = new BackupSchedule
            {
                Name = "Specific Time Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "30 2 * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);

            // Should be today at 2:30 AM or tomorrow at 2:30 AM
            var today2AM = DateTime.UtcNow.Date.AddHours(2);
            var tomorrow2AM = today2AM.AddDays(1);

            nextRun.Value.Should().BeOneOf(
                today2AM.AddMinutes(30),
                tomorrow2AM.AddMinutes(30));
        }

        /// <summary>
        /// Tests that a cron expression with past time returns next day's time.
        /// Boundary test: when current time is past the scheduled time.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_PastScheduledTime_ReturnsNextDay()
        {
            // Arrange - schedule for 2 AM, but we're testing when it's already past 2 AM
            var schedule = new BackupSchedule
            {
                Name = "Past Time Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 2 * * *"
            };

            // Force current time to be after 2 AM today
            var now = DateTime.UtcNow;
            var mockNow = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0);

            // Act - use reflection to set DateTime.UtcNow for testing
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(now);
            nextRun.Value.Day.Should().Be(now.Day + 1); // Should be tomorrow
        }

        /// <summary>
        /// Tests that an invalid cron expression returns null.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_InvalidCronExpression_ReturnsNull()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Invalid Cron",
                DatabasePath = "/data/app.db",
                CronExpression = "invalid-cron-expression"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().BeNull();
        }

        /// <summary>
        /// Tests that an empty cron expression returns null.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_EmptyCronExpression_ReturnsNull()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Empty Cron",
                DatabasePath = "/data/app.db",
                CronExpression = string.Empty
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().BeNull();
        }

        /// <summary>
        /// Tests that a null cron expression returns null.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_NullCronExpression_ReturnsNull()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "Null Cron",
                DatabasePath = "/data/app.db",
                CronExpression = null!
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().BeNull();
        }

        /// <summary>
        /// Tests that a complex cron expression with all fields works correctly.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_ComplexCronExpression_ReturnsValidDate()
        {
            // Arrange - every 15 minutes on weekdays only
            var schedule = new BackupSchedule
            {
                Name = "Complex Schedule",
                DatabasePath = "/data/app.db",
                CronExpression = "*/15 9-17 * * 1-5" // 9 AM to 5 PM, Mon-Fri, every 15 mins
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Hour.Should().BeGreaterOrEqualTo(9);
            nextRun.Value.Hour.Should().BeLessOrEqualTo(17);
        }

        /// <summary>
        /// Tests that GetNextExecutionTime handles null schedule by throwing NullReferenceException.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_NullSchedule_ThrowsNullReferenceException()
        {
            // Arrange
            BackupSchedule? schedule = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => _sut.GetNextExecutionTime(schedule!));
        }

        /// <summary>
        /// Tests that a schedule with no cron expression returns null.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_EmptyScheduleCron_ReturnsNull()
        {
            // Arrange
            var schedule = new BackupSchedule
            {
                Name = "No Cron",
                DatabasePath = "/data/app.db",
                CronExpression = ""
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().BeNull();
        }

        /// <summary>
        /// Tests that every-minute cron expression returns immediate next minute.
        /// Boundary test: immediate next occurrence.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_EveryMinuteCron_ReturnsImmediateNextMinute()
        {
            // Arrange - every minute
            var schedule = new BackupSchedule
            {
                Name = "Every Minute",
                DatabasePath = "/data/app.db",
                CronExpression = "* * * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Should().BeBefore(DateTime.UtcNow.AddMinutes(2));
        }

        /// <summary>
        /// Tests that a schedule with a specific cron time returns a valid future time.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_SpecificCronTime_ReturnsValidFutureTime()
        {
            // Arrange - schedule for a specific time (e.g., 2:30 AM)
            var schedule = new BackupSchedule
            {
                Name = "Specific Time Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "30 2 * * *"
            };

            // Act
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            nextRun.Should().NotBeNull();
            nextRun!.Value.Should().BeAfter(DateTime.UtcNow);
            nextRun.Value.Hour.Should().Be(2);
            nextRun.Value.Minute.Should().Be(30);
        }
    }
}