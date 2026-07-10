# RotationServiceTests

Unit test class for verifying the behavior of `RotationService` in the docker-sqlite-backup project. It validates rotation policy handling, backup eligibility for deletion, disk space calculations, and policy persistence. The tests cover both success and failure scenarios, including validation, edge cases, and state transitions.

## API

### `RotationServiceTests`
Constructor for the test class. Initializes test dependencies and mocks required for testing rotation behavior.

### `ExecuteRotationAsync_NoPolicyFound_ReturnsZero`
Verifies that when no rotation policy exists, the rotation operation returns zero deleted backups.

- **Parameters**: None
- **Return value**: `Task<int>` returning zero
- **Throws**: None

### `ExecuteRotationAsync_NoRotationStrategy_ReturnsZero`
Ensures that when no rotation strategy is configured, the rotation operation returns zero deleted backups.

- **Parameters**: None
- **Return value**: `Task<int>` returning zero
- **Throws**: None

### `ExecuteRotationAsync_WithBackupsEligibleForDeletion_ReturnsDeletedCount`
Confirms that backups eligible for deletion are correctly identified and counted during rotation.

- **Parameters**: None
- **Return value**: `Task<int>` returning the count of deleted backups
- **Throws**: None

### `ExecuteRotationAsync_UpdatesLastRotatedAt`
Validates that the `LastRotatedAt` timestamp is updated after a successful rotation.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: None

### `GetBackupsForRotationAsync_NoPolicyFound_ReturnsEmpty`
Checks that no backups are returned for rotation when no policy exists.

- **Parameters**: None
- **Return value**: `Task<IEnumerable<Backup>>` returning an empty enumerable
- **Throws**: None

### `GetBackupsForRotationAsync_BelowMinimumCount_ReturnsEmpty`
Ensures that backups below the minimum count threshold are not returned for rotation.

- **Parameters**: None
- **Return value**: `Task<IEnumerable<Backup>>` returning an empty enumerable
- **Throws**: None

### `GetBackupsForRotationAsync_DeleteFailedBackupsEnabled_IncludesFailedBackups`
Confirms that failed backups are included in rotation when the corresponding policy flag is enabled.

- **Parameters**: None
- **Return value**: `Task<IEnumerable<Backup>>` returning a sequence that may include failed backups
- **Throws**: None

### `SaveRotationPolicyAsync_InvalidPolicy_ThrowsValidationException`
Validates that saving an invalid rotation policy throws a `ValidationException`.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: `ValidationException` when the policy is invalid

### `SaveRotationPolicyAsync_ValidPolicy_SetsLastModifiedAt`
Ensures that a valid rotation policy is saved and its `LastModifiedAt` timestamp is updated.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: None

### `CalculateDiskSpaceFreedAsync_WithBackupsToRotate_SumsSizes`
Verifies that the total disk space freed is correctly calculated based on backups selected for rotation.

- **Parameters**: None
- **Return value**: `Task<long>` returning the summed size in bytes
- **Throws**: None

### `GetRotationPolicyAsync_ExistingPolicy_ReturnsPolicy`
Confirms that an existing rotation policy is correctly retrieved.

- **Parameters**: None
- **Return value**: `Task<RotationPolicy>` returning the policy instance
- **Throws**: None

## Usage

### Example 1: Testing rotation with a valid policy
