// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables("BACKUP_");
    })
    .ConfigureServices((context, services) =>
    {
        var appSettings = new AppSettings();
        context.Configuration.GetSection("AppSettings").Bind(appSettings);

        services.AddBackupServices(appSettings);
        services.AddHostedService<BackupWorker>();

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(Enum.Parse<LogLevel>(appSettings.LogLevel ?? "Information"));
        });
    });

using var host = builder.Build();

// Initialize database
var appSettings = host.Services.GetRequiredService<AppSettings>();
var repository = await host.Services.InitializeDatabaseAsync();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("Docker SQLite Backup Tool v{Version}", BackupConstants.ApplicationVersion);
logger.LogInformation("Database initialized at {DatabasePath}", appSettings.DatabasePath);

await host.RunAsync();
