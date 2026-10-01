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

            // Act
            var exception = new ScheduleException(message);

            // Assert
            Assert.IsType<ScheduleException>(exception);
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndScheduleId_ThrowsScheduleException()
        {
            // Arrange
            string message = "Test message";
            Guid scheduleId = Guid.NewGuid();

            // Act
            var exception = new ScheduleException(message, scheduleId);

            // Assert
            Assert.IsType<ScheduleException>(exception);
            Assert.Equal(scheduleId, exception.ScheduleId);
        }

        [Fact]
        public void InvalidCronExpressionException_ThrowsInvalidCronExpressionException()
        {
            // Arrange
            string cronExpression = "Invalid cron expression";

            // Act
            var exception = new InvalidCronExpressionException(cronExpression);

            // Assert
            Assert.IsType<InvalidCronExpressionException>(exception);
        }

        [Fact]
        public void InvalidScheduleException_ThrowsInvalidScheduleException()
        {
            // Arrange
            string message = "Test message";
            Guid scheduleId = Guid.NewGuid();

            // Act
            var exception = new InvalidScheduleException(message, scheduleId);

            // Assert
            Assert.IsType<InvalidScheduleException>(exception);
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
