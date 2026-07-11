#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Utilities;
using Microsoft.Data.Sqlite;
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
    private readonly AppSettings? _appSettings;

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IBackupEventPublisher? eventPublisher = null,
        AppSettings? appSettings = null)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Performs a comprehensive health check across all components.
    /// </summary>
    public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult();

        // Check storage
        try
        {
            var storageHealth = CheckStorageHealth();
            result.Components["storage"] = storageHealth;
            _logger.LogInformation("Storage health check completed: {Status}", storageHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check storage health");
            result.Components["storage"] = new ComponentHealth { IsHealthy = false, Message = "Failed to check storage health" };
        }

        // Check disk space
        try
        {
            var diskHealth = CheckDiskSpace();
            result.Components["disk"] = diskHealth;
            _logger.LogInformation("Disk space check completed: {Status}", diskHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space");
            result.Components["disk"] = new ComponentHealth { IsHealthy = false, Message = "Failed to check disk space" };
        }

        // Check database
        try
        {
            var dbHealth = CheckDatabaseHealth();
            result.Components["database"] = dbHealth;
            _logger.LogInformation("Database health check completed: {Status}", dbHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check database health");
            result.Components["database"] = new ComponentHealth { IsHealthy = false, Message = "Failed to check database health" };
        }

        // Calculate overall status
        result.Status = result.Components.Values.All(c => c.IsHealthy) ? "healthy" : "degraded";

        await PublishHealthCheckEventAsync(result, cancellationToken);

        _logger.LogInformation("Health check completed: {Status}", result.Status);

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
    /// Checks if the backup metadata database is accessible by opening a read-only
    /// connection and running a fast structural scan (<c>PRAGMA quick_check</c>).
    /// </summary>
    private ComponentHealth CheckDatabaseHealth()
    {
        try
        {
            var databasePath = _appSettings?.DatabasePath ?? "backups.sqlite";
            if (!Path.IsPathRooted(databasePath))
            {
                databasePath = Path.Combine(AppContext.BaseDirectory, databasePath);
            }

            if (!File.Exists(databasePath))
            {
                return new ComponentHealth
                {
                    Name = "database",
                    IsHealthy = false,
                    Message = $"Database file not found: {databasePath}"
                };
            }

            using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly;");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check";
            var result = command.ExecuteScalar() as string;

            var isHealthy = string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase);
            return new ComponentHealth
            {
                Name = "database",
                IsHealthy = isHealthy,
                Message = isHealthy
                    ? "Database is accessible"
                    : $"Database quick_check reported: {result}"
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
        if (_eventPublisher  is not null)
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
