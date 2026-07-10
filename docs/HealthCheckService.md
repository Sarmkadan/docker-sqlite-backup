# HealthCheckService

`HealthCheckService` provides a centralized mechanism for evaluating the overall health of the application and its constituent components. It aggregates the status of multiple subsystems into a single, timestamped result, exposing both a high-level summary and per-component details for diagnostic purposes.

## API

### HealthCheckService

```csharp
public HealthCheckService()
```

Default constructor. Initializes a new instance of the service without pre-registered components. Components are typically added or resolved internally before a health check is performed.

### PerformHealthCheckAsync

```csharp
public async Task<HealthCheckResult> PerformHealthCheckAsync()
```

Executes health checks against all registered components asynchronously and returns an aggregated `HealthCheckResult`.

- **Returns**: A `Task<HealthCheckResult>` whose result contains the overall status, a descriptive message, the timestamp of the check, and a dictionary of per-component results.
- **Exceptions**: May throw if any underlying component check throws an unhandled exception, depending on the implementation’s error-handling strategy. The method itself does not define pre-condition guards.

### Name

```csharp
public string Name { get; }
```

Gets the logical name of this health check service instance. Useful when multiple services are registered and need to be distinguished in logs or dashboards.

### IsHealthy

```csharp
public bool IsHealthy { get; }
```

Indicates whether the most recent health check resulted in a fully healthy state. This property reflects the outcome of the last call to `PerformHealthCheckAsync`. Its value is `false` until the first check completes successfully.

### Message

```csharp
public string Message { get; }
```

A human-readable summary describing the current health status. Typically set after a health check completes; may be `null` or empty before the first check.

### CheckedAt

```csharp
public DateTime CheckedAt { get; }
```

The timestamp at which the last health check was performed. If no check has been executed, the value defaults to `DateTime.MinValue` or an equivalent sentinel.

### Status

```csharp
public string Status { get; }
```

A string representation of the aggregate health status (e.g., `"Healthy"`, `"Degraded"`, `"Unhealthy"`). Derived from the combined states of all components after a check.

### Components

```csharp
public Dictionary<string, ComponentHealth> Components { get; }
```

A dictionary keyed by component name, containing the detailed health status of each subsystem evaluated during the last check. Returns an empty dictionary before the first health check is performed.

### ToString

```csharp
public override string ToString()
```

Returns a formatted string that includes the service name, overall status, message, and the timestamp of the last check. Intended for logging and diagnostic output.

## Usage

### Example 1: Basic Health Check and Status Inspection

```csharp
var healthService = new HealthCheckService();

// Perform the health check
HealthCheckResult result = await healthService.PerformHealthCheckAsync();

// Inspect the overall outcome
Console.WriteLine($"Service: {healthService.Name}");
Console.WriteLine($"Healthy: {healthService.IsHealthy}");
Console.WriteLine($"Status: {healthService.Status}");
Console.WriteLine($"Message: {healthService.Message}");
Console.WriteLine($"Checked at: {healthService.CheckedAt:O}");

// Drill into individual components
foreach (var kvp in healthService.Components)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value.Status}");
}
```

### Example 2: Periodic Monitoring Loop

```csharp
var healthService = new HealthCheckService();
var cts = new CancellationTokenSource();

// Periodically check health every 30 seconds
while (!cts.Token.IsCancellationRequested)
{
    await healthService.PerformHealthCheckAsync();

    if (!healthService.IsHealthy)
    {
        // Log the degraded state with component details
        var failedComponents = healthService.Components
            .Where(c => c.Value.Status != "Healthy")
            .Select(c => c.Key);

        Console.WriteLine(
            $"[{healthService.CheckedAt:HH:mm:ss}] Degraded: " +
            $"{string.Join(", ", failedComponents)}");
    }

    await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
}
```

## Notes

- **Initial state**: Before the first call to `PerformHealthCheckAsync`, properties such as `IsHealthy`, `Status`, and `Message` reflect default or empty values. `CheckedAt` may be `DateTime.MinValue`. Consumers should guard against acting on uninitialized state.
- **Thread safety**: The service is not guaranteed to be thread-safe. Concurrent calls to `PerformHealthCheckAsync` may overwrite the stored result properties (`IsHealthy`, `Message`, `CheckedAt`, `Status`, `Components`) in a non-atomic fashion. External synchronization is required if multiple threads access the instance simultaneously.
- **Component failures**: If an individual component check throws an exception, the overall result depends on the internal aggregation policy. The exception may propagate directly, or the component may be recorded as unhealthy with an error message. Callers should not assume that `PerformHealthCheckAsync` always succeeds.
- **Timestamp granularity**: `CheckedAt` uses `DateTime`, which has system-clock resolution. For high-precision timing requirements, consider supplementing with a separate stopwatch or diagnostic timestamp.
- **Dictionary mutability**: The `Components` dictionary returned is the live internal collection. Modifying it externally may corrupt the service state. Treat it as read-only.
