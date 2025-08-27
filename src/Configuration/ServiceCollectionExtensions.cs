#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Data;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.DependencyInjection;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all application services in the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="appSettings">The application settings.</param>
    /// <exception cref="ArgumentNullException">Thrown when services or appSettings is null.</exception>
    public static IServiceCollection AddBackupServices(
        this IServiceCollection services,
        AppSettings appSettings)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (appSettings == null)
        {
            throw new ArgumentNullException(nameof(appSettings));
        }

        try
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
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IIntegrityCheckerService, IntegrityCheckerService>();

            return services;
        }
        catch (Exception ex) when (ex is not ConfigurationException)
        {
            throw new ConfigurationException("Service registration", "Failed to register backup services.", ex);
        }
    }

    /// <summary>
    /// Initializes the database schema and returns the repository.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when serviceProvider is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when database initialization fails.</exception>
    public static async Task<IBackupRepository> InitializeDatabaseAsync(
        this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        try
        {
            var repository = serviceProvider.GetRequiredService<IBackupRepository>();
            await repository.InitializeAsync();
            return repository;
        }
        catch (Exception ex) when (ex is not ConfigurationException)
        {
            throw new ConfigurationException("Database initialization", "Failed to initialize database schema.", ex);
        }
    }
}
