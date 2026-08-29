using System;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Health;
using Xunit;

namespace DockerSqliteBackup.Tests.Health
{
    public class DockerHealthCheckEvaluatorTests
    {
        private readonly AppSettings _defaultSettings = new()
        {
            ScheduleCheckIntervalSeconds = 60, // 1 minute
            HealthCheckGraceFactor = 1.5
        };

        [Fact]
        public void Evaluate_NullSnapshot_ReturnsHealthyWithNoActivityMessage()
        {
            // Act
            var outcome = DockerHealthCheckEvaluator.Evaluate(null, _defaultSettings);

            // Assert
            Assert.True(outcome.IsHealthy);
            Assert.Contains("No backup activity recorded yet; awaiting first scheduled run.", outcome.Reason);
        }

        [Fact]
        public void Evaluate_FreshHealthySnapshot_ReturnsHealthy()
        {
            // Arrange
            var snapshot = new HealthStatusSnapshot
            {
                LastBackupCompletedAt = DateTime.UtcNow.AddSeconds(-30), // 30 seconds ago
                LastBackupCompletedCronExpression = "0 * * * * *" // every minute
            };

            // Act
            var outcome = DockerHealthCheckEvaluator.Evaluate(snapshot, _defaultSettings);

            // Assert
            Assert.True(outcome.IsHealthy);
            Assert.Contains("Last successful backup at", outcome.Reason);
            Assert.Contains("is within the freshness window.", outcome.Reason);
        }

        [Fact]
        public void Evaluate_StaleSnapshot_ReturnsUnhealthy()
        {
            // Arrange
            var snapshot = new HealthStatusSnapshot
            {
                LastBackupCompletedAt = DateTime.UtcNow.AddMinutes(-100), // 100 minutes ago
                LastBackupCompletedCronExpression = "0 * * * * *" // every minute -> expected interval 1 minute
            };
            // With grace factor 1.5, threshold = 1.5 minutes. Age 100 minutes > threshold -> unhealthy.

            // Act
            var outcome = DockerHealthCheckEvaluator.Evaluate(snapshot, _defaultSettings);

            // Assert
            Assert.False(outcome.IsHealthy);
            Assert.Contains("Last successful backup at", outcome.Reason);
            Assert.Contains("is", outcome.Reason);
            Assert.Contains("minutes old, exceeding the allowed", outcome.Reason);
        }

        [Fact]
        public void Evaluate_SnapshotWithFailedBackup_ReturnsUnhealthy()
        {
            // Arrange
            var snapshot = new HealthStatusSnapshot
            {
                LastBackupFailedAt = DateTime.UtcNow.AddMinutes(-10),
                LastBackupFailedMessage = "Disk full",
                LastBackupCompletedAt = DateTime.UtcNow.AddHours(-2) // successful backup 2 hours ago
            };

            // Act
            var outcome = DockerHealthCheckEvaluator.Evaluate(snapshot, _defaultSettings);

            // Assert
            Assert.False(outcome.IsHealthy);
            Assert.Contains("Last backup failed at", outcome.Reason);
            Assert.Contains("Disk full", outcome.Reason);
        }

        [Fact]
        public void Evaluate_SnapshotWithFailedRestoreVerification_ReturnsUnhealthy()
        {
            // Arrange
            var snapshot = new HealthStatusSnapshot
            {
                LastRestoreVerificationAt = DateTime.UtcNow.AddMinutes(-5),
                LastRestoreVerificationPassed = false,
                LastRestoreVerificationMessage = "Checksum mismatch"
            };

            // Act
            var outcome = DockerHealthCheckEvaluator.Evaluate(snapshot, _defaultSettings);

            // Assert
            Assert.False(outcome.IsHealthy);
            Assert.Contains("Last restore verification failed at", outcome.Reason);
            Assert.Contains("Checksum mismatch", outcome.Reason);
        }
    }
}