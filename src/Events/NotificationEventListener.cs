#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Integration;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Event listener that sends notifications on important backup events.
/// Supports multiple notification channels (console, email, Slack, webhooks).
/// </summary>
public class NotificationEventListener : IBackupEventListener
{
    private readonly List<INotificationClient> _notificationClients = [];
    private readonly WebhookClient? _webhookClient;
    private readonly ILogger<NotificationEventListener> _logger;
    private readonly string? _webhookUrl;

    public NotificationEventListener(
        ILogger<NotificationEventListener> logger,
        WebhookClient? webhookClient = null,
        string? webhookUrl = null)
    {
        _logger = logger;
        _webhookClient = webhookClient;
        _webhookUrl = webhookUrl;

        // Add default console notification for all environments
        _notificationClients.Add(new ConsoleNotificationClient());
    }

    /// <summary>
    /// Registers a notification client.
    /// </summary>
    public void AddNotificationClient(INotificationClient client)
    {
        _notificationClients.Add(client);
    }

    /// <summary>
    /// Handles backup events and sends appropriate notifications.
    /// </summary>
    public async Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        switch (@event)
        {
            case BackupCompletedEvent completedEvent:
                await HandleBackupCompletedAsync(completedEvent, cancellationToken);
                break;
            case BackupFailedEvent failedEvent:
                await HandleBackupFailedAsync(failedEvent, cancellationToken);
                break;
            case BackupRetryEvent retryEvent:
                await HandleBackupRetryAsync(retryEvent, cancellationToken);
                break;
            case ScheduleCreatedEvent createdEvent:
                await HandleScheduleCreatedAsync(createdEvent, cancellationToken);
                break;
            case RestoreVerificationCompletedEvent verificationEvent:
                await HandleVerificationCompletedAsync(verificationEvent, cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Gets the event types this listener handles.
    /// </summary>
    public IEnumerable<string> GetSupportedEventTypes()
    {
        yield return "backup.completed";
        yield return "backup.failed";
        yield return "backup.retry";
        yield return "schedule.created";
        yield return "restore.verification.completed";
    }

    /// <summary>
    /// Checks if this listener can handle the given event type.
    /// </summary>
    public bool CanHandle(string eventType)
    {
        return GetSupportedEventTypes().Contains(eventType);
    }

    private async Task HandleBackupCompletedAsync(BackupCompletedEvent @event, CancellationToken ct)
    {
        var title = "✓ Backup Completed";
        var message = $"Backup completed successfully in {@event.Duration.TotalSeconds:F2} seconds. " +
                     $"Size: {FormatBytes(@event.Result.BackupFileSizeBytes)}";

        await SendNotificationsAsync(title, message, ct);

        if (_webhookClient  is not null && !string.IsNullOrEmpty(_webhookUrl))
        {
            try
            {
                await _webhookClient.SendBackupNotificationAsync(_webhookUrl, @event.Result, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send backup notification webhook");
            }
        }
    }

    private async Task HandleBackupFailedAsync(BackupFailedEvent @event, CancellationToken ct)
    {
        var title = "✗ Backup Failed";
        var message = $"Backup failed: {@event.ErrorMessage}";

        await SendNotificationsAsync(title, message, ct);
    }

    private async Task HandleBackupRetryAsync(BackupRetryEvent @event, CancellationToken ct)
    {
        var title = "⟲ Backup Retry";
        var message = $"Retrying backup (attempt {AttemptDisplay(@event.AttemptNumber)})";

        await SendNotificationsAsync(title, message, ct);
    }

    private async Task HandleScheduleCreatedAsync(ScheduleCreatedEvent @event, CancellationToken ct)
    {
        var title = "✓ Schedule Created";
        var message = $"New schedule created: {@event.Schedule.Name} (Cron: {@event.Schedule.CronExpression})";

        await SendNotificationsAsync(title, message, ct);
    }

    private async Task HandleVerificationCompletedAsync(
        RestoreVerificationCompletedEvent @event,
        CancellationToken ct)
    {
        var title = @event.IsValid ? "✓ Verification Passed" : "✗ Verification Failed";
        var message = @event.ValidationMessage ?? "Restore verification completed";

        await SendNotificationsAsync(title, message, ct);
    }

    private async Task SendNotificationsAsync(string title, string message, CancellationToken ct)
    {
        var tasks = _notificationClients.Select(client => client.SendAsync(title, message, ct));
        await Task.WhenAll(tasks);
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static string AttemptDisplay(int attempt)
    {
        return attempt switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{attempt}th"
        };
    }
}
