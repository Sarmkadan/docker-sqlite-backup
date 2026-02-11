#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Middleware;

/// <summary>
/// Middleware for rate limiting API requests. Prevents abuse by limiting
/// requests per IP address within a time window.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly Func<RateLimitContext, Task> _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitConfig _config;
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = [];

    public RateLimitingMiddleware(
        Func<RateLimitContext, Task> next,
        ILogger<RateLimitingMiddleware> logger,
        RateLimitConfig? config = null)
    {
        _next = next;
        _logger = logger;
        _config = config ?? new RateLimitConfig();
    }

    /// <summary>
    /// Invokes the rate limiting middleware.
    /// </summary>
    public async Task InvokeAsync(RateLimitContext context)
    {
        var clientId = context.ClientId ?? "unknown";
        var bucket = _buckets.GetOrAdd(clientId, _ => new RateLimitBucket(_config.WindowDuration));

        if (!bucket.AllowRequest())
        {
            _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
            context.RateLimited = true;
            context.RetryAfter = bucket.GetRetryAfter();
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Cleans up old buckets from memory.
    /// </summary>
    public void CleanupExpiredBuckets()
    {
        var expiredKeys = _buckets
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _buckets.TryRemove(key, out _);
    }
}

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public class RateLimitConfig
{
    public int RequestsPerWindow { get; set; } = 100;
    public TimeSpan WindowDuration { get; set; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Context for rate limiting middleware.
/// </summary>
public class RateLimitContext
{
    public string? ClientId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool RateLimited { get; set; }
    public TimeSpan RetryAfter { get; set; }
}

/// <summary>
/// Represents a rate limit bucket for a client.
/// </summary>
internal class RateLimitBucket
{
    private readonly TimeSpan _windowDuration;
    private int _requestCount;
    private DateTime _windowStart;

    public bool IsExpired => DateTime.UtcNow - _windowStart > _windowDuration.Add(TimeSpan.FromMinutes(5));

    public RateLimitBucket(TimeSpan windowDuration)
    {
        _windowDuration = windowDuration;
        _windowStart = DateTime.UtcNow;
    }

    public bool AllowRequest()
    {
        var now = DateTime.UtcNow;

        // Reset window if expired
        if (now - _windowStart > _windowDuration)
        {
            _windowStart = now;
            _requestCount = 0;
        }

        _requestCount++;
        return _requestCount <= 100; // Default to 100 requests per window
    }

    public TimeSpan GetRetryAfter()
    {
        return _windowStart.Add(_windowDuration) - DateTime.UtcNow;
    }
}
