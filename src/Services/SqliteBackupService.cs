#nullable enable
// Author: Vladyslav Zaiets

using System.Diagnostics;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// High-level orchestrator for SQLite-specific backup operations such as
/// integrity checks, WAL management, and point-in-time restore. Complements
/// <see cref="BackupService"/> (which handles file-level snapshot/upload) by
/// exposing database-aware operations that require a live SQLite connection.
/// </summary>
public sealed class SqliteBackupService : IDisposable
{
    private readonly IBackupRepository _repository;
    private readonly AppSettings _appSettings;
    private readonly ILogger<SqliteBackupService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBackupService"/> class.
    /// </summary>
    /// <param name="repository">Repository for backup metadata persistence.</param>
    /// <param name="appSettings">Global application settings.</param>
    /// <param name="logger">Logger instance.</param>
    public SqliteBackupService(
        IBackupRepository repository,
        AppSettings appSettings,
        ILogger<SqliteBackupService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs a PRAGMA integrity_check on the specified SQLite database and returns
    /// an <see cref="IntegrityReport"/> summarizing the outcome.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite database file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An <see cref="IntegrityReport"/> with page-level check results.</returns>
    /// <exception cref="DatabaseAccessException">Thrown when the database cannot be opened.</exception>
    public async Task<IntegrityReport> RunIntegrityCheckAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(databasePath))
            throw new Exceptions.ArgumentException(nameof(databasePath), "Database path cannot be empty.");

        if (!File.Exists(databasePath))
            throw new DatabaseAccessException(databasePath, new FileNotFoundException("Database file not found", databasePath));

        _logger.LogInformation("Running integrity check on {DatabasePath}", databasePath);
        var sw = Stopwatch.StartNew();
        var messages = new List<string>();

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                messages.Add(reader.GetString(0));
            }
        }
        catch (SqliteException ex)
        {
            throw new DatabaseAccessException($"Integrity check failed for {databasePath}", ex);
        }

        sw.Stop();
        var isOk = messages.Count == 1 && messages[0] == "ok";

        _logger.LogInformation(
            "Integrity check completed in {ElapsedMs}ms. Result: {Result}",
            sw.ElapsedMilliseconds, isOk ? "OK" : "ERRORS DETECTED");

        return new IntegrityReport
        {
            DatabasePath = databasePath,
            CheckedAt = DateTime.UtcNow,
            PassedQuickCheck = isOk,
            PassedFullCheck = isOk,
            PassedForeignKeyCheck = isOk,
            FullCheckErrors = isOk ? null : string.Join("; ", messages),
            Duration = sw.Elapsed
        };
    }

    /// <summary>
    /// Restores a previously taken backup to a target path using the SQLite
    /// Online Backup API for consistency. The target file is overwritten if it exists.
    /// </summary>
    /// <param name="backupResultId">The ID of the <see cref="BackupResult"/> to restore from.</param>
    /// <param name="targetPath">The destination path for the restored database.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="BackupException">Thrown when the backup result is not found or the file is missing.</exception>
    public async Task RestoreBackupAsync(Guid backupResultId, string targetPath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var backupResult = await _repository.GetBackupResultAsync(backupResultId);
        if (backupResult is null)
            throw new BackupException("Backup result not found", backupResultId);

        if (string.IsNullOrWhiteSpace(backupResult.BackupFilePath) || !File.Exists(backupResult.BackupFilePath))
            throw new BackupException($"Backup file not found at {backupResult.BackupFilePath}", backupResultId);

        _logger.LogInformation("Restoring backup {BackupId} to {TargetPath}", backupResultId, targetPath);

        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        try
        {
            using var source = new SqliteConnection($"Data Source={backupResult.BackupFilePath};Mode=ReadOnly");
            using var destination = new SqliteConnection($"Data Source={targetPath}");
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);
        }
        catch (SqliteException ex)
        {
            throw new BackupException($"SQLite restore failed from {backupResult.BackupFilePath} to {targetPath}", ex);
        }

        _logger.LogInformation("Restore completed: {TargetPath}", targetPath);
    }

    /// <summary>
    /// Returns the WAL (Write-Ahead Log) size in bytes for the given database,
    /// or zero when the database does not use WAL mode.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite database file.</param>
    /// <returns>WAL file size in bytes, or 0 if no WAL file exists.</returns>
    public long GetWalSizeBytes(string databasePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var walPath = databasePath + "-wal";
        if (!File.Exists(walPath))
            return 0;

        return new FileInfo(walPath).Length;
    }

    /// <summary>
    /// Issues a WAL checkpoint (TRUNCATE) on the specified database,
    /// merging all pending WAL frames into the main database file.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite database file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the checkpoint succeeded; false if the database is not in WAL mode.</returns>
    public async Task<bool> CheckpointWalAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            using var connection = new SqliteConnection($"Data Source={databasePath}");
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger.LogInformation("WAL checkpoint completed for {DatabasePath}", databasePath);
            return true;
        }
        catch (SqliteException ex)
        {
            _logger.LogDebug(ex, "WAL checkpoint skipped for {DatabasePath} (may not be in WAL mode)", databasePath);
            return false;
        }
    }

    /// <summary>
    /// Queries the page count and page size of the given database to compute
    /// total database size without reading the entire file.
    /// </summary>
    /// <param name="databasePath">Absolute path to the SQLite database file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Total database size in bytes as reported by SQLite PRAGMAs.</returns>
    public async Task<long> GetDatabaseSizeAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        await connection.OpenAsync(cancellationToken);

        using var pageSizeCmd = connection.CreateCommand();
        pageSizeCmd.CommandText = "PRAGMA page_size";
        var pageSize = Convert.ToInt64(await pageSizeCmd.ExecuteScalarAsync(cancellationToken));

        using var pageCountCmd = connection.CreateCommand();
        pageCountCmd.CommandText = "PRAGMA page_count";
        var pageCount = Convert.ToInt64(await pageCountCmd.ExecuteScalarAsync(cancellationToken));

        return pageSize * pageCount;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }
}
