# BackupJobTests

Unit test class for `BackupJob` functionality, covering status transitions, retry logic, validation, rotation policies, and elapsed time calculations. Tests verify correct behavior under various job states, policy configurations, and edge cases.

## API

### `MarkStarted_PendingJob_SetsStatusToInProgress`
Verifies that invoking `MarkStarted` on a job in the `Pending` state transitions its status to `InProgress`. No parameters or return value; does not throw.

### `MarkCompleted_InProgressJob_SetsStatusAndClearsProcessingFlag`
Ensures that calling `MarkCompleted` on an `InProgress` job updates its status to `Completed` and clears the processing flag. No parameters or return value; does not throw.

### `CanRetry_FailedJobWithRemainingRetries_ReturnsTrue`
Confirms that `CanRetry` returns `true` when the job has failed and has remaining retry attempts. No parameters; returns `bool`; does not throw.

### `CanRetry_FailedJobWithExhaustedRetries_ReturnsFalse`
Validates that `CanRetry` returns `false` when the job has failed and no retry attempts remain. No parameters; returns `bool`; does not throw.

### `CanRetry_SuccessfulJob_ReturnsFalse`
Checks that `CanRetry` returns `false` for a job that completed successfully. No parameters; returns `bool`; does not throw.

### `GetElapsedTime_NotStarted_ReturnsZero`
Asserts that `GetElapsedTime` returns `0` for a job that has not started. No parameters; returns `TimeSpan`; does not throw.

### `GetElapsedTime_CompletedJob_ReturnsPositiveDuration`
Ensures that `GetElapsedTime` returns a positive duration for a completed job. No parameters; returns `TimeSpan`; does not throw.

### `IncrementRetry_CalledMultipleTimes_IncrementsCorrectly`
Verifies that successive calls to `IncrementRetry` increment the retry counter correctly. No parameters or return value; does not throw.

### `IsValid_DefaultPolicy_ReturnsTrue`
Confirms that `IsValid` returns `true` when using default rotation policy values. No parameters; returns `bool`; does not throw.

### `IsValid_ZeroMaxBackupCount_ReturnsTrue_UnlimitedBackupsAllowed`
Validates that `IsValid` returns `true` when `MaxBackupCount` is set to `0`, indicating unlimited backups are allowed. No parameters; returns `bool`; does not throw.

### `IsValid_NegativeMaxBackupCount_ReturnsFalse`
Ensures that `IsValid` returns `false` when `MaxBackupCount` is negative. No parameters; returns `bool`; does not throw.

### `IsValid_ZeroMaxAgeDays_ReturnsFalse`
Checks that `IsValid` returns `false` when `MaxAgeDays` is set to `0`. No parameters; returns `bool`; does not throw.

### `IsValid_MinimumBackupCountExceedsMax_ReturnsFalse`
Validates that `IsValid` returns `false` when the configured `MinimumBackupCount` exceeds `MaxBackupCount`. No parameters; returns `bool`; does not throw.

### `ShouldRotate_NoRotationStrategy_AlwaysReturnsFalse`
Asserts that `ShouldRotate` always returns `false` when no rotation strategy is configured. No parameters; returns `bool`; does not throw.

### `ShouldRotate_MaxFileCountExceeded_ReturnsTrue`
Confirms that `ShouldRotate` returns `true` when the number of backup files exceeds `MaxBackupCount`. No parameters; returns `bool`; does not throw.

### `ShouldRotate_MaxFileCountWithinLimit_ReturnsFalse`
Ensures that `ShouldRotate` returns `false` when the number of backup files is within the `MaxBackupCount` limit. No parameters; returns `bool`; does not throw.

### `ShouldRotate_MaxAgeExceeded_ReturnsTrue`
Validates that `ShouldRotate` returns `true` when the oldest backup file exceeds `MaxAgeDays`. No parameters; returns `bool`; does not throw.

### `ShouldRotate_MaxAgeNotExceeded_ReturnsFalse`
Checks that `ShouldRotate` returns `false` when the oldest backup file is within the `MaxAgeDays` limit. No parameters; returns `bool`; does not throw.

### `ShouldRotate_CombinedStrategyWithOldFile_ReturnsTrue`
Ensures that `ShouldRotate` returns `true` when either the file count or age exceeds its respective limit in a combined strategy. No parameters; returns `bool`; does not throw.

### `IsValid_ValidSchedule_ReturnsTrue`
Confirms that `IsValid` returns `true` when the job schedule is valid. No parameters; returns `bool`; does not throw.

## Usage
