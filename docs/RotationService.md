# RotationService

`RotationService` (`src/Services/RotationService.cs`) implements `IRotationService`. It stores and retrieves the rotation policy for a backup schedule, computes which backups the policy would delete, executes the deletion, and previews the disk space that would be freed.

Backup candidates come from the backup metadata history in `IBackupRepository`, not from a directory scan. Each deletion removes the backup file (if it still exists on disk) and then the metadata record.

## Dependencies

| Dependency | Purpose |
|---|---|
| `IBackupRepository` | Reads and writes `RotationPolicy` and backup history (`GetBackupHistoryAsync`, `DeleteBackupResultAsync`). |
| `ILogger<RotationService>` | Logs policy evaluation, deletions, and failures. |

Both constructor arguments are required. Passing `null` throws `ArgumentNullException` (the project's `DockerSqliteBackup.Exceptions.ArgumentNullException`).

## Rotation policy

The policy is a `RotationPolicy` (`src/Domain/RotationPolicy.cs`). The fields that `RotationService` reads:

| Field | Effect in `RotationService` |
|---|---|
| `Strategy` | Mapped to `Constants.RotationStrategy`. `NoRotation` disables rotation in `ExecuteRotationAsync` and `PreviewRotationAsync`. Other values are evaluated by `ShouldRotate`. |
| `MinimumBackupCount` | The newest N backups (by `StartedAt`, descending) are never candidates. |
| `MaxBackupCount`, `MaxAgeDays` | Thresholds used by `RotationPolicy.ShouldRotate`. |
| `DeleteFailedBackups` | Failed backups beyond the minimum count are deleted directly (see the candidate rules below). |
| `VerifyBeforeDeletion` | Non-failed candidates are kept unless `Status` equals `BackupStatus.VerifiedSuccess`. |
| `LastRotatedAt` | Set to the current UTC time after `ExecuteRotationAsync` runs past the `NoRotation` check. |
| `LastModifiedAt` | Set to the current UTC time by `SaveRotationPolicyAsync`. |

## API

### `RotationService(IBackupRepository repository, ILogger<RotationService> logger)`

Constructor. Stores both dependencies.

---

### `Task<int> ExecuteRotationAsync(Guid scheduleId)`

Applies the policy for `scheduleId` and deletes the matching backups.

**Steps**
1. Throws `System.ArgumentException` if `scheduleId` is `Guid.Empty`.
2. Loads the policy. If there is no policy, or `Strategy` is `NoRotation`, logs and returns `0`. `LastRotatedAt` is not updated in this case.
3. Gets candidates from `GetBackupsForRotationAsync(scheduleId)`.
4. For each candidate, deletes the file if `BackupFilePath` is set and the file exists, then calls `DeleteBackupResultAsync`. The count increments only after both steps succeed.
5. If a single candidate fails (any exception), logs a warning and continues with the next one. Failures do not propagate.
6. Sets `LastRotatedAt` to `DateTime.UtcNow` and saves the policy through `IBackupRepository.SaveRotationPolicyAsync`. This happens even if nothing was deleted, and it bypasses the validation in `SaveRotationPolicyAsync`.

**Returns** the number of backups successfully deleted.

---

### `Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId)`

Returns the policy stored for the schedule, or `null` if none exists. Passes straight through to the repository.

---

### `Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy)`

Validates and saves a policy.

**Steps**
1. `policy == null` throws `DockerSqliteBackup.Exceptions.ArgumentNullException`.
2. `policy.IsValid()` returning `false` throws `ValidationException` (the field name is `policy`). Nothing is saved.
3. Sets `LastModifiedAt` to `DateTime.UtcNow`.
4. Saves through the repository. On success, logs and returns the saved policy. Any exception from the repository is wrapped in `RotationException` with the original as the inner exception.

`RotationPolicy.IsValid()` rules: `MaxBackupCount` must not be negative, `MaxAgeDays` must be at least 1, and `MinimumBackupCount` must be at least 1. When `MaxBackupCount` is greater than 0, `MinimumBackupCount` must not exceed it. `MaxBackupCount == 0` means unlimited by count.

---

### `Task<IEnumerable<BackupResult>> GetBackupsForRotationAsync(Guid scheduleId)`

Returns the backups that the current policy would delete. This method does not delete anything. It is the candidate logic shared by execution, the disk space calculation, and preview.

**Candidate rules**

1. Loads the full history for the schedule (`GetBackupHistoryAsync(scheduleId, int.MaxValue)`) and the policy. If there is no policy, returns an empty sequence.
2. Sorts the history by `StartedAt`, newest first.
3. Walks the list starting at index `MinimumBackupCount`, so the newest `MinimumBackupCount` backups are always kept. `remainingCount` starts at the total history count and decreases by one for each backup added.
4. For each backup:
   - If `DeleteFailedBackups` is true and the backup is not successful, it is added. `ShouldRotate` and `VerifyBeforeDeletion` are not checked for this case.
   - Otherwise, it is added only if `policy.ShouldRotate(remainingCount, StartedAt, isFailed)` is true.
   - If `VerifyBeforeDeletion` is true and `Status` is not `VerifiedSuccess`, it is skipped and an information log entry is written.

Note that the failed-backup branch runs before the `NoRotation` check in `ExecuteRotationAsync`. See the notes below.

---

### `Task<long> CalculateDiskSpaceFreedAsync(Guid scheduleId)`

Returns the sum of `BackupFileSizeBytes` over `GetBackupsForRotationAsync(scheduleId)`. Does not check `NoRotation` (see notes).

---

### `Task<(IEnumerable<BackupResult> backupsToDelete, long diskSpaceFreed)> PreviewRotationAsync(Guid scheduleId)`

Dry run. If there is no policy or `Strategy` is `NoRotation`, returns an empty list and `0`. Otherwise returns the same candidates as `GetBackupsForRotationAsync` and their total size, and logs the result. Nothing is deleted.

## Usage

```csharp
// Save a policy: keep at least 3 backups, at most 10 by count, 30 days by age
var policy = new RotationPolicy
{
    ScheduleId = scheduleId,
    Strategy = (int)RotationStrategy.Combined,
    MaxBackupCount = 10,
    MaxAgeDays = 30,
    MinimumBackupCount = 3,
    VerifyBeforeDeletion = true,
    DeleteFailedBackups = true
};
await rotationService.SaveRotationPolicyAsync(policy);

// Preview before deleting anything
var (candidates, bytes) = await rotationService.PreviewRotationAsync(scheduleId);
logger.LogInformation("Would delete {Count} backups, freeing {Bytes} bytes", candidates.Count(), bytes);

// Apply the policy
int deleted = await rotationService.ExecuteRotationAsync(scheduleId);
```

## Notes

- **Partial failures.** `ExecuteRotationAsync` deletes candidates one by one. A failure on one backup is logged and skipped, so the method can return a count lower than the number of candidates without throwing. A crash mid-loop leaves the remaining candidates in place. There is no transaction.
- **`NoRotation` is inconsistent across methods.** `ExecuteRotationAsync` and `PreviewRotationAsync` return early for `NoRotation`. `GetBackupsForRotationAsync` and `CalculateDiskSpaceFreedAsync` do not. With `NoRotation` and `DeleteFailedBackups` enabled, `CalculateDiskSpaceFreedAsync` can report a non-zero value for failed backups that `ExecuteRotationAsync` will not delete.
- **Failed backups bypass verification.** When `DeleteFailedBackups` is true, failed backups beyond `MinimumBackupCount` are deleted even if `VerifyBeforeDeletion` is enabled.
- **Policy saved during execution.** `ExecuteRotationAsync` writes `LastRotatedAt` through the repository without re-validating the policy.
- **No directory locking.** The method does not lock the backup directory. Files changed between the preview and the execution can make the results differ.
- **Repeated reads.** `ExecuteRotationAsync` loads the policy in step 2, and `GetBackupsForRotationAsync` loads it again. The two reads are not atomic.
- **Concurrency.** The service holds no mutable state of its own. Concurrent `SaveRotationPolicyAsync` and `ExecuteRotationAsync` calls for the same schedule can race, and execution uses whichever policy the repository returns when it reads it.
