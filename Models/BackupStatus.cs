// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Enumeration of possible backup statuses throughout the backup lifecycle.
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// Backup is pending execution.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Backup is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Backup completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Backup failed during execution.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Backup was cancelled by user or system.
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Backup verification failed.
    /// </summary>
    VerificationFailed = 5
}
