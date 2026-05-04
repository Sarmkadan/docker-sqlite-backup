// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Enumeration of supported storage backends for backup artifacts.
/// </summary>
public enum StorageType
{
    /// <summary>
    /// Local filesystem storage.
    /// </summary>
    Local = 0,

    /// <summary>
    /// Amazon S3 or S3-compatible storage.
    /// </summary>
    S3 = 1,

    /// <summary>
    /// Google Cloud Storage.
    /// </summary>
    GCS = 2,

    /// <summary>
    /// Azure Blob Storage.
    /// </summary>
    AzureBlob = 3
}
