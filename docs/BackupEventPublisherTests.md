# BackupEventPublisherTests

Unit tests for `BackupEventPublisher`, verifying correct behavior of event publishing and listener management in the SQLite backup system.

## API

### `BackupEventPublisherTests`

Public test class containing scenarios for `BackupEventPublisher` functionality.

### `async Task PublishAsync_NoListeners_DoesNotThrow`

Verifies that publishing an event with no registered listeners completes without throwing an exception.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Does not throw under any condition

### `async Task PublishAsync_WithMatchingListener_InvokesListener`

Ensures that a listener subscribed to the exact event type receives the published event.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if the listener is not invoked

### `async Task PublishAsync_ListenerDoesNotMatchEventType_ListenerNotInvoked`

Confirms that a listener subscribed to a different event type is not invoked when an unrelated event is published.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if the listener is incorrectly invoked

### `async Task PublishAsync_ListenerThrows_DoesNotPropagateException`

Validates that exceptions thrown by a listener do not propagate to the publisher or disrupt other listeners.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Does not throw; test fails if exception escapes

### `async Task PublishAsync_MultipleMatchingListeners_AllInvoked`

Checks that all listeners subscribed to the same event type are invoked in registration order when the event is published.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if any listener is skipped

### `async Task Subscribe_SameListenerTwice_RegistersOnlyOnce`

Ensures idempotent subscription; registering the same listener multiple times results in a single effective subscription.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if the listener is invoked more than once

### `async Task Unsubscribe_RegisteredListener_NoLongerReceivesEvents`

Confirms that unsubscribing a listener prevents it from receiving future events.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if the listener is still invoked after unsubscription

### `void Unsubscribe_UnregisteredListener_DoesNotThrow`

Validates that attempting to unsubscribe a listener that was never registered does not throw an exception.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: Does not throw under any condition

### `async Task PublishAsync_OneListenerFails_OtherListenerStillInvoked`

Ensures that a failure in one listener does not prevent other listeners from being invoked.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: Fails the test if any non-failing listener is not invoked

## Usage
