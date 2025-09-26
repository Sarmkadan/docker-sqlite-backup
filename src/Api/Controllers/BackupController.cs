// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller for backup operations. Handles endpoints for executing backups,
/// listing history, and managing backup artifacts.
/// </summary>
public class BackupController
{
    private readonly IBackupService _backupService;
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<BackupController> _logger;

    public BackupController(
        IBackupService backupService,
        IScheduleService scheduleService,
        ILogger<BackupController> logger)
    {
        _backupService = backupService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a manual backup for the specified schedule.
    /// </summary>
    public async Task<ApiResponse<object>> ExecuteBackup(ExecuteBackupRequest request, CancellationToken ct = default)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleAsync(request.ScheduleId, ct);
            if (schedule == null)
                return ApiResponse<object>.ErrorResponse("SCHEDULE_NOT_FOUND", $"Schedule {request.ScheduleId} not found");

            var result = await _backupService.ExecuteBackupAsync(schedule, ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                result.Id,
                result.BackupFilePath,
                result.BackupFileSizeBytes,
                result.Checksum,
                result.StartedAt,
                result.CompletedAt
            }, "Backup executed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup execution failed");
            return ApiResponse<object>.ErrorResponse("BACKUP_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets backup history for a schedule.
    /// </summary>
    public async Task<ApiResponse<object>> GetHistory(Guid scheduleId, int limit = 10, CancellationToken ct = default)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleAsync(scheduleId, ct);
            if (schedule == null)
                return ApiResponse<object>.ErrorResponse("SCHEDULE_NOT_FOUND", $"Schedule {scheduleId} not found");

            var backups = await _backupService.GetBackupHistoryAsync(scheduleId, limit);

            return ApiResponse<object>.SuccessResponse(new
            {
                ScheduleId = scheduleId,
                ScheduleName = schedule.Name,
                BackupCount = backups.Count(),
                Backups = backups.Select(b => new
                {
                    b.Id,
                    b.BackupFilePath,
                    b.BackupFileSizeBytes,
                    b.Status,
                    b.StartedAt,
                    b.CompletedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve backup history");
            return ApiResponse<object>.ErrorResponse("HISTORY_RETRIEVAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific backup result by ID.
    /// </summary>
    public async Task<ApiResponse<object>> GetBackup(Guid backupId, CancellationToken ct = default)
    {
        try
        {
            var backup = await _backupService.GetBackupResultAsync(backupId);
            if (backup == null)
                return ApiResponse<object>.ErrorResponse("BACKUP_NOT_FOUND", $"Backup {backupId} not found");

            return ApiResponse<object>.SuccessResponse(new
            {
                backup.Id,
                backup.ScheduleId,
                backup.BackupFilePath,
                backup.BackupFileSizeBytes,
                backup.Checksum,
                backup.Status,
                backup.StartedAt,
                backup.CompletedAt,
                backup.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve backup");
            return ApiResponse<object>.ErrorResponse("BACKUP_RETRIEVAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Deletes a backup artifact.
    /// </summary>
    public async Task<ApiResponse> DeleteBackup(Guid backupId, CancellationToken ct = default)
    {
        try
        {
            await _backupService.DeleteBackupAsync(backupId);
            return ApiResponse.SuccessResponse("Backup deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete backup");
            return ApiResponse.ErrorResponse("DELETE_FAILED", ex.Message);
        }
    }
}
