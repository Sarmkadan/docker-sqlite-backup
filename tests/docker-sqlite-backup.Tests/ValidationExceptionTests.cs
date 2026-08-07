using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Exceptions.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="ValidationException"/> class.
    /// </summary>
    public class ValidationExceptionTests
    {
        /// <summary>
        /// Verifies that the parameterless constructor creates a <see cref="ValidationException"/> with default values.
        /// </summary>
        [Fact]
        public void DefaultConstructor_CreatesInstance()
        {
            // Act
            var exception = new ValidationException();

            // Assert
            exception.Message.Should().NotBeNullOrEmpty();
            exception.ParameterName.Should().BeNull();
            exception.Errors.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the constructor with a message parameter creates a <see cref="ValidationException"/> with the specified message.
        /// </summary>
        [Fact]
        public void Constructor_WithMessage_CreatesInstanceWithMessage()
        {
            // Arrange
            var message = "Test validation error message";

            // Act
            var exception = new ValidationException(message);

            // Assert
            exception.Message.Should().Be(message);
            exception.ParameterName.Should().BeNull();
            exception.Errors.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the constructor with a message and inner exception creates a <see cref="ValidationException"/> with the specified message and inner exception.
        /// </summary>
        [Fact]
        public void Constructor_WithMessageAndInnerException_CreatesInstanceWithBoth()
        {
            // Arrange
            var message = "Test validation error message";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ValidationException(message, innerException);

            // Assert
            exception.Message.Should().Be(message);
            exception.InnerException.Should().BeSameAs(innerException);
            exception.ParameterName.Should().BeNull();
            exception.Errors.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the constructor with parameter name and message creates a <see cref="ValidationException"/> with the specified parameter name and formatted message.
        /// </summary>
        [Fact]
        public void Constructor_WithParameterNameAndMessage_CreatesInstanceWithParameterName()
        {
            // Arrange
            var parameterName = "username";
            var message = "Username is required";

            // Act
            var exception = new ValidationException(parameterName, message);

            // Assert
            exception.Message.Should().Be($"Validation failed for '{parameterName}': {message}");
            exception.ParameterName.Should().Be(parameterName);
            exception.Errors.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the constructor with an errors dictionary creates a <see cref="ValidationException"/> with the provided errors.
        /// </summary>
        [Fact]
        public void Constructor_WithErrorsDictionary_CreatesInstanceWithErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string>
            {
                { "username", "Username is required" },
                { "email", "Email is invalid" }
            };

            // Act
            var exception = new ValidationException(errors);

            // Assert
            exception.Message.Should().Contain("Validation failed:");
            exception.Message.Should().Contain("username: Username is required");
            exception.Message.Should().Contain("email: Email is invalid");
            exception.ParameterName.Should().BeNull();
            exception.Errors.Should().BeSameAs(errors);
        }

        /// <summary>
        /// Verifies that the constructor with parameter name, message, and inner exception creates a <see cref="ValidationException"/> with all properties set correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithParameterNameMessageAndInnerException_CreatesInstanceWithAll()
        {
            // Arrange
            var parameterName = "connectionString";
            var message = "Connection string cannot be empty";
            var innerException = new FormatException("Invalid format");

            // Act
            var exception = new ValidationException(parameterName, message, innerException);

            // Assert
            exception.Message.Should().Be($"Validation failed for '{parameterName}': {message}");
            exception.ParameterName.Should().Be(parameterName);
            exception.InnerException.Should().BeSameAs(innerException);
            exception.Errors.Should().BeNull();
        }

        /// <summary>
        /// Verifies that the <see cref="ValidationException.ParameterName"/> property returns the correct parameter name.
        /// </summary>
        [Fact]
        public void ParameterName_Getter_ReturnsCorrectValue()
        {
            // Arrange
            var parameterName = "testParam";
            var exception = new ValidationException(parameterName, "Test message");

            // Act & Assert
            exception.ParameterName.Should().Be(parameterName);
        }

        /// <summary>
        /// Verifies that the <see cref="ValidationException.Errors"/> property returns the correct dictionary of errors.
        /// </summary>
        [Fact]
        public void Errors_Getter_ReturnsCorrectDictionary()
        {
            // Arrange
            var errors = new Dictionary<string, string>
            {
                { "field1", "Error 1" },
                { "field2", "Error 2" }
            };
            var exception = new ValidationException(errors);

            // Act & Assert
            exception.Errors.Should().BeSameAs(errors);
        }
    }
}