using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests;

    /// <summary>
    /// Unit tests for <see cref="DockerSqliteBackupException"/>, the base exception of the backup tool.
    /// Verifies the constructors, the <c>Message</c> and <c>InnerException</c> handling, the base type relationship, and that the exception can be thrown and caught as its own type.
    /// </summary>
public class DockerSqliteBackupExceptionTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates a usable exception with a non-empty default message and no inner exception.
    /// </summary>
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

    /// <summary>
    /// Verifies that the message constructor stores the supplied message unchanged and leaves <c>InnerException</c> null.
    /// </summary>
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

    /// <summary>
    /// Verifies that the message constructor accepts null, empty, and whitespace-only messages without throwing, and leaves <c>InnerException</c> null.
    /// The <c>Message</c> is asserted only to be non-null, because <see cref="System.Exception.Message"/> substitutes a default text when no message is given.
    /// </summary>
    /// <param name="message">The message value under test: null, empty, or whitespace.</param>
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

    /// <summary>
    /// Verifies that the message and inner exception constructor stores both the message and the exact inner exception instance.
    /// </summary>
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

    /// <summary>
    /// Verifies that the message and inner exception constructor accepts empty or whitespace-only messages and still stores the supplied inner exception instance.
    /// </summary>
    /// <param name="message">The message value under test: empty or whitespace.</param>
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

    /// <summary>
    /// Verifies that the message and inner exception constructor stores the message and keeps <c>InnerException</c> null when a null inner exception is passed.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="DockerSqliteBackupException"/> derives from <see cref="Exception"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that the <c>Message</c> property returns the text passed to the message constructor.
    /// </summary>
    [Fact]
    public void DockerSqliteBackupException_MessageProperty_ShouldBeReadable()
    {
        // Arrange
        var message = "Test error message";
        var exception = new DockerSqliteBackupException(message);

        // Act & Assert
        exception.Message.Should().Be(message);
    }

    /// <summary>
    /// Verifies that the <c>InnerException</c> property returns the exact inner exception instance passed to the constructor.
    /// </summary>
    [Fact]
    public void DockerSqliteBackupException_InnerExceptionProperty_ShouldBeReadable()
    {
        // Arrange
        var innerException = new TimeoutException("Timeout occurred");
        var exception = new DockerSqliteBackupException("Test message", innerException);

        // Act & Assert
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
    /// Verifies that <c>ToString()</c> output includes the outer message, the inner exception type name, and the inner exception message.
    /// </summary>
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

    /// <summary>
    /// Verifies that the exception can be thrown and caught as <see cref="Exception"/>, keeps its concrete type, and keeps its message.
    /// </summary>
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

    /// <summary>
    /// Verifies that an <see cref="AggregateException"/> passed as the inner exception is stored by reference and keeps its type.
    /// </summary>
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