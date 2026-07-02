#nullable enable

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown when validation of input data fails.
/// </summary>
public class ValidationException : DockerSqliteBackupException
{
    /// <summary>
    /// Gets the name of the validated object or parameter.
    /// </summary>
    public string? ParameterName { get; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a parameter name and message.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    public ValidationException(string parameterName, string message)
        : base($"Validation failed for '{parameterName}': {message}")
    {
        ParameterName = parameterName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with validation errors.
    /// </summary>
    /// <param name="errors">A dictionary of validation errors (property/field name -> error message).</param>
    public ValidationException(IReadOnlyDictionary<string, string> errors)
        : base(FormatValidationErrors(errors))
    {
        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a parameter name, message, and inner exception.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ValidationException(string parameterName, string message, Exception innerException)
        : base($"Validation failed for '{parameterName}': {message}", innerException)
    {
        ParameterName = parameterName;
    }

    private static string FormatValidationErrors(IReadOnlyDictionary<string, string> errors)
    {
        var errorMessages = errors.Select(e => $"{e.Key}: {e.Value}");
        return $"Validation failed:\n- " + string.Join("\n- ", errorMessages);
    }
}

/// <summary>
/// Exception thrown when a required argument is null.
/// </summary>
public class ArgumentNullException : ValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentNullException"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that was null.</param>
    public ArgumentNullException(string parameterName)
        : base(parameterName, $"The argument '{parameterName}' cannot be null.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentNullException"/> class with a custom message.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that was null.</param>
    /// <param name="message">Custom error message.</param>
    public ArgumentNullException(string parameterName, string message)
        : base(parameterName, message)
    {
    }
}

/// <summary>
/// Exception thrown when an argument value is invalid.
/// </summary>
public class ArgumentException : ValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentException"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that caused the exception.</param>
    /// <param name="message">The error message.</param>
    public ArgumentException(string parameterName, string message)
        : base(parameterName, message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArgumentException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="parameterName">The name of the parameter that caused the exception.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ArgumentException(string parameterName, string message, Exception innerException)
        : base(parameterName, message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a collection or string is null or empty.
/// </summary>
public class EmptyCollectionException : ValidationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyCollectionException"/> class.
    /// </summary>
    /// <param name="parameterName">The name of the collection parameter.</param>
    public EmptyCollectionException(string parameterName)
        : base(parameterName, $"The collection '{parameterName}' cannot be null or empty.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyCollectionException"/> class with a custom message.
    /// </summary>
    /// <param name="parameterName">The name of the collection parameter.</param>
    /// <param name="message">Custom error message.</param>
    public EmptyCollectionException(string parameterName, string message)
        : base(parameterName, message)
    {
    }
}
