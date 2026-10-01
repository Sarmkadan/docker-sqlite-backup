#nullable enable
// Author: Vladyslav Zaiets

using Cronos;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Manages the timing aspect of backup schedules: evaluates cron expressions,
/// detects missed windows after container restarts, and computes upcoming
/// execution plans. Used by <see cref="BackupWorker"/> to decide when a
/// schedule should fire, and by management APIs to preview future runs.
/// </summary>
public sealed class BackupScheduler
{
    private readonly IScheduleService _scheduleService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<BackupScheduler> _logger;

    private readonly Dictionary<Guid, CronExpression> _cronCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupScheduler"/> class.
    /// </summary>
    /// <param name="scheduleService">The schedule service for CRUD operations.</param>
    /// <param name="appSettings">Global application settings.</param>
    /// <param name="logger">Logger instance.</param>
    public BackupScheduler(
        IScheduleService scheduleService,
        AppSettings appSettings,
        ILogger<BackupScheduler> logger)
    {
        _scheduleService = scheduleService ?? throw new ArgumentNullException(nameof(scheduleService));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns all active schedules that are due for execution at the current time
    /// or were missed while the service was offline.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Collection of schedules that should be executed now.</returns>
    public async Task<IReadOnlyList<BackupSchedule>> GetDueSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var activeSchedules = await _scheduleService.GetActiveSchedulesAsync();
        var now = DateTime.UtcNow;
        var due = new List<BackupSchedule>();

        foreach (var schedule in activeSchedules)
        {
            var cronExpression = GetOrParseCron(schedule);
            if (cronExpression is null)
                continue;

            var anchor = schedule.LastBackupAt ?? schedule.CreatedAt;
            var nextOccurrence = cronExpression.GetNextOccurrence(anchor);

            if (nextOccurrence.HasValue && nextOccurrence.Value <= now)
            {
                due.Add(schedule);

                // Detect missed windows
                var missedCount = CountMissedOccurrences(cronExpression, anchor, now);
                if (missedCount > 1)
                {
                    _logger.LogWarning(
                        "Schedule {ScheduleId} ({ScheduleName}) missed {MissedCount} backup windows since {Anchor:u}",
                        schedule.Id, schedule.Name, missedCount, anchor);
                }
            }
        }

        return due;
    }

    /// <summary>
    /// Computes the next N upcoming execution times for a given schedule.
    /// </summary>
    /// <param name="schedule">The schedule to forecast.</param>
    /// <param name="count">Number of future occurrences to compute. Defaults to 5.</param>
    /// <returns>List of future UTC execution times.</returns>
    /// <exception cref="InvalidCronExpressionException">Thrown when the cron expression cannot be parsed.</exception>
    public IReadOnlyList<DateTime> GetUpcomingExecutions(BackupSchedule schedule, int count = 5)
    {
        var cronExpression = GetOrParseCron(schedule)
            ?? throw new InvalidCronExpressionException(schedule.CronExpression);

        var results = new List<DateTime>(count);
        var anchor = DateTime.UtcNow;

        for (var i = 0; i < count; i++)
        {
            var next = cronExpression.GetNextOccurrence(anchor);
            if (!next.HasValue)
                break;

            results.Add(next.Value);
            anchor = next.Value;
        }

        return results;
    }

    /// <summary>
    /// Updates the <see cref="BackupSchedule.NextScheduledRunTime"/> for all active
    /// schedules so that missed-schedule detection works after a container restart.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Number of schedules whose next-run time was updated.</returns>
    public async Task<int> RefreshNextRunTimesAsync(CancellationToken cancellationToken = default)
    {
        var activeSchedules = await _scheduleService.GetActiveSchedulesAsync();
        var updated = 0;

        foreach (var schedule in activeSchedules)
        {
            var cronExpression = GetOrParseCron(schedule);
            if (cronExpression is null)
                continue;

            var nextRun = cronExpression.GetNextOccurrence(DateTime.UtcNow);
            if (nextRun.HasValue && nextRun != schedule.NextScheduledRunTime)
            {
                schedule.NextScheduledRunTime = nextRun.Value;
                schedule.NextRunTime = nextRun.Value;
                await _scheduleService.UpdateScheduleAsync(schedule);
                updated++;
            }
        }

        _logger.LogInformation("Refreshed next-run times for {UpdatedCount} schedules", updated);
        return updated;
    }

    /// <summary>
    /// Validates a cron expression and returns its parsed form, or null if invalid.
    /// </summary>
    /// <param name="cronExpression">The cron expression string to validate.</param>
    /// <returns>The parsed <see cref="CronExpression"/>, or null if parsing fails.</returns>
    public CronExpression? TryParseCron(string cronExpression)
    {
        try
        {
            return CronExpression.Parse(cronExpression);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Evicts cached cron expressions for a schedule, forcing a re-parse on next evaluation.
    /// Call this after a schedule's cron expression is modified.
    /// </summary>
    /// <param name="scheduleId">The schedule whose cached expression should be cleared.</param>
    public void InvalidateCronCache(Guid scheduleId)
    {
        _cronCache.Remove(scheduleId);
    }

    private CronExpression? GetOrParseCron(BackupSchedule schedule)
    {
        if (_cronCache.TryGetValue(schedule.Id, out var cached))
            return cached;

        try
        {
            var parsed = CronExpression.Parse(schedule.CronExpression);
            _cronCache[schedule.Id] = parsed;
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid cron expression for schedule {ScheduleId}: {Cron}",
                schedule.Id, schedule.CronExpression);
            return null;
        }
    }

    private static int CountMissedOccurrences(CronExpression cron, DateTime from, DateTime until)
    {
        var count = 0;
        var cursor = from;

        while (true)
        {
            var next = cron.GetNextOccurrence(cursor);
            if (!next.HasValue || next.Value > until)
                break;

            count++;
            cursor = next.Value;
        }

        return count;
    }
}
