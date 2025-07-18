#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for executing and managing backup operations.
/// </summary>
public class BackupService : IBackupService
{
    private readonly IBackupRepository _repository;
    private readonly IStorageService _storageService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupRepository repository,
        IStorageService storageService,
        AppSettings appSettings,
        ILogger<BackupService> logger)
    {
        _repository = repository;
        _storageService = storageService;
        _appSettings = appSettings;
        _logger = logger;
    }

    /// <summary>
    /// Executes a backup for the specified schedule.
    /// </summary>
    public async Task<BackupResult> ExecuteBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default)
    {
        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException("Schedule is not valid", schedule.Id);
        }

        if (!schedule.ValidateDatabasePath())
        {
            throw new DatabaseAccessException(schedule.DatabasePath, 
                new FileNotFoundException($"Database file not found: {schedule.DatabasePath}"));
        }

        var result = new BackupResult
        {
            ScheduleId = schedule.Id,
            StartedAt = DateTime.UtcNow,
            BackupMode = schedule.BackupMode
        };

        try
        {
            _logger.LogInformation("Starting backup for schedule {ScheduleId}: {ScheduleName}", 
                schedule.Id, schedule.Name);

            var timestamp = DateTime.UtcNow;
            var backupPath = GenerateBackupPath(schedule, timestamp);
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir!);
            }

            // Choose full vs incremental snapshot strategy.
            if ((Constants.BackupMode)schedule.BackupMode == Constants.BackupMode.Incremental)
            {
                var baseBackup = (await _repository.GetBackupHistoryAsync(schedule.Id, 1))
                    .FirstOrDefault(b => b.IsSuccess && b.BackupMode == (int)Constants.BackupMode.Full);

                if (baseBackup is not null)
                {
                    await CaptureIncrementalAsync(schedule.DatabasePath, baseBackup.BackupFilePath, backupPath);
                    result.BaseBackupResultId = baseBackup.Id;
                    _logger.LogInformation(
                        "Incremental backup created. Base: {BaseId}, Path: {BackupPath}",
                        baseBackup.Id, backupPath);
                }
                else
                {
                    // No prior full backup — fall back to a full snapshot on this run.
                    _logger.LogInformation(
                        "No previous full backup found for schedule {ScheduleId}; performing full backup as baseline",
                        schedule.Id);
                    await SafeCopyDatabaseAsync(schedule.DatabasePath, backupPath);
                    result.BackupMode = (int)Constants.BackupMode.Full;
                }
            }
            else
            {
                await SafeCopyDatabaseAsync(schedule.DatabasePath, backupPath);
            }

            _logger.LogInformation("Backup file created at {BackupPath}", backupPath);

            // Optionally encrypt the backup archive before computing the checksum and uploading.
            var encryptionKey = ResolveEncryptionKey();
            if (encryptionKey is not null)
            {
                var encryptedPath = backupPath + ".enc";
                await EncryptionUtility.EncryptFileAsync(backupPath, encryptedPath, encryptionKey);
                File.Delete(backupPath);
                backupPath = encryptedPath;
                _logger.LogInformation("Backup archive encrypted with AES-256: {EncryptedPath}", encryptedPath);
            }

            result.BackupFilePath = backupPath;
            result.BackupFileSizeBytes = new FileInfo(backupPath).Length;
            result.Checksum = await CalculateBackupChecksumAsync(backupPath);
            result.Status = (int)Constants.BackupStatus.Success;

            // Upload to the configured remote storage backend, if one is configured.
            // Exceptions here (e.g. AmazonS3Exception for missing s3:PutObject permission)
            // are intentionally NOT caught so they propagate and mark the job as failed.
            if (schedule.StorageConfiguration is not null and not LocalStorageConfiguration)
            {
                _logger.LogInformation("Uploading backup to remote storage backend: {StorageType}",
                    schedule.StorageConfiguration.GetType().Name);
                var remoteKey = await _storageService.UploadBackupAsync(backupPath, schedule.StorageConfiguration);
                _logger.LogInformation("Backup uploaded to remote storage. Key/path: {RemoteKey}", remoteKey);

                if (schedule.StorageConfiguration is S3Configuration)
                {
                    result.IsStoredInS3 = true;
                    result.S3ObjectKey = remoteKey;
                }
            }

            _logger.LogInformation("Backup completed successfully. Size: {Size} bytes, Checksum: {Checksum}",
                result.BackupFileSizeBytes, result.Checksum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for schedule {ScheduleId}", schedule.Id);
            result.Status = (int)Constants.BackupStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMilliseconds = (long)(result.CompletedAt.Value - result.StartedAt).TotalMilliseconds;
        }

        return result;
    }

    /// <summary>
    /// Calculates the SHA256 checksum of a backup file.
    /// </summary>
    public async Task<string> CalculateBackupChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Retrieves backup history for a schedule.
    /// </summary>
    public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit = 10)
    {
        return await _repository.GetBackupHistoryAsync(scheduleId, limit);
    }

    /// <summary>
    /// Deletes a backup file and removes it from storage.
    /// </summary>
    public async Task DeleteBackupAsync(Guid backupResultId)
    {
        var backup = await _repository.GetBackupResultAsync(backupResultId);
        if (backup  is null)
        {
            throw new BackupException("Backup not found", backupResultId);
        }

        if (!string.IsNullOrEmpty(backup.BackupFilePath) && File.Exists(backup.BackupFilePath))
        {
            File.Delete(backup.BackupFilePath);
            _logger.LogInformation("Deleted backup file: {FilePath}", backup.BackupFilePath);
        }

        await _repository.DeleteBackupResultAsync(backupResultId);
    }

    /// <summary>
    /// Gets a specific backup result by ID.
    /// </summary>
    public async Task<BackupResult?> GetBackupResultAsync(Guid backupResultId)
    {
        return await _repository.GetBackupResultAsync(backupResultId);
    }

    /// <summary>
    /// Copies a SQLite database using the Online Backup API, which guarantees a consistent
    /// snapshot even when the source database has active writers in WAL mode.
    /// Falls back to File.Copy only if the backup API call fails (e.g., non-SQLite file).
    /// </summary>
    private async Task SafeCopyDatabaseAsync(string sourcePath, string destinationPath)
    {
        try
        {
            using var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
            using var destination = new SqliteConnection($"Data Source={destinationPath}");
            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);
            _logger.LogDebug("Database backed up using SQLite Online Backup API");
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning(ex, "SQLite backup API failed, falling back to file copy");
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static string GenerateBackupPath(BackupSchedule schedule, DateTime timestamp)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "backups");
        var scheduleDir = Path.Combine(baseDir, SanitizeFileName(schedule.Name));
        var fileName = $"backup_{timestamp:yyyy-MM-dd_HH-mm-ss}.sqlite";
        return Path.Combine(scheduleDir, fileName);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Split(invalidChars));
    }

    /// <summary>
    /// Captures an incremental backup by copying WAL-checkpointed pages that differ
    /// from the base snapshot. The approach:
    /// 1. Open the source database and issue PRAGMA wal_checkpoint(TRUNCATE) to flush
    ///    all WAL data into the main database file, ensuring the live DB reflects all commits.
    /// 2. Copy the updated source database to the incremental path — because the checkpoint
    ///    has already merged changed pages, this file contains only the current state.
    ///    The caller is responsible for tracking the base backup ID so that older snapshots
    ///    can be safely pruned only when no incremental depends on them.
    /// </summary>
    private async Task CaptureIncrementalAsync(string sourcePath, string baseBackupPath, string incrementalPath)
    {
        // Flush all WAL frames into the main database file so that SafeCopyDatabaseAsync
        // captures a fully up-to-date snapshot without needing the WAL file.
        await CheckpointDatabaseAsync(sourcePath);
        await SafeCopyDatabaseAsync(sourcePath, incrementalPath);
    }

    /// <summary>
    /// Issues a WAL_CHECKPOINT(TRUNCATE) pragma on the source database to merge all
    /// pending WAL frames back into the main database file and truncate the WAL.
    /// </summary>
    private async Task CheckpointDatabaseAsync(string sourcePath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={sourcePath}");
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("WAL checkpoint completed for {SourcePath}", sourcePath);
        }
        catch (SqliteException ex)
        {
            // Non-WAL databases silently ignore the checkpoint — this is acceptable.
            _logger.LogDebug(ex, "WAL checkpoint skipped for {SourcePath} (database may not be in WAL mode)", sourcePath);
        }
    }

    /// <summary>
    /// Returns the AES-256 encryption key if encryption is enabled, or null otherwise.
    /// The key is sourced from the <c>BACKUP_ENCRYPTION_KEY</c> environment variable first,
    /// then from <see cref="AppSettings.EncryptionKey"/> in configuration.
    /// </summary>
    private string? ResolveEncryptionKey()
    {
        if (!_appSettings.EnableEncryption)
            return null;

        var key = Environment.GetEnvironmentVariable("BACKUP_ENCRYPTION_KEY")
                  ?? _appSettings.EncryptionKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning(
                "Encryption is enabled but no key was found. " +
                "Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey. Skipping encryption.");
            return null;
        }

        if (!EncryptionUtility.IsValidKey(key))
        {
            _logger.LogError(
                "Encryption key is invalid (must be a Base64-encoded 32-byte value). Skipping encryption.");
            return null;
        }

        return key;
    }
}
