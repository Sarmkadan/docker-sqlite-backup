// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using Cronos;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Manages backup scheduling and execution based on CRON expressions.
/// </summary>
public class ScheduleService
{
    private readonly ILogger<ScheduleService> _logger;
    private readonly Dictionary<string, ScheduleEntry> _activeSchedules = new();

    private class ScheduleEntry
    {
        public string BackupJobId { get; set; } = null!;
        public CronExpression CronExpression { get; set; } = null!;
        public TimeZoneInfo TimeZone { get; set; } = null!;
        public Func<CancellationToken, Task> BackupAction { get; set; } = null!;
        public DateTime? LastExecutionTime { get; set; }
        public DateTime? NextExecutionTime { get; set; }
    }

    public ScheduleService(ILogger<ScheduleService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a backup schedule for execution.
    /// </summary>
    public void RegisterSchedule(
        BackupJob job,
        Func<CancellationToken, Task> backupAction)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        if (job.Schedule == null)
            throw new ScheduleException("Backup job has no schedule configured", job.Id, null);

        if (!job.Schedule.IsEnabled)
        {
            _logger.LogWarning("Schedule for job {JobId} is disabled", job.Id);
            return;
        }

        try
        {
            var cronExpression = CronExpression.Parse(job.Schedule.CronExpression);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(job.Schedule.TimeZone);

            var scheduleEntry = new ScheduleEntry
            {
                BackupJobId = job.Id,
                CronExpression = cronExpression,
                TimeZone = timeZone,
                BackupAction = backupAction
            };

            _activeSchedules[job.Id] = scheduleEntry;

            // Calculate next execution time
            scheduleEntry.NextExecutionTime = GetNextExecutionTime(scheduleEntry);

            _logger.LogInformation(
                "Registered schedule for job {JobId}: {CronExpression} in timezone {TimeZone}, next execution: {NextExecution}",
                job.Id, job.Schedule.CronExpression, job.Schedule.TimeZone, scheduleEntry.NextExecutionTime);
        }
        catch (CronFormatException ex)
        {
            throw new ScheduleException($"Invalid CRON expression: {job.Schedule.CronExpression}", ex, job.Id, job.Schedule.CronExpression);
        }
        catch (TimeZoneNotFoundException ex)
        {
            throw new ScheduleException($"Invalid timezone: {job.Schedule.TimeZone}", ex, job.Id, null);
        }
    }

    /// <summary>
    /// Unregisters a backup schedule.
    /// </summary>
    public void UnregisterSchedule(string backupJobId)
    {
        if (_activeSchedules.Remove(backupJobId))
        {
            _logger.LogInformation("Unregistered schedule for job {JobId}", backupJobId);
        }
    }

    /// <summary>
    /// Checks and executes any backup jobs that are due.
    /// Should be called periodically (e.g., every minute) by a background job runner.
    /// </summary>
    public async Task ExecuteDueBackupsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var dueMgJobs = _activeSchedules.Values
            .Where(s => s.NextExecutionTime.HasValue && s.NextExecutionTime <= now)
            .ToList();

        foreach (var schedule in dueMgJobs)
        {
            _logger.LogInformation("Executing backup for job {JobId} (due at {DueTime})",
                schedule.BackupJobId, schedule.NextExecutionTime);

            try
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                // Set timeout for backup execution
                cts.CancelAfter(TimeSpan.FromHours(1));

                await schedule.BackupAction(cts.Token);

                schedule.LastExecutionTime = now;
                schedule.NextExecutionTime = GetNextExecutionTime(schedule);

                _logger.LogInformation(
                    "Backup for job {JobId} completed successfully. Next execution: {NextExecution}",
                    schedule.BackupJobId, schedule.NextExecutionTime);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Backup for job {JobId} exceeded timeout", schedule.BackupJobId);
                schedule.NextExecutionTime = GetNextExecutionTime(schedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup execution failed for job {JobId}", schedule.BackupJobId);
                schedule.NextExecutionTime = GetNextExecutionTime(schedule);
            }
        }
    }

    /// <summary>
    /// Gets all registered schedules with their execution status.
    /// </summary>
    public List<ScheduleStatus> GetScheduleStatuses()
    {
        return _activeSchedules.Values.Select(s => new ScheduleStatus
        {
            BackupJobId = s.BackupJobId,
            NextExecutionTime = s.NextExecutionTime,
            LastExecutionTime = s.LastExecutionTime
        }).ToList();
    }

    /// <summary>
    /// Gets the status of a specific schedule.
    /// </summary>
    public ScheduleStatus? GetScheduleStatus(string backupJobId)
    {
        if (_activeSchedules.TryGetValue(backupJobId, out var schedule))
        {
            return new ScheduleStatus
            {
                BackupJobId = schedule.BackupJobId,
                NextExecutionTime = schedule.NextExecutionTime,
                LastExecutionTime = schedule.LastExecutionTime
            };
        }

        return null;
    }

    /// <summary>
    /// Triggers immediate execution of a specific backup job, ignoring schedule.
    /// </summary>
    public async Task<bool> TriggerManualBackupAsync(string backupJobId, CancellationToken cancellationToken = default)
    {
        if (!_activeSchedules.TryGetValue(backupJobId, out var schedule))
        {
            _logger.LogWarning("Schedule not found for job {JobId}", backupJobId);
            return false;
        }

        try
        {
            _logger.LogInformation("Triggering manual backup for job {JobId}", backupJobId);
            await schedule.BackupAction(cancellationToken);

            schedule.LastExecutionTime = DateTime.UtcNow;
            schedule.NextExecutionTime = GetNextExecutionTime(schedule);

            _logger.LogInformation("Manual backup completed for job {JobId}. Next scheduled: {NextExecution}",
                backupJobId, schedule.NextExecutionTime);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual backup trigger failed for job {JobId}", backupJobId);
            return false;
        }
    }

    /// <summary>
    /// Calculates the next execution time for a schedule based on CRON expression.
    /// </summary>
    private DateTime? GetNextExecutionTime(ScheduleEntry schedule)
    {
        try
        {
            var nextUtc = schedule.CronExpression.GetNextOccurrence(
                DateTime.UtcNow,
                schedule.TimeZone);

            return nextUtc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating next execution time for job {JobId}", schedule.BackupJobId);
            return null;
        }
    }

    /// <summary>
    /// Represents the execution status of a schedule.
    /// </summary>
    public class ScheduleStatus
    {
        public string BackupJobId { get; set; } = null!;
        public DateTime? NextExecutionTime { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public int DaysUntilNextExecution => NextExecutionTime.HasValue
            ? (int)Math.Ceiling((NextExecutionTime.Value - DateTime.UtcNow).TotalDays)
            : 0;
    }
}
