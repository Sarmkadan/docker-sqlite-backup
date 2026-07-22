using System;
using System.Collections.Generic;
using DockerSqliteBackup.Exceptions;
using Xunit;
using ValidationException = DockerSqliteBackup.Exceptions.ValidationException;

namespace DockerSqliteBackup.Tests;

public class ValidationExceptionExtensionsTests
{
    [Fact]
    public void HasError_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ValidationException ex = null;
        string key = "testKey";

        // Act
        Action act = () => ex.HasError(key);

        // Assert
        var exception = Assert.Throws<global::System.ArgumentNullException>(act);
        Assert.Equal("ex", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasError_WithNullOrEmptyKey_ThrowsArgumentException(string invalidKey)
    {
        // Arrange
        var ex = new ValidationException("Test parameter", "Test message");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ex.HasError(invalidKey));
    }

    [Fact]
    public void HasError_WithEmptyErrorsDictionary_ReturnsFalse()
    {
        // Arrange
        var errors = new Dictionary<string, string>();
        var ex = new ValidationException(errors);

        // Act
        var result = ex.HasError("anyKey");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasError_WithValidationExceptionWithoutErrors_ReturnsFalse()
    {
        // Arrange
        var ex = new ValidationException("Test parameter", "Test message");

        // Act
        var result = ex.HasError("anyKey");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasError_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" }
        };
        var ex = new ValidationException(errors);

        // Act
        var result = ex.HasError("username");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasError_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" }
        };
        var ex = new ValidationException(errors);

        // Act
        var result = ex.HasError("password");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetError_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ValidationException ex = null;
        string key = "testKey";

        // Act
        Action act = () => ex.GetError(key);

        // Assert
        var exception = Assert.Throws<global::System.ArgumentNullException>(act);
        Assert.Equal("ex", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetError_WithNullOrEmptyKey_ThrowsArgumentException(string invalidKey)
    {
        // Arrange
        var ex = new ValidationException("Test parameter", "Test message");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => ex.GetError(invalidKey));
    }

    [Fact]
    public void GetError_WithEmptyErrorsDictionary_ReturnsNull()
    {
        // Arrange
        var errors = new Dictionary<string, string>();
        var ex = new ValidationException(errors);

        // Act
        var result = ex.GetError("anyKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetError_WithValidationExceptionWithoutErrors_ReturnsNull()
    {
        // Arrange
        var ex = new ValidationException("Test parameter", "Test message");

        // Act
        var result = ex.GetError("anyKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetError_WithExistingKey_ReturnsErrorMessage()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" }
        };
        var ex = new ValidationException(errors);

        // Act
        var result = ex.GetError("email");

        // Assert
        Assert.Equal("Email is invalid", result);
    }

    [Fact]
    public void GetError_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" }
        };
        var ex = new ValidationException(errors);

        // Act
        var result = ex.GetError("password");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ToDetailedString_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        ValidationException ex = null;

        // Act
        Action act = () => ex.ToDetailedString();

        // Assert
        var exception = Assert.Throws<global::System.ArgumentNullException>(act);
        Assert.Equal("ex", exception.ParamName);
    }

    [Fact]
    public void ToDetailedString_WithSimpleMessage_ReturnsFormattedString()
    {
        // Arrange
        var ex = new ValidationException("Test parameter", "Test message");

        // Act
        var result = ex.ToDetailedString();

        // Assert
        Assert.StartsWith("Validation failed: Validation failed for 'Test parameter': Test message", result);
        Assert.Contains("Parameter: Test parameter", result);
    }

    [Fact]
    public void ToDetailedString_WithErrors_ReturnsFormattedStringWithErrors()
    {
        // Arrange
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" }
        };
        var ex = new ValidationException(errors);

        // Act
        var result = ex.ToDetailedString();

        // Assert
        Assert.StartsWith("Validation failed:", result);
        Assert.Contains("Username is required", result);
        Assert.Contains("Email is invalid", result);
        Assert.Contains("- username: Username is required", result);
        Assert.Contains("- email: Email is invalid", result);
    }

    [Fact]
    public void ToDetailedString_WithEmptyErrors_ReturnsFormattedString()
    {
        // Arrange
        var errors = new Dictionary<string, string>();
        var ex = new ValidationException(errors);

        // Act
        var result = ex.ToDetailedString();

        // Assert
        Assert.StartsWith("Validation failed:", result);
        Assert.DoesNotContain("Errors:", result);
    }
}