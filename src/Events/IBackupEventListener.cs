// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Events;

/// <summary>
/// Interface for event listeners. Implement to handle specific backup events.
/// </summary>
public interface IBackupEventListener
{
    /// <summary>
    /// Handles a backup event.
    /// </summary>
    Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the event types this listener handles.
    /// </summary>
    IEnumerable<string> GetSupportedEventTypes();

    /// <summary>
    /// Determines if this listener can handle the given event type.
    /// </summary>
    bool CanHandle(string eventType);
}

/// <summary>
/// Generic event listener for a specific event type.
/// </summary>
public abstract class EventListener<TEvent> : IBackupEventListener where TEvent : BackupEvent
{
    public virtual Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is TEvent typedEvent)
            return OnEventAsync(typedEvent, cancellationToken);

        return Task.CompletedTask;
    }

    public virtual IEnumerable<string> GetSupportedEventTypes()
    {
        var eventType = Activator.CreateInstance(typeof(TEvent)) as BackupEvent;
        yield return eventType?.EventType ?? typeof(TEvent).Name;
    }

    public virtual bool CanHandle(string eventType)
    {
        return GetSupportedEventTypes().Contains(eventType);
    }

    /// <summary>
    /// Override this method to handle the typed event.
    /// </summary>
    protected abstract Task OnEventAsync(TEvent @event, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for event publishers.
/// </summary>
public interface IBackupEventPublisher
{
    /// <summary>
    /// Publishes an event to all registered listeners.
    /// </summary>
    Task PublishAsync(BackupEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes a listener to events.
    /// </summary>
    void Subscribe(IBackupEventListener listener);

    /// <summary>
    /// Unsubscribes a listener from events.
    /// </summary>
    void Unsubscribe(IBackupEventListener listener);
}
