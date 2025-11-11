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
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="appSettings">The application settings containing database path and other configuration.</param>
    /// <returns>The <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="appSettings"/> is <see langword="null"/>.</exception>
    /// <exception cref="ConfigurationException">Thrown when service registration fails.</exception>
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

            // Register services as singletons: they are stateless and are consumed by
            // the singleton BackupWorker hosted service. Scoped registrations would fail
            // scope validation at startup when the host environment is Development.
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IScheduleService, ScheduleService>();
            services.AddSingleton<IStorageService, StorageService>();
            services.AddSingleton<IRotationService, RotationService>();
            services.AddSingleton<IVerificationService, VerificationService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<IIntegrityCheckerService, IntegrityCheckerService>();

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
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve required services.</param>
    /// <returns>The initialized <see cref="IBackupRepository"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
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
            var repository = serviceProvider.GetRequiredService<IBackupRepository>()
                ?? throw new ConfigurationException("Database initialization", "Failed to resolve IBackupRepository from service provider.");
            await repository.InitializeAsync();
            return repository;
        }
        catch (Exception ex) when (ex is not ConfigurationException)
        {
            throw new ConfigurationException("Database initialization", "Failed to initialize database schema.", ex);
        }
    }
}