# RetentionPolicy

`RetentionPolicy` (namespace `DockerSqliteBackup.Domain`, file `src/Domain/RetentionPolicy.cs`) describes how long backup files should be kept, grouped into daily, weekly and monthly tiers, plus an optional total storage cap. It also defines the `RetentionTier` enum that classifies a backup by age.

`RetentionPolicy` is a domain model with classification helpers. It does not delete anything. Actual file and metadata removal is performed by `RotationService` using `RotationPolicy` (see `docs/RotationPolicy.md`). See [Current limitations](#current-limitations) before relying on this type for cleanup.

## Properties

| Property | Type | Default | Meaning |
|---|---|---|---|
| `Id` | `Guid` | `Guid.NewGuid()` | Unique identifier. |
| `Name` | `string` | `""` | Human-readable label, for example `"Production 30-day"`. Must not be empty for the policy to be valid. |
| `DailyRetentionDays` | `int` | `7` | Age window, in days, for the daily tier. Must be at least `1`. |
| `WeeklyRetentionWeeks` | `int` | `4` | Age window, in weeks, for the weekly tier. `0` disables the tier. Must not be negative. |
| `MonthlyRetentionMonths` | `int` | `12` | Age window, in months, for the monthly tier. `0` disables the tier. Must not be negative. |
| `MaxTotalStorageBytes` | `long` | `0` | Storage cap in bytes. `0` means unlimited. Must not be negative. |
| `PrioritizeVerifiedBackups` | `bool` | `true` | Intended to prefer deleting unverified backups under storage pressure. |
| `CreatedAt` | `DateTime` | `DateTime.UtcNow` | Creation time. |
| `LastModifiedAt` | `DateTime` | `DateTime.UtcNow` | Last modification time. |

## Retention rules

Tiers are evaluated in order, using the backup age in UTC (`DateTime.UtcNow - backupDate`):

1. **Daily:** age `<= DailyRetentionDays` days.
2. **Weekly:** `WeeklyRetentionWeeks > 0` and age `<= WeeklyRetentionWeeks * 7` days.
3. **Monthly:** `MonthlyRetentionMonths > 0` and age `<= MonthlyRetentionMonths * 30` days. A month is approximated as 30 days.
4. **Expired:** everything older than the last enabled window.

The first matching tier wins. A disabled tier (`0`) is skipped, so with `WeeklyRetentionWeeks = 0` a backup older than the daily window goes straight to the monthly check.

Representative helpers exist for the weekly and monthly tiers:

- `IsWeeklyRepresentative(DateTime)` returns `true` when the backup date falls on a Sunday.
- `IsMonthlyRepresentative(DateTime)` returns `true` when the backup date is the 1st of the month.

These are static and independent of `ClassifyBackup`. Callers that want "one backup per week or month" must apply them explicitly.

## Methods

- `bool IsValid()`: returns `false` if `Name` is blank, `DailyRetentionDays < 1`, or any of `WeeklyRetentionWeeks`, `MonthlyRetentionMonths`, `MaxTotalStorageBytes` is negative. Otherwise `true`.
- `RetentionTier ClassifyBackup(DateTime backupDate)`: returns the tier for a backup created at `backupDate`.
- `bool IsExpired(DateTime backupDate)`: `true` when `ClassifyBackup` returns `RetentionTier.Expired`.
- `static bool IsWeeklyRepresentative(DateTime backupDate)`, `static bool IsMonthlyRepresentative(DateTime backupDate)`: see above.
- `ToString()`: returns a summary of `Id`, `Name` and the three retention windows.

## RetentionTier

| Value | Meaning |
|---|---|
| `Daily` | Within the daily window. |
| `Weekly` | Within the weekly window. |
| `Monthly` | Within the monthly window. |
| `Expired` | Outside all enabled windows. Eligible for deletion. |

## Usage

```csharp
var policy = new RetentionPolicy
{
    Name = "Production 30-day",
    DailyRetentionDays = 7,
    WeeklyRetentionWeeks = 4,
    MonthlyRetentionMonths = 12,
    MaxTotalStorageBytes = 50L * 1024 * 1024 * 1024 // 50 GB
};

if (!policy.IsValid())
    throw new InvalidOperationException("Invalid retention policy");

DateTime createdAt = DateTime.UtcNow.AddDays(-20);
RetentionTier tier = policy.ClassifyBackup(createdAt); // Weekly
bool delete = policy.IsExpired(createdAt);            // false
```

## Cleanup

Cleanup of backup files is not implemented on top of `RetentionPolicy`. The only deletion path in the codebase is `RotationService.ExecuteRotationAsync(Guid scheduleId)`, which:

1. Loads the `RotationPolicy` for the schedule. Returns `0` if there is no policy or the strategy is `NoRotation`.
2. Selects candidate backups via `GetBackupsForRotationAsync`.
3. Deletes each backup file from disk and removes its `BackupResult` record through the repository.
4. Stamps `LastRotatedAt` on the rotation policy and saves it.

If you need retention-driven cleanup, `RetentionPolicy.ClassifyBackup` or `IsExpired` can supply the decision, but a caller must still perform the deletion and the repository update.

## Current limitations

These behaviours are visible in the source and should be kept in mind:

- **Not wired to deletion.** No production code calls `ClassifyBackup` or `IsExpired` to remove files. `RetentionPolicy` is referenced only from its own file.
- **Representative helpers are not used by `ClassifyBackup`.** The XML comments say the weekly tier keeps Sunday backups and the monthly tier keeps 1st-of-month backups, but `ClassifyBackup` returns `Weekly` or `Monthly` for every backup inside the window. Filtering to representatives is not implemented.
- **`MaxTotalStorageBytes` is not enforced.** No code compares total size against the cap.
- **`PrioritizeVerifiedBackups` is not read.** The flag has no effect in the current code.
- **Monthly window uses 30-day months.** Expiry will drift by a few days per year compared with calendar months.
- **Timestamps are not normalised.** `ClassifyBackup` compares against `DateTime.UtcNow`, so `backupDate` must be UTC. A local-time value shifts the result by the UTC offset.
- **`CreatedAt` and `LastModifiedAt` are not updated automatically.** Callers must set `LastModifiedAt` when they change the policy.

## Tests

There are currently no unit tests for `RetentionPolicy` or `RetentionTier`. Adding them under `tests/docker-sqlite-backup.Tests/Domain/` is the obvious next step, covering the boundaries of each tier and the validation rules.
