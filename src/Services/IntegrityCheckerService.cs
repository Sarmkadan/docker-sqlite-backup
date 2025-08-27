#nullable enable
// Author: Vladyslav Zaiets

using System.Text;
using DockerSqliteBackup.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Performs deep integrity checks on SQLite database files using built-in PRAGMA
/// commands. Each check runs inside a read-only connection so the source file is
/// never modified. Three levels of checking are supported:
/// <list type="bullet">
///   <item><description><c>PRAGMA quick_check</c> — fast structural scan (B-tree pointers, page sizes).</description></item>
///   <item><description><c>PRAGMA integrity_check</c> — full record-level validation of every B-tree cell.</description></item>
///   <item><description><c>PRAGMA foreign_key_check</c> — verifies all FK constraints are satisfied.</description></item>
/// </list>
/// </summary>
public class IntegrityCheckerService : IIntegrityCheckerService
{
    private readonly ILogger<IntegrityCheckerService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="IntegrityCheckerService"/>.
    /// </summary>
    public IntegrityCheckerService(ILogger<IntegrityCheckerService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IntegrityReport> CheckDatabaseAsync(
        string databasePath,
        bool fullCheck = true,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!File.Exists(databasePath))
        {
            _logger.LogWarning("Database file not found: {DatabasePath}", databasePath);
            throw new FileNotFoundException($"Database file not found: {databasePath}", databasePath);
        }

        var started = DateTime.UtcNow;
        var report = new IntegrityReport { DatabasePath = databasePath, CheckedAt = started };

        _logger.LogInformation(
            "Starting integrity check on {DatabasePath} (fullCheck={FullCheck})",
            databasePath, fullCheck);

        var cs = $"Data Source={databasePath};Mode=ReadOnly;";
        using var connection = new SqliteConnection(cs);
        try
        {
            await connection.OpenAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open database connection: {DatabasePath}", databasePath);
            throw;
        }

        // 1. Collect database metadata.
        try
        {
            await PopulateMetadataAsync(connection, report, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect database metadata: {DatabasePath}", databasePath);
            throw;
        }

        // 2. Quick check — always performed.
        try
        {
            (report.PassedQuickCheck, report.QuickCheckErrors) =
                await RunPragmaCheckAsync(connection, "quick_check", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run quick check: {DatabasePath}", databasePath);
            throw;
        }

        if (fullCheck)
        {
            // 3. Full integrity check.
            try
            {
                (report.PassedFullCheck, report.FullCheckErrors) =
                    await RunPragmaCheckAsync(connection, "integrity_check", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run full integrity check: {DatabasePath}", databasePath);
                throw;
            }

            // 4. Foreign key check.
            try
            {
                (report.PassedForeignKeyCheck, report.ForeignKeyErrors) =
                    await RunForeignKeyCheckAsync(connection, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run foreign key check: {DatabasePath}", databasePath);
                throw;
            }
        }
        else
        {
            // Skip full checks — treat as passed in quick-only mode.
            report.PassedFullCheck = report.PassedQuickCheck;
            report.PassedForeignKeyCheck = true;
        }

        report.Duration = DateTime.UtcNow - started;

        _logger.LogInformation(
            "Integrity check completed in {Elapsed}ms. Healthy={Healthy}",
            (long)report.Duration.TotalMilliseconds, report.IsHealthy);

        return report;
    }

    /// <inheritdoc />
    public async Task<bool> QuickCheckAsync(string databasePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        if (!File.Exists(databasePath))
            throw new FileNotFoundException($"Database file not found: {databasePath}", databasePath);

        var cs = $"Data Source={databasePath};Mode=ReadOnly;";
        using var connection = new SqliteConnection(cs);
        await connection.OpenAsync(ct);

        var (passed, _) = await RunPragmaCheckAsync(connection, "quick_check", ct);
        return passed;
    }

    /// <inheritdoc />
    public async Task<IntegrityReport> CheckBackupFileAsync(
        string backupFilePath,
        CancellationToken ct = default)
    {
        return await CheckDatabaseAsync(backupFilePath, fullCheck: true, ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static async Task PopulateMetadataAsync(
        SqliteConnection connection,
        IntegrityReport report,
        CancellationToken ct)
    {
        report.PageSize = await ReadScalarLongAsync(connection, "PRAGMA page_size", ct);
        report.PageCount = await ReadScalarLongAsync(connection, "PRAGMA page_count", ct);
        report.FreePageCount = await ReadScalarLongAsync(connection, "PRAGMA freelist_count", ct);
        report.JournalMode = await ReadScalarStringAsync(connection, "PRAGMA journal_mode", ct);

        // Count user tables.
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table'";
        report.TableCount = (int)(long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);

        // Detect uncheckpointed WAL frames.
        if (string.Equals(report.JournalMode, "wal", StringComparison.OrdinalIgnoreCase))
        {
            // wal_checkpoint returns (busy, log, checkpointed); if log > checkpointed there are
            // outstanding frames that have not been applied to the main database file.
            using var walCmd = connection.CreateCommand();
            walCmd.CommandText = "PRAGMA wal_checkpoint(PASSIVE)";
            using var walReader = await walCmd.ExecuteReaderAsync(ct);
            if (await walReader.ReadAsync(ct))
            {
                var log = walReader.IsDBNull(1) ? 0 : walReader.GetInt64(1);
                var checkpointed = walReader.IsDBNull(2) ? 0 : walReader.GetInt64(2);
                report.HasUncheckpointedWal = log > checkpointed;
            }
        }
    }

    /// <summary>
    /// Executes a PRAGMA that returns multiple rows of error strings.
    /// Returns <c>(true, null)</c> when the only row is "ok".
    /// </summary>
    private static async Task<(bool passed, string? errors)> RunPragmaCheckAsync(
        SqliteConnection connection,
        string pragmaName,
        CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA {pragmaName}";

        var errors = new StringBuilder();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
            if (!string.Equals(row, "ok", StringComparison.OrdinalIgnoreCase))
                errors.AppendLine(row);
        }

        var errorText = errors.ToString().Trim();
        return (errorText.Length == 0, errorText.Length > 0 ? errorText : null);
    }

    /// <summary>
    /// Runs <c>PRAGMA foreign_key_check</c> and returns whether no violations were found.
    /// Each violation row is formatted as "table row parent fkid".
    /// </summary>
    private static async Task<(bool passed, string? errors)> RunForeignKeyCheckAsync(
        SqliteConnection connection,
        CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_key_check";

        var violations = new StringBuilder();
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var table = reader.IsDBNull(0) ? "?" : reader.GetString(0);
            var rowId = reader.IsDBNull(1) ? "?" : reader.GetInt64(1).ToString();
            var parent = reader.IsDBNull(2) ? "?" : reader.GetString(2);
            violations.AppendLine($"table={table} rowid={rowId} parent={parent}");
        }

        var text = violations.ToString().Trim();
        return (text.Length == 0, text.Length > 0 ? text : null);
    }

    private static async Task<long> ReadScalarLongAsync(
        SqliteConnection connection, string sql, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is long l ? l : 0L;
    }

    private static async Task<string> ReadScalarStringAsync(
        SqliteConnection connection, string sql, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync(ct);
        return result?.ToString() ?? string.Empty;
    }
}
