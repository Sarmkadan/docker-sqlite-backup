#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Audit;
using DockerSqliteBackup.Caching;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Health;
using DockerSqliteBackup.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace DockerSqliteBackup.Extensions;

/// <summary>
/// Extension methods for configuring services in the DI container.
/// Provides convenient registration of backup system components.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Phase 2 infrastructure services to the DI container.
    /// </summary>
    public static IServiceCollection AddBackupInfrastructure(this IServiceCollection services)
    {
        // Caching
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Event system
        services.AddSingleton<IBackupEventPublisher, BackupEventPublisher>();
        services.AddSingleton<MetricsEventListener>();
        services.AddSingleton<NotificationEventListener>();

        // Health checks
        services.AddSingleton<HealthCheckService>();

        // Audit logging
        services.AddSingleton<AuditLogger>();

        // Integration
        services.AddSingleton<HttpClientFactory>();
        services.AddSingleton<WebhookClient>();
        services.AddSingleton<INotificationClient, ConsoleNotificationClient>();

        return services;
    }

    /// <summary>
    /// Adds event listeners to the publisher.
    /// </summary>
    public static IServiceCollection AddEventListeners(
        this IServiceCollection services,
        params Type[] listenerTypes)
    {
        foreach (var listenerType in listenerTypes)
        {
            if (!typeof(IBackupEventListener).IsAssignableFrom(listenerType))
                throw new ArgumentException($"{listenerType.Name} must implement {typeof(IBackupEventListener).Name}");

            services.AddSingleton(typeof(IBackupEventListener), listenerType);
        }

        return services;
    }

    /// <summary>
    /// Adds a custom notification client.
    /// </summary>
    public static IServiceCollection AddNotificationClient<T>(this IServiceCollection services)
        where T : class, INotificationClient
    {
        services.AddSingleton<INotificationClient, T>();
        return services;
    }

    /// <summary>
    /// Adds caching with custom expiration settings.
    /// </summary>
    public static IServiceCollection AddMemoryCache(
        this IServiceCollection services,
        TimeSpan? cleanupInterval = null)
    {
        services.AddSingleton<ICacheService>(sp => new MemoryCacheService(cleanupInterval));
        return services;
    }

    /// <summary>
    /// Initializes the event system with default listeners.
    /// </summary>
    public static async Task InitializeEventSystemAsync(this IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IBackupEventPublisher>();
        var listeners = provider.GetServices<IBackupEventListener>();

        foreach (var listener in listeners)
            publisher.Subscribe(listener);
    }
}
