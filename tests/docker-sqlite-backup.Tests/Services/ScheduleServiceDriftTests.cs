using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services
{
    public class ScheduleServiceDriftTests
    {
        private readonly Mock<IBackupRepository> _repositoryMock;
        private readonly Mock<ILogger<ScheduleService>> _loggerMock;
        private readonly ScheduleService _sut;

        public ScheduleServiceDriftTests()
        {
            _repositoryMock = new Mock<IBackupRepository>();
            _loggerMock = new Mock<ILogger<ScheduleService>>();
            _sut = new ScheduleService(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void GetNextExecutionTime_UsesNextScheduledRunTimeAsAnchor_PreventsDrift()
        {
            // Arrange
            // Cron: every 10 minutes
            // Previous scheduled run: 1:00 PM
            // Now: 1:05 PM (but the job actually finished at 1:15 PM due to long duration)
            var anchor = new DateTime(2026, 8, 4, 13, 0, 0, DateTimeKind.Utc);
            var now = new DateTime(2026, 8, 4, 13, 15, 0, DateTimeKind.Utc);
            
            var schedule = new BackupSchedule
            {
                Name = "Frequent Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "*/10 * * * *",
                NextScheduledRunTime = anchor
            };

            // Act
            // We need to pass the schedule, but the service uses UtcNow inside.
            // I'll need to mock DateTime if possible, but the service uses direct DateTime.UtcNow.
            // Instead, I will assume the service is updated to use the anchor.
            
            // To make this test pass with the *old* logic (using UtcNow), it will return 1:20 PM.
            // To make it pass with the *new* logic (using anchor), it should return 1:10 PM.
            
            // Wait, if anchor is 1:00, next occurrence *after* 1:00 is 1:10.
            
            var nextRun = _sut.GetNextExecutionTime(schedule);

            // Assert
            // This assertion expects 1:10 PM, which is what it *should* be.
            nextRun.Should().Be(anchor.AddMinutes(10));
        }
    }
}
