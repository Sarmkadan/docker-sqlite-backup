#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
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
    // Component identifiers
    private const string StorageComponentName = "storage";
    private const string DiskComponentName = "disk";
    private const string DatabaseComponentName = "database";
    private const string SystemComponentName = "system";

    // Overall status values
    private const string HealthyStatus = "healthy";
    private const string DegradedStatus = "degraded";

    // Storage probe
    private const string HealthCheckFileName = ".health-check";
    private const string HealthCheckFileContent = "health";

    // Disk space threshold (in bytes)
    private const long MinimumFreeDiskSpaceBytes = 1024L * 1024L * 1024L; // 1 GB

    // Database
    private const string DefaultDatabaseFileName = "backups.sqlite";
    private const string ReadOnlyConnectionOptions = "Mode=ReadOnly;";
    private const string QuickCheckCommandText = "PRAGMA quick_check";
    private const string QuickCheckPassedResult = "ok";

    private readonly IBackupEventPublisher? _eventPublisher;
    private readonly ILogger<HealthCheckService> _logger;
    private readonly AppSettings? _appSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthCheckService"/> class.
    /// </summary>
    /// <param name="logger">Logger used to report the progress and outcome of each check.</param>
    /// <param name="eventPublisher">Optional publisher for health check events. When <c>null</c>, no event is published.</param>
    /// <param name="appSettings">Optional application settings used to locate the backup database. Defaults to <c>backups.sqlite</c> when <c>null</c>.</param>
    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IBackupEventPublisher? eventPublisher = null,
        AppSettings? appSettings = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _eventPublisher = eventPublisher;
        _appSettings = appSettings;
    }

    /// <summary>
    /// Performs a comprehensive health check across all components.
    /// Each component (storage, disk, database) is checked independently, so a failure in one
    /// does not prevent the others from being evaluated.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the event publishing step.</param>
    /// <returns>
    /// A <see cref="HealthCheckResult"/> with the per-component results and an overall status
    /// of <c>"healthy"</c> or <c>"degraded"</c>.
    /// </returns>
    public async Task<HealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthCheckResult();

        // Check storage
        try
        {
            var storageHealth = CheckStorageHealth();
            result.Components[StorageComponentName] = storageHealth;
            _logger.LogInformation("Storage health check completed: {Status}", storageHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check storage health");
            result.Components[StorageComponentName] = new ComponentHealth { IsHealthy = false, Message = "Failed to check storage health" };
        }

        // Check disk space
        try
        {
            var diskHealth = CheckDiskSpace();
            result.Components[DiskComponentName] = diskHealth;
            _logger.LogInformation("Disk space check completed: {Status}", diskHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check disk space");
            result.Components[DiskComponentName] = new ComponentHealth { IsHealthy = false, Message = "Failed to check disk space" };
        }

        // Check database
        try
        {
            var dbHealth = CheckDatabaseHealth();
            result.Components[DatabaseComponentName] = dbHealth;
            _logger.LogInformation("Database health check completed: {Status}", dbHealth.IsHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check database health");
            result.Components[DatabaseComponentName] = new ComponentHealth { IsHealthy = false, Message = "Failed to check database health" };
        }

        // Calculate overall status
        result.Status = result.Components.Values.All(c => c.IsHealthy) ? HealthyStatus : DegradedStatus;

        await PublishHealthCheckEventAsync(result, cancellationToken);

        _logger.LogInformation("Health check completed: {Status}", result.Status);

        return result;
    }

    /// <summary>
    /// Checks if storage is accessible and writable by creating and deleting a temporary file
    /// in the backups directory.
    /// </summary>
    /// <returns>A <see cref="ComponentHealth"/> describing the storage state.</returns>
    /// <remarks>Exceptions are caught and reported as an unhealthy component rather than thrown.</remarks>
    private ComponentHealth CheckStorageHealth()
    {
        try
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, BackupConstants.BackupsDirectoryName);
            PathUtility.EnsureDirectoryExists(backupDir);

            var testFile = Path.Combine(backupDir, HealthCheckFileName);
            File.WriteAllText(testFile, HealthCheckFileContent);
            File.Delete(testFile);

            return new ComponentHealth
            {
                Name = StorageComponentName,
                IsHealthy = true,
                Message = "Storage is accessible and writable"
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                Name = StorageComponentName,
                IsHealthy = false,
                Message = $"Storage check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks available disk space. The component is unhealthy when free space is at or below 1 GB.
    /// </summary>
    /// <returns>A <see cref="ComponentHealth"/> describing the disk state.</returns>
    /// <remarks>Exceptions are caught and reported as an unhealthy component rather than thrown.</remarks>
    private ComponentHealth CheckDiskSpace()
    {
        try
        {
            var availableSpace = FileSystemUtility.GetAvailableDiskSpace();

            var isHealthy = availableSpace > MinimumFreeDiskSpaceBytes;
            var message = $"Free disk space: {StringUtility.FormatBytes(availableSpace)}";

            if (!isHealthy)
                message += " (Below minimum threshold)";

            return new ComponentHealth
            {
                Name = DiskComponentName,
                IsHealthy = isHealthy,
                Message = message
            };
        }
        catch (Exception ex)
        {
            return new ComponentHealth
            {
                Name = DiskComponentName,
                IsHealthy = false,
                Message = $"Disk check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if the backup metadata database is accessible by opening a read-only
    /// connection and running a fast structural scan (<c>PRAGMA quick_check</c>).
    /// </summary>
    /// <returns>
    /// A <see cref="ComponentHealth"/> that is healthy only when <c>quick_check</c> returns <c>ok</c>.
    /// </returns>
    /// <remarks>Exceptions are caught and reported as an unhealthy component rather than thrown.</remarks>
    private ComponentHealth CheckDatabaseHealth()
    {
        try
        {
            var databasePath = _appSettings?.DatabasePath ?? DefaultDatabaseFileName;
            if (!Path.IsPathRooted(databasePath))
            {
                databasePath = Path.Combine(AppContext.BaseDirectory, databasePath);
            }

            if (!File.Exists(databasePath))
            {
                return new ComponentHealth
                {
                    Name = DatabaseComponentName,
                    IsHealthy = false,
                    Message = $"Database file not found: {databasePath}"
                };
            }

            using var connection = new SqliteConnection($"Data Source={databasePath};{ReadOnlyConnectionOptions}");
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = QuickCheckCommandText;
            var result = command.ExecuteScalar() as string;

            var isHealthy = string.Equals(result, QuickCheckPassedResult, StringComparison.OrdinalIgnoreCase);
            return new ComponentHealth
            {
                Name = DatabaseComponentName,
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
                Name = DatabaseComponentName,
                IsHealthy = false,
                Message = $"Database check failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Publishes a system-level health check event, if an event publisher is configured.
    /// </summary>
    /// <param name="result">The health check result whose overall status is published.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    /// <returns>A task that completes when the event has been published or skipped.</returns>
    private async Task PublishHealthCheckEventAsync(
        HealthCheckResult result,
        CancellationToken cancellationToken)
    {
        if (_eventPublisher  is not null)
        {
            var @event = new HealthCheckEvent
            {
                ComponentName = SystemComponentName,
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
    /// <summary>Gets or sets the component identifier, such as <c>storage</c>, <c>disk</c> or <c>database</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the component is healthy.</summary>
    public bool IsHealthy { get; set; }

    /// <summary>Gets or sets a human-readable description of the component state.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC time at which the component was checked.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the overall health status of the system.
/// </summary>
public class HealthCheckResult
{
    /// <summary>Gets or sets the overall status: <c>"healthy"</c>, <c>"degraded"</c>, or <c>"unknown"</c> before a check has run.</summary>
    public string Status { get; set; } = "unknown";

    /// <summary>Gets or sets the UTC time at which the health check was performed.</summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the individual component results, keyed by component name.</summary>
    public Dictionary<string, ComponentHealth> Components { get; set; } = [];

    /// <summary>
    /// Gets a summary of the health check result.
    /// </summary>
    /// <returns>
    /// The overall status, followed by the name and message of each unhealthy component when any exist.
    /// </returns>
    public override string ToString()
    {
        var unhealthyComponents = Components.Values.Where(c => !c.IsHealthy).ToList();

        if (!unhealthyComponents.Any())
            return $"System is {Status}";

        var issues = string.Join(", ", unhealthyComponents.Select(c => $"{c.Name}: {c.Message}"));
        return $"System is {Status}. Issues: {issues}";
    }
}
