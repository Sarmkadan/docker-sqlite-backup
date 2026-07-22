using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class DockerSqliteBackupExceptionTests
{
    [Fact]
    public void DockerSqliteBackupException_DefaultConstructor_ShouldCreateException()
    {
        // Act
        var exception = new DockerSqliteBackupException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void DockerSqliteBackupException_Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test backup error message";

        // Act
        var exception = new DockerSqliteBackupException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void DockerSqliteBackupException_Constructor_WithEmptyOrNullMessage_ShouldHandleGracefully(string message)
    {
        // Act
        var exception = new DockerSqliteBackupException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNull(); // Exception.Message is never null
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void DockerSqliteBackupException_Constructor_WithMessageAndInnerException_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "Test backup error with inner exception";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DockerSqliteBackupException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DockerSqliteBackupException_Constructor_WithNullOrEmptyMessageAndInnerException_ShouldHandleGracefully(string message)
    {
        // Arrange
        var innerException = new IOException("IO error");

        // Act
        var exception = new DockerSqliteBackupException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void DockerSqliteBackupException_Constructor_WithNullInnerException_ShouldSetMessageAndNullInnerException()
    {
        // Arrange
        var message = "Test backup error with null inner exception";

        // Act
        var exception = new DockerSqliteBackupException(message, null);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void DockerSqliteBackupException_InheritsFromException()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new DockerSqliteBackupException(message);

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void DockerSqliteBackupException_MessageProperty_ShouldBeReadable()
    {
        // Arrange
        var message = "Test error message";
        var exception = new DockerSqliteBackupException(message);

        // Act & Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void DockerSqliteBackupException_InnerExceptionProperty_ShouldBeReadable()
    {
        // Arrange
        var innerException = new TimeoutException("Timeout occurred");
        var exception = new DockerSqliteBackupException("Test message", innerException);

        // Act & Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void DockerSqliteBackupException_ToString_ShouldIncludeAllInformation()
    {
        // Arrange
        var message = "Test backup error";
        var innerException = new InvalidOperationException("Inner error");
        var exception = new DockerSqliteBackupException(message, innerException);

        // Act
        var toStringResult = exception.ToString();

        // Assert
        toStringResult.Should().Contain(message);
        toStringResult.Should().Contain("InvalidOperationException");
        toStringResult.Should().Contain("Inner error");
    }

    [Fact]
    public void DockerSqliteBackupException_CanBeCaughtAsBaseException()
    {
        // Arrange
        var message = "Test backup error";
        Exception exception = null;

        // Act
        try
        {
            throw new DockerSqliteBackupException(message);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        exception.Should().NotBeNull();
        exception.Should().BeAssignableTo<DockerSqliteBackupException>();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void DockerSqliteBackupException_WithComplexInnerException_ShouldPreserveStackTrace()
    {
        // Arrange
        var innerExceptions = new Exception[] { new InvalidOperationException("First error"), new IOException("Second error") };
        var innerException = new AggregateException("Multiple errors", innerExceptions);
        var message = "Backup operation failed";

        // Act
        var exception = new DockerSqliteBackupException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.InnerException.Should().BeAssignableTo<AggregateException>();
    }
}