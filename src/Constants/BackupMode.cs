#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Constants;

/// <summary>
/// Defines the backup mode for a schedule.
/// </summary>
public enum BackupMode
{
    /// <summary>
    /// Full snapshot of the entire database on every run (default).
    /// Produces a self-contained, independently restorable file.
    /// </summary>
    Full = 0,

    /// <summary>
    /// Incremental backup that captures only pages changed since the last
    /// full or incremental backup. Uses the SQLite WAL (Write-Ahead Log)
    /// checkpoint mechanism to identify and copy changed pages.
    /// An incremental backup must be combined with its base snapshot to restore.
    /// </summary>
    Incremental = 1
}
