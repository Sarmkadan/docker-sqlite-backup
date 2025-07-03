// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Middleware;

/// <summary>
/// Middleware that enriches each request with a unique correlation ID and request context.
/// Enables request tracing and debugging across service boundaries.
/// </summary>
public class RequestContextMiddleware
{
    private readonly Func<RequestContext, Task> _next;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(Func<RequestContext, Task> next, ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to attach request context.
    /// </summary>
    public async Task InvokeAsync(RequestContext context)
    {
        context.CorrelationId ??= Guid.NewGuid().ToString();
        context.StartTime = DateTime.UtcNow;

        _logger.LogInformation(
            "Request started: {Method} {Path} [{CorrelationId}]",
            context.Method,
            context.Path,
            context.CorrelationId);

        try
        {
            await _next(context);

            _logger.LogInformation(
                "Request completed: {Method} {Path} {StatusCode} {Duration}ms [{CorrelationId}]",
                context.Method,
                context.Path,
                context.StatusCode,
                context.GetDurationMilliseconds(),
                context.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Request failed: {Method} {Path} {Duration}ms [{CorrelationId}]",
                context.Method,
                context.Path,
                context.GetDurationMilliseconds(),
                context.CorrelationId);
            throw;
        }
    }
}

/// <summary>
/// Contains request-specific context data accessible throughout the request lifetime.
/// </summary>
public class RequestContext
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
    public Dictionary<string, object> Items { get; set; } = [];
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int StatusCode { get; set; } = 200;
    public string? UserId { get; set; }

    /// <summary>
    /// Gets the request duration in milliseconds.
    /// </summary>
    public long GetDurationMilliseconds()
    {
        var endTime = EndTime ?? DateTime.UtcNow;
        return (long)(endTime - StartTime).TotalMilliseconds;
    }

    /// <summary>
    /// Stores a value in the request context.
    /// </summary>
    public void SetItem(string key, object value)
    {
        Items[key] = value;
    }

    /// <summary>
    /// Retrieves a value from the request context.
    /// </summary>
    public bool TryGetItem<T>(string key, out T? value)
    {
        value = default;
        if (Items.TryGetValue(key, out var item) && item is T t)
        {
            value = t;
            return true;
        }

        return false;
    }
}
