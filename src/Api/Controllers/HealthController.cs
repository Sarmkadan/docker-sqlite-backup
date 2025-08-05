// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Health;
using DockerSqliteBackup.Events;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller for system health checks and monitoring.
/// Provides endpoints for checking system status, metrics, and diagnostics.
/// </summary>
public class HealthController
{
    private readonly HealthCheckService _healthCheckService;
    private readonly MetricsEventListener? _metricsListener;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        HealthCheckService healthCheckService,
        MetricsEventListener? metricsListener,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _metricsListener = metricsListener;
        _logger = logger;
    }

    /// <summary>
    /// Performs a comprehensive health check.
    /// </summary>
    public async Task<ApiResponse<object>> HealthCheck(CancellationToken ct = default)
    {
        try
        {
            var result = await _healthCheckService.PerformHealthCheckAsync(ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                Status = result.Status,
                CheckedAt = result.CheckedAt,
                Components = result.Components.Select(c => new
                {
                    Name = c.Key,
                    c.Value.IsHealthy,
                    c.Value.Message,
                    c.Value.CheckedAt
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return ApiResponse<object>.ErrorResponse("HEALTH_CHECK_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets a simple status indicator.
    /// </summary>
    public async Task<ApiResponse<object>> Status(CancellationToken ct = default)
    {
        try
        {
            var health = await _healthCheckService.PerformHealthCheckAsync(ct);

            return ApiResponse<object>.SuccessResponse(new
            {
                Status = health.Status,
                IsHealthy = health.Status == "healthy",
                Timestamp = health.CheckedAt,
                Version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.ErrorResponse("STATUS_CHECK_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets backup metrics and statistics.
    /// </summary>
    public ApiResponse<object> GetMetrics()
    {
        try
        {
            if (_metricsListener == null)
                return ApiResponse<object>.ErrorResponse("METRICS_UNAVAILABLE", "Metrics listener not configured");

            var metrics = _metricsListener.GetMetrics();

            return ApiResponse<object>.SuccessResponse(new
            {
                metrics.TotalBackups,
                metrics.SuccessfulBackups,
                metrics.FailedBackups,
                metrics.SuccessRate,
                metrics.TotalBytesTransferred,
                metrics.AverageDurationSeconds,
                metrics.FailureReasons,
                metrics.CapturedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metrics");
            return ApiResponse<object>.ErrorResponse("METRICS_RETRIEVAL_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets system diagnostics information.
    /// </summary>
    public ApiResponse<object> GetDiagnostics()
    {
        try
        {
            var diagnostics = new
            {
                FrameworkVersion = ".NET 10.0",
                OSVersion = Environment.OSVersion.ToString(),
                ProcessorCount = Environment.ProcessorCount,
                AvailableMemory = GC.GetTotalMemory(false),
                AppStartTime = DateTime.UtcNow.AddSeconds(-System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds),
                BaseDirectory = AppContext.BaseDirectory
            };

            return ApiResponse<object>.SuccessResponse(diagnostics);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.ErrorResponse("DIAGNOSTICS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Resets metrics to zero.
    /// </summary>
    public ApiResponse ResetMetrics()
    {
        try
        {
            _metricsListener?.ResetMetrics();
            return ApiResponse.SuccessResponse("Metrics have been reset");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset metrics");
            return ApiResponse.ErrorResponse("RESET_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Gets a liveness probe for Kubernetes.
    /// </summary>
    public ApiResponse<object> LivenessProbe()
    {
        return ApiResponse<object>.SuccessResponse(new { Live = true });
    }

    /// <summary>
    /// Gets a readiness probe for Kubernetes.
    /// </summary>
    public async Task<ApiResponse<object>> ReadinessProbe(CancellationToken ct = default)
    {
        try
        {
            var health = await _healthCheckService.PerformHealthCheckAsync(ct);
            var isReady = health.Status == "healthy";

            return ApiResponse<object>.SuccessResponse(new
            {
                Ready = isReady,
                Status = health.Status
            });
        }
        catch
        {
            return ApiResponse<object>.ErrorResponse("NOT_READY", "Service is not ready");
        }
    }
}
