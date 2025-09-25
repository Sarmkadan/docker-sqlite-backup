// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Cronos;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for managing backup schedules.
/// </summary>
public class ScheduleService : IScheduleService
{
    private readonly IBackupRepository _repository;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IBackupRepository repository,
        ILogger<ScheduleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new backup schedule.
    /// </summary>
    public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)
    {
        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException("Schedule configuration is invalid", schedule.Id);
        }

        if (!ValidateCronExpression(schedule.CronExpression))
        {
            throw new InvalidCronExpressionException(schedule.CronExpression);
        }

        if (!schedule.ValidateDatabasePath())
        {
            _logger.LogWarning("Database path does not exist for schedule {ScheduleId}: {DatabasePath}",
                schedule.Id, schedule.DatabasePath);
        }

        schedule.CreatedAt = DateTime.UtcNow;
        schedule.LastModifiedAt = DateTime.UtcNow;

        var created = await _repository.CreateScheduleAsync(schedule);
        _logger.LogInformation("Schedule created: {ScheduleId} - {ScheduleName}", created.Id, created.Name);

        return created;
    }

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)
    {
        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException("Schedule configuration is invalid", schedule.Id);
        }

        if (!ValidateCronExpression(schedule.CronExpression))
        {
            throw new InvalidCronExpressionException(schedule.CronExpression);
        }

        schedule.LastModifiedAt = DateTime.UtcNow;
        var updated = await _repository.UpdateScheduleAsync(schedule);

        _logger.LogInformation("Schedule updated: {ScheduleId} - {ScheduleName}", updated.Id, updated.Name);
        return updated;
    }

    /// <summary>
    /// Deletes a backup schedule.
    /// </summary>
    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        await _repository.DeleteScheduleAsync(scheduleId);
        _logger.LogInformation("Schedule deleted: {ScheduleId}", scheduleId);
    }

    /// <summary>
    /// Gets a specific schedule by ID.
    /// </summary>
    public async Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId)
    {
        return await _repository.GetScheduleAsync(scheduleId);
    }

    /// <summary>
    /// Gets all active schedules.
    /// </summary>
    public async Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync()
    {
        return await _repository.GetActiveSchedulesAsync();
    }

    /// <summary>
    /// Validates a cron expression.
    /// </summary>
    public bool ValidateCronExpression(string cronExpression)
    {
        try
        {
            CronExpression.Parse(cronExpression);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the next execution time for a schedule.
    /// </summary>
    public DateTime? GetNextExecutionTime(BackupSchedule schedule)
    {
        try
        {
            var expression = CronExpression.Parse(schedule.CronExpression);
            var nextOccurrence = expression.GetNextOccurrence(DateTime.UtcNow);
            return nextOccurrence;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate next execution time for schedule {ScheduleId}",
                schedule.Id);
            return null;
        }
    }

    /// <summary>
    /// Deactivates a schedule.
    /// </summary>
    public async Task DeactivateScheduleAsync(Guid scheduleId)
    {
        var schedule = await _repository.GetScheduleAsync(scheduleId);
        if (schedule != null)
        {
            schedule.IsActive = false;
            schedule.LastModifiedAt = DateTime.UtcNow;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation("Schedule deactivated: {ScheduleId}", scheduleId);
        }
    }

    /// <summary>
    /// Activates a schedule.
    /// </summary>
    public async Task ActivateScheduleAsync(Guid scheduleId)
    {
        var schedule = await _repository.GetScheduleAsync(scheduleId);
        if (schedule != null)
        {
            schedule.IsActive = true;
            schedule.LastModifiedAt = DateTime.UtcNow;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation("Schedule activated: {ScheduleId}", scheduleId);
        }
    }
}
