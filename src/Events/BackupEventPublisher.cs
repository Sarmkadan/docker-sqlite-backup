#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Publisher for backup events. Manages event listeners and publishes events
/// to all registered subscribers. Supports both synchronous and asynchronous handling.
/// </summary>
public class BackupEventPublisher : IBackupEventPublisher
{
    private readonly List<IBackupEventListener> _listeners = [];
    private readonly ILogger<BackupEventPublisher> _logger;
    private readonly object _subscriberLock = new();

    public BackupEventPublisher(ILogger<BackupEventPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Publishes an event to all registered listeners.
    /// Listeners are called in parallel but exceptions are caught and logged.
    /// </summary>
    public async Task PublishAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publishing event: {EventType} [{EventId}] from correlation {CorrelationId}",
            @event.EventType,
            @event.EventId,
            @event.CorrelationId);

        var applicableListeners = GetApplicableListeners(@event.EventType);

        if (!applicableListeners.Any())
        {
            _logger.LogDebug("No listeners registered for event type: {EventType}", @event.EventType);
            return;
        }

        var tasks = applicableListeners.Select(listener => HandleListenerAsync(listener, @event, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Subscribes a listener to events.
    /// </summary>
    public void Subscribe(IBackupEventListener listener)
    {
        lock (_subscriberLock)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
                _logger.LogInformation(
                    "Event listener subscribed: {ListenerType} for events: {EventTypes}",
                    listener.GetType().Name,
                    string.Join(", ", listener.GetSupportedEventTypes()));
            }
        }
    }

    /// <summary>
    /// Unsubscribes a listener from events.
    /// </summary>
    public void Unsubscribe(IBackupEventListener listener)
    {
        lock (_subscriberLock)
        {
            if (_listeners.Remove(listener))
            {
                _logger.LogInformation(
                    "Event listener unsubscribed: {ListenerType}",
                    listener.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Gets all listeners that can handle a specific event type.
    /// </summary>
    private IEnumerable<IBackupEventListener> GetApplicableListeners(string eventType)
    {
        lock (_subscriberLock)
        {
            return _listeners.Where(l => l.CanHandle(eventType)).ToList();
        }
    }

    /// <summary>
    /// Handles a single listener's event processing with error handling.
    /// </summary>
    private async Task HandleListenerAsync(
        IBackupEventListener listener,
        BackupEvent @event,
        CancellationToken cancellationToken)
    {
        try
        {
            await listener.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Event listener {ListenerType} failed to handle event {EventType}",
                listener.GetType().Name,
                @event.EventType);
        }
    }
}
