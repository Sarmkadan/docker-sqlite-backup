# NotificationClient

The `NotificationClient` file in `src/Integration/` defines the contract for notification delivery channels in the `docker-sqlite-backup` application, plus a default console implementation. It contains two public types:

- `INotificationClient`: the interface every delivery channel implements. It has a send operation and a configuration check.
- `ConsoleNotificationClient`: a channel that writes notifications to the console. It is the default when no external channel is configured.

Callers depend on `INotificationClient` rather than a concrete channel, so new channels (for example, webhook or chat-based delivery) can be added without changing the code that sends notifications.

All types live in the `DockerSqliteBackup.Integration` namespace.

## API

### `public interface INotificationClient`

Contract for a notification delivery channel.

---

### `Task SendAsync(string title, string message, CancellationToken cancellationToken = default)`

Sends a notification through the channel.

**Parameters:**
- `title` (`string`): Short notification title.
- `message` (`string`): Notification body text.
- `cancellationToken` (`CancellationToken`): Token to cancel the send operation. Defaults to `CancellationToken.None`.

**Return value:** A `Task` that completes when the notification has been delivered.

**Throws:** Implementation-specific. The interface does not declare exceptions; see each implementation.

---

### `bool IsConfigured()`

Indicates whether the channel has all settings required to deliver notifications. Typically checked before `SendAsync` when delivery is optional and a missing configuration should be skipped.

**Parameters:** None.

**Return value:** `true` if the channel is configured; otherwise `false`.

**Throws:** None.

---

### `public class ConsoleNotificationClient : INotificationClient`

Writes notifications to the console. It requires no configuration, so it is always usable and is the fallback channel when no external one is set up.

#### `public Task SendAsync(string title, string message, CancellationToken cancellationToken = default)`

Writes the notification to the console in yellow, formatted as `[title] message`, and then resets the console colour.

**Parameters:**
- `title` (`string`): Short notification title. Must not be `null`.
- `message` (`string`): Notification body text. Must not be `null`.
- `cancellationToken` (`CancellationToken`): Ignored. The console write is synchronous.

**Return value:** A completed `Task`.

**Throws:**
- `ArgumentNullException`: If `title` or `message` is `null`.

---

#### `public bool IsConfigured()`

Always returns `true`, because the console channel needs no configuration.

**Parameters:** None.

**Return value:** `true`.

**Throws:** None.

## Usage

### Example 1: Sending a notification through the console channel

```csharp
using DockerSqliteBackup.Integration;

INotificationClient client = new ConsoleNotificationClient();

if (client.IsConfigured())
{
    await client.SendAsync(
        "Backup completed",
        "daily.db.gz was created and verified (2.4 MB).");
}
```

Output (yellow text in the console):

```
[Backup completed] daily.db.gz was created and verified (2.4 MB).
```

### Example 2: Implementing a custom channel

A new channel implements `INotificationClient`. It reports whether it is configured, and it honours the cancellation token when it performs real I/O.

```csharp
using DockerSqliteBackup.Integration;

public class EmailNotificationClient : INotificationClient
{
    private readonly string? _smtpHost;

    public EmailNotificationClient(string? smtpHost)
    {
        _smtpHost = smtpHost;
    }

    public bool IsConfigured() => !string.IsNullOrWhiteSpace(_smtpHost);

    public Task SendAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();

        // Deliver the message via _smtpHost here.
        return Task.CompletedTask;
    }
}
```

Callers can then choose between channels at runtime:

```csharp
INotificationClient client = new EmailNotificationClient(smtpHost);

if (!client.IsConfigured())
{
    client = new ConsoleNotificationClient();
}

await client.SendAsync("Backup failed", "Checksum mismatch on daily.db.gz.", cancellationToken);
```
