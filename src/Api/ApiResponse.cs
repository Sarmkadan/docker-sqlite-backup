#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Api;

/// <summary>
/// Standard API response envelope for all API responses.
/// Provides consistent structure for success and error responses.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Operation completed successfully"
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string errorCode, string message, string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            TraceId = traceId
        };
    }
}

/// <summary>
/// Standard API response without data (void response).
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }

    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Operation completed successfully"
        };
    }

    public static ApiResponse ErrorResponse(string errorCode, string message, string? traceId = null)
    {
        return new ApiResponse
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message,
            TraceId = traceId
        };
    }
}

/// <summary>
/// Request model for creating a backup schedule.
/// </summary>
public class CreateScheduleRequest
{
    public string Name { get; set; } = string.Empty;
    public string DatabasePath { get; set; } = string.Empty;
    public string CronExpression { get; set; } = "0 2 * * *"; // Default: 2 AM daily
    public string RotationStrategy { get; set; } = "bycount";
    public int RetentionDays { get; set; } = 30;
    public int MaxBackups { get; set; } = 10;
}

/// <summary>
/// Request model for updating a backup schedule.
/// </summary>
public class UpdateScheduleRequest
{
    public string? Name { get; set; }
    public string? CronExpression { get; set; }
    public bool? IsEnabled { get; set; }
}

/// <summary>
/// Request model for executing a manual backup.
/// </summary>
public class ExecuteBackupRequest
{
    public Guid ScheduleId { get; set; }
    public bool SkipVerification { get; set; }
}

/// <summary>
/// Request model for restoring a backup.
/// </summary>
public class RestoreRequest
{
    public Guid BackupResultId { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public bool CreateBackup { get; set; } = true; // Create backup of current DB before restore
}

/// <summary>
/// Request model for webhook configuration.
/// </summary>
public class WebhookConfigRequest
{
    public string Url { get; set; } = string.Empty;
    public string[] Events { get; set; } = [];
    public string? Secret { get; set; }
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Response model for paginated results.
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (Total + PageSize - 1) / PageSize;
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
