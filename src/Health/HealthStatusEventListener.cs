#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Events;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Health;

/// <summary>
/// Event listener that persists the last backup completion, backup failure, and restore
/// verification outcome to a <see cref="HealthStatusStore"/>, so the <c>healthcheck</c> CLI
/// subcommand can evaluate freshness from a separate process invocation.
/// </summary>
public class HealthStatusEventListener : IBackupEventListener
{
    private readonly HealthStatusStore _store;
    private readonly ILogger<HealthStatusEventListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStatusEventListener"/> class.
    /// </summary>
    /// <param name="store">The status store used to persist snapshots.</param>
    /// <param name="logger">The logger used to report persistence failures.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="store"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public HealthStatusEventListener(HealthStatusStore store, ILogger<HealthStatusEventListener> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Handles a backup event, updating the persisted health status snapshot for the event
    /// types this listener supports.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="event"/> is <see langword="null"/>.</exception>
    public Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            switch (@event)
            {
                case BackupCompletedEvent completed:
                    _store.Update(snapshot =>
                    {
                        snapshot.LastBackupCompletedAt = completed.OccurredAt;
                        snapshot.LastBackupCompletedCronExpression = completed.ScheduleCronExpression;
                    });
                    break;
                case BackupFailedEvent failed:
                    _store.Update(snapshot =>
                    {
                        snapshot.LastBackupFailedAt = failed.OccurredAt;
                        snapshot.LastBackupFailedMessage = failed.ErrorMessage;
                    });
                    break;
                case RestoreVerificationCompletedEvent verification:
                    _store.Update(snapshot =>
                    {
                        snapshot.LastRestoreVerificationAt = verification.OccurredAt;
                        snapshot.LastRestoreVerificationPassed = verification.IsValid;
                        snapshot.LastRestoreVerificationMessage = verification.ValidationMessage;
                    });
                    break;
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to persist health status for event {EventType}", @event.EventType);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the event types this listener handles.
    /// </summary>
    /// <returns>The event type strings this listener reacts to.</returns>
    public IEnumerable<string> GetSupportedEventTypes()
    {
        yield return "backup.completed";
        yield return "backup.failed";
        yield return "restore.verification.completed";
    }

    /// <summary>
    /// Determines if this listener can handle the given event type.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns><see langword="true"/> when this listener supports the event type; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventType"/> is <see langword="null"/>.</exception>
    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return GetSupportedEventTypes().Contains(eventType);
    }
}
