// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Constants;

/// <summary>
/// Represents the status of a backup operation.
/// </summary>
public enum BackupStatus
{
    /// <summary>The backup is pending and has not yet been started.</summary>
    Pending = 0,

    /// <summary>The backup is currently in progress.</summary>
    InProgress = 1,

    /// <summary>The backup has completed successfully.</summary>
    Success = 2,

    /// <summary>The backup has failed.</summary>
    Failed = 3,

    /// <summary>The backup was cancelled before completion.</summary>
    Cancelled = 4,

    /// <summary>The backup is verified and ready for restore.</summary>
    VerifiedSuccess = 5,

    /// <summary>The backup failed verification.</summary>
    VerificationFailed = 6
}
