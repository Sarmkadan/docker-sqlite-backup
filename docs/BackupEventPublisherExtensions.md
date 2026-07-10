# BackupEventPublisherExtensions

The `BackupEventPublisherExtensions` class provides a set of static extension methods designed to simplify event publication and manage temporary event subscriptions within the `docker-sqlite-backup` project. These utilities enhance the robustness of the event handling system by providing mechanisms for asynchronous publication, automatic caller information tracking for diagnostics, and deterministic lifecycle management of event subscriptions through `IDisposable`.

## API

### PublishWithCallerInfoAsync
Publishes an event while automatically capturing and attaching contextual caller information (such as file path, line number, and member name) to the event payload. This facilitates detailed diagnostic logging and debugging of event-driven operations.
*   **Parameters:** `publisher` (the event publisher instance), `eventData` (the data to be published).
*   **Returns:** `Task` representing the asynchronous operation.
*   **Throws:** `InvalidOperationException` if the publisher is not initialized or if the event data is null.

### SubscribeTemporarily
Registers a temporary event subscription. This method returns an `IDisposable` object that maintains the active subscription. When the returned object is disposed, the subscription is automatically removed.
*   **Parameters:** `publisher` (the event publisher instance), `handler` (the callback function to execute when the event occurs).
*   **Returns:** `IDisposable` representing the temporary subscription handle.
*   **Throws:** `ArgumentNullException` if the publisher or handler is null.

### PublishAsync<TEvent>
Publishes an event of the specified generic type `TEvent` asynchronously.
*   **Parameters:** `publisher` (the event publisher instance), `eventData` (the event data of type `TEvent`).
*   **Returns:** `Task` representing the asynchronous operation.
*   **Throws:** `InvalidOperationException` if the publisher is not initialized.

### SubscriptionHandle
A class representing a managed event subscription. It encapsulates the subscription state and implements `IDisposable` to ensure that resources are cleaned up and the subscription is unsubscribed when no longer needed.

### Dispose
Used within `SubscriptionHandle` to release the subscription. When called, it ensures that the associated handler is detached from the event publisher.
*   **Returns:** `void`.

## Usage

```csharp
// Example 1: Publishing events
// Publishing an event with automatic caller information for enhanced logging
await BackupEventPublisherExtensions.PublishWithCallerInfoAsync(publisher, new BackupStartedEvent());

// Publishing a specific event type asynchronously
await BackupEventPublisherExtensions.PublishAsync<BackupCompletedEvent>(publisher, new BackupCompletedEvent(backupId));
```

```csharp
// Example 2: Managing temporary subscriptions
// Subscribing temporarily using a using block for deterministic cleanup
using (IDisposable subscription = BackupEventPublisherExtensions.SubscribeTemporarily(publisher, (BackupEvent e) => 
{
    Console.WriteLine($"Event received: {e.GetType().Name}");
}))
{
    // The handler is active only within the scope of this block
    await PerformBackupOperationAsync();
} 
// Subscription is automatically removed here when the Dispose method is called
```

## Notes

*   **Thread Safety:** The event handlers invoked by `SubscribeTemporarily` must be thread-safe, as events may be published from multiple threads concurrently.
*   **Lifecycle Management:** Always utilize the `using` pattern or explicit `Dispose` calls when using `SubscribeTemporarily` to prevent memory leaks and unintended handler execution after the required duration.
*   **Error Handling:** Exceptions thrown within an event handler will propagate to the publisher's execution context. Ensure that handlers contain appropriate error-handling logic to prevent halting the publication process for subsequent subscribers.
*   **Multiple Disposals:** The `Dispose` method on `SubscriptionHandle` is designed to be idempotent; calling it multiple times will have no additional effect and will not throw an exception.
