using System;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class BackupJobExtensionsTests
    {
        [Fact]
        public void Duration_ReturnsSameAsGetElapsedTime()
        {
            // Arrange
            var started = DateTime.UtcNow.AddMinutes(-3).AddSeconds(-15);
            var job = new BackupJob
            {
                StartedAt = started,
                Status = (int)BackupStatus.InProgress,
                IsProcessing = true
            };

            // Act
            var duration = job.Duration();

            // Assert
            var expected = DateTime.UtcNow - started;
            Assert.True(Math.Abs((duration - expected).TotalSeconds) < 1,
                $"Expected duration close to {expected}, got {duration}");
        }

        [Fact]
        public void IsRetryable_ReturnsTrueWhenFailedAndRetriesRemain()
        {
            // Arrange
            var job = new BackupJob
            {
                Status = (int)BackupStatus.Failed,
                RetryCount = 1,
                MaxRetries = 3
            };

            // Act
            var result = job.IsRetryable();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRetryable_ReturnsFalseWhenNoRetriesRemain()
        {
            // Arrange
            var job = new BackupJob
            {
                Status = (int)BackupStatus.Failed,
                RetryCount = 3,
                MaxRetries = 3
            };

            // Act
            var result = job.IsRetryable();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ToAuditString_ContainsKeyInformation()
        {
            // Arrange
            var jobId = Guid.NewGuid();
            var job = new BackupJob
            {
                Id = jobId,
                Status = (int)BackupStatus.Success,
                StartedAt = DateTime.UtcNow.AddSeconds(-30),
                IsProcessing = false,
                RetryCount = 0,
                MaxRetries = 3,
                Result = null
            };

            // Act
            var audit = job.ToAuditString();

            // Assert
            Assert.Contains($"Id={jobId}", audit);
            Assert.Contains("Status=Success", audit);
            Assert.Contains("Retries=0/3", audit);
        }
    }
}
