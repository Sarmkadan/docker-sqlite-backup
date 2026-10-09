#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

namespace DockerSqliteBackup.Integration;

/// <summary>
/// Interface for notification delivery channels.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Sends a notification through the channel.
    /// </summary>
    /// <param name="title">Short notification title.</param>
    /// <param name="message">Notification body text.</param>
    /// <param name="cancellationToken">Token to cancel the send operation.</param>
    /// <returns>A task that completes when the notification has been delivered.</returns>
    Task SendAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indicates whether the channel has all settings required to deliver notifications.
    /// </summary>
    /// <returns><c>true</c> if the channel is configured; otherwise <c>false</c>.</returns>
    bool IsConfigured();
}

/// <summary>
/// Writes notifications to the console. Used as the default when no external
/// notification channel is configured.
/// </summary>
public class ConsoleNotificationClient : INotificationClient
{
    /// <summary>
    /// Writes the notification to the console in yellow as <c>[title] message</c>.
    /// </summary>
    /// <param name="title">Short notification title.</param>
    /// <param name="message">Notification body text.</param>
    /// <param name="cancellationToken">Ignored; the console write is synchronous.</param>
    /// <returns>A completed task.</returns>
    public Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{title}] {message}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Always returns <c>true</c>, because the console requires no configuration.
    /// </summary>
    /// <returns><c>true</c>.</returns>
    public bool IsConfigured() => true;
}
