// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Data;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services in the DI container.
    /// </summary>
    public static IServiceCollection AddBackupServices(
        this IServiceCollection services,
        AppSettings appSettings)
    {
        // Register configuration
        services.AddSingleton(appSettings);

        // Register database context
        var connectionString = $"Data Source={appSettings.DatabasePath}";
        services.AddSingleton<IBackupRepository>(sp =>
        {
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BackupRepository>>();
            return new BackupRepository(connectionString, logger);
        });

        // Register services
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddScoped<IStorageService, StorageService>();
        services.AddScoped<IRotationService, RotationService>();
        services.AddScoped<IVerificationService, VerificationService>();

        return services;
    }

    /// <summary>
    /// Initializes the database schema and returns the repository.
    /// </summary>
    public static async Task<IBackupRepository> InitializeDatabaseAsync(
        this IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IBackupRepository>();
        await repository.InitializeAsync();
        return repository;
    }
}
