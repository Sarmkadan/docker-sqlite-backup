# StorageConfiguration

`StorageConfiguration` is an abstract base class representing a named storage destination for database backups. It defines the common identity, metadata, and validation contract that all concrete storage implementations must fulfill, enabling polymorphic management of multiple backup targets within the system.

## API

### `public Guid Id`

Gets or sets the unique identifier for this storage configuration instance. This value is typically assigned upon creation and remains immutable for the lifetime of the configuration.

### `public string Name`

Gets or sets the human-readable name assigned to this storage configuration. Names are expected to be unique within a given scope to allow users to distinguish between multiple configured destinations.

### `public abstract int StorageType`

Gets an integer discriminator that identifies the concrete storage backend type. Derived classes must override this property to return a constant value corresponding to their specific implementation (e.g., local filesystem, cloud blob storage, remote server). Consumers use this value for type filtering and serialization.

### `public bool IsDefault`

Gets or sets whether this storage configuration is the default destination. When `true`, backup operations that do not explicitly specify a target will use this configuration. Only one configuration per scope should be marked as default.

### `public DateTime CreatedAt`

Gets or sets the timestamp indicating when this configuration was first persisted. This value is set at creation time and is not intended to be modified thereafter.

### `public DateTime LastModifiedAt`

Gets or sets the timestamp of the most recent modification to this configuration. This value should be updated whenever any mutable property changes.

### `public abstract bool IsValid`

Gets a value indicating whether the current configuration state is valid. Derived classes override this property to perform synchronous validation of all required fields and constraints. Returns `true` if the configuration is ready for use; otherwise `false`.

### `public abstract Task<bool> TestConnectionAsync()`

Asynchronously tests whether the storage backend is reachable and functional with the current configuration settings. Derived classes implement the actual connectivity check appropriate to their storage type.

- **Returns:** A `Task<bool>` that resolves to `true` if the connection test succeeds, or `false` if the backend is unreachable or authentication fails.
- **Exceptions:** Implementations may throw `InvalidOperationException` if `IsValid` is `false` at the time of invocation. Network-related exceptions from underlying clients may propagate depending on the concrete implementation.

## Usage

### Example 1: Iterating configurations and testing connectivity

```csharp
async Task ValidateAllConfigurations(IEnumerable<StorageConfiguration> configurations)
{
    foreach (var config in configurations)
    {
        if (!config.IsValid)
        {
            Console.WriteLine($"Configuration '{config.Name}' is invalid. Skipping test.");
            continue;
        }

        bool isConnected = await config.TestConnectionAsync();
        Console.WriteLine($"Configuration '{config.Name}' (Type: {config.StorageType}): " +
                          $"Connection test {(isConnected ? "passed" : "failed")}.");
    }
}
```

### Example 2: Selecting the default configuration for a backup job

```csharp
StorageConfiguration? ResolveTarget(IEnumerable<StorageConfiguration> configurations, string? requestedName)
{
    if (requestedName is not null)
    {
        return configurations.FirstOrDefault(c => c.Name == requestedName && c.IsValid);
    }

    var defaultConfig = configurations.FirstOrDefault(c => c.IsDefault && c.IsValid);

    if (defaultConfig is null)
    {
        throw new InvalidOperationException(
            "No valid default storage configuration is set and no target was specified.");
    }

    return defaultConfig;
}
```

## Notes

- **Abstract contract:** `StorageConfiguration` cannot be instantiated directly. All storage backends must derive from it and provide implementations for `StorageType`, `IsValid`, and `TestConnectionAsync()`.
- **Validation ordering:** Callers should check `IsValid` before invoking `TestConnectionAsync()`. Concrete implementations may throw if invoked in an invalid state, but this behavior is implementation-dependent and not guaranteed across all derived types.
- **Default uniqueness:** The system does not enforce single-default semantics at the base class level. Consumers are responsible for ensuring that only one configuration has `IsDefault` set to `true` within a given scope.
- **Thread safety:** This class does not provide built-in synchronization. Concurrent reads of immutable fields (`Id`, `CreatedAt`) are safe, but concurrent writes to mutable properties (`Name`, `IsDefault`, `LastModifiedAt`) from multiple threads must be externally synchronized by the caller.
- **Timestamp management:** `LastModifiedAt` is not automatically updated by the base class. Derived types and consuming code must explicitly set this property when changes occur.
