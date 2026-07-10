# RotationPolicy
The `RotationPolicy` type represents a set of rules governing the rotation of backups in a database, ensuring that only a specified number of backups are retained and that backups are verified before deletion. This type is crucial in maintaining a healthy backup system, preventing unnecessary data accumulation, and ensuring data integrity.

## API
The `RotationPolicy` type exposes the following public members:
* `Id`: A unique identifier of type `Guid` representing the rotation policy.
* `ScheduleId`: A `Guid` identifier referencing the schedule associated with this rotation policy.
* `Strategy`: An integer indicating the rotation strategy employed.
* `MaxBackupCount`: The maximum number of backups to retain, specified as an integer.
* `MaxAgeDays`: The maximum age of backups in days, represented as an integer.
* `VerifyBeforeDeletion`: A boolean indicating whether backups should be verified before deletion.
* `MinimumBackupCount`: The minimum number of backups that must be retained, specified as an integer.
* `DeleteFailedBackups`: A boolean indicating whether failed backups should be deleted.
* `CreatedAt`: A `DateTime` object representing the creation time of the rotation policy.
* `LastModifiedAt`: A `DateTime` object indicating the last modification time of the rotation policy.
* `LastRotatedAt`: A nullable `DateTime` object representing the last rotation time, if applicable.
* `IsValid`: A boolean indicating whether the rotation policy is valid.
* `ShouldRotate`: A boolean indicating whether rotation should be performed based on the policy.

## Usage
Here are two examples demonstrating the usage of `RotationPolicy`:
```csharp
// Example 1: Creating a new rotation policy
RotationPolicy policy = new RotationPolicy
{
    Id = Guid.NewGuid(),
    ScheduleId = Guid.NewGuid(),
    Strategy = 1,
    MaxBackupCount = 5,
    MaxAgeDays = 30,
    VerifyBeforeDeletion = true,
    MinimumBackupCount = 2,
    DeleteFailedBackups = true
};

// Example 2: Evaluating the validity and rotation requirement of a policy
RotationPolicy existingPolicy = // retrieve from database or other source
if (existingPolicy.IsValid && existingPolicy.ShouldRotate)
{
    // Perform rotation according to the policy
}
```

## Notes
When working with `RotationPolicy`, consider the following:
* The `Strategy` and other integer fields should be interpreted according to the specific rotation strategy and system configuration.
* The `LastRotatedAt` field may be null if rotation has not occurred or if the policy is newly created.
* The `IsValid` and `ShouldRotate` properties should be evaluated together to determine the correct course of action for backup rotation.
* This type is designed to be used in a thread-safe manner, but concurrent modifications to the same `RotationPolicy` instance should be avoided to prevent data inconsistencies.
