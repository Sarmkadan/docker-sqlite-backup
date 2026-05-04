// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Thrown when a backup operation fails or encounters an error.
/// </summary>
public class BackupException : Exception
{
    /// <summary>
    /// The backup job ID associated with this error, if available.
    /// </summary>
    public string? BackupJobId { get; set; }

    /// <summary>
    /// The backup snapshot ID associated with this error, if available.
    /// </summary>
    public string? SnapshotId { get; set; }

    /// <summary>
    /// Initializes a new instance of BackupException with a message.
    /// </summary>
    public BackupException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of BackupException with a message and inner exception.
    /// </summary>
    public BackupException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of BackupException with message and backup context.
    /// </summary>
    public BackupException(string message, string? backupJobId, string? snapshotId)
        : base(message)
    {
        BackupJobId = backupJobId;
        SnapshotId = snapshotId;
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and backup context.
    /// </summary>
    public BackupException(string message, Exception innerException, string? backupJobId, string? snapshotId)
        : base(message, innerException)
    {
        BackupJobId = backupJobId;
        SnapshotId = snapshotId;
    }
}
