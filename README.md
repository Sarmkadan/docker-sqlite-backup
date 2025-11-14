// entire file content ...
// ... goes in between

## BackupJobExtensions

The `BackupJobExtensions` class provides extension methods for `BackupJob` that simplify status checks, duration formatting, retry progress tracking, and result retrieval. It includes convenience methods for determining job status, formatting durations, and assessing retry attempts.

### Usage Examples

```csharp
// Get a backup job instance
var job = new BackupJob("production.db", DateTime.UtcNow);

// Check job status
bool isSuccessful = BackupJobExtensions.IsSuccessful(job);
bool isFailed = BackupJobExtensions.IsFailed(job);
bool isPending = BackupJobExtensions.IsPending(job);
bool isInProgress = BackupJobExtensions.IsInProgress(job);

// Format job duration
string duration = BackupJobExtensions.GetFormattedDuration(job);

// Get retry progress
string retryProgress = BackupJobExtensions.GetRetryProgress(job);

// Check if job has exceeded retries
bool hasExceededRetries = BackupJobExtensions.HasExceededRetries(job);

// Get job result
var result = BackupJobExtensions.GetResult(job);
```