// ... rest of README content ...
## VerificationException

The `VerificationException` is a custom exception type that represents a verification failure during the backup process. It provides additional information about the verification failure, including the backup ID and any associated errors.

### Usage Example

```csharp
try
{
    // Attempt to verify a backup
    var verificationResult = new RestoreVerification();
    if (!verificationResult.IsValid)
    {
        throw new VerificationException("Backup verification failed", verificationResult.BackupId, verificationResult.Errors);
    }
}
catch (VerificationException ex)
{
    Console.WriteLine($"Verification failed for backup {ex.BackupId}: {ex.Message}");
    if (ex.Errors != null)
    {
        Console.WriteLine("Errors:");
        foreach (var error in ex.Errors)
        {
            Console.WriteLine(error);
        }
    }
}
```
```