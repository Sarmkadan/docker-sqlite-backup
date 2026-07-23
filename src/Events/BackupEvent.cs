#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Represents an event in the backup system. Base class for all domain events.
/// </summary>
public abstract class BackupEvent
{
    public string EventType { get; }
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    protected BackupEvent(string eventType)
    {
        EventType = eventType;
    }
}

/// <summary>
/// Event fired when a backup starts.
/// </summary>
public class BackupStartedEvent : BackupEvent
{
    public BackupSchedule Schedule { get; set; } = null!;
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the reason for the backup start.
    /// "Scheduled" for regular scheduled backups, "CatchUp" for backups triggered after container restart.
    /// </summary>
    public string Reason { get; set; } = "Scheduled";

    public BackupStartedEvent() : base("backup.started") { }
}

/// <summary>
/// Event fired when a backup completes successfully.
/// </summary>
public class BackupCompletedEvent : BackupEvent
{
    public BackupResult Result { get; set; } = null!;
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the cron expression of the schedule that produced this backup, used by
    /// <see cref="DockerSqliteBackup.Health.HealthStatusEventListener"/> to compute the
    /// expected freshness window for the Docker healthcheck.
    /// </summary>
    public string? ScheduleCronExpression { get; set; }

    public BackupCompletedEvent() : base("backup.completed") { }
}

/// <summary>
/// Event fired when a backup fails.
/// </summary>
public class BackupFailedEvent : BackupEvent
{
    public Guid ScheduleId { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }

    public BackupFailedEvent() : base("backup.failed") { }
}

/// <summary>
/// Event fired when a backup is retried.
/// </summary>
public class BackupRetryEvent : BackupEvent
{
    public Guid ScheduleId { get; set; }
    public int AttemptNumber { get; set; }
    public string? PreviousError { get; set; }

    public BackupRetryEvent() : base("backup.retry") { }
}

/// <summary>
/// Event fired when a schedule is created.
/// </summary>
public class ScheduleCreatedEvent : BackupEvent
{
    public BackupSchedule Schedule { get; set; } = null!;

    public ScheduleCreatedEvent() : base("schedule.created") { }
}

/// <summary>
/// Event fired when a schedule is updated.
/// </summary>
public class ScheduleUpdatedEvent : BackupEvent
{
    public Guid ScheduleId { get; set; }
    public Dictionary<string, object> Changes { get; set; } = [];

    public ScheduleUpdatedEvent() : base("schedule.updated") { }
}

/// <summary>
/// Event fired when a schedule is deleted.
/// </summary>
public class ScheduleDeletedEvent : BackupEvent
{
    public Guid ScheduleId { get; set; }
    public string ScheduleName { get; set; } = string.Empty;

    public ScheduleDeletedEvent() : base("schedule.deleted") { }
}

/// <summary>
/// Event fired when restoration verification completes.
/// </summary>
public class RestoreVerificationCompletedEvent : BackupEvent
{
    public Guid BackupResultId { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }

    public RestoreVerificationCompletedEvent() : base("restore.verification.completed") { }
}

/// <summary>
/// Event fired for health status changes.
/// </summary>
public class HealthCheckEvent : BackupEvent
{
    public string ComponentName { get; set; } = string.Empty;
    public string Status { get; set; } = "ok";
    public string? Message { get; set; }

    public HealthCheckEvent() : base("health.check") { }
}
