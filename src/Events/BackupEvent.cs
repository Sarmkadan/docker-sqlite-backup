#nullable enable
// Author: Vladyslav Zaiets

using System;
using System.Collections.Generic;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Contains validation constants used across event types to bound untrusted input.
/// </summary>
internal static class ValidationConstants
{
    /// <summary>Maximum allowed length for generic string fields.</summary>
    public const int MaxStringLength = 1024;

    /// <summary>Maximum allowed length for schedule‑related names.</summary>
    public const int MaxNameLength = 256;

    /// <summary>Maximum allowed length for the <c>Reason</c> field of <see cref="BackupStartedEvent"/>.</summary>
    public const int MaxReasonLength = 128;

    /// <summary>Maximum allowed length for a cron expression.</summary>
    public const int MaxCronExpressionLength = 256;

    /// <summary>Maximum allowed retry attempt count.</summary>
    public const int MaxRetryCount = 10;
}

/// <summary>
/// Represents an event in the backup system. Base class for all domain events.
/// </summary>
public abstract class BackupEvent
{
    /// <summary>Gets the event type identifier.</summary>
    public string EventType { get; }

    /// <summary>Gets the unique identifier of the event.</summary>
    public Guid EventId { get; } = Guid.NewGuid();

    /// <summary>Gets the timestamp when the event occurred (UTC).</summary>
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    /// <summary>Gets or sets the correlation identifier for tracing.</summary>
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Initializes a new instance of <see cref="BackupEvent"/>.
    /// </summary>
    /// <param name="eventType">The event type identifier.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null, empty, or exceeds <see cref="ValidationConstants.MaxStringLength"/>.</exception>
    protected BackupEvent(string eventType)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventType);
        if (eventType.Length > ValidationConstants.MaxStringLength)
            throw new ArgumentException($"Event type length exceeds {ValidationConstants.MaxStringLength}.", nameof(eventType));

        EventType = eventType;
    }
}

/// <summary>
/// Event fired when a backup starts.
/// </summary>
public class BackupStartedEvent : BackupEvent
{
    private string _reason = "Scheduled";

    /// <summary>Gets or sets the backup schedule that triggered the start.</summary>
    public BackupSchedule Schedule { get; set; } = null!;

    /// <summary>Gets or sets the start time of the backup.</summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the reason for the backup start.
    /// "Scheduled" for regular scheduled backups, "CatchUp" for backups triggered after container restart.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value is null, empty, or exceeds <see cref="ValidationConstants.MaxReasonLength"/>.</exception>
    public string Reason
    {
        get => _reason;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if (value.Length > ValidationConstants.MaxReasonLength)
                throw new ArgumentException($"Reason length exceeds {ValidationConstants.MaxReasonLength}.", nameof(value));
            _reason = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BackupStartedEvent"/>.
    /// </summary>
    public BackupStartedEvent() : base("backup.started") { }
}

/// <summary>
/// Event fired when a backup completes successfully.
/// </summary>
public class BackupCompletedEvent : BackupEvent
{
    private string? _scheduleCronExpression;

    /// <summary>Gets or sets the result of the backup.</summary>
    public BackupResult Result { get; set; } = null!;

    /// <summary>Gets or sets the duration of the backup operation.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the cron expression of the schedule that produced this backup,
    /// used by <see cref="DockerSqliteBackup.Health.HealthStatusEventListener"/> to compute the
    /// expected freshness window for the Docker healthcheck.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxCronExpressionLength"/>.</exception>
    public string? ScheduleCronExpression
    {
        get => _scheduleCronExpression;
        set
        {
            if (value != null && value.Length > ValidationConstants.MaxCronExpressionLength)
                throw new ArgumentException($"Cron expression length exceeds {ValidationConstants.MaxCronExpressionLength}.", nameof(value));
            _scheduleCronExpression = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BackupCompletedEvent"/>.
    /// </summary>
    public BackupCompletedEvent() : base("backup.completed") { }
}

/// <summary>
/// Event fired when a backup fails.
/// </summary>
public class BackupFailedEvent : BackupEvent
{
    private string _errorMessage = string.Empty;
    private string? _stackTrace;

    /// <summary>Gets or sets the identifier of the schedule that failed.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the error message describing the failure.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxStringLength"/>.</exception>
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if (value.Length > ValidationConstants.MaxStringLength)
                throw new ArgumentException($"ErrorMessage length exceeds {ValidationConstants.MaxStringLength}.", nameof(value));
            _errorMessage = value;
        }
    }

