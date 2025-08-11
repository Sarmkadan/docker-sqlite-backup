#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Caching;

/// <summary>
/// Interface for caching service. Abstracts cache implementation details
/// to allow swapping between in-memory and distributed caches.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    /// Gets a value from the cache asynchronously.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    /// Sets a value in the cache asynchronously.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    void Remove(string key);

    /// <summary>
    /// Removes a value from the cache asynchronously.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all values from the cache.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Gets or sets a value, executing factory if not in cache.
    /// </summary>
    T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null);

    /// <summary>
    /// Gets or sets a value asynchronously, executing factory if not in cache.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
}
