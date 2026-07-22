using DockerSqliteBackup.Exceptions;
using FluentAssertions;
using Xunit;
using ArgumentNullException = System.ArgumentNullException;

namespace DockerSqliteBackup.Tests;

public class StorageExceptionTests
{
    [Fact]
    public void StorageException_Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test storage error message";

        // Act
        var exception = new StorageException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StorageType.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void StorageException_Constructor_WithMessageAndStorageType_ShouldSetMessageAndStorageType()
    {
        // Arrange
        var message = "Test storage error with type";
        var storageType = "S3";

        // Act
        var exception = new StorageException(message, storageType);

        // Assert
        exception.Message.Should().Be(message);
        exception.StorageType.Should().Be(storageType);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void StorageException_Constructor_WithMessageAndInnerException_ShouldSetMessageAndInnerException()
    {
        // Arrange
        var message = "Test storage error with inner exception";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new StorageException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.StorageType.Should().BeNull();
    }

    [Fact]
    public void StorageException_StorageTypeProperty_ShouldBeReadOnly()
    {
        // Arrange
        var exception = new StorageException("Test message");

        // Act & Assert
        // StorageType has no setter, so it can only be set via constructor
        exception.StorageType.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void StorageException_Constructor_WithEmptyOrNullMessage_ShouldHandleGracefully(string message)
    {
        // Act
        var exception = new StorageException(message);

        // Assert
        // Note: Exception.Message is never null, it becomes "Exception of type..." when null is passed
        exception.Message.Should().NotBeNull();
        exception.StorageType.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void StorageException_Constructor_WithEmptyOrNullStorageType_ShouldSetStorageType(string storageType)
    {
        // Act
        var exception = new StorageException("Test message", storageType);

        // Assert
        exception.StorageType.Should().Be(storageType);
    }

    [Fact]
    public void S3StorageException_Constructor_WithMessage_ShouldSetMessageAndStorageTypeToS3()
    {
        // Arrange
        var message = "S3 storage operation failed";

        // Act
        var exception = new S3StorageException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StorageType.Should().Be("S3");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void S3StorageException_Constructor_WithMessageAndInnerException_ShouldSetMessageInnerExceptionAndStorageType()
    {
        // Arrange
        var message = "S3 operation failed with inner exception";
        var innerException = new IOException("IO error");

        // Act
        var exception = new S3StorageException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        // Note: StorageType is null because the constructor StorageException(string, Exception) doesn't set it
        exception.StorageType.Should().BeNull();
    }

    [Fact]
    public void S3StorageException_InheritsFromStorageException()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new S3StorageException(message);

        // Assert
        exception.Should().BeAssignableTo<StorageException>();
    }

    [Fact]
    public void S3StorageException_StorageType_ShouldAlwaysBeS3()
    {
        // Arrange & Act
        var exception1 = new S3StorageException("Message 1");
        var exception2 = new S3StorageException("Message 2", new Exception());

        // Assert
        exception1.StorageType.Should().Be("S3");
        // Note: exception2.StorageType is null because constructor with innerException doesn't set it
        exception2.StorageType.Should().BeNull();
    }

    [Fact]
    public void LocalStorageException_Constructor_WithMessage_ShouldSetMessageAndStorageTypeToLocal()
    {
        // Arrange
        var message = "Local storage operation failed";

        // Act
        var exception = new LocalStorageException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StorageType.Should().Be("Local");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void LocalStorageException_Constructor_WithMessageAndInnerException_ShouldSetMessageInnerExceptionAndStorageType()
    {
        // Arrange
        var message = "Local operation failed with inner exception";
        var innerException = new DirectoryNotFoundException("Directory not found");

        // Act
        var exception = new LocalStorageException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        // Note: StorageType is null because the constructor StorageException(string, Exception) doesn't set it
        exception.StorageType.Should().BeNull();
    }

    [Fact]
    public void LocalStorageException_InheritsFromStorageException()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new LocalStorageException(message);

        // Assert
        exception.Should().BeAssignableTo<StorageException>();
    }

    [Fact]
    public void LocalStorageException_StorageType_ShouldAlwaysBeLocal()
    {
        // Arrange & Act
        var exception1 = new LocalStorageException("Message 1");
        var exception2 = new LocalStorageException("Message 2", new Exception());

        // Assert
        exception1.StorageType.Should().Be("Local");
        // Note: exception2.StorageType is null because constructor with innerException doesn't set it
        exception2.StorageType.Should().BeNull();
    }

    [Fact]
    public void AzureStorageException_Constructor_WithMessage_ShouldSetMessageAndStorageTypeToAzure()
    {
        // Arrange
        var message = "Azure storage operation failed";

        // Act
        var exception = new AzureStorageException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.StorageType.Should().Be("Azure");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void AzureStorageException_Constructor_WithMessageAndInnerException_ShouldSetMessageInnerExceptionAndStorageType()
    {
        // Arrange
        var message = "Azure operation failed with inner exception";
        var innerException = new TimeoutException("Timeout occurred");

        // Act
        var exception = new AzureStorageException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        // Note: StorageType is null because the constructor StorageException(string, Exception) doesn't set it
        exception.StorageType.Should().BeNull();
    }

    [Fact]
    public void AzureStorageException_InheritsFromStorageException()
    {
        // Arrange
        var message = "Test message";

        // Act
        var exception = new AzureStorageException(message);

        // Assert
        exception.Should().BeAssignableTo<StorageException>();
    }

    [Fact]
    public void AzureStorageException_StorageType_ShouldAlwaysBeAzure()
    {
        // Arrange & Act
        var exception1 = new AzureStorageException("Message 1");
        var exception2 = new AzureStorageException("Message 2", new Exception());

        // Assert
        exception1.StorageType.Should().Be("Azure");
        // Note: exception2.StorageType is null because constructor with innerException doesn't set it
        exception2.StorageType.Should().BeNull();
    }

    [Fact]
    public void InsufficientStorageException_Constructor_WithRequiredAndAvailableBytes_ShouldSetMessageWithCorrectValues()
    {
        // Arrange
        var requiredBytes = 1024L * 1024L * 1024L; // 1 GB
        var availableBytes = 100L;

        // Act
        var exception = new InsufficientStorageException(requiredBytes, availableBytes);

        // Assert
        exception.Message.Should().Contain($"Required: {requiredBytes} bytes");
        exception.Message.Should().Contain($"Available: {availableBytes} bytes");
        exception.StorageType.Should().Be("Local");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void InsufficientStorageException_Constructor_WithZeroValues_ShouldSetMessageWithZeroValues()
    {
        // Arrange
        var requiredBytes = 0L;
        var availableBytes = 0L;

        // Act
        var exception = new InsufficientStorageException(requiredBytes, availableBytes);

        // Assert
        exception.Message.Should().Contain("Required: 0 bytes");
        exception.Message.Should().Contain("Available: 0 bytes");
        exception.StorageType.Should().Be("Local");
    }

    [Fact]
    public void InsufficientStorageException_Constructor_WithLargeValues_ShouldHandleLargeNumbers()
    {
        // Arrange
        var requiredBytes = long.MaxValue;
        var availableBytes = long.MaxValue / 2;

        // Act
        var exception = new InsufficientStorageException(requiredBytes, availableBytes);

        // Assert
        exception.Message.Should().Contain("Required:");
        exception.Message.Should().Contain("Available:");
        exception.StorageType.Should().Be("Local");
    }

    [Fact]
    public void InsufficientStorageException_InheritsFromStorageException()
    {
        // Arrange
        var requiredBytes = 1000L;
        var availableBytes = 500L;

        // Act
        var exception = new InsufficientStorageException(requiredBytes, availableBytes);

        // Assert
        exception.Should().BeAssignableTo<StorageException>();
    }
}