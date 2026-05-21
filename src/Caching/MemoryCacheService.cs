#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;

namespace DockerSqliteBackup.Caching;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary.
/// Suitable for single-instance applications and development.
/// Supports automatic expiration with background cleanup.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = [];
    private readonly Timer _cleanupTimer;
    private readonly object _cleanupLock = new();

    public MemoryCacheService(TimeSpan? cleanupInterval = null)
    {
        var interval = cleanupInterval ?? TimeSpan.FromMinutes(5);
        _cleanupTimer = new Timer(_ => CleanupExpiredEntries(), null, interval, interval);
    }

    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired())
            {
                _cache.TryRemove(key, out _);
                return default;
            }

            return Deserialize<T>(entry.Value);
        }

        return default;
    }

    /// <summary>
    /// Gets a value from the cache asynchronously.
    /// </summary>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Get<T>(key));
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var entry = new CacheEntry
        {
            Value = Serialize(value),
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
        };

        _cache[key] = entry;
    }

    /// <summary>
    /// Sets a value in the cache asynchronously.
    /// </summary>
    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        Set(key, value, expiration);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes a value from the cache asynchronously.
    /// </summary>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all values from the cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Checks if a key exists in the cache.
    /// </summary>
    public bool Exists(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired())
            {
                _cache.TryRemove(key, out _);
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets or sets a value, executing factory if not in cache.
    /// </summary>
    public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        var cached = Get<T>(key);
        if (cached  is not null)
            return cached;

        var value = factory();
        Set(key, value, expiration);
        return value;
    }

    /// <summary>
    /// Gets or sets a value asynchronously, executing factory if not in cache.
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = Get<T>(key);
        if (cached  is not null)
            return cached;

        var value = await factory(cancellationToken).ConfigureAwait(false);
        await SetAsync(key, value, expiration, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Cleans up expired entries from the cache.
    /// </summary>
    private void CleanupExpiredEntries()
    {
        lock (_cleanupLock)
        {
            var expired = _cache
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expired)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    private static string Serialize<T>(T? value)
    {
        return JsonSerializer.Serialize(value);
    }

    private static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Represents a cached entry with optional expiration.
    /// </summary>
    private class CacheEntry
    {
        public string Value { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }

        public bool IsExpired()
        {
            return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt;
        }
    }
}
