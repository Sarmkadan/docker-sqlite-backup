// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Models;

namespace DockerSqliteBackup;

/// <summary>
/// Main entry point for the SQLite backup tool.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var host = ConfigureServices();
        var app = host.Services;

        try
        {
            // Initialize the backup database
            var connectionManager = app.GetRequiredService<ConnectionManager>();
            connectionManager.Initialize();

            // Load configuration
            var config = app.GetRequiredService<IConfiguration>();
            var appConfig = config.GetSection("BackupService").Get<BackupServiceConfiguration>()
                ?? throw new InvalidOperationException("BackupService configuration is missing");

            _logger.LogInformation("Docker SQLite Backup Tool initialized");
            _logger.LogInformation("Database path: {DatabasePath}", appConfig.MetadataDatabasePath);

            // Create sample backup job for demonstration
            await CreateSampleBackupJobAsync(app, appConfig);

            // Start backup scheduler
            var scheduleService = app.GetRequiredService<ScheduleService>();
            var backupRepository = app.GetRequiredService<IBackupRepository>();
            var backupService = app.GetRequiredService<BackupService>();

            // Load all backup jobs and register their schedules
            var allJobs = await backupRepository.GetAllBackupJobsAsync();
            foreach (var job in allJobs.Where(j => j.Schedule?.IsEnabled == true))
            {
                scheduleService.RegisterSchedule(job, ct => backupService.ExecuteBackupAsync(job, ct));
            }

            _logger.LogInformation("Backup scheduler started with {JobCount} active jobs", allJobs.Count);

            // Run backup scheduler loop
            await RunSchedulerAsync(scheduleService, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application error");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    /// <summary>
    /// Configures dependency injection container.
    /// </summary>
    private static IHost ConfigureServices()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        // Configuration
        services.Configure<BackupServiceConfiguration>(config.GetSection("BackupService"));

        // Logging
        services.AddLogging(builder =>
        {
            builder
                .ClearProviders()
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information);
        });

        // Data Access
        services.AddSingleton(sp =>
        {
            var appConfig = config.GetSection("BackupService").Get<BackupServiceConfiguration>()
                ?? throw new InvalidOperationException("BackupService configuration is missing");
            var logger = sp.GetRequiredService<ILogger<ConnectionManager>>();
            return new ConnectionManager(appConfig.MetadataDatabasePath, logger);
        });

        services.AddSingleton<IBackupRepository, BackupRepository>();

        // Services
        services.AddSingleton<IVerificationService, VerificationService>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<BackupService>();
        services.AddSingleton<RestoreService>();
        services.AddSingleton<ScheduleService>();

        // Configuration
        services.AddSingleton(config);

        var host = new HostBuilder()
            .ConfigureServices(_ =>
            {
                foreach (var service in services)
                {
                    if (service.ImplementationInstance != null)
                        _.AddSingleton(service.ServiceType, service.ImplementationInstance);
                    else if (service.ImplementationType != null)
                        _.AddSingleton(service.ServiceType, service.ImplementationType);
                    else if (service.ImplementationFactory != null)
                        _.AddSingleton(service.ServiceType, service.ImplementationFactory);
                }
            })
            .Build();

        return host;
    }

    /// <summary>
    /// Runs the backup scheduler loop that checks for and executes due backups.
    /// </summary>
    private static async Task RunSchedulerAsync(ScheduleService scheduleService, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting backup scheduler loop");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Check and execute due backups
                await scheduleService.ExecuteDueBackupsAsync(cancellationToken);

                // Log schedule status
                var schedules = scheduleService.GetScheduleStatuses();
                foreach (var schedule in schedules)
                {
                    _logger.LogDebug("Job {JobId}: next execution at {NextExecution}",
                        schedule.BackupJobId, schedule.NextExecutionTime);
                }

                // Wait a minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scheduler loop cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduler loop");
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Creates a sample backup job if none exist (for demonstration).
    /// </summary>
    private static async Task CreateSampleBackupJobAsync(IServiceProvider app, BackupServiceConfiguration config)
    {
        var repository = app.GetRequiredService<IBackupRepository>();
        var allJobs = await repository.GetAllBackupJobsAsync();

        if (allJobs.Count > 0)
            return; // Sample job already exists

        _logger.LogInformation("Creating sample backup job");

        var job = new BackupJob
        {
            Id = "sample-job-001",
            Name = "Sample SQLite Database Backup",
            DatabasePath = "/data/sample.db",
            MaxRetentionCount = 30,
            MaxRetentionDays = 90,
            EnableVerification = true,
            EnableCompression = true,
            Schedule = new BackupSchedule
            {
                CronExpression = "0 2 * * *", // Daily at 2 AM
                Description = "Daily backup at 2 AM UTC",
                IsEnabled = true,
                TimeZone = "UTC",
                TimeoutSeconds = 3600
            },
            StorageProvider = new StorageProvider
            {
                Type = StorageType.Local,
                Name = "Local Storage",
                Location = "/backup-storage",
                IsActive = true
            }
        };

        await repository.SaveBackupJobAsync(job);
        _logger.LogInformation("Sample backup job created: {JobId}", job.Id);
    }

    private static ILogger<Program>? _logger;

    static Program()
    {
        // Get logger for this class
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var provider = services.BuildServiceProvider();
        _logger = provider.GetRequiredService<ILogger<Program>>();
    }
}
