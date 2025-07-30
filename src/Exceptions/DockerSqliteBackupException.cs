#nullable enable

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Base exception for all DockerSqliteBackup-specific exceptions.
/// </summary>
public class DockerSqliteBackupException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class.
    /// </summary>
    public DockerSqliteBackupException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DockerSqliteBackupException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DockerSqliteBackupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
