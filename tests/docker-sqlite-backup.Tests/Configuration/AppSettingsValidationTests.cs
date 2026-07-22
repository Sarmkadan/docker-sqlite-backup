using System;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;

namespace DockerSqliteBackup.Tests.Configuration
{
    public class AppSettingsValidationTests
    {
        [Fact]
        public void Validate_WithNullSettings_ThrowsArgumentNullException()
        {
            // Arrange
            AppSettings settings = null!;

            // Act
            Action act = () => settings.Validate();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Validate_WithNullSettings_IsValid_ThrowsArgumentNullException()
        {
            // Arrange
            AppSettings settings = null!;

            // Act
            Action act = () => settings.IsValid();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Validate_WithNullSettings_EnsureValid_ThrowsArgumentNullException()
        {
            // Arrange
            AppSettings settings = null!;

            // Act
            Action act = () => settings.EnsureValid();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsValid_WithValidSettings_ReturnsTrue()
        {
            // Arrange
            var settings = new AppSettings
            {
                DatabasePath = "/var/backups/db.sqlite",
                MaxConcurrentBackups = 5,
                BackupTimeoutSeconds = 7200,
                ScheduleCheckIntervalSeconds = 120,
                LogLevel = "Debug",
                RetentionDays = 60,
                MaxBackupCount = 20,
                LocalStoragePath = "/var/backups",
                EnableVerificationByDefault = true,
                EnableS3StorageByDefault = false,
                CompressBackups = true,
                CompressionLevel = 9,
                NotificationEmails = new[] { "admin@example.com", "backup@example.org" },
                EnableEncryption = false,
                EncryptionKey = null
            };

            // Act
            var isValid = settings.IsValid();

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithValidSettings_ReturnsEmptyList()
        {
            // Arrange
            var settings = new AppSettings
            {
                DatabasePath = "/var/backups/db.sqlite",
                MaxConcurrentBackups = 5,
                BackupTimeoutSeconds = 7200,
                ScheduleCheckIntervalSeconds = 120,
                LogLevel = "Debug",
                RetentionDays = 60,
                MaxBackupCount = 20,
                LocalStoragePath = "/var/backups",
                EnableVerificationByDefault = true,
                EnableS3StorageByDefault = false,
                CompressBackups = true,
                CompressionLevel = 9,
                NotificationEmails = new[] { "admin@example.com", "backup@example.org" },
                EnableEncryption = false,
                EncryptionKey = null
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().BeEmpty();
        }

        [Fact]
        public void EnsureValid_WithValidSettings_DoesNotThrow()
        {
            // Arrange
            var settings = new AppSettings
            {
                DatabasePath = "/var/backups/db.sqlite",
                MaxConcurrentBackups = 5,
                BackupTimeoutSeconds = 7200,
                ScheduleCheckIntervalSeconds = 120,
                LogLevel = "Debug",
                RetentionDays = 60,
                MaxBackupCount = 20,
                LocalStoragePath = "/var/backups",
                EnableVerificationByDefault = true,
                EnableS3StorageByDefault = false,
                CompressBackups = true,
                CompressionLevel = 9,
                NotificationEmails = new[] { "admin@example.com", "backup@example.org" },
                EnableEncryption = false,
                EncryptionKey = null
            };

            // Act
            Action act = () => settings.EnsureValid();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_NotificationEmailsNull_ThrowsArgumentNullException()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = null!
            };

            // Act
            Action act = () => settings.Validate();

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Validate_NotificationEmailsWithWhitespaceEntry_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = new[] { "admin@example.com", "   " }
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("NotificationEmails"));
        }

        [Fact]
        public void Validate_NotificationEmailsWithInvalidEmailFormat_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = new[] { "admin@example.com", "not-an-email" }
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("invalid email format"));
        }

        [Fact]
        public void Validate_NotificationEmailsWithValidEmails_ReturnsEmptyList()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = new[] { "admin@example.com", "backup@example.org" }
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EnableEncryptionTrueWithNullEncryptionKey_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = null
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("EncryptionKey"))
                .And.Contain(problem => problem.Contains("must be provided"));
        }

        [Fact]
        public void Validate_EnableEncryptionTrueWithEmptyEncryptionKey_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = "   "
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("EncryptionKey"))
                .And.Contain(problem => problem.Contains("must be provided"));
        }

        [Fact]
        public void Validate_EncryptionKeyInvalidBase64Format_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = "not-valid-base64!!"
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("valid Base64-encoded string"));
        }

        [Fact]
        public void Validate_EncryptionKeyWrongLength_ReturnsValidationProblem()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = Convert.ToBase64String(new byte[16]) // 16 bytes instead of 32
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().ContainSingle()
                .And.Contain(problem => problem.Contains("32-byte"));
        }

        [Fact]
        public void Validate_EncryptionKeyCorrectLength_ReturnsEmptyList()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = Convert.ToBase64String(new byte[32]) // Valid 32-byte key
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EncryptionDisabledWithValidKey_ReturnsEmptyList()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = false,
                EncryptionKey = Convert.ToBase64String(new byte[32])
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EncryptionDisabledWithNullKey_ReturnsEmptyList()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = false,
                EncryptionKey = null
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().BeEmpty();
        }

        [Fact]
        public void Validate_AllPropertiesInvalid_ReturnsMultipleProblems()
        {
            // Arrange
            var settings = new AppSettings
            {
                DatabasePath = "/var/backups/db.sqlite",
                MaxConcurrentBackups = 5,
                BackupTimeoutSeconds = 7200,
                ScheduleCheckIntervalSeconds = 120,
                LogLevel = "Debug",
                RetentionDays = 60,
                MaxBackupCount = 20,
                LocalStoragePath = "/var/backups",
                NotificationEmails = new[] { "invalid-email", "   " },
                EnableEncryption = true,
                EncryptionKey = "invalid-base64"
            };

            // Act
            var problems = settings.Validate();

            // Assert
            problems.Should().HaveCount(3);
        }

        [Fact]
        public void IsValid_WithInvalidNotificationEmails_ReturnsFalse()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = new[] { "invalid-email" }
            };

            // Act
            var isValid = settings.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithInvalidEncryptionKey_ReturnsFalse()
        {
            // Arrange
            var settings = new AppSettings
            {
                EnableEncryption = true,
                EncryptionKey = "invalid-key"
            };

            // Act
            var isValid = settings.IsValid();

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void EnsureValid_WithInvalidSettings_ThrowsArgumentException()
        {
            // Arrange
            var settings = new AppSettings
            {
                NotificationEmails = new[] { "invalid-email" }
            };

            // Act
            Action act = () => settings.EnsureValid();

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }
}
