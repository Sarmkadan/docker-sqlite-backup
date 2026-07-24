#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Integration;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Extension methods for <see cref="NotificationEventListener"/> that provide additional functionality
/// for managing notification clients and handling specific event types.
/// </summary>
public static class NotificationEventListenerExtensions
{
    /// <summary>
    /// Adds multiple notification clients to the listener at once.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <param name="clients">Collection of notification clients to add</param>
    /// <returns>The listener instance for method chaining</returns>
    /// <exception cref="ArgumentNullException"><paramref name="listener"/> or <paramref name="clients"/> is <see langword="null"/></exception>
    public static NotificationEventListener AddNotificationClients(
        this NotificationEventListener listener,
        IEnumerable<INotificationClient> clients)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(clients);

        foreach (var client in clients)
        {
            listener.AddNotificationClient(client);
        }

        return listener;
    }

    /// <summary>
    /// Adds a collection of notification clients from a factory method.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <param name="clientFactory">Factory method that creates notification clients</param>
    /// <returns>The listener instance for method chaining</returns>
    /// <exception cref="ArgumentNullException"><paramref name="listener"/> or <paramref name="clientFactory"/> is <see langword="null"/></exception>
    public static NotificationEventListener AddNotificationClients(
        this NotificationEventListener listener,
        Func<IEnumerable<INotificationClient>> clientFactory)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var clients = clientFactory();
        return listener.AddNotificationClients(clients);
    }

    /// <summary>
    /// Creates and handles a backup event asynchronously based on the event type string.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <param name="eventType">The type of event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    /// <exception cref="ArgumentNullException"><paramref name="listener"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is <see langword="null"/>, empty, or whitespace</exception>
    /// <exception cref="InvalidOperationException">Thrown when the event type is not supported</exception>
    public static Task HandleAsync(
        this NotificationEventListener listener,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType, nameof(eventType));

        if (!listener.CanHandle(eventType))
        {
            throw new InvalidOperationException($"Event type '{eventType}' is not supported by this listener");
        }

        // Create the appropriate event type based on the event type string
        BackupEvent backupEvent = eventType switch
        {
            "backup.completed" => new BackupCompletedEvent(),
            "backup.failed" => new BackupFailedEvent(),
            "backup.retry" => new BackupRetryEvent(),
            "schedule.created" => new ScheduleCreatedEvent(),
            "restore.verification.completed" => new RestoreVerificationCompletedEvent(),
            "restore.verification.failed" => new RestoreVerificationFailedEvent(),
            _ => throw new InvalidOperationException($"Event type '{eventType}' is not supported by this listener")
        };

        return listener.HandleAsync(backupEvent, cancellationToken);
    }

    /// <summary>
    /// Gets all supported event types as a hash set for efficient lookups.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <returns>HashSet containing all supported event types</returns>
    /// <exception cref="ArgumentNullException"><paramref name="listener"/> is <see langword="null"/></exception>
    public static HashSet<string> GetSupportedEventTypesSet(this NotificationEventListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        return new HashSet<string>(listener.GetSupportedEventTypes());
    }

    /// <summary>
    /// Checks if this listener can handle any of the provided event types.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <param name="eventTypes">Collection of event types to check</param>
    /// <returns>True if any of the event types are supported</returns>
    /// <exception cref="ArgumentNullException"><paramref name="listener"/> or <paramref name="eventTypes"/> is <see langword="null"/></exception>
    public static bool CanHandleAny(
        this NotificationEventListener listener,
        IEnumerable<string> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(eventTypes);

        var supportedTypes = listener.GetSupportedEventTypesSet();
        return eventTypes.Any(supportedTypes.Contains);
    }
}