using Xunit;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Tests
{
    public class BackupExceptionTests
    {
        [Fact]
        public void Constructor_WithMessage_ThrowsBackupException()
        {
            var exception = new BackupException("Test message");
            Assert.IsType<BackupException>(exception);
            Assert.Equal("Test message", exception.Message);
        }

        [Fact]
        public void Constructor_WithMessageAndBackupId_ThrowsBackupException()
        {
            Guid backupId = Guid.NewGuid();
            var exception = new BackupException("Test message", backupId);
            Assert.IsType<BackupException>(exception);
            Assert.Equal(backupId, exception.BackupId);
        }

        [Fact]
        public void Constructor_WithMessageAndScheduleIdAndBackupId_ThrowsBackupException()
        {
            Guid scheduleId = Guid.NewGuid();
            Guid backupId = Guid.NewGuid();
            var exception = new BackupException("Test message", scheduleId, backupId);
            Assert.IsType<BackupException>(exception);
            Assert.Equal(scheduleId, exception.ScheduleId);
            Assert.Equal(backupId, exception.BackupId);
        }

        [Fact]
        public void DatabaseAccessException_ThrowsDatabaseAccessException()
        {
            Exception innerException = new Exception("Test inner exception");
            var exception = new DatabaseAccessException("Test database path", innerException);
            Assert.IsType<DatabaseAccessException>(exception);
        }

        [Fact]
        public void BackupTimeoutException_ThrowsBackupTimeoutException()
        {
            var exception = new BackupTimeoutException("Test message", TimeSpan.FromSeconds(10));
            Assert.IsType<BackupTimeoutException>(exception);
        }

        [Fact]
        public void BackupCorruptedException_ThrowsBackupCorruptedException()
        {
            Guid backupId = Guid.NewGuid();
            var exception = new BackupCorruptedException("Test message", backupId);
            Assert.IsType<BackupCorruptedException>(exception);
        }
    }
}
