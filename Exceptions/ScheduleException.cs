// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Thrown when a backup schedule operation encounters an error.
/// </summary>
public class ScheduleException : Exception
{
    /// <summary>
    /// The backup job ID associated with this schedule error, if available.
    /// </summary>
    public string? BackupJobId { get; set; }

    /// <summary>
    /// The CRON expression that caused the error, if applicable.
    /// </summary>
    public string? CronExpression { get; set; }

    /// <summary>
    /// Initializes a new instance of ScheduleException with a message.
    /// </summary>
    public ScheduleException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of ScheduleException with a message and inner exception.
    /// </summary>
    public ScheduleException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with message and schedule context.
    /// </summary>
    public ScheduleException(string message, string? backupJobId, string? cronExpression)
        : base(message)
    {
        BackupJobId = backupJobId;
        CronExpression = cronExpression;
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and schedule context.
    /// </summary>
    public ScheduleException(string message, Exception innerException, string? backupJobId, string? cronExpression)
        : base(message, innerException)
    {
        BackupJobId = backupJobId;
        CronExpression = cronExpression;
    }
}
