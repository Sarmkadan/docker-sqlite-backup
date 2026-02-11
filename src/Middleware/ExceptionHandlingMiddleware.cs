#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Exceptions;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Middleware;

/// <summary>
/// Middleware for centralizing exception handling. Catches exceptions from the pipeline
/// and formats them as consistent error responses. Logs all exceptions for debugging.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly Func<ExceptionContext, Task> _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(Func<ExceptionContext, Task> next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to handle exceptions in the pipeline.
    /// </summary>
    public async Task InvokeAsync(ExceptionContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in pipeline");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(ExceptionContext context, Exception exception)
    {
        var response = exception switch
        {
            BackupException be => new ErrorResponse
            {
                Code = "BACKUP_ERROR",
                Message = be.Message,
                StatusCode = 400,
                CorrelationId = context.CorrelationId
            },
            ScheduleException se => new ErrorResponse
            {
                Code = "SCHEDULE_ERROR",
                Message = se.Message,
                StatusCode = 400,
                CorrelationId = context.CorrelationId
            },
            StorageException ste => new ErrorResponse
            {
                Code = "STORAGE_ERROR",
                Message = ste.Message,
                StatusCode = 500,
                CorrelationId = context.CorrelationId
            },
            VerificationException ve => new ErrorResponse
            {
                Code = "VERIFICATION_ERROR",
                Message = ve.Message,
                StatusCode = 400,
                CorrelationId = context.CorrelationId
            },
            ArgumentException ae => new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = ae.Message,
                StatusCode = 400,
                CorrelationId = context.CorrelationId
            },
            _ => new ErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = exception.Message,
                StatusCode = 500,
                CorrelationId = context.CorrelationId,
                Details = exception.StackTrace
            }
        };

        context.Response = response;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents an error response returned by exception handling middleware.
/// </summary>
public class ErrorResponse
{
    public string Code { get; set; } = "UNKNOWN_ERROR";
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 500;
    public string CorrelationId { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Context passed through exception handling middleware.
/// </summary>
public class ExceptionContext
{
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public object? Response { get; set; }
}
