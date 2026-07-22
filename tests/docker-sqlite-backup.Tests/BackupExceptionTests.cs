using Xunit;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Tests
{
    public class BackupExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_ThrowsBackupException()
        {
            // Arrange
            string message = "Test message";

            // Act and Assert
            Assert.IsType<BackupException>(Assert.Throws<BackupException>(() => new BackupException(message)));
        }

        [Fact]
        public void Constructor_WithMessageAndBackupId_ThrowsBackupException()
        {
            // Arrange
            string message = "Test message";
            Guid backupId = Guid.NewGuid();

            // Act and Assert
            Assert.IsType<BackupException>(Assert.Throws<BackupException>(() => new BackupException(message, backupId)));
        }

        [Fact]
        public void Constructor_WithMessageAndScheduleIdAndBackupId_ThrowsBackupException()
        {
            // Arrange
            string message = "Test message";
            Guid scheduleId = Guid.NewGuid();
            Guid backupId = Guid.NewGuid();

            // Act and Assert
            Assert.IsType<BackupException>(Assert.Throws<BackupException>(() => new BackupException(message, scheduleId, backupId)));
        }

        [Fact]
        public void DatabaseAccessException_ThrowsDatabaseAccessException()
        {
            // Arrange
            string databasePath = "Test database path";
            Exception innerException = new Exception("Test inner exception");

            // Act and Assert
            Assert.IsType<DatabaseAccessException>(Assert.Throws<DatabaseAccessException>(() => new DatabaseAccessException(databasePath, innerException)));
        }

        [Fact]
        public void BackupTimeoutException_ThrowsBackupTimeoutException()
        {
            // Arrange
            string message = "Test message";
            TimeSpan timeout = TimeSpan.FromSeconds(10);

            // Act and Assert
            Assert.IsType<BackupTimeoutException>(Assert.Throws<BackupTimeoutException>(() => new BackupTimeoutException(message, timeout)));
        }

        [Fact]
        public void BackupCorruptedException_ThrowsBackupCorruptedException()
        {
            // Arrange
            string message = "Test message";
            Guid backupId = Guid.NewGuid();

            // Act and Assert
            Assert.IsType<BackupCorruptedException>(Assert.Throws<BackupCorruptedException>(() => new BackupCorruptedException(message, backupId)));
        }
    }
}
