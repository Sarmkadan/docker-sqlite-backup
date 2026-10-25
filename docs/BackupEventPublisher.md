# BackupEventPublisher

`BackupEventPublisher` (`src/Events/BackupEventPublisher.cs`) is the in-process event bus of `docker-sqlite-backup`. Backup components publish `BackupEvent` instances to it, and it delivers each event to every registered `IBackupEventListener` that reports it can handle that event type. Listeners are registered at runtime with `Subscribe` and removed with `Unsubscribe`.

The publisher is registered as a singleton behind the `IBackupEventPublisher` interface in `src/Configuration/ServiceCollectionExtensions.cs`. Services such as `VerificationService`, `HealthCheckService`, and `S3StorageBackend` take `IBackupEventPublisher` as an optional or required dependency and call `PublishAsync` on it. Code should depend on the interface and only use the concrete type when it needs the `BackupEventPublisherExtensions` helpers.

## Responsibilities

- Keep a thread-safe list of subscribed listeners.
- Filter listeners per event with `IBackupEventListener.CanHandle(eventType)`.
- Invoke the applicable listeners in parallel and wait for all of them to finish.
- Catch and log listener failures so one faulty listener cannot break publishing or other listeners.
- Log subscription changes and publication activity.

## API

### Constructor

```csharp
public BackupEventPublisher(ILogger<BackupEventPublisher> logger)
```

Creates a publisher that reports through `logger`.

- **Exceptions:** `ArgumentNullException` if `logger` is `null`.

### Methods

```csharp
public async Task PublishAsync(BackupEvent @event, CancellationToken cancellationToken = default)
```

Publishes `@event` to all listeners whose `CanHandle(@event.EventType)` returns `true`.

- Logs the publication at `Information` level with the event type, event ID, and correlation ID.
- If no listener matches, logs at `Debug` level and returns.
- Otherwise starts every matching listener's `HandleAsync` concurrently with `cancellationToken` and waits for all of them with `Task.WhenAll`.
- A listener that throws is logged at `Error` level and does not propagate. Other listeners still run to completion.
- Any other unexpected error during dispatch is also logged, not rethrown.
- **Exceptions:** `ArgumentNullException` if `@event` is `null`. Any other failure is logged, so `PublishAsync` does not throw for listener errors.

```csharp
public void Subscribe(IBackupEventListener listener)
```

Adds `listener` to the subscriber list.

- If the same listener instance is already subscribed, the call does nothing (no duplicate is added).
- Logs the subscription at `Information` level with the listener type name and the event types it reports via `GetSupportedEventTypes()`.
- **Exceptions:** `ArgumentNullException` if `listener` is `null`.

```csharp
public void Unsubscribe(IBackupEventListener listener)
```

Removes `listener` from the subscriber list.

- If the listener is not subscribed, the call does nothing.
- Logs the removal at `Information` level when a listener is actually removed.
- **Exceptions:** `ArgumentNullException` if `listener` is `null`.

### Interface

The class implements `IBackupEventPublisher`, which declares `PublishAsync`, `Subscribe`, and `Unsubscribe`. Consumers that only publish or manage subscriptions should accept that interface.

## Listeners

A listener implements `IBackupEventListener`:

| Member | Purpose |
|---|---|
| `Task HandleAsync(BackupEvent, CancellationToken)` | Processes one event. |
| `IEnumerable<string> GetSupportedEventTypes()` | Lists the event type names the listener handles. Used for logging. |
| `bool CanHandle(string eventType)` | Decides whether the publisher should call `HandleAsync` for this event type. |

For listeners that handle one event class, derive from the abstract `EventListener<TEvent>` helper instead of implementing the interface directly. It overrides `HandleAsync` to forward only `TEvent` instances to `OnEventAsync`, and it derives the supported event type from a temporary `TEvent` instance. This requires `TEvent` to have a public parameterless constructor.

## Usage

### Publishing an event

```csharp
using DockerSqliteBackup.Events;

public class BackupRunner
{
    private readonly IBackupEventPublisher _events;

    public BackupRunner(IBackupEventPublisher events)
    {
        _events = events;
    }

    public async Task ReportFailureAsync(Guid scheduleId, Exception ex, CancellationToken ct)
    {
        var @event = new BackupFailedEvent
        {
            ScheduleId = scheduleId,
            ErrorMessage = ex.Message,
            StackTrace = ex.StackTrace
        };

        await _events.PublishAsync(@event, ct);
    }
}
```

### Writing and subscribing a listener

```csharp
using DockerSqliteBackup.Events;
using Microsoft.Extensions.Logging;

public sealed class FailureLogListener : EventListener<BackupFailedEvent>
{
    private readonly ILogger<FailureLogListener> _logger;

    public FailureLogListener(ILogger<FailureLogListener> logger)
    {
        _logger = logger;
    }

    protected override Task OnEventAsync(BackupFailedEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogError("Backup failed for schedule {ScheduleId}: {Error}", @event.ScheduleId, @event.ErrorMessage);
        return Task.CompletedTask;
    }
}

// Registration and cleanup
var listener = new FailureLogListener(logger);
publisher.Subscribe(listener);

// ... later
publisher.Unsubscribe(listener);
```

### Temporary subscription

`BackupEventPublisherExtensions.SubscribeTemporarily` subscribes a listener and returns an `IDisposable` that unsubscribes it on disposal:

```csharp
using (publisher.SubscribeTemporarily(listener))
{
    // Events published while this scope is alive reach the listener.
    await DoWorkThatPublishesEventsAsync(ct);
}
// listener is unsubscribed here
```

## Notes

- `PublishAsync` completes only after every matching listener has finished. A slow listener delays the caller, so long-running work should be offloaded inside the listener.
- Listeners run concurrently. A listener must not assume that another listener has already run for the same event.
- Errors from listeners are swallowed after logging. Do not rely on `PublishAsync` to report listener failures to the caller.
- Subscriptions are held in memory only. They are not persisted, and a new process starts with no listeners except those it subscribes at startup.
- The subscriber list is guarded by a lock, so `Subscribe`, `Unsubscribe`, and `PublishAsync` may be called from multiple threads. The lock is only held while the matching list is built, not while listeners run.
- Duplicate detection uses `List.Contains`, which relies on `Equals`. Listener classes that do not override `Equals` are compared by reference, so two separate instances of the same listener class are both registered.
