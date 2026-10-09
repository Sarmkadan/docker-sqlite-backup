#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for schedule-related errors.
/// </summary>
public class ScheduleException : DockerSqliteBackupException
{
    /// <summary>Gets the schedule ID associated with the error.</summary>
    public Guid? ScheduleId { get; }

    /// <summary>
    /// Initializes a new instance of the ScheduleException class.
    /// </summary>
    /// <param name="message">The error message describing the schedule failure.</param>
    public ScheduleException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the ScheduleException class with a schedule ID.
    /// </summary>
    /// <param name="message">The error message describing the schedule failure.</param>
    /// <param name="scheduleId">The ID of the schedule associated with the error.</param>
    public ScheduleException(string message, Guid scheduleId) : base(message)
    {
        ScheduleId = scheduleId;
    }

    /// <summary>
    /// Initializes a new instance of the ScheduleException class with an inner exception.
    /// </summary>
    /// <param name="message">The error message describing the schedule failure.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    public ScheduleException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a cron expression is invalid.
/// </summary>
public class InvalidCronExpressionException : ScheduleException
{
    /// <summary>
    /// Initializes a new instance of the InvalidCronExpressionException class.
    /// </summary>
    /// <param name="cronExpression">The invalid cron expression that was provided.</param>
    public InvalidCronExpressionException(string cronExpression)
        : base($"Invalid cron expression: '{cronExpression}'") { }
}

/// <summary>
/// Exception thrown when schedule validation fails.
/// </summary>
public class InvalidScheduleException : ScheduleException
{
    /// <summary>
    /// Initializes a new instance of the InvalidScheduleException class.
    /// </summary>
    /// <param name="message">The error message describing the validation failure.</param>
    /// <param name="scheduleId">The ID of the schedule that failed validation.</param>
    public InvalidScheduleException(string message, Guid scheduleId)
        : base(message, scheduleId) { }
}
