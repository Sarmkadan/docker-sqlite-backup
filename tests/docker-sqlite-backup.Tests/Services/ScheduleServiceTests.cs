// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Tests for the ScheduleService class.
/// </summary>
namespace DockerSqliteBackup.Tests.Services
{
    public class ScheduleServiceTests
    {
        private readonly Mock<IBackupRepository> _repositoryMock;
        private readonly Mock<ILogger<ScheduleService>> _loggerMock;
        private readonly ScheduleService _sut;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleServiceTests"/> class.
        /// </summary>
        public ScheduleServiceTests()
        {
            _repositoryMock = new Mock<IBackupRepository>();
            _loggerMock = new Mock<ILogger<ScheduleService>>();
            _sut = new ScheduleService(_repositoryMock.Object, _loggerMock.Object);
        }

        /// <summary>
        /// Verifies that a valid cron expression is accepted.
        /// </summary>
        [Fact]
        public void ValidateCronExpression_ValidExpression_ReturnsTrue()
        {
            _sut.ValidateCronExpression("0 2 * * *").Should().BeTrue();
        }

        /// <summary>
        /// Verifies that an invalid cron expression is rejected.
        /// </summary>
        [Fact]
        public void ValidateCronExpression_InvalidExpression_ReturnsFalse()
        {
            _sut.ValidateCronExpression("not-a-cron").Should().BeFalse();
        }

        /// <summary>
        /// Verifies that a valid schedule returns a date in the future.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_ValidSchedule_ReturnsDateInFuture()
        {
            var schedule = new BackupSchedule { CronExpression = "0 2 * * *" };

            var next = _sut.GetNextExecutionTime(schedule);

            next.Should().NotBeNull();
            next!.Value.Should().BeAfter(DateTime.UtcNow);
        }

        /// <summary>
        /// Verifies that an invalid cron expression returns null.
        /// </summary>
        [Fact]
        public void GetNextExecutionTime_InvalidCronExpression_ReturnsNull()
        {
            var schedule = new BackupSchedule { CronExpression = "invalid-cron" };

            var next = _sut.GetNextExecutionTime(schedule);

            next.Should().BeNull();
        }

        /// <summary>
        /// Verifies that creating a schedule delegates to the repository.
        /// </summary>
        [Fact]
        public async Task CreateScheduleAsync_ValidSchedule_DelegatesToRepository()
        {
            var schedule = new BackupSchedule
            {
                Name = "Nightly Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 2 * * *"
            };
            _repositoryMock
                .Setup(r => r.CreateScheduleAsync(It.IsAny<BackupSchedule>()))
                .ReturnsAsync(schedule);

            var result = await _sut.CreateScheduleAsync(schedule);

            _repositoryMock.Verify(r => r.CreateScheduleAsync(It.IsAny<BackupSchedule>()), Times.Once);
            result.Should().NotBeNull();
            result.Name.Should().Be("Nightly Backup");
        }

        /// <summary>
        /// Verifies that an invalid schedule throws an exception.
        /// </summary>
        [Fact]
        public async Task CreateScheduleAsync_InvalidSchedule_ThrowsInvalidScheduleException()
        {
            var schedule = new BackupSchedule
            {
                Name = "",
                DatabasePath = "/data/app.db",
                CronExpression = "0 2 * * *"
            };

            await _sut.Invoking(s => s.CreateScheduleAsync(schedule))
                .Should().ThrowAsync<InvalidScheduleException>();
        }

        /// <summary>
        /// Verifies that an invalid cron expression throws an exception.
        /// </summary>
        [Fact]
        public async Task CreateScheduleAsync_InvalidCronExpression_ThrowsInvalidCronExpressionException()
        {
            var schedule = new BackupSchedule
            {
                Name = "Nightly Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "bad-cron"
            };

            await _sut.Invoking(s => s.CreateScheduleAsync(schedule))
                .Should().ThrowAsync<InvalidCronExpressionException>();
        }

        /// <summary>
        /// Verifies that getting a schedule by ID returns the schedule from the repository.
        /// </summary>
        [Fact]
        public async Task GetScheduleAsync_ExistingId_ReturnsScheduleFromRepository()
        {
            var scheduleId = Guid.NewGuid();
            var schedule = new BackupSchedule { Id = scheduleId, Name = "Test" };
            _repositoryMock
                .Setup(r => r.GetScheduleAsync(scheduleId))
                .ReturnsAsync(schedule);

            var result = await _sut.GetScheduleAsync(scheduleId);

            result.Should().NotBeNull();
            result!.Id.Should().Be(scheduleId);
        }

        /// <summary>
        /// Verifies that getting a non-existent schedule by ID returns null.
        /// </summary>
        [Fact]
        public async Task GetScheduleAsync_NonExistingId_ReturnsNull()
        {
            _repositoryMock
                .Setup(r => r.GetScheduleAsync(It.IsAny<Guid>()))
                .ReturnsAsync((BackupSchedule?)null);

            var result = await _sut.GetScheduleAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        /// <summary>
        /// Verifies that deactivating a schedule updates the schedule in the repository.
        /// </summary>
        [Fact]
        public async Task DeactivateScheduleAsync_ExistingSchedule_UpdatesIsActiveFalse()
        {
            var scheduleId = Guid.NewGuid();
            var schedule = new BackupSchedule
            {
                Id = scheduleId,
                Name = "Active Backup",
                DatabasePath = "/data/app.db",
                CronExpression = "0 2 * * *",
                IsActive = true
            };
            _repositoryMock
                .Setup(r => r.GetScheduleAsync(scheduleId))
                .ReturnsAsync(schedule);
            _repositoryMock
                .Setup(r => r.UpdateScheduleAsync(It.IsAny<BackupSchedule>()))
                .ReturnsAsync(schedule);

            await _sut.DeactivateScheduleAsync(scheduleId);

            _repositoryMock.Verify(
                r => r.UpdateScheduleAsync(It.Is<BackupSchedule>(s => !s.IsActive)),
                Times.Once);
        }
    }
}
