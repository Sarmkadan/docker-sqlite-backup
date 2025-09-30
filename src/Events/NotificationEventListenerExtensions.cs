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
    public static NotificationEventListener AddNotificationClients(
        this NotificationEventListener listener,
        IEnumerable<INotificationClient> clients)
    {
        if (clients == null)
        {
            throw new ArgumentNullException(nameof(clients));
        }

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
    public static NotificationEventListener AddNotificationClients(
        this NotificationEventListener listener,
        Func<IEnumerable<INotificationClient>> clientFactory)
    {
        if (clientFactory == null)
        {
            throw new ArgumentNullException(nameof(clientFactory));
        }

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
    /// <exception cref="InvalidOperationException">Thrown when the event type is not supported</exception>
    public static Task HandleAsync(
        this NotificationEventListener listener,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        if (listener == null)
        {
            throw new ArgumentNullException(nameof(listener));
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("Event type cannot be null or empty", nameof(eventType));
        }

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
            _ => throw new InvalidOperationException($"Event type '{eventType}' is not supported by this listener")
        };

        return listener.HandleAsync(backupEvent, cancellationToken);
    }

    /// <summary>
    /// Gets all supported event types as a hash set for efficient lookups.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <returns>HashSet containing all supported event types</returns>
    public static HashSet<string> GetSupportedEventTypesSet(this NotificationEventListener listener)
    {
        if (listener == null)
        {
            throw new ArgumentNullException(nameof(listener));
        }

        return new HashSet<string>(listener.GetSupportedEventTypes());
    }

    /// <summary>
    /// Checks if this listener can handle any of the provided event types.
    /// </summary>
    /// <param name="listener">The event listener instance</param>
    /// <param name="eventTypes">Collection of event types to check</param>
    /// <returns>True if any of the event types are supported</returns>
    public static bool CanHandleAny(
        this NotificationEventListener listener,
        IEnumerable<string> eventTypes)
    {
        if (listener == null)
        {
            throw new ArgumentNullException(nameof(listener));
        }

        if (eventTypes == null)
        {
            throw new ArgumentNullException(nameof(eventTypes));
        }

        var supportedTypes = listener.GetSupportedEventTypesSet();
        return eventTypes.Any(supportedTypes.Contains);
    }
}