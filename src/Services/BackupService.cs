#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for executing and managing backup operations.
/// </summary>
public class BackupService : IBackupService
{
    private readonly IBackupRepository _repository;
    private readonly IStorageService _storageService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupRepository repository,
        IStorageService storageService,
        ILogger<BackupService> logger)
    {
        _repository = repository;
        _storageService = storageService;
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
            StartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting backup for schedule {ScheduleId}: {ScheduleName}", 
                schedule.Id, schedule.Name);

            // Copy the database file
            var timestamp = DateTime.UtcNow;
            var backupPath = GenerateBackupPath(schedule, timestamp);
            var backupDir = Path.GetDirectoryName(backupPath);
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir!);
            }

            File.Copy(schedule.DatabasePath, backupPath, overwrite: true);
            _logger.LogInformation("Backup file created at {BackupPath}", backupPath);

            result.BackupFilePath = backupPath;
            result.BackupFileSizeBytes = new FileInfo(backupPath).Length;
            result.Checksum = await CalculateBackupChecksumAsync(backupPath);
            result.Status = (int)Constants.BackupStatus.Success;

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
}
