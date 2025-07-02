// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Constants;

/// <summary>
/// Defines the type of storage destination for backups.
/// </summary>
public enum StorageType
{
    /// <summary>Store backups locally on the file system.</summary>
    Local = 0,

    /// <summary>Store backups in AWS S3.</summary>
    S3 = 1,

    /// <summary>Store backups in both local and S3 (redundancy).</summary>
    Both = 2
}
