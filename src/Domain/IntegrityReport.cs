#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Detailed report produced by an integrity check run against a SQLite database file.
/// Captures results from quick check, full integrity scan, foreign-key validation,
/// and key database metadata.
/// </summary>
public class IntegrityReport
{
    /// <summary>Gets or sets the unique identifier for this report.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the path of the database file that was checked.</summary>
    public string DatabasePath { get; set; } = string.Empty;

    /// <summary>Gets or sets when the check was performed.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the total time taken to complete all checks.</summary>
    public TimeSpan Duration { get; set; }

    // ── Quick check ──────────────────────────────────────────────────────────

    /// <summary>
    /// Whether the quick integrity check (<c>PRAGMA quick_check</c>) passed.
    /// Quick check verifies page structure without walking every record.
    /// </summary>
    public bool PassedQuickCheck { get; set; }

    /// <summary>Error details from the quick check, or <c>null</c> when the check passed.</summary>
    public string? QuickCheckErrors { get; set; }

    // ── Full integrity check ─────────────────────────────────────────────────

    /// <summary>
    /// Whether the full integrity check (<c>PRAGMA integrity_check</c>) passed.
    /// Full check walks every B-tree record and validates cell content.
    /// </summary>
    public bool PassedFullCheck { get; set; }

    /// <summary>Error details from the full integrity check, or <c>null</c> when the check passed.</summary>
    public string? FullCheckErrors { get; set; }

    // ── Foreign key check ────────────────────────────────────────────────────

    /// <summary>
    /// Whether the foreign key check (<c>PRAGMA foreign_key_check</c>) found no violations.
    /// </summary>
    public bool PassedForeignKeyCheck { get; set; }

    /// <summary>Description of any foreign key violations found, or <c>null</c> when none.</summary>
    public string? ForeignKeyErrors { get; set; }

    // ── Database metadata ────────────────────────────────────────────────────

    /// <summary>Total number of pages in the database (from <c>PRAGMA page_count</c>).</summary>
    public long PageCount { get; set; }

    /// <summary>Page size in bytes (from <c>PRAGMA page_size</c>).</summary>
    public long PageSize { get; set; }

    /// <summary>Number of free (unused) pages in the database.</summary>
    public long FreePageCount { get; set; }

    /// <summary>Journal mode of the database (e.g., WAL, DELETE, MEMORY).</summary>
    public string JournalMode { get; set; } = string.Empty;

    /// <summary>Whether the database has outstanding WAL frames that have not been checkpointed.</summary>
    public bool HasUncheckpointedWal { get; set; }

    /// <summary>Total number of user tables found in the database.</summary>
    public int TableCount { get; set; }

    // ── Derived state ────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when all three checks (quick, full, foreign-key) passed.
    /// </summary>
    public bool IsHealthy => PassedQuickCheck && PassedFullCheck && PassedForeignKeyCheck;

    /// <summary>
    /// Human-readable summary of the integrity report.
    /// </summary>
    public string Summary =>
        IsHealthy
            ? $"Database is healthy. Pages: {PageCount}, Tables: {TableCount}, Mode: {JournalMode}"
            : $"Database has integrity issues. Quick: {PassedQuickCheck}, Full: {PassedFullCheck}, FK: {PassedForeignKeyCheck}";
}
