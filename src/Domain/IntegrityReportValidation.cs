#nullable enable

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides validation helpers for <see cref="IntegrityReport"/> instances.
/// </summary>
public static class IntegrityReportValidation
{
    /// <summary>
    /// Validates an <see cref="IntegrityReport"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The report to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the report is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Validate(this IntegrityReport value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (value.Id == Guid.Empty)
        {
            problems.Add("Id must not be empty (Guid.Empty).");
        }

        // Validate DatabasePath
        if (string.IsNullOrWhiteSpace(value.DatabasePath))
        {
            problems.Add("DatabasePath must not be null or whitespace.");
        }
        else if (value.DatabasePath.Length > 4096)
        {
            problems.Add("DatabasePath exceeds maximum length of 4096 characters.");
        }

        // Validate CheckedAt
        if (value.CheckedAt == default)
        {
            problems.Add("CheckedAt must be set to a non-default DateTime value.");
        }
        else if (value.CheckedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("CheckedAt cannot be in the future.");
        }

        // Validate Duration
        if (value.Duration < TimeSpan.Zero)
        {
            problems.Add("Duration must not be negative.");
        }

        // Validate PassedQuickCheck
        // No validation needed - boolean can always be set

        // Validate QuickCheckErrors
        if (value.PassedQuickCheck && value.QuickCheckErrors is not null)
        {
            problems.Add("QuickCheckErrors must be null when PassedQuickCheck is true.");
        }

        if (value.QuickCheckErrors is not null && value.QuickCheckErrors.Length > 10_000)
        {
            problems.Add("QuickCheckErrors exceeds maximum length of 10000 characters.");
        }

        // Validate PassedFullCheck
        // No validation needed - boolean can always be set

        // Validate FullCheckErrors
        if (value.PassedFullCheck && value.FullCheckErrors is not null)
        {
            problems.Add("FullCheckErrors must be null when PassedFullCheck is true.");
        }

        if (value.FullCheckErrors is not null && value.FullCheckErrors.Length > 10_000)
        {
            problems.Add("FullCheckErrors exceeds maximum length of 10000 characters.");
        }

        // Validate PassedForeignKeyCheck
        // No validation needed - boolean can always be set

        // Validate ForeignKeyErrors
        if (value.PassedForeignKeyCheck && value.ForeignKeyErrors is not null)
        {
            problems.Add("ForeignKeyErrors must be null when PassedForeignKeyCheck is true.");
        }

        if (value.ForeignKeyErrors is not null && value.ForeignKeyErrors.Length > 10_000)
        {
            problems.Add("ForeignKeyErrors exceeds maximum length of 10000 characters.");
        }

        // Validate PageCount
        if (value.PageCount < 0)
        {
            problems.Add("PageCount must not be negative.");
        }

        // Validate PageSize
        if (value.PageSize < 512 || value.PageSize > 65536)
        {
            problems.Add("PageSize must be between 512 and 65536 bytes (SQLite page size limits).");
        }

        // Validate FreePageCount
        if (value.FreePageCount < 0)
        {
            problems.Add("FreePageCount must not be negative.");
        }
        else if (value.FreePageCount > value.PageCount)
        {
            problems.Add("FreePageCount cannot exceed PageCount.");
        }

        // Validate JournalMode
        if (string.IsNullOrWhiteSpace(value.JournalMode))
        {
            problems.Add("JournalMode must not be null or whitespace.");
        }
        else if (value.JournalMode.Length > 100)
        {
            problems.Add("JournalMode exceeds maximum length of 100 characters.");
        }
        else if (!IsValidJournalMode(value.JournalMode))
        {
            problems.Add($"JournalMode '{value.JournalMode}' is not a valid SQLite journal mode. Valid modes: DELETE, TRUNCATE, PERSIST, MEMORY, WAL, OFF.");
        }

        // Validate HasUncheckpointedWal
        // No validation needed - boolean can always be set

        // Validate TableCount
        if (value.TableCount < 0)
        {
            problems.Add("TableCount must not be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an <see cref="IntegrityReport"/> instance is valid.
    /// </summary>
    /// <param name="value">The report to check.</param>
    /// <returns><c>true</c> if the report is valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static bool IsValid(this IntegrityReport value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that an <see cref="IntegrityReport"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <param name="value">The report to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the report is invalid, containing a list of all problems.</exception>
    public static void EnsureValid(this IntegrityReport value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"IntegrityReport is invalid. Problems: {string.Join("; ", problems)}", nameof(value));
    }

    /// <summary>
    /// Determines whether the specified journal mode string is a valid SQLite journal mode.
    /// </summary>
    /// <param name="mode">The journal mode to validate.</param>
    /// <returns><c>true</c> if the mode is valid; otherwise, <c>false</c>.</returns>
    private static bool IsValidJournalMode(string mode)
    {
        return mode is "DELETE" or "TRUNCATE" or "PERSIST" or "MEMORY" or "WAL" or "OFF";
    }
}