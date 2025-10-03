# RotationService

`RotationService` manages the lifecycle of backup rotation policies and their execution within the `docker-sqlite-backup` system. It provides methods to retrieve, persist, and evaluate rotation rules, enumerate backups eligible for rotation, execute the rotation process itself, and estimate the disk space that would be reclaimed by applying a given policy.

## API

### RotationService

```csharp
public RotationService
```

Instantiates the service. Construction details are internal to the project assembly; the service is typically obtained through dependency injection.

---

### ExecuteRotationAsync

```csharp
public async Task<int> ExecuteRotationAsync
```

Applies the currently active rotation policy to the set of stored backups. Identifies backups that fall outside the retention criteria and deletes them.

**Returns**  
`Task<int>` — the number of backup files actually removed during execution.

**Throws**  
- `InvalidOperationException` when no rotation policy has been saved prior to calling this method.  
- `IOException` or `UnauthorizedAccessException` when the backup storage location cannot be read or modified.

---

### GetRotationPolicyAsync

```csharp
public async Task<RotationPolicy?> GetRotationPolicyAsync
```

Retrieves the persisted rotation policy, if one exists.

**Returns**  
`Task<RotationPolicy?>` — the current `RotationPolicy` object, or `null` when no policy has been saved.

---

### SaveRotationPolicyAsync

```csharp
public async Task<RotationPolicy> SaveRotationPolicyAsync
```

Persists a rotation policy for future use by `ExecuteRotationAsync` and `CalculateDiskSpaceFreedAsync`.

**Parameters**  
A `RotationPolicy` instance describing retention rules (e.g., maximum backup count, age thresholds).

**Returns**  
`Task<RotationPolicy>` — the same policy object that was saved, allowing fluent chaining or immediate inspection.

**Throws**  
- `ArgumentNullException` when the supplied policy is `null`.  
- `ArgumentException` when the policy contains invalid or contradictory retention parameters.

---

### GetBackupsForRotationAsync

```csharp
public async Task<IEnumerable<BackupResult>> GetBackupsForRotationAsync
```

Enumerates all backups currently present in the storage location, ordered by creation date descending. This list represents the candidate set that a rotation policy would evaluate.

**Returns**  
`Task<IEnumerable<BackupResult>>` — a collection of `BackupResult` objects, each describing a single backup file with metadata such as file name, size, and timestamp.

**Throws**  
- `DirectoryNotFoundException` when the backup storage directory does not exist.  
- `IOException` when the directory cannot be enumerated.

---

### CalculateDiskSpaceFreedAsync

```csharp
public async Task<long> CalculateDiskSpaceFreedAsync
```

Simulates the application of the currently saved rotation policy against the existing backups and computes the total disk space that would be reclaimed, without performing any actual deletions.

**Returns**  
`Task<long>` — the number of bytes that would be freed if `ExecuteRotationAsync` were called immediately under the same conditions.

**Throws**  
- `InvalidOperationException` when no rotation policy has been saved.  
- `IOException` when backup metadata cannot be read.

## Usage

### Example 1: Save a policy, preview impact, then execute

```csharp
var rotationService = serviceProvider.GetRequiredService<RotationService>();

// Define a policy: keep the 7 most recent backups
var policy = new RotationPolicy { MaxBackupCount = 7 };
await rotationService.SaveRotationPolicyAsync(policy);

// Preview how much space rotation would free
long freedBytes = await rotationService.CalculateDiskSpaceFreedAsync();
Console.WriteLine($"Rotation would reclaim {freedBytes / 1024.0 / 1024.0:F2} MB");

// Execute rotation and report results
int removedCount = await rotationService.ExecuteRotationAsync();
Console.WriteLine($"Removed {removedCount} backup(s)");
```

### Example 2: Inspect backups before deciding on a policy

```csharp
var rotationService = serviceProvider.GetRequiredService<RotationService>();

// Check whether a policy already exists
RotationPolicy? existingPolicy = await rotationService.GetRotationPolicyAsync();
if (existingPolicy is null)
{
    Console.WriteLine("No rotation policy configured.");
}

// List all backups to inform policy decisions
IEnumerable<BackupResult> backups = await rotationService.GetBackupsForRotationAsync();
foreach (var backup in backups)
{
    Console.WriteLine($"{backup.FileName} — {backup.CreationTimestamp:O} — {backup.SizeBytes} bytes");
}

// Save a new policy based on observed backup count
var newPolicy = new RotationPolicy { MaxBackupAgeDays = 30 };
await rotationService.SaveRotationPolicyAsync(newPolicy);
```

## Notes

- **Null policy state**: Calling `ExecuteRotationAsync` or `CalculateDiskSpaceFreedAsync` without first saving a policy throws `InvalidOperationException`. Always guard these calls with a check via `GetRotationPolicyAsync` or ensure a policy is saved during application startup.
- **Atomicity**: `ExecuteRotationAsync` performs deletions sequentially. If the process is interrupted (e.g., application crash), some eligible backups may remain undeleted. The method does not wrap all deletions in a single transaction.
- **Concurrent modifications**: The service does not lock the backup directory. If external processes add or remove backup files between a call to `GetBackupsForRotationAsync` and `ExecuteRotationAsync`, the rotation outcome may differ from the preview provided by `CalculateDiskSpaceFreedAsync`.
- **Thread safety**: The service itself maintains no mutable instance state beyond the persisted policy reference. Multiple calls from different threads are safe, but concurrent calls to `SaveRotationPolicyAsync` and `ExecuteRotationAsync` may race — the policy in effect during execution is the one most recently saved.
- **Policy validation**: `SaveRotationPolicyAsync` validates the policy object synchronously before persisting. Invalid combinations (e.g., both `MaxBackupCount` and `MaxBackupAgeDays` set to zero) cause an `ArgumentException` at save time rather than during execution.
