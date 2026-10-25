#nullable enable
// Author: Vladyslav Zaiets

using Cronos;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for managing backup schedules.
/// </summary>
public sealed class ScheduleService : IScheduleService
{
    // Exception Messages
    private const string ScheduleConfigurationInvalidMessage = "Schedule configuration is invalid";
    private const string CreateScheduleFailedMessage = "Failed to create schedule in repository";
    private const string UpdateScheduleFailedMessage = "Failed to update schedule in repository";

    // Log Messages
    private const string DatabasePathMissingLogMessage = "Database path does not exist for schedule {ScheduleId}: {DatabasePath}";
    private const string ScheduleCreatedLogMessage = "Schedule created: {ScheduleId} - {ScheduleName}";
    private const string ScheduleUpdatedLogMessage = "Schedule updated: {ScheduleId} - {ScheduleName}";
    private const string ScheduleDeletedLogMessage = "Schedule deleted: {ScheduleId}";
    private const string ScheduleDeactivatedLogMessage = "Schedule deactivated: {ScheduleId}";
    private const string ScheduleActivatedLogMessage = "Schedule activated: {ScheduleId}";
    private const string NextExecutionTimeFailedLogMessage = "Failed to calculate next execution time for schedule {ScheduleId}";

    private readonly IBackupRepository _repository;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IBackupRepository repository,
        ILogger<ScheduleService> logger)
    {
        System.ArgumentNullException.ThrowIfNull(repository);
        System.ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new backup schedule.
    /// </summary>
    /// <param name="schedule">The schedule to create.</param>
    /// <returns>The created schedule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schedule is null.</exception>
    /// <exception cref="InvalidScheduleException">Thrown when schedule configuration is invalid.</exception>
    /// <exception cref="InvalidCronExpressionException">Thrown when cron expression is invalid.</exception>
    public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)
    {
        System.ArgumentNullException.ThrowIfNull(schedule);

        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException(ScheduleConfigurationInvalidMessage, schedule.Id);
        }

        if (!ValidateCronExpression(schedule.CronExpression))
        {
            throw new InvalidCronExpressionException(schedule.CronExpression);
        }

        if (!schedule.ValidateDatabasePath())
        {
            _logger.LogWarning(DatabasePathMissingLogMessage,
                schedule.Id, schedule.DatabasePath);
        }

        schedule.CreatedAt = DateTime.UtcNow;
        schedule.LastModifiedAt = DateTime.UtcNow;

        try
        {
            var created = await _repository.CreateScheduleAsync(schedule);
            _logger.LogInformation(ScheduleCreatedLogMessage, created.Id, created.Name);
            return created;
        }
        catch (Exception ex)
        {
            throw new ScheduleException(CreateScheduleFailedMessage, ex);
        }
    }

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    /// <param name="schedule">The schedule to update.</param>
    /// <returns>The updated schedule.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schedule is null.</exception>
    /// <exception cref="InvalidScheduleException">Thrown when schedule configuration is invalid.</exception>
    /// <exception cref="InvalidCronExpressionException">Thrown when cron expression is invalid.</exception>
    /// <exception cref="ScheduleException">Thrown when update fails.</exception>
    public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)
    {
        System.ArgumentNullException.ThrowIfNull(schedule);

        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException(ScheduleConfigurationInvalidMessage, schedule.Id);
        }

        if (!ValidateCronExpression(schedule.CronExpression))
        {
            throw new InvalidCronExpressionException(schedule.CronExpression);
        }

        schedule.LastModifiedAt = DateTime.UtcNow;

        try
        {
            var updated = await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation(ScheduleUpdatedLogMessage, updated.Id, updated.Name);
            return updated;
        }
        catch (Exception ex)
        {
            throw new ScheduleException(UpdateScheduleFailedMessage, ex);
        }
    }

    /// <summary>
    /// Deletes a backup schedule.
    /// </summary>
    public async Task DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteScheduleAsync(scheduleId);
        _logger.LogInformation(ScheduleDeletedLogMessage, scheduleId);
    }

    /// <summary>
    /// Gets a specific schedule by ID.
    /// </summary>
    public async Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
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
    /// Gets all schedules.
    /// </summary>
    public async Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllSchedulesAsync();
    }

    /// <summary>
    /// Creates a new backup schedule from name and database path.
    /// </summary>
    public Task<BackupSchedule> CreateScheduleAsync(string name, string databasePath, CancellationToken cancellationToken = default)
    {
        var schedule = new BackupSchedule
        {
            Name = name,
            DatabasePath = databasePath
        };
        return CreateScheduleAsync(schedule);
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
            var anchor = schedule.NextScheduledRunTime ?? DateTime.UtcNow;
            var nextOccurrence = expression.GetNextOccurrence(anchor);
            return nextOccurrence;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, NextExecutionTimeFailedLogMessage,
                schedule.Id);
            return null;
        }
    }

    /// <summary>
    /// Deactivates a schedule.
    /// </summary>
    public async Task DeactivateScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _repository.GetScheduleAsync(scheduleId);
        if (schedule  is not null)
        {
            schedule.IsActive = false;
            schedule.LastModifiedAt = DateTime.UtcNow;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation(ScheduleDeactivatedLogMessage, scheduleId);
        }
    }

    /// <summary>
    /// Activates a schedule.
    /// </summary>
    public async Task ActivateScheduleAsync(Guid scheduleId)
    {
        var schedule = await _repository.GetScheduleAsync(scheduleId);
        if (schedule  is not null)
        {
            schedule.IsActive = true;
            schedule.LastModifiedAt = DateTime.UtcNow;
            await _repository.UpdateScheduleAsync(schedule);
            _logger.LogInformation(ScheduleActivatedLogMessage, scheduleId);
        }
    }
}
