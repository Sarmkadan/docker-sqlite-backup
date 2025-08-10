// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for verifying backup integrity and restoration.
/// </summary>
public class VerificationService : IVerificationService
{
    private readonly IBackupRepository _repository;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IBackupRepository repository,
        ILogger<VerificationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Verifies a backup by attempting to restore and validate the database.
    /// </summary>
    public async Task<RestoreVerification> VerifyBackupAsync(BackupResult backup, CancellationToken cancellationToken = default)
    {
        var verification = new RestoreVerification
        {
            BackupResultId = backup.Id,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting verification for backup {BackupId}", backup.Id);

            // Verify checksum first
            if (!await VerifyChecksumAsync(backup.BackupFilePath, backup.Checksum))
            {
                throw new BackupCorruptedException(
                    $"Checksum mismatch for backup file: {backup.BackupFilePath}",
                    backup.Id);
            }

            // Restore to temporary location
            var tempPath = await RestoreToTemporaryAsync(backup);
            verification.TemporaryDirectory = tempPath;

            // Perform integrity check
            var (isValid, errors) = await PerformIntegrityCheckAsync(tempPath);
            verification.IntegrityCheckPassed = isValid;
            verification.IntegrityCheckErrors = errors;

            if (!isValid)
            {
                throw new IntegrityCheckFailedException(
                    "Database integrity check failed",
                    backup.Id,
                    errors);
            }

            // Get database statistics
            var fileInfo = new FileInfo(tempPath);
            verification.DatabaseSizeBytes = fileInfo.Length;
            verification.RecordCount = await CountRecordsAsync(tempPath);

            verification.MarkCompleted(true, "Backup verification successful");
            _logger.LogInformation("Backup verification completed successfully for {BackupId}", backup.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup verification failed for {BackupId}", backup.Id);
            verification.MarkCompleted(false, $"Verification failed: {ex.Message}");
            verification.ErrorMessage = ex.Message;
        }
        finally
        {
            // Clean up temporary files
            try
            {
                if (!string.IsNullOrEmpty(verification.TemporaryDirectory) && Directory.Exists(verification.TemporaryDirectory))
                {
                    await CleanupTemporaryFilesAsync(verification.TemporaryDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temporary files for verification {VerificationId}",
                    verification.Id);
            }
        }

        await _repository.SaveRestoreVerificationAsync(verification);
        return verification;
    }

    /// <summary>
    /// Gets the verification history for a backup.
    /// </summary>
    public async Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId)
    {
        return await _repository.GetVerificationHistoryAsync(backupResultId);
    }

    /// <summary>
    /// Performs an integrity check on a SQLite database file.
    /// </summary>
    public async Task<(bool IsValid, string? Errors)> PerformIntegrityCheckAsync(string databasePath)
    {
        try
        {
            var connectionString = $"Data Source={databasePath};Mode=ReadOnly;";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check";
            var result = (string?)await command.ExecuteScalarAsync();

            return result == "ok" ? (true, null) : (false, result);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Verifies the checksum of a backup file.
    /// </summary>
    public async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum)
    {
        if (string.IsNullOrEmpty(expectedChecksum))
        {
            _logger.LogWarning("No expected checksum provided for verification of {FilePath}", filePath);
            return true;
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        var calculatedChecksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

        return calculatedChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Restores a backup to a temporary location for verification.
    /// </summary>
    public async Task<string> RestoreToTemporaryAsync(BackupResult backup)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"sqlite-verify-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var tempDbPath = Path.Combine(tempDir, "restore-check.sqlite");
        await Task.Run(() => File.Copy(backup.BackupFilePath, tempDbPath, overwrite: true));

        return tempDbPath;
    }

    /// <summary>
    /// Cleans up temporary files from verification attempts.
    /// </summary>
    public async Task CleanupTemporaryFilesAsync(string tempDirectory)
    {
        await Task.Run(() =>
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        });
    }

    /// <summary>
    /// Counts the number of records in all tables in a database.
    /// </summary>
    private async Task<long> CountRecordsAsync(string databasePath)
    {
        try
        {
            var connectionString = $"Data Source={databasePath};Mode=ReadOnly;";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            long totalRecords = 0;

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
            using var reader = await command.ExecuteReaderAsync();
            
            var tableNames = new List<string>();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }

            foreach (var tableName in tableNames)
            {
                using var countCommand = connection.CreateCommand();
                countCommand.CommandText = $"SELECT COUNT(*) FROM \"{tableName}\"";
                var count = (long?)await countCommand.ExecuteScalarAsync() ?? 0;
                totalRecords += count;
            }

            return totalRecords;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count records in database {DatabasePath}", databasePath);
            return 0;
        }
    }
}
