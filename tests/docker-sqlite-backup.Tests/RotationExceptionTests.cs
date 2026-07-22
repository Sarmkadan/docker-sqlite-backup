using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class RotationExceptionTests
{
    [Fact]
    public void RotationException_Constructor_Default_ShouldInitialize()
    {
        // Act
        var exception = new RotationException();

        // Assert
        exception.Should().BeAssignableTo<DockerSqliteBackupException>();
        exception.Message.Should().NotBeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void RotationException_Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Rotation failed";

        // Act
        var exception = new RotationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void RotationException_Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Rotation failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new RotationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void RotationException_Constructor_WithNullMessage_ShouldHandleGracefully()
    {
        // Act
        var exception = new RotationException((string)null!);

        // Assert
        // System.Exception sets a default message if null is passed
        exception.Message.Should().NotBeNull();
    }

    [Fact]
    public void RotationException_Constructor_WithNullInnerException_ShouldSetInnerToNull()
    {
        // Act
        var exception = new RotationException("Test message", null!);

        // Assert
        exception.InnerException.Should().BeNull();
    }
}
