#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service interface for managing backup schedules.
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Creates a new backup schedule.
    /// </summary>
    Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Deletes a backup schedule.
    /// </summary>
    Task DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific schedule by ID.
    /// </summary>
    Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active schedules.
    /// </summary>
    Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync();

    /// <summary>
    /// Gets all schedules.
    /// </summary>
    Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new backup schedule from name and database path.
    /// </summary>
    Task<BackupSchedule> CreateScheduleAsync(string name, string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a cron expression.
    /// </summary>
    bool ValidateCronExpression(string cronExpression);

    /// <summary>
    /// Gets the next execution time for a schedule.
    /// </summary>
    DateTime? GetNextExecutionTime(BackupSchedule schedule);

    /// <summary>
    /// Deactivates a schedule.
    /// </summary>
    Task DeactivateScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a schedule.
    /// </summary>
    Task ActivateScheduleAsync(Guid scheduleId);
}
