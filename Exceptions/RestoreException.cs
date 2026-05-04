// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Thrown when a database restore operation fails or encounters an error.
/// </summary>
public class RestoreException : Exception
{
    /// <summary>
    /// The restore point ID associated with this error, if available.
    /// </summary>
    public string? RestorePointId { get; set; }

    /// <summary>
    /// The target database path, if available.
    /// </summary>
    public string? TargetDatabasePath { get; set; }

    /// <summary>
    /// Initializes a new instance of RestoreException with a message.
    /// </summary>
    public RestoreException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of RestoreException with a message and inner exception.
    /// </summary>
    public RestoreException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of RestoreException with message and restore context.
    /// </summary>
    public RestoreException(string message, string? restorePointId, string? targetPath)
        : base(message)
    {
        RestorePointId = restorePointId;
        TargetDatabasePath = targetPath;
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and restore context.
    /// </summary>
    public RestoreException(string message, Exception innerException, string? restorePointId, string? targetPath)
        : base(message, innerException)
    {
        RestorePointId = restorePointId;
        TargetDatabasePath = targetPath;
    }
}
