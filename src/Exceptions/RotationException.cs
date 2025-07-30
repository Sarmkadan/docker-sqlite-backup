#nullable enable

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown when rotation-related errors occur.
/// </summary>
public class RotationException : DockerSqliteBackupException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RotationException"/> class.
    /// </summary>
    public RotationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RotationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public RotationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
