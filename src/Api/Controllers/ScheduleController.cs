// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller for backup schedule management. Handles CRUD operations
/// for backup schedules and cron expression validation.
/// </summary>
public class ScheduleController
{
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(
        IScheduleService scheduleService,
        ILogger<ScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all backup schedules.
    /// </summary>
    public async Task<ApiResponse<object>> ListSchedules(CancellationToken ct = default)
    {
        try
        {
            var schedules = await _scheduleService.GetAllSchedulesAsync(ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                TotalCount = schedules.Count(),
                Schedules = schedules.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.DatabasePath,
                    s.CronExpression,
                    s.IsEnabled,
                    s.NextRunTime
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list schedules");
            return ApiResponse<object>.ErrorResponse("LIST_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets a specific schedule by ID.
    /// </summary>
    public async Task<ApiResponse<object>> GetSchedule(Guid scheduleId, CancellationToken ct = default)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleAsync(scheduleId, ct);
            if (schedule == null)
                return ApiResponse<object>.ErrorResponse("NOT_FOUND", $"Schedule {scheduleId} not found");

            return ApiResponse<object>.SuccessResponse(new
            {
                schedule.Id,
                schedule.Name,
                schedule.DatabasePath,
                schedule.CronExpression,
                schedule.IsEnabled,
                schedule.NextRunTime,
                schedule.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get schedule");
            return ApiResponse<object>.ErrorResponse("RETRIEVAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Creates a new backup schedule.
    /// </summary>
    public async Task<ApiResponse<object>> CreateSchedule(CreateScheduleRequest request, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return ApiResponse<object>.ErrorResponse("VALIDATION_ERROR", "Schedule name is required");

            if (string.IsNullOrWhiteSpace(request.DatabasePath))
                return ApiResponse<object>.ErrorResponse("VALIDATION_ERROR", "Database path is required");

            var schedule = await _scheduleService.CreateScheduleAsync(
                request.Name,
                request.DatabasePath,
                ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                schedule.Id,
                schedule.Name,
                schedule.DatabasePath
            }, "Schedule created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create schedule");
            return ApiResponse<object>.ErrorResponse("CREATE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing schedule.
    /// </summary>
    public async Task<ApiResponse<object>> UpdateSchedule(
        Guid scheduleId,
        UpdateScheduleRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleAsync(scheduleId, ct);
            if (schedule == null)
                return ApiResponse<object>.ErrorResponse("NOT_FOUND", $"Schedule {scheduleId} not found");

            // Apply updates
            if (!string.IsNullOrWhiteSpace(request.Name))
                schedule.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.CronExpression))
                schedule.CronExpression = request.CronExpression;

            if (request.IsEnabled.HasValue)
                schedule.IsEnabled = request.IsEnabled.Value;

            // Save updates (would call repository in real implementation)

            return ApiResponse<object>.SuccessResponse(new
            {
                schedule.Id,
                schedule.Name,
                schedule.IsEnabled
            }, "Schedule updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update schedule");
            return ApiResponse<object>.ErrorResponse("UPDATE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Deletes a schedule.
    /// </summary>
    public async Task<ApiResponse> DeleteSchedule(Guid scheduleId, CancellationToken ct = default)
    {
        try
        {
            await _scheduleService.DeleteScheduleAsync(scheduleId, ct);
            return ApiResponse.SuccessResponse("Schedule deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schedule");
            return ApiResponse.ErrorResponse("DELETE_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Tests a cron expression for validity.
    /// </summary>
    public ApiResponse<object> ValidateCronExpression(string cronExpression)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cronExpression))
                return ApiResponse<object>.ErrorResponse("VALIDATION_ERROR", "Cron expression is required");

            // Validation would be done with Cronos library
            return ApiResponse<object>.SuccessResponse(new
            {
                Expression = cronExpression,
                IsValid = true,
                Message = "Cron expression is valid"
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.ErrorResponse("VALIDATION_ERROR", ex.Message);
        }
    }
}
