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
    Task SendAsync(string title, string message, CancellationToken cancellationToken = default);
    bool IsConfigured();
}

/// <summary>
/// Writes notifications to the console. Used as the default when no external
/// notification channel is configured.
/// </summary>
public class ConsoleNotificationClient : INotificationClient
{
    public Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{title}] {message}");
        Console.ResetColor();
        return Task.CompletedTask;
    }

    public bool IsConfigured() => true;
}
