#nullable enable
// Author: Vladyslav Zaiets

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
    /// <param name="eventType">The event type name to check, as returned by <see cref="BackupEvent.EventType"/>.</param>
    /// <returns><c>true</c> if this listener handles the event type; otherwise, <c>false</c>.</returns>
    bool CanHandle(string eventType);
}

/// <summary>
/// Generic event listener for a specific event type.
/// </summary>
public abstract class EventListener<TEvent> : IBackupEventListener where TEvent : BackupEvent
{
    /// <summary>
    /// Passes the event to <see cref="OnEventAsync"/> when it is of type <typeparamref name="TEvent"/>;
    /// otherwise, ignores it.
    /// </summary>
    /// <param name="event">The backup event to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the event has been handled or ignored.</returns>
    public virtual Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event is TEvent typedEvent)
            return OnEventAsync(typedEvent, cancellationToken);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the event type of <typeparamref name="TEvent"/>.
    /// </summary>
    /// <remarks>
    /// Creates a temporary instance of <typeparamref name="TEvent"/> to read its <see cref="BackupEvent.EventType"/>.
    /// Falls back to the CLR type name when that value is unavailable.
    /// Requires <typeparamref name="TEvent"/> to have a public parameterless constructor;
    /// otherwise <see cref="Activator.CreateInstance(Type)"/> throws.
    /// </remarks>
    /// <returns>A sequence containing the single event type this listener handles.</returns>
    public virtual IEnumerable<string> GetSupportedEventTypes()
    {
        var eventType = Activator.CreateInstance(typeof(TEvent)) as BackupEvent;
        yield return eventType?.EventType ?? typeof(TEvent).Name;
    }

    /// <summary>
    /// Determines if this listener can handle the given event type.
    /// </summary>
    /// <param name="eventType">The event type name to check.</param>
    /// <returns><c>true</c> if <paramref name="eventType"/> matches a supported event type; otherwise, <c>false</c>.</returns>
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