    /// <summary>
    /// Gets or sets the optional stack trace of the failure.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxStringLength"/>.</exception>
    public string? StackTrace
    {
        get => _stackTrace;
        set
        {
            if (value != null && value.Length > ValidationConstants.MaxStringLength)
                throw new ArgumentException($"StackTrace length exceeds {ValidationConstants.MaxStringLength}.", nameof(value));
            _stackTrace = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BackupFailedEvent"/>.
    /// </summary>
    public BackupFailedEvent() : base("backup.failed") { }
}

/// <summary>
/// Event fired when a backup is retried.
/// </summary>
public class BackupRetryEvent : BackupEvent
{
    private int _attemptNumber;

    /// <summary>Gets or sets the identifier of the schedule being retried.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the attempt number of the retry.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is less than 1 or greater than <see cref="ValidationConstants.MaxRetryCount"/>.</exception>
    public int AttemptNumber
    {
        get => _attemptNumber;
        set
        {
            if (value < 1 || value > ValidationConstants.MaxRetryCount)
                throw new ArgumentOutOfRangeException(nameof(value), $"AttemptNumber must be between 1 and {ValidationConstants.MaxRetryCount}.");
            _attemptNumber = value;
        }
    }

    /// <summary>Gets or sets the previous error message, if any.</summary>
    public string? PreviousError { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="BackupRetryEvent"/>.
    /// </summary>
    public BackupRetryEvent() : base("backup.retry") { }
}

/// <summary>
/// Event fired when a schedule is created.
/// </summary>
public class ScheduleCreatedEvent : BackupEvent
{
    /// <summary>Gets or sets the schedule that was created.</summary>
    public BackupSchedule Schedule { get; set; } = null!;

    /// <summary>Gets the identifier of the newly created schedule.</summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Schedule"/> is null.</exception>
    public Guid ScheduleId => Schedule?.Id ?? throw new InvalidOperationException("Schedule must be set before accessing ScheduleId.");

    /// <summary>Gets the name of the newly created schedule.</summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Schedule"/> is null.</exception>
    public string ScheduleName => Schedule?.Name ?? throw new InvalidOperationException("Schedule must be set before accessing ScheduleName.");

    /// <summary>Gets the cron expression of the newly created schedule.</summary>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="Schedule"/> is null.</exception>
    public string? ScheduleCronExpression => Schedule?.CronExpression;

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduleCreatedEvent"/>.
    /// </summary>
    public ScheduleCreatedEvent() : base("schedule.created") { }
}

/// <summary>
/// Event fired when a schedule is updated.
/// </summary>
public class ScheduleUpdatedEvent : BackupEvent
{
    /// <summary>Gets or sets the identifier of the schedule being updated.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the new state of the schedule after the update.</summary>
    public BackupSchedule NewSchedule { get; set; } = null!;

    /// <summary>Gets or sets the previous state of the schedule before the update, if available.</summary>
    public BackupSchedule? OldSchedule { get; set; }

    private Dictionary<string, object> _changes = [];

    /// <summary>
    /// Gets or sets the collection of changes applied to the schedule.
    /// Keys represent the property name, values represent the new value.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the dictionary contains more than 50 entries.</exception>
    public Dictionary<string, object> Changes
    {
        get => _changes;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count > 50)
                throw new ArgumentException("Changes dictionary cannot contain more than 50 entries.", nameof(value));
            _changes = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduleUpdatedEvent"/>.
    /// </summary>
    public ScheduleUpdatedEvent() : base("schedule.updated") { }
}

/// <summary>
/// Event fired when a schedule is deleted.
/// </summary>
public class ScheduleDeletedEvent : BackupEvent
{
    private string _scheduleName = string.Empty;
    private string? _scheduleCronExpression;

    /// <summary>Gets or sets the identifier of the schedule being deleted.</summary>
    public Guid ScheduleId { get; set; }

    /// <summary>Gets or sets the name of the schedule being deleted.</summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxNameLength"/>.</exception>
    public string ScheduleName
    {
        get => _scheduleName;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if (value.Length > ValidationConstants.MaxNameLength)
                throw new ArgumentException($"ScheduleName length exceeds {ValidationConstants.MaxNameLength}.", nameof(value));
            _scheduleName = value;
        }
    }

    /// <summary>
    /// Gets or sets the cron expression of the schedule being deleted.
    /// This mirrors the property on <see cref="ScheduleCreatedEvent"/> and <see cref="ScheduleUpdatedEvent"/>.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxCronExpressionLength"/>.</exception>
    public string? ScheduleCronExpression
    {
        get => _scheduleCronExpression;
        set
        {
            if (value != null && value.Length > ValidationConstants.MaxCronExpressionLength)
                throw new ArgumentException($"Cron expression length exceeds {ValidationConstants.MaxCronExpressionLength}.", nameof(value));
            _scheduleCronExpression = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ScheduleDeletedEvent"/>.
    /// </summary>
    public ScheduleDeletedEvent() : base("schedule.deleted") { }
}

/// <summary>
/// Event fired when restoration verification completes.
/// </summary>
public class RestoreVerificationCompletedEvent : BackupEvent
{
    /// <summary>Gets or sets the identifier of the backup result that was verified.</summary>
    public Guid BackupResultId { get; set; }

    /// <summary>Gets or sets a value indicating whether the verification succeeded.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets an optional validation message.</summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="RestoreVerificationCompletedEvent"/>.
    /// </summary>
    public RestoreVerificationCompletedEvent() : base("restore.verification.completed") { }
}

/// <summary>
/// Identifies which stage of the restore verification pipeline produced a
/// <see cref="RestoreVerificationFailedEvent"/>.
/// </summary>
public enum RestoreVerificationFailureStage
{
    /// <summary>The failure occurred while validating a restored backup's checksum.</summary>
    ChecksumMismatch,

    /// <summary>The failure occurred while restoring the backup to a temporary location.</summary>
    RestoreFailed,

    /// <summary>The failure occurred during the SQLite <c>PRAGMA integrity_check</c> step.</summary>
    IntegrityCheckFailed,

    /// <summary>The failure occurred for a reason not covered by the other verification stages.</summary>
    Unknown
}

/// <summary>
/// Event fired when restoration verification fails, whether due to a checksum mismatch,
/// a failed restore, or a failed integrity check. Emitted so monitoring can distinguish
/// "verification never ran" from "verification ran and failed".
/// </summary>
public class RestoreVerificationFailedEvent : BackupEvent
{
    /// <summary>Gets or sets the identifier of the backup that failed verification.</summary>
    public Guid BackupResultId { get; set; }

    /// <summary>Gets or sets the pipeline stage at which verification failed.</summary>
    public RestoreVerificationFailureStage FailureStage { get; set; } = RestoreVerificationFailureStage.Unknown;

    /// <summary>Gets or sets the message of the exception that caused the failure.</summary>
    public string ExceptionMessage { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of <see cref="RestoreVerificationFailedEvent"/> class.
    /// </summary>
    public RestoreVerificationFailedEvent() : base("restore.verification.failed") { }
}

/// <summary>
/// Event fired for health status changes.
/// </summary>
public class HealthCheckEvent : BackupEvent
{
    private string _componentName = string.Empty;
    private string _status = "ok";
    private string? _message;

    /// <summary>Gets or sets the name of the component whose health changed.</summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxNameLength"/>.</exception>
    public string ComponentName
    {
        get => _componentName;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if (value.Length > ValidationConstants.MaxNameLength)
                throw new ArgumentException($"ComponentName length exceeds {ValidationConstants.MaxNameLength}.", nameof(value));
            _componentName = value;
        }
    }

    /// <summary>Gets or sets the health status (e.g., "ok", "degraded", "failed").</summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxStringLength"/>.</exception>
    public string Status
    {
        get => _status;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if (value.Length > ValidationConstants.MaxStringLength)
                throw new ArgumentException($"Status length exceeds {ValidationConstants.MaxStringLength}.", nameof(value));
            _status = value;
        }
    }

    /// <summary>Gets or sets an optional message providing additional health information.</summary>
    /// <exception cref="ArgumentException">Thrown when the value exceeds <see cref="ValidationConstants.MaxStringLength"/>.</exception>
    public string? Message
    {
        get => _message;
        set
        {
            if (value != null && value.Length > ValidationConstants.MaxStringLength)
                throw new ArgumentException($"Message length exceeds {ValidationConstants.MaxStringLength}.", nameof(value));
            _message = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HealthCheckEvent"/>.
    /// </summary>
    public HealthCheckEvent() : base("health.check") { }
}
