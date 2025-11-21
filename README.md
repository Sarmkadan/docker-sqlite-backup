// entire file content ...
// ... goes in between

## RotationPolicy

The `RotationPolicy` class defines how backup files are rotated and deleted. It allows you to set limits on the number of backups, their age, and whether failed backups should be removed. The policy can be validated and used to decide if a particular backup should be rotated.

```csharp
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Constants;
using System;

var policy = new RotationPolicy
{
    ScheduleId = Guid.NewGuid(),
    Strategy = (int)RotationStrategy.Combined,
    MaxBackupCount = 10,
    MaxAgeDays = 30,
    VerifyBeforeDeletion = true,
    MinimumBackupCount = 3,
    DeleteFailedBackups = true,
    CreatedAt = DateTime.UtcNow,
    LastModifiedAt = DateTime.UtcNow
};

if (!policy.IsValid())
{
    throw new InvalidOperationException("Rotation policy is not valid.");
}

// Example: decide whether to rotate a backup that was created 40 days ago
var backupDate = DateTime.UtcNow.AddDays(-40);
bool shouldRotate = policy.ShouldRotate(
    totalBackupCount: 12,
    backupDate: backupDate,
    isFailed: false);

Console.WriteLine($"Should rotate: {shouldRotate}");
```
