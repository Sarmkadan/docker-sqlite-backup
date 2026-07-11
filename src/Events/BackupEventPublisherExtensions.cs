#nullable enable

using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Extension methods for <see cref="BackupEventPublisher"/> that provide convenient
/// operations for event publishing and subscription management.
/// </summary>
public static class BackupEventPublisherExtensions
{
    /// <summary>
    /// Publishes an event and returns a task that completes when all listeners have processed it.
    /// Includes the caller's member name and file path in the event for correlation.
    /// </summary>
    /// <param name="publisher">The event publisher.</param>
    /// <param name="event">The event to publish.</param>
    /// <param name="memberName">Automatically populated with the calling member name.</param>
    /// <param name="filePath">Automatically populated with the source file path.</param>
    /// <param name="lineNumber">Automatically populated with the line number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    public static async Task PublishWithCallerInfoAsync(
        this BackupEventPublisher publisher,
        BackupEvent @event,
        [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
        [System.Runtime.CompilerServices.CallerFilePath] string filePath = "",
        [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(@event);

        @event.CorrelationId = Guid.NewGuid().ToString();

        publisher.GetLogger()?.LogInformation(
            "Publishing event from {MemberName} ({FilePath}:{LineNumber}): {EventType} [{EventId}]",
            memberName,
            filePath,
            lineNumber,
            @event.EventType,
            @event.EventId);

        await publisher.PublishAsync(@event, cancellationToken);
    }

    /// <summary>
    /// Subscribes a listener and returns a disposable that unsubscribes when disposed.
    /// Useful for temporary subscriptions in using blocks.
    /// </summary>
    /// <param name="publisher">The event publisher.</param>
    /// <param name="listener">The listener to subscribe.</param>
    /// <returns>A disposable that unsubscribes the listener when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> or <paramref name="listener"/> is <see langword="null"/>.</exception>
    public static IDisposable SubscribeTemporarily(
        this BackupEventPublisher publisher,
        IBackupEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(listener);

        publisher.Subscribe(listener);
        return new SubscriptionHandle(publisher, listener);
    }

    /// <summary>
    /// Publishes an event to all listeners that support the specific event type.
    /// This is a type-safe wrapper around PublishAsync for known event types.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="publisher">The event publisher.</param>
    /// <param name="event">The typed event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    public static async Task PublishAsync<TEvent>(
        this BackupEventPublisher publisher,
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : BackupEvent
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(@event);

        await publisher.PublishAsync(@event, cancellationToken);
    }

    /// <summary>
    /// Gets the logger associated with the publisher, if available.
    /// </summary>
    /// <param name="publisher">The event publisher.</param>
    /// <returns>The logger instance or null.</returns>
    private static ILogger<BackupEventPublisher>? GetLogger(this BackupEventPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        // Access the logger field directly since it's the most reliable approach
        // This avoids reflection overhead and potential issues with field name changes
        return publisher.GetLoggerField();
    }

    /// <summary>
    /// Gets the logger field from the publisher.
    /// </summary>
    private static ILogger<BackupEventPublisher>? GetLoggerField(this BackupEventPublisher publisher)
    {
        // Use reflection to access the private logger field since it's not publicly exposed
        var loggerField = typeof(BackupEventPublisher)
            .GetField("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return loggerField?.GetValue(publisher) as ILogger<BackupEventPublisher>;
    }

    /// <summary>
    /// Handle for managing temporary subscriptions.
    /// </summary>
    private sealed class SubscriptionHandle : IDisposable
    {
        private readonly BackupEventPublisher _publisher;
        private readonly IBackupEventListener _listener;
        private bool _disposed;

        public SubscriptionHandle(BackupEventPublisher publisher, IBackupEventListener listener)
        {
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(listener);

            _publisher = publisher;
            _listener = listener;
        }

        public void Dispose() => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _publisher.Unsubscribe(_listener);
                _disposed = true;
            }
        }
    }
}
