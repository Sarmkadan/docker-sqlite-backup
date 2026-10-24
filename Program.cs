// Author: Vladyslav Zaiets

#nullable enable

using DockerSqliteBackup;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Health;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// The 'healthcheck' subcommand is invoked by the Docker HEALTHCHECK instruction
// (see README/Dockerfile). It evaluates the persisted status file without starting
// the full host, and exits with a non-zero code when the container is unhealthy.
if (args.Length > 0 && string.Equals(args[0], "healthcheck", StringComparison.OrdinalIgnoreCase))
{
    var healthCheckConfiguration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables("BACKUP_")
        .Build();

    var healthCheckSettings = new AppSettings();
    healthCheckConfiguration.GetSection("AppSettings").Bind(healthCheckSettings);

    var statusStore = new HealthStatusStore(healthCheckSettings.HealthCheckStatusFilePath);
    var outcome = DockerHealthCheckEvaluator.Evaluate(statusStore.Load(), healthCheckSettings);

    Console.WriteLine(outcome.Reason);
    return outcome.IsHealthy ? 0 : 1;
}

// The '--list' flag prints the backups that already exist in local storage and exits
// without starting the host. Local backups are written under AppContext.BaseDirectory/backups.
if (args.Length > 0 && string.Equals(args[0], "--list", StringComparison.OrdinalIgnoreCase))
{
    var listConfiguration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables("BACKUP_")
        .Build();

    var listSettings = new AppSettings();
    listConfiguration.GetSection("AppSettings").Bind(listSettings);

    using var listServices = new ServiceCollection()
        .AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Warning))
        .AddBackupServices(listSettings)
        .BuildServiceProvider();

    var storageService = listServices.GetRequiredService<IStorageService>();
    var backups = await storageService.ListBackupInfosAsync(new LocalStorageConfiguration
    {
        Name = "Local",
        BaseDirectory = Path.Combine(AppContext.BaseDirectory, "backups"),
    });

    if (backups.Count == 0)
    {
        Console.WriteLine("No backups found.");
        return 0;
    }

    Console.WriteLine($"{"Name",-40} {"Size",15}  {"Created (UTC)",-19}");
    foreach (var backup in backups)
    {
        Console.WriteLine($"{backup.Name,-40} {backup.Size,15:N0}  {backup.Created:yyyy-MM-dd HH:mm:ss}");
    }

    return 0;
}

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables("BACKUP_");
    })
    .ConfigureServices((context, services) =>
    {
        // Registered first so the ValidateOnStart hosted service runs before BackupWorker starts.
        services.AddBackupOptions(context.Configuration);

        var appSettings = new AppSettings();
        context.Configuration.GetSection("AppSettings").Bind(appSettings);

        services.AddBackupServices(appSettings);
        services.AddHostedService<BackupWorker>();

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            // Fall back to Information instead of crashing on a malformed LogLevel value.
            var minimumLevel = Enum.TryParse<LogLevel>(appSettings.LogLevel, ignoreCase: true, out var parsed)
                ? parsed
                : LogLevel.Information;
            logging.SetMinimumLevel(minimumLevel);
        });
    });

using var host = builder.Build();

// Initialize database
var appSettings = host.Services.GetRequiredService<AppSettings>();
var repository = await host.Services.InitializeDatabaseAsync();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Wire the health status listener so backup/verification events keep the
// HEALTHCHECK status file up to date across the lifetime of this process.
var eventPublisher = host.Services.GetRequiredService<IBackupEventPublisher>();
var healthStatusListener = host.Services.GetRequiredService<HealthStatusEventListener>();
eventPublisher.Subscribe(healthStatusListener);

logger.LogInformation("Docker SQLite Backup Tool v{Version}", BackupConstants.ApplicationVersion);
logger.LogInformation("Database initialized at {DatabasePath}", appSettings.DatabasePath);

if (appSettings.DryRun)
{
    logger.LogWarning("Dry-run mode enabled: no backups will be created, uploaded, verified, or deleted");
}

await host.RunAsync();
return 0;
