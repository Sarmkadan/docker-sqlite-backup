#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for schedule-related errors.
/// </summary>
public class ScheduleException : Exception
{
    /// <summary>Gets the schedule ID associated with the error.</summary>
    public Guid? ScheduleId { get; }

    /// <summary>
    /// Initializes a new instance of the ScheduleException class.
    /// </summary>
    public ScheduleException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with schedule ID.
    /// </summary>
    public ScheduleException(string message, Guid scheduleId) : base(message)
    {
        ScheduleId = scheduleId;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public ScheduleException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a cron expression is invalid.
/// </summary>
public class InvalidCronExpressionException : ScheduleException
{
    public InvalidCronExpressionException(string cronExpression)
        : base($"Invalid cron expression: '{cronExpression}'") { }
}

/// <summary>
/// Exception thrown when schedule validation fails.
/// </summary>
public class InvalidScheduleException : ScheduleException
{
    public InvalidScheduleException(string message, Guid scheduleId)
        : base(message, scheduleId) { }
}
