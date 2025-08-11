#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for backup verification errors.
/// </summary>
public class VerificationException : Exception
{
    /// <summary>Gets the backup ID being verified.</summary>
    public Guid? BackupId { get; }

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
    public string? Errors { get; }

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
    public RestoreVerificationFailedException(string message, Guid backupId)
        : base(message, backupId) { }
}
