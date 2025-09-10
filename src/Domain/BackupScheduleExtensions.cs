#nullable enable

using Cronos;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides extension methods for <see cref="BackupSchedule"/> to enhance scheduling and backup operations.
/// </summary>
public static class BackupScheduleExtensions
{
    /// <summary>
    /// Gets the next backup run time based on the schedule's cron expression.
    /// </summary>
    /// <param name="schedule">The backup schedule.</param>
    /// <returns>The next scheduled run time, or null if the schedule is not active.</returns>
    public static DateTime? GetNextRunTime(this BackupSchedule schedule)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        if (!schedule.IsActive || string.IsNullOrWhiteSpace(schedule.CronExpression))
        {
            return null;
        }

        try
        {
            var cron = CronExpression.Parse(schedule.CronExpression, CronFormat.Standard);
            var next = cron.GetNextOccurrence(DateTime.UtcNow);
            return next;
        }
        catch (CronFormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Determines if a backup should be performed based on the schedule configuration.
    /// </summary>
    /// <param name="schedule">The backup schedule.</param>
    /// <returns>True if a backup should be performed; otherwise, false.</returns>
    public static bool ShouldPerformBackup(this BackupSchedule schedule)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        if (!schedule.IsActive || !schedule.IsValid())
        {
            return false;
        }

        if (!schedule.ValidateDatabasePath())
        {
            return false;
        }

        // If we have a last backup time, check if enough time has passed
        if (schedule.LastBackupAt.HasValue)
        {
            var nextRun = schedule.GetNextRunTime();
            if (nextRun.HasValue && DateTime.UtcNow < nextRun.Value)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the formatted backup name for this schedule.
    /// </summary>
    /// <param name="schedule">The backup schedule.</param>
    /// <param name="timestamp">Optional timestamp for the backup; defaults to current UTC time.</param>
    /// <returns>A formatted backup name including schedule name and timestamp.</returns>
    public static string GetBackupName(this BackupSchedule schedule, DateTime? timestamp = null)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        var time = timestamp ?? DateTime.UtcNow;
        var safeName = string.IsNullOrWhiteSpace(schedule.Name)
            ? "backup"
            : schedule.Name.Trim().Replace(" ", "_").ToLowerInvariant();

        return $"{safeName}_{time:yyyyMMdd_HHmmss}";
    }

    /// <summary>
    /// Gets the backup file extension based on the backup mode.
    /// </summary>
    /// <param name="schedule">The backup schedule.</param>
    /// <returns>The appropriate file extension for the backup type.</returns>
    public static string GetBackupFileExtension(this BackupSchedule schedule)
    {
        if (schedule is null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        return schedule.BackupMode switch
        {
            (int)Constants.BackupMode.Full => ".full.db",
            (int)Constants.BackupMode.Incremental => ".inc.db",
            _ => ".db"
        };
    }
}