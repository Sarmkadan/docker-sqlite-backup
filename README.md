// entire file content ...
// ... goes in between

## NotificationEventListener

The `NotificationEventListener` class is responsible for sending notifications on important backup events. It supports multiple notification channels and can be extended with custom notification clients.

### Usage Example

```csharp
using DockerSqliteBackup.Events;

// Create a notification event listener
var listener = new NotificationEventListener(
    logger: new Logger<NotificationEventListener>(),
    webhookClient: new WebhookClient(),
    webhookUrl: "https://example.com/webhook");

// Add a custom notification client
listener.AddNotificationClient(new CustomNotificationClient());

// Handle a backup completed event
var completedEvent = new BackupCompletedEvent
{
    Result = new BackupResult
    {
        Id = Guid.NewGuid(),
        ScheduleId = Guid.NewGuid(),
        StartedAt = DateTime.UtcNow,
        CompletedAt = DateTime.UtcNow,
        Status = BackupStatus.Success,
        SizeBytes = 1024 * 1024,
        FilePath = "/backups/app.db.20260714"
    },
    Duration = TimeSpan.FromMinutes(5)
};

await listener.HandleAsync(completedEvent);
```

// ... rest of code ...
