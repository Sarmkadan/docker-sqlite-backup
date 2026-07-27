// tests/docker-sqlite-backup.Tests/AuditLoggerTests.cs

using Xunit;
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Audit;

namespace DockerSqliteBackup.Tests
{
    public class AuditLoggerTests
    {
        [Fact]
        public void Constructor_LogsAuditEntry_WhenAuditLogPathIsProvided()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger, "path/to/audit.log");

            // Act
            auditLogger.LogBackupOperation(Guid.NewGuid(), "BackupOperation", true);

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogBackupOperation_LogsAuditEntry_WhenOperationIsSuccessful()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogBackupOperation(Guid.NewGuid(), "BackupOperation", true);

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogScheduleChange_LogsAuditEntry_WhenScheduleChangeIsSuccessful()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogScheduleChange(Guid.NewGuid(), "ScheduleChange", new Dictionary<string, string>());

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogConfigChange_LogsAuditEntry_WhenConfigChangeIsSuccessful()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogConfigChange("ConfigSetting", "OldValue", "NewValue");

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogDataAccess_LogsAuditEntry_WhenDataAccessIsSuccessful()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogDataAccess("DataResource", "DataAccess", "User123");

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogEntry_LogsAuditEntry_WhenEntryIsProvided()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            var entry = new AuditEntry { Timestamp = DateTime.UtcNow, Category = "Category", Action = "Action" };
            auditLogger.LogEntry(entry);

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogEntry_LogsAuditEntry_WhenEntryIsNull()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogEntry(null);

            // Assert
            Assert.Empty(logger.Logs);
        }

        [Fact]
        public void LogBackupOperation_LogsAuditEntry_WhenOperationFails()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogBackupOperation(Guid.NewGuid(), "BackupOperation", false);

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogScheduleChange_LogsAuditEntry_WhenScheduleChangeFails()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogScheduleChange(Guid.NewGuid(), "ScheduleChange", new Dictionary<string, string>());

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogConfigChange_LogsAuditEntry_WhenConfigChangeFails()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogConfigChange("ConfigSetting", "OldValue", "NewValue");

            // Assert
            Assert.True(logger.Logs.Any());
        }

        [Fact]
        public void LogDataAccess_LogsAuditEntry_WhenDataAccessFails()
        {
            // Arrange
            var logger = new TestLogger<AuditLogger>();
            var auditLogger = new AuditLogger(logger);

            // Act
            auditLogger.LogDataAccess("DataResource", "DataAccess", "User123", false);

            // Assert
            Assert.True(logger.Logs.Any());
        }

        private class TestLogger<T> : ILogger<T> where T : class
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Logs.Add(formatter(state, exception));
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }

            public List<string> Logs { get; } = new List<string>();
        }
    }
}
