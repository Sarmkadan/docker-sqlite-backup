#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Caching;

/// <summary>
/// Builder for creating consistent cache keys. Ensures cache keys follow
/// a naming convention and avoids key collisions.
/// </summary>
public class CacheKeyBuilder
{
    private const string KeyPrefix = "backup:";
    private readonly List<string> _parts = [];

    /// <summary>
    /// Adds a part to the cache key.
    /// </summary>
    public CacheKeyBuilder Add(string part)
    {
        if (!string.IsNullOrWhiteSpace(part))
            _parts.Add(part);

        return this;
    }

    /// <summary>
    /// Adds a value part to the cache key.
    /// </summary>
    public CacheKeyBuilder Add(Guid value)
    {
        _parts.Add(value.ToString());
        return this;
    }

    /// <summary>
    /// Adds an integer part to the cache key.
    /// </summary>
    public CacheKeyBuilder Add(int value)
    {
        _parts.Add(value.ToString());
        return this;
    }

    /// <summary>
    /// Adds a long part to the cache key.
    /// </summary>
    public CacheKeyBuilder Add(long value)
    {
        _parts.Add(value.ToString());
        return this;
    }

    /// <summary>
    /// Builds the final cache key.
    /// </summary>
    public string Build()
    {
        return KeyPrefix + string.Join(":", _parts);
    }

    /// <summary>
    /// Clears all parts from the builder.
    /// </summary>
    public void Clear()
    {
        _parts.Clear();
    }

    /// <summary>
    /// Predefined cache key builders for common resources.
    /// </summary>
    public static class Keys
    {
        public static string Schedule(Guid scheduleId) => new CacheKeyBuilder()
            .Add("schedule")
            .Add(scheduleId)
            .Build();

        public static string AllSchedules() => new CacheKeyBuilder()
            .Add("schedules")
            .Add("all")
            .Build();

        public static string BackupResult(Guid backupId) => new CacheKeyBuilder()
            .Add("backup")
            .Add(backupId)
            .Build();

        public static string BackupHistory(Guid scheduleId, int limit = 10) => new CacheKeyBuilder()
            .Add("backups")
            .Add("history")
            .Add(scheduleId)
            .Add(limit)
            .Build();

        public static string HealthStatus() => new CacheKeyBuilder()
            .Add("health")
            .Add("status")
            .Build();

        public static string Metrics() => new CacheKeyBuilder()
            .Add("metrics")
            .Add("summary")
            .Build();

        public static string ConfigValue(string configKey) => new CacheKeyBuilder()
            .Add("config")
            .Add(configKey)
            .Build();
    }
}
