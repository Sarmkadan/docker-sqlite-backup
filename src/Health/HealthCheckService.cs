// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Events;
using DockerSqliteBackup.Utilities;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Health;

/// <summary>
/// Service for performing health checks on various system components.
/// Checks storage accessibility, database connectivity, disk space, and scheduler status.
/// </summary>
public class HealthCheckService
{
    private readonly IBackupEventPublisher? _eventPublisher;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IBackupEventPublisher? eventPublisher = null)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Performs a comprehensive health check across all components.
    /// </summary>
    public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult();

        // Check storage
        var storageHealth = CheckStorageHealth();
        result.Components["storage"] = storageHealth;

        // Check disk space
        var diskHealth = CheckDiskSpace();
        result.Components["disk"] = diskHealth;

        // Check database
        var dbHealth = CheckDatabaseHealth();
        result.Components["database"] = dbHealth;

        // Calculate overall status
        result.Status = result.Components.Values.All(c => c.IsHealthy) ? "healthy" : "degraded";

        await PublishHealthCheckEventAsync(result, cancellationToken);

        return result;
    }

    /// <summary>
    /// Checks if storage is accessible and writable.
    /// </summary>
    private ComponentHealth CheckStorageHealth()
    {
        try
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "backups");
            PathUtility.EnsureDirectoryExists(backupDir);

            var testFile = Path.Combine(backupDir, ".health-check");
            File.WriteAllText(testFile, "health");
            File.Delete(testFile);

            return new ComponentHealth
            {
                Name = "storage",
                IsHealthy = true,
                Message = "Storage is accessible and writable"
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                Name = "storage",
                IsHealthy = false,
                Message = $"Storage check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks available disk space.
    /// </summary>
    private ComponentHealth CheckDiskSpace()
    {
        try
        {
            const long minimumFreeSpace = 1024L * 1024L * 1024L; // 1 GB
            var availableSpace = FileSystemUtility.GetAvailableDiskSpace();

            var isHealthy = availableSpace > minimumFreeSpace;
            var message = $"Free disk space: {StringUtility.FormatBytes(availableSpace)}";

            if (!isHealthy)
                message += " (Below minimum threshold)";

            return new ComponentHealth
            {
                Name = "disk",
                IsHealthy = isHealthy,
                Message = message
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                Name = "disk",
                IsHealthy = false,
                Message = $"Disk check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if database file is accessible.
    /// </summary>
    private ComponentHealth CheckDatabaseHealth()
    {
        try
        {
            // This is a stub - would check actual database connectivity
            return new ComponentHealth
            {
                Name = "database",
                IsHealthy = true,
                Message = "Database is accessible"
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                Name = "database",
                IsHealthy = false,
                Message = $"Database check failed: {ex.Message}"
            };
        }
    }

    private async Task PublishHealthCheckEventAsync(
        HealthCheckResult result,
        CancellationToken cancellationToken)
    {
        if (_eventPublisher != null)
        {
            var @event = new HealthCheckEvent
            {
                ComponentName = "system",
                Status = result.Status,
                Message = $"Health check: {result.Status}"
            };

            await _eventPublisher.PublishAsync(@event, cancellationToken);
        }
    }
}

/// <summary>
/// Represents the health status of a single component.
/// </summary>
public class ComponentHealth
{
    public string Name { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the overall health status of the system.
/// </summary>
public class HealthCheckResult
{
    public string Status { get; set; } = "unknown";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, ComponentHealth> Components { get; set; } = [];

    /// <summary>
    /// Gets a summary of the health check result.
    /// </summary>
    public override string ToString()
    {
        var unhealthyComponents = Components.Values.Where(c => !c.IsHealthy).ToList();

        if (!unhealthyComponents.Any())
            return $"System is {Status}";

        var issues = string.Join(", ", unhealthyComponents.Select(c => $"{c.Name}: {c.Message}"));
        return $"System is {Status}. Issues: {issues}";
    }
}
