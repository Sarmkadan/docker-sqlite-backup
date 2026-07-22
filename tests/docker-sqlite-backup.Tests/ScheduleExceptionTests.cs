using Xunit;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Tests
{
    public class ScheduleExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_ThrowsScheduleException()
        {
            // Arrange
            string message = "Test message";

            // Act and Assert
            Assert.IsType<ScheduleException>(Assert.Throws<ScheduleException>(() => new ScheduleException(message)));
        }

        [Fact]
        public void Constructor_WithMessageAndScheduleId_ThrowsScheduleException()
        {
            // Arrange
            string message = "Test message";
            Guid scheduleId = Guid.NewGuid();

            // Act and Assert
            Assert.IsType<ScheduleException>(Assert.Throws<ScheduleException>(() => new ScheduleException(message, scheduleId)));
        }

        [Fact]
        public void InvalidCronExpressionException_ThrowsInvalidCronExpressionException()
        {
            // Arrange
            string cronExpression = "Invalid cron expression";

            // Act and Assert
            Assert.IsType<InvalidCronExpressionException>(Assert.Throws<InvalidCronExpressionException>(() => new InvalidCronExpressionException(cronExpression)));
        }

        [Fact]
        public void InvalidScheduleException_ThrowsInvalidScheduleException()
        {
            // Arrange
            string message = "Test message";
            Guid scheduleId = Guid.NewGuid();

            // Act and Assert
            Assert.IsType<InvalidScheduleException>(Assert.Throws<InvalidScheduleException>(() => new InvalidScheduleException(message, scheduleId)));
        }

        [Fact]
        public void ScheduleId_Property_ReturnsScheduleId()
        {
            // Arrange
            Guid scheduleId = Guid.NewGuid();
            ScheduleException exception = new ScheduleException("Test message", scheduleId);

            // Act and Assert
            Assert.Equal(scheduleId, exception.ScheduleId);
        }

        [Fact]
        public void ScheduleId_Property_ReturnsNull_WhenNotSet()
        {
            // Arrange
            ScheduleException exception = new ScheduleException("Test message");

            // Act and Assert
            Assert.Null(exception.ScheduleId);
        }
    }
}
