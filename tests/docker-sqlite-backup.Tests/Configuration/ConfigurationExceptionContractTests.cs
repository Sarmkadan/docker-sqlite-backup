#nullable enable

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Configuration;

/// <summary>
/// Asserts that configuration validation failures use the <see cref="DockerSqliteBackupException"/> hierarchy,
/// so callers can catch backup-related failures uniformly.
/// </summary>
public class ConfigurationExceptionContractTests
{
    [Fact]
    public void EnsureValid_WithInvalidSettings_ThrowsInvalidConfigurationException()
    {
        // Arrange
        var settings = new AppSettings { NotificationEmails = ["not-an-email"] };

        // Act
        Action act = () => settings.EnsureValid();

        // Assert
        act.Should().Throw<DockerSqliteBackupException>()
            .Which.Should().BeOfType<InvalidConfigurationException>();
    }

    [Fact]
    public void Validate_WithEncryptionEnabledAndNoKey_ThrowsInvalidConfigurationWithKey()
    {
        // Arrange
        var settings = new AppSettings { EnableEncryption = true, EncryptionKey = null };

        // Act
        Action act = () => settings.Validate(throwOnError: true);

        // Assert
        act.Should().Throw<InvalidConfigurationException>()
            .Which.ConfigurationKey.Should().Be(nameof(AppSettings.EncryptionKey));
    }

    [Fact]
    public void Validate_WithNullNotificationEmails_ThrowsInvalidConfigurationWithKey()
    {
        // Arrange
        var settings = new AppSettings { NotificationEmails = null! };

        // Act
        Action act = () => settings.Validate(throwOnError: true);

        // Assert
        act.Should().Throw<DockerSqliteBackupException>()
            .Which.Should().BeOfType<InvalidConfigurationException>()
            .Which.ConfigurationKey.Should().Be(nameof(AppSettings.NotificationEmails));
    }
}
