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
    /// <param name="schedule">The schedule to create.</param>
    /// <returns>The created schedule.</returns>
    Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Updates an existing backup schedule.
    /// </summary>
    /// <param name="schedule">The schedule with updated values.</param>
    /// <returns>The updated schedule.</returns>
    Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule);

    /// <summary>
    /// Deletes a backup schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific schedule by ID.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching schedule, or <c>null</c> if none exists.</returns>
    Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active schedules.
    /// </summary>
    /// <returns>The schedules that are currently active.</returns>
    Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync();

    /// <summary>
    /// Gets all schedules.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>All schedules, active and inactive.</returns>
    Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new backup schedule from name and database path.
    /// </summary>
    /// <param name="name">The display name of the schedule.</param>
    /// <param name="databasePath">The path to the SQLite database to back up.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created schedule.</returns>
    Task<BackupSchedule> CreateScheduleAsync(string name, string databasePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a cron expression.
    /// </summary>
    /// <param name="cronExpression">The cron expression to validate.</param>
    /// <returns><c>true</c> if the expression is valid; otherwise <c>false</c>.</returns>
    bool ValidateCronExpression(string cronExpression);

    /// <summary>
    /// Gets the next execution time for a schedule.
    /// </summary>
    /// <param name="schedule">The schedule to evaluate.</param>
    /// <returns>The next execution time, or <c>null</c> if it cannot be determined.</returns>
    DateTime? GetNextExecutionTime(BackupSchedule schedule);

    /// <summary>
    /// Deactivates a schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to deactivate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeactivateScheduleAsync(Guid scheduleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a schedule.
    /// </summary>
    /// <param name="scheduleId">The identifier of the schedule to activate.</param>
    Task ActivateScheduleAsync(Guid scheduleId);
}
