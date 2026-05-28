#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for performing deep integrity checks on SQLite database files.
/// Combines quick scans, full page-level validation, and foreign key audits
/// into a single <see cref="IntegrityReport"/>.
/// </summary>
public interface IIntegrityCheckerService
{
    /// <summary>
    /// Runs a comprehensive integrity check on the database at <paramref name="databasePath"/>.
    /// When <paramref name="fullCheck"/> is <c>true</c> (the default) all three checks are
    /// performed: quick check, full integrity check, and foreign key check.
    /// When <c>false</c>, only the quick check is executed for speed.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite file to inspect.</param>
    /// <param name="fullCheck">
    /// <c>true</c> to run all checks (may be slow on very large databases);
    /// <c>false</c> for a quick structural-only scan.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A populated <see cref="IntegrityReport"/> instance.</returns>
    Task<IntegrityReport> CheckDatabaseAsync(
        string databasePath,
        bool fullCheck = true,
        CancellationToken ct = default);

    /// <summary>
    /// Runs a fast structural check only (<c>PRAGMA quick_check</c>).
    /// Use this for frequent polling where full scan latency is unacceptable.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the quick check passes; <c>false</c> otherwise.</returns>
    Task<bool> QuickCheckAsync(string databasePath, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the integrity history for a specific backup result by running the
    /// integrity checker against the backup file path recorded in the result.
    /// </summary>
    /// <param name="backupFilePath">Local path to the backup file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A populated <see cref="IntegrityReport"/> instance.</returns>
    Task<IntegrityReport> CheckBackupFileAsync(string backupFilePath, CancellationToken ct = default);
}
