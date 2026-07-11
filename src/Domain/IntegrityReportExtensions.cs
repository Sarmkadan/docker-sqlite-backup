#nullable enable

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides extension methods for <see cref="IntegrityReport"/> to facilitate common operations
/// such as validation, formatting, and analysis of database integrity reports.
/// </summary>
public static class IntegrityReportExtensions
{
    /// <summary>
    /// Determines whether the database has any integrity issues across all checks.
    /// </summary>
    /// <param name="report">The integrity report to check.</param>
    /// <returns><c>true</c> if any check failed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static bool HasIntegrityIssues(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return !report.IsHealthy;
    }

    /// <summary>
    /// Gets the total size of the database file in bytes.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns>The total database size in bytes, or 0 if page count or page size is unavailable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static long GetDatabaseSizeInBytes(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.PageCount * report.PageSize;
    }

    /// <summary>
    /// Gets the percentage of free pages in the database.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns>The percentage of free pages (0-100), or 0 if page count is 0.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static double GetFreePagePercentage(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.PageCount == 0
            ? 0.0
            : (double)report.FreePageCount / report.PageCount * 100.0;
    }

    /// <summary>
    /// Gets a formatted string containing all error messages from the integrity report.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns>A formatted string with all error details, or an empty string if no errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static string GetAllErrors(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var errors = new List<string>();

        if (!report.PassedQuickCheck && !string.IsNullOrEmpty(report.QuickCheckErrors))
        {
            errors.Add($"Quick check errors: {report.QuickCheckErrors}");
        }

        if (!report.PassedFullCheck && !string.IsNullOrEmpty(report.FullCheckErrors))
        {
            errors.Add($"Full check errors: {report.FullCheckErrors}");
        }

        if (!report.PassedForeignKeyCheck && !string.IsNullOrEmpty(report.ForeignKeyErrors))
        {
            errors.Add($"Foreign key errors: {report.ForeignKeyErrors}");
        }

        return errors.Count == 0 ? string.Empty : string.Join(Environment.NewLine, errors);
    }

    /// <summary>
    /// Determines whether the database is using WAL (Write-Ahead Logging) mode.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns><c>true</c> if the journal mode is WAL; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static bool IsWalMode(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return string.Equals(report.JournalMode, "WAL", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a human-readable description of the database health status.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns>A formatted string describing the database health.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static string GetHealthStatus(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.IsHealthy
            ? $"✅ Database is healthy. Size: {report.GetDatabaseSizeInBytes():N0} bytes, Tables: {report.TableCount}, Mode: {report.JournalMode}"
            : $"❌ Database has integrity issues. Issues: {GetIssuesSummary(report)}. Size: {report.GetDatabaseSizeInBytes():N0} bytes";
    }

    /// <summary>
    /// Determines whether the database has low free space (more than 50% free pages).
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <param name="thresholdPercentage">The free page percentage threshold to consider as low space (default: 50).</param>
    /// <returns><c>true</c> if free pages exceed the threshold; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static bool HasLowFreeSpace(this IntegrityReport report, double thresholdPercentage = 50.0)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.GetFreePagePercentage() > thresholdPercentage;
    }

    /// <summary>
    /// Gets a summary of the database metadata in a standardized format.
    /// </summary>
    /// <param name="report">The integrity report.</param>
    /// <returns>A formatted string with key database metadata.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is <c>null</c>.</exception>
    public static string GetMetadataSummary(this IntegrityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return $"""
Database: {report.DatabasePath}
Checked: {report.CheckedAt:yyyy-MM-dd HH:mm:ss}
Duration: {report.Duration.TotalSeconds:N2}s
Size: {report.GetDatabaseSizeInBytes():N0} bytes ({report.PageCount} pages × {report.PageSize} bytes/page)
Free space: {report.FreePageCount} pages ({report.GetFreePagePercentage():N2}%)
Mode: {report.JournalMode}
Tables: {report.TableCount}
WAL uncheckpointed: {report.HasUncheckpointedWal}
""".Trim();
    }

    /// <summary>
    /// Gets a comma-separated summary of integrity issues found in the report.
    /// </summary>
    /// <param name="report">The integrity report to analyze.</param>
    /// <returns>A formatted string listing all issues, or an empty string if none.</returns>
    private static string GetIssuesSummary(IntegrityReport report)
    {
        var issues = new List<string>();
        if (!report.PassedQuickCheck) issues.Add("quick check failed");
        if (!report.PassedFullCheck) issues.Add("full integrity check failed");
        if (!report.PassedForeignKeyCheck) issues.Add("foreign key violations detected");
        return issues.Count == 0 ? string.Empty : string.Join(", ", issues);
    }
}