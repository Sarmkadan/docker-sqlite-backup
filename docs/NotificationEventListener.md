# NotificationEventListener

The `NotificationEventListener` class provides a centralized mechanism for receiving and dispatching event notifications within the `docker-sqlite-backup` application. It maintains a collection of registered notification clients and coordinates the delivery of events to those clients based on supported event types. This class is typically used to decouple event producers from consumers, allowing multiple clients to subscribe to specific event types without direct dependencies.

## API

### `public NotificationEventListener()`

Initializes a new instance of the `NotificationEventListener` class. The instance starts with an empty set of registered clients and no supported event types.

**Parameters:** None.

**Return value:** None.

**Throws:** None.

---

### `public void AddNotificationClient(INotificationClient client)`

Registers a notification client to receive events. The client must implement the `INotificationClient` interface.

**Parameters:**
- `client` (`INotificationClient`): The client to register. Must not be `null`.

**Return value:** None.

**Throws:**
- `ArgumentNullException`: If `client` is `null`.
- `InvalidOperationException`: If the client is already registered.

---

### `public async Task HandleAsync(NotificationEvent notificationEvent)`

Processes a notification event by delivering it to all registered clients that support the event's type. The method awaits the completion of each client's handling logic.

**Parameters:**
- `notificationEvent` (`NotificationEvent`): The event to handle. Must not be `null` and must have a non-null, non-empty `EventType` property.

**Return value:** A `Task` representing the asynchronous operation.

**Throws:**
- `ArgumentNullException`: If `notificationEvent` is `null`.
- `ArgumentException`: If `notificationEvent.EventType` is `null` or empty.
- `InvalidOperationException`: If no clients are registered.

---

### `public IEnumerable<string> GetSupportedEventTypes()`

Returns the collection of event type strings that this listener can currently handle. The returned collection is based on the event types supported by all currently registered clients.

**Parameters:** None.

**Return value:** An `IEnumerable<string>` containing the distinct event type strings. May be empty if no clients are registered.

**Throws:** None.

---

### `public bool CanHandle(string eventType)`

Determines whether the listener can handle an event of the specified type. This is `true` if at least one registered client supports the given event type.

**Parameters:**
- `eventType` (`string`): The event type to check. Must not be `null` or empty.

**Return value:** `true` if the event type is supported; otherwise `false`.

**Throws:**
- `ArgumentNullException`: If `eventType` is `null`.
- `ArgumentException`: If `eventType` is empty.

## Usage

### Example 1: Registering a client and handling an event

```csharp
using DockerSqliteBackup.Notifications;

var listener = new NotificationEventListener();
var emailClient = new EmailNotificationClient("smtp.example.com");
var logClient = new LogNotificationClient("backup.log");

listener.AddNotificationClient(emailClient);
listener.AddNotificationClient(logClient);

var backupCompletedEvent = new NotificationEvent
{
    EventType = "BackupCompleted",
    Data = new { Database = "mydb.sqlite", Timestamp = DateTime.UtcNow }
};

await listener.HandleAsync(backupCompletedEvent);
```

### Example 2: Checking support before handling

```csharp
using DockerSqliteBackup.Notifications;

var listener = new NotificationEventListener();
listener.AddNotificationClient(new SlackNotificationClient("#backups"));

string eventType = "BackupFailed";
if (listener.CanHandle(eventType))
{
    var failureEvent = new NotificationEvent
    {
        EventType = eventType,
        Data = new { Error = "Disk full", Database = "mydb.sqlite" }
    };
    await listener.HandleAsync(failureEvent);
}
else
{
    Console.WriteLine($"No client registered for event type '{eventType}'.");
}
```

## Notes

- **Thread safety:** The `NotificationEventListener` is not inherently thread-safe. Concurrent calls to `AddNotificationClient`, `HandleAsync`, `GetSupportedEventTypes`, or `CanHandle` from multiple threads may result in inconsistent state or exceptions. External synchronization (e.g., a lock) is required if the instance is shared across threads.
- **Duplicate clients:** Attempting to register the same client instance more than once throws an `InvalidOperationException`. Equality is determined by the client's reference identity.
- **Empty event types:** Both `HandleAsync` and `CanHandle` reject `null` or empty event type strings. Clients should ensure that `NotificationEvent.EventType` is always a non-empty string.
- **No clients registered:** Calling `HandleAsync` when no clients are registered throws an `InvalidOperationException`. Use `GetSupportedEventTypes` or `CanHandle` to verify registration before handling.
- **Asynchronous handling:** `HandleAsync` awaits each client's handling method sequentially. If a client throws an exception, the exception propagates and subsequent clients may not receive the event. Consider implementing fault-tolerant clients or wrapping calls in try-catch blocks.
