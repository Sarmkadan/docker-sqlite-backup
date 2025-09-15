# MetricsEventListener

`MetricsEventListener` is a dedicated event listener that intercepts backup-related domain events, aggregates operational metrics, and exposes a consolidated snapshot of backup activity. It tracks total, successful, and failed backup counts, data volume transferred, average execution duration, and a breakdown of failure reasons, providing a single point of query for monitoring and diagnostics.

## API

### public MetricsEventListener

Default constructor. Initializes internal counters and the failure-reason dictionary to zero/empty state. No parameters, no exceptions.

### public async Task HandleAsync

Processes a single domain event asynchronously. Inspects the event payload, updates internal counters (total, success, failure), records transferred bytes and duration when applicable, and increments the corresponding failure-reason entry if the event represents a fault. Returns a completed `Task`. Does not throw; malformed or unrecognized events are silently ignored.

### public IEnumerable\<string\> GetSupportedEventTypes

Returns the collection of event type identifiers this listener subscribes to. The returned sequence is immutable and reflects the types registered at construction. No parameters, never null.

### public bool CanHandle

Accepts an event type string and returns `true` if that type is among the supported event types, `false` otherwise. Pure query, no side effects, no exceptions.

### public BackupMetrics GetMetrics

Constructs and returns a `BackupMetrics` value object populated from the current internal state: `TotalBackups`, `SuccessfulBackups`, `FailedBackups`, `SuccessRate`, `TotalBytesTransferred`, `AverageDurationSeconds`, `FailureReasons`, and `CapturedAt` set to `DateTime.UtcNow`. The returned object is a snapshot; subsequent events do not mutate it. Never returns null.

### public void ResetMetrics

Resets all counters, sums, and the failure-reason dictionary to their initial zero/empty state. Does not affect the supported event types. No parameters, no return value, no exceptions.

### public long TotalBackups

Gets the total number of backup events processed, regardless of outcome. Read-only property backed by the internal counter.

### public long SuccessfulBackups

Gets the count of backup events that completed successfully. Read-only property.

### public long FailedBackups

Gets the count of backup events that terminated with a failure. Read-only property.

### public double SuccessRate

Returns the ratio of successful backups to total backups as a value between 0.0 and 1.0. Returns 0.0 when `TotalBackups` is zero. Read-only computed property.

### public long TotalBytesTransferred

Gets the cumulative sum of bytes transferred across all processed backup events where size data was available. Read-only property.

### public double AverageDurationSeconds

Returns the mean duration in seconds across all processed backup events that reported a duration. Returns 0.0 when no duration data has been accumulated. Read-only computed property.

### public Dictionary\<string, int\> FailureReasons

Returns a dictionary mapping failure-reason strings to their occurrence counts. The returned dictionary is a defensive copy; mutations to it do not affect the internal state. Read-only property (the getter creates the copy).

### public DateTime CapturedAt

Returns the timestamp of the most recent `GetMetrics` snapshot, or `DateTime.MinValue` if `GetMetrics` has never been called. Read-only property.

### public override string ToString

Returns a formatted string summarizing the current metrics, including total, successful, and failed backup counts, success rate as a percentage, total bytes transferred, and average duration. Format is stable and intended for logging and diagnostics.

## Usage

### Example 1: Basic monitoring loop

```csharp
var listener = new MetricsEventListener();

// Simulate event ingestion
await listener.HandleAsync(new BackupCompletedEvent(
    BackupId: Guid.NewGuid(),
    BytesTransferred: 1_048_576,
    Duration: TimeSpan.FromSeconds(12.5)));

await listener.HandleAsync(new BackupFailedEvent(
    BackupId: Guid.NewGuid(),
    Reason: "DiskFull"));

BackupMetrics snapshot = listener.GetMetrics();

Console.WriteLine($"Success rate: {snapshot.SuccessRate:P1}");
Console.WriteLine($"Total bytes: {snapshot.TotalBytesTransferred:N0}");
Console.WriteLine($"Avg duration: {snapshot.AverageDurationSeconds:F2}s");

foreach (var kv in snapshot.FailureReasons)
{
    Console.WriteLine($"  {kv.Key}: {kv.Value}");
}
```

### Example 2: Periodic reset for windowed reporting

```csharp
var listener = new MetricsEventListener();
var timer = new PeriodicTimer(TimeSpan.FromHours(1));

while (await timer.WaitForNextTickAsync())
{
    BackupMetrics windowMetrics = listener.GetMetrics();

    Log.Information(
        "Hourly backup stats — Total: {Total}, OK: {Ok}, Fail: {Fail}, Rate: {Rate:P2}, Bytes: {Bytes}",
        windowMetrics.TotalBackups,
        windowMetrics.SuccessfulBackups,
        windowMetrics.FailedBackups,
        windowMetrics.SuccessRate,
        windowMetrics.TotalBytesTransferred);

    listener.ResetMetrics();
}
```

## Notes

- **Thread safety:** All public members that mutate state (`HandleAsync`, `ResetMetrics`) and all properties that read counters are designed for concurrent use. Internal counters use atomic operations or locking where necessary, ensuring consistent reads without torn values. `GetMetrics` captures a coherent snapshot under the same synchronization regime.
- **Snapshot isolation:** `GetMetrics` and the `FailureReasons` property return copies or newly constructed objects. Modifying a returned `BackupMetrics` or the failure-reason dictionary has no effect on the listener’s internal state.
- **Zero-baseline behavior:** When no events have been processed, `SuccessRate` and `AverageDurationSeconds` return `0.0` rather than `NaN` or throwing. `TotalBackups`, `SuccessfulBackups`, `FailedBackups`, and `TotalBytesTransferred` return `0`. `CapturedAt` returns `DateTime.MinValue` until the first `GetMetrics` call.
- **Unrecognized events:** `HandleAsync` silently ignores events whose type is not in the supported set. No exception is thrown, and counters remain unchanged.
- **Reset semantics:** `ResetMetrics` clears all accumulated data but does not alter the set of supported event types. After reset, the listener behaves as if freshly constructed with respect to metrics, while remaining subscribed to the same event types.
- **ToString output:** The format of `ToString` is intended for human-readable diagnostics and may change across versions if additional fields are added. For programmatic consumption, use `GetMetrics` and its properties.
