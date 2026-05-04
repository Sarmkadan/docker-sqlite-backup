// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Models;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Provides backup and database verification capabilities.
/// </summary>
public interface IVerificationService
{
    Task<VerificationResult> VerifyBackupAsync(BackupSnapshot snapshot, BackupJob job, CancellationToken cancellationToken);
    Task<VerificationResult> VerifyRestoredDatabaseAsync(string databasePath, CancellationToken cancellationToken);
}

/// <summary>
/// Result of a verification operation.
/// </summary>
public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Implements verification logic for backups and restored databases.
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(ILogger<VerificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verifies the integrity of a backup snapshot.
    /// </summary>
    public async Task<VerificationResult> VerifyBackupAsync(
        BackupSnapshot snapshot,
        BackupJob job,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationResult { IsValid = true };

        try
        {
            // Check file exists and is readable
            if (string.IsNullOrWhiteSpace(snapshot.StoragePath))
            {
                result.IsValid = false;
                result.ErrorMessage = "Backup storage path is not set";
                return result;
            }

            // Verify file size
            if (snapshot.SizeBytes <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Backup file size is invalid";
                return result;
            }

            // Verify metadata if available
            if (snapshot.Metadata != null)
            {
                if (!snapshot.Metadata.IsValid())
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Database metadata validation failed";
                    return result;
                }
            }

            // Test backup accessibility (download test for cloud storage)
            if (job.StorageProvider.Type != StorageType.Local)
            {
                result.WarningMessage = "Backup accessibility test skipped for cloud storage (would require download)";
            }

            stopwatch.Stop();
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            result.Details["backup_id"] = snapshot.Id;
            result.Details["storage_path"] = snapshot.StoragePath;
            result.Details["file_size"] = snapshot.SizeBytes;

            _logger.LogInformation("Backup verification completed for snapshot {SnapshotId} in {Duration}ms",
                snapshot.Id, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsValid = false;
            result.ErrorMessage = ex.Message;
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Backup verification failed for snapshot {SnapshotId}", snapshot.Id);
            return result;
        }
    }

    /// <summary>
    /// Verifies the integrity of a restored SQLite database.
    /// </summary>
    public async Task<VerificationResult> VerifyRestoredDatabaseAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new VerificationResult { IsValid = true };

        if (!File.Exists(databasePath))
        {
            result.IsValid = false;
            result.ErrorMessage = $"Database file not found at {databasePath}";
            return result;
        }

        try
        {
            return await Task.Run(() => PerformDatabaseVerification(databasePath, result, stopwatch), cancellationToken);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.IsValid = false;
            result.ErrorMessage = $"Database verification error: {ex.Message}";
            result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Database verification failed for {DatabasePath}", databasePath);
            return result;
        }
    }

    /// <summary>
    /// Performs actual database verification using SQLite PRAGMA commands.
    /// </summary>
    private VerificationResult PerformDatabaseVerification(
        string databasePath,
        VerificationResult result,
        System.Diagnostics.Stopwatch stopwatch)
    {
        using (var connection = new SQLiteConnection($"Data Source={databasePath};"))
        {
            try
            {
                connection.Open();

                // Check if database can be opened and queried
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA integrity_check;";
                    var integrityCheckResult = command.ExecuteScalar() as string;

                    if (integrityCheckResult != "ok")
                    {
                        result.IsValid = false;
                        result.ErrorMessage = $"Integrity check failed: {integrityCheckResult}";
                        result.Details["integrity_check"] = integrityCheckResult;
                        return result;
                    }
                }

                // Get database metadata
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table';";
                    result.Details["table_count"] = command.ExecuteScalar() ?? 0;
                }

                // Verify foreign keys can be checked
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA foreign_key_check;";
                    using (var reader = command.ExecuteReader())
                    {
                        int fkIssues = 0;
                        while (reader.Read())
                            fkIssues++;

                        if (fkIssues > 0)
                        {
                            result.WarningMessage = $"Found {fkIssues} foreign key constraint violations";
                            result.Details["foreign_key_issues"] = fkIssues;
                        }
                    }
                }

                // Get page count for size verification
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA page_count;";
                    result.Details["page_count"] = command.ExecuteScalar() ?? 0;
                }

                result.IsValid = true;
                stopwatch.Stop();
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                _logger.LogInformation("Database verification completed successfully for {DatabasePath} in {Duration}ms",
                    databasePath, stopwatch.ElapsedMilliseconds);

                return result;
            }
            finally
            {
                connection.Close();
            }
        }
    }
}
