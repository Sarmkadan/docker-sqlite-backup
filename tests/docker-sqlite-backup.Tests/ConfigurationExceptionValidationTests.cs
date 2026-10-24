#nullable enable

using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class ConfigurationExceptionValidationTests
{
    [Fact]
    public void Validate_WithConfigurationKey_ReturnsNoProblems()
    {
        // Arrange
        var exception = new ConfigurationException("Backup:Schedule", "Invalid cron expression");

        // Act
        var problems = ConfigurationExceptionValidation.Validate(exception);

        // Assert
        problems.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WithNullOrEmptyKey_ReturnsSingleProblem(string? key)
    {
        // Arrange
        var exception = new ConfigurationException(key!, "Something failed");

        // Act
        var problems = ConfigurationExceptionValidation.Validate(exception);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Contain("ConfigurationKey");
    }

    [Fact]
    public void IsValid_WithConfigurationKey_ReturnsTrue()
    {
        // Arrange
        var exception = new ConfigurationException("Storage:Local:Path", "Directory missing");

        // Act
        var result = ConfigurationExceptionValidation.IsValid(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_WithNullOrEmptyKey_ReturnsFalse(string? key)
    {
        // Arrange
        var exception = new ConfigurationException(key!, "Something failed");

        // Act
        var result = ConfigurationExceptionValidation.IsValid(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithConfigurationKey_DoesNotThrow()
    {
        // Arrange
        var exception = new ConfigurationException("Backup:Schedule", "Invalid cron expression");

        // Act
        Action act = () => ConfigurationExceptionValidation.EnsureValid(exception);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EnsureValid_WithNullOrEmptyKey_ThrowsArgumentExceptionMentioningKey(string? key)
    {
        // Arrange
        var exception = new ConfigurationException(key!, "Something failed");

        // Act
        Action act = () => ConfigurationExceptionValidation.EnsureValid(exception);

        // Assert
        act.Should().Throw<DockerSqliteBackup.Exceptions.ArgumentException>()
            .Which.Message.Should().Contain("ConfigurationKey");
    }

    [Fact]
    public void Validate_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigurationException? exception = null;

        // Act
        Action act = () => ConfigurationExceptionValidation.Validate(exception);

        // Assert
        act.Should().Throw<DockerSqliteBackup.Exceptions.ArgumentNullException>()
            .Which.ParameterName.Should().Be("value");
    }

    [Fact]
    public void IsValid_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigurationException? exception = null;

        // Act
        Action act = () => ConfigurationExceptionValidation.IsValid(exception);

        // Assert
        act.Should().Throw<DockerSqliteBackup.Exceptions.ArgumentNullException>()
            .Which.ParameterName.Should().Be("value");
    }

    [Fact]
    public void EnsureValid_NullInstance_ThrowsArgumentNullException()
    {
        // Arrange
        ConfigurationException? exception = null;

        // Act
        Action act = () => ConfigurationExceptionValidation.EnsureValid(exception);

        // Assert
        act.Should().Throw<DockerSqliteBackup.Exceptions.ArgumentNullException>()
            .Which.ParameterName.Should().Be("value");
    }
}
