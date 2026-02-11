#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Constants;

/// <summary>
/// Strategy for rotating and removing old backup files.
/// </summary>
public enum RotationStrategy
{
    /// <summary>Keep backups based on the maximum number of files.</summary>
    MaxFileCount = 0,

    /// <summary>Keep backups based on the maximum age in days.</summary>
    MaxAge = 1,

    /// <summary>Keep backups based on both file count and age.</summary>
    Combined = 2,

    /// <summary>Keep all backups indefinitely.</summary>
    NoRotation = 3
}
