// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Metadata about a SQLite database at the time of backup.
/// </summary>
public class BackupMetadata
{
    /// <summary>
    /// SQLite version of the database.
    /// </summary>
    public string? SqliteVersion { get; set; }

    /// <summary>
    /// Number of tables in the database.
    /// </summary>
    public int TableCount { get; set; }

    /// <summary>
    /// List of table names in the database.
    /// </summary>
    public List<string> TableNames { get; set; } = new();

    /// <summary>
    /// Total number of indexes in the database.
    /// </summary>
    public int IndexCount { get; set; }

    /// <summary>
    /// Whether the database has any views.
    /// </summary>
    public bool HasViews { get; set; }

    /// <summary>
    /// Number of views in the database.
    /// </summary>
    public int ViewCount { get; set; }

    /// <summary>
    /// Whether the database is in WAL (Write-Ahead Logging) mode.
    /// </summary>
    public bool IsWalMode { get; set; }

    /// <summary>
    /// Page size of the database in bytes.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Number of pages in the database.
    /// </summary>
    public long PageCount { get; set; }

    /// <summary>
    /// Free pages in the database.
    /// </summary>
    public long FreePages { get; set; }

    /// <summary>
    /// User version stored in the database.
    /// </summary>
    public int UserVersion { get; set; }

    /// <summary>
    /// Application ID stored in the database.
    /// </summary>
    public int ApplicationId { get; set; }

    /// <summary>
    /// Whether the database is encrypted.
    /// </summary>
    public bool IsEncrypted { get; set; }

    /// <summary>
    /// Integrity check result (PRAGMA integrity_check).
    /// </summary>
    public bool IntegrityCheckPassed { get; set; }

    /// <summary>
    /// Detailed integrity check output if check failed.
    /// </summary>
    public string? IntegrityCheckOutput { get; set; }

    /// <summary>
    /// Timestamp when metadata was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Host machine name where the backup was created.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Backup tool version that created this metadata.
    /// </summary>
    public string? ToolVersion { get; set; }

    /// <summary>
    /// Gets the estimated database size in bytes.
    /// </summary>
    public long GetEstimatedDatabaseSizeBytes()
    {
        return PageSize * PageCount;
    }

    /// <summary>
    /// Gets the percentage of utilized database pages.
    /// </summary>
    public double GetPageUtilizationPercentage()
    {
        if (PageCount == 0)
            return 0;

        var usedPages = PageCount - FreePages;
        return ((double)usedPages / PageCount) * 100;
    }

    /// <summary>
    /// Validates metadata integrity.
    /// </summary>
    /// <returns>True if metadata is valid, false otherwise.</returns>
    public bool IsValid()
    {
        if (PageSize <= 0 || PageCount < 0)
            return false;

        if (FreePages > PageCount)
            return false;

        return !string.IsNullOrWhiteSpace(SqliteVersion);
    }
}
