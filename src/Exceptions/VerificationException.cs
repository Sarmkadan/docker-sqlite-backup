#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for backup verification errors.
/// </summary>
public class VerificationException : DockerSqliteBackupException
{
    /// <summary>
    /// Initializes a new instance of the VerificationException class.
    /// </summary>
    public VerificationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with backup ID.
    /// </summary>
    public VerificationException(string message, Guid backupId) : base(message)
    {
        BackupId = backupId;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public VerificationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when database integrity check fails.
/// </summary>
public class IntegrityCheckFailedException : VerificationException
{
    /// <summary>
    /// Gets the raw error output reported by the integrity check, or <c>null</c> if none was captured.
    /// </summary>
    public string? Errors { get; }

    /// <summary>
    /// Initializes a new instance of the IntegrityCheckFailedException class.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="backupId">The identifier of the backup that failed the integrity check.</param>
    /// <param name="errors">The raw error output from the integrity check, if available.</param>
    public IntegrityCheckFailedException(string message, Guid backupId, string? errors = null)
        : base(message, backupId)
    {
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when restore verification fails.
/// </summary>
public class RestoreVerificationFailedException : VerificationException
{
    /// <summary>
    /// Initializes a new instance of the RestoreVerificationFailedException class.
    /// </summary>
    /// <param name="message">The error message describing the failed restore verification.</param>
    /// <param name="backupId">The identifier of the backup whose restore verification failed.</param>
    public RestoreVerificationFailedException(string message, Guid backupId)
        : base(message, backupId) { }
}
