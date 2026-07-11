# RotationServiceExtensions

Extension methods for `RotationService` that provide asynchronous operations for managing backup rotation policies and cleanup.

## API

### `ExecuteRotationAsync`

Performs a backup rotation by deleting old backups according to the current rotation policy.

**Parameters**
- `service` (`RotationService`): The rotation service instance.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Return Value**
- `Task<int>`: The number of backups deleted during rotation.

**Exceptions**
- `ArgumentNullException`: Thrown if `service` is `null`.
- `OperationCanceledException`: Thrown if the operation is canceled via `cancellationToken`.

---

### `GetRotationPolicyAsync`

Retrieves the current rotation policy used for backup management.

**Parameters**
- `service` (`RotationService`): The rotation service instance.

**Return Value**
- `Task<RotationPolicy>`: A task that resolves to the current `RotationPolicy` instance.

**Exceptions**
- `ArgumentNullException`: Thrown if `service` is `null`.

---

### `ShouldRotateAsync`

Determines whether a backup rotation should be performed based on the current policy and state.

**Parameters**
- `service` (`RotationService`): The rotation service instance.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Return Value**
- `Task<bool>`: A task that resolves to `true` if rotation is required; otherwise, `false`.

**Exceptions**
- `ArgumentNullException`: Thrown if `service` is `null`.
- `OperationCanceledException`: Thrown if the operation is canceled via `cancellationToken`.

---
### `GetBackupsToDeleteCountAsync`

Calculates the number of backups that would be deleted if a rotation were performed.

**Parameters**
- `service` (`RotationService`): The rotation service instance.
- `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

**Return Value**
- `Task<int>`: A task that resolves to the count of backups to be deleted.

**Exceptions**
- `ArgumentNullException`: Thrown if `service` is `null`.
- `OperationCanceledException`: Thrown if the operation is canceled via `cancellationToken`.

## Usage
