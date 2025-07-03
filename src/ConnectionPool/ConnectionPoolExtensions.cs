// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.ConnectionPool;

/// <summary>
/// Extension methods for registering the SQLite connection pool in the DI container.
/// </summary>
public static class ConnectionPoolExtensions
{
    /// <summary>
    /// Registers <see cref="IConnectionPool"/> as a singleton, deriving sensible defaults
    /// from the application's <see cref="AppSettings"/> (database path and max concurrency).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="appSettings">
    /// Application settings.  <see cref="AppSettings.DatabasePath"/> becomes the connection string
    /// and <see cref="AppSettings.MaxConcurrentBackups"/> seeds <see cref="ConnectionPoolOptions.MaxPoolSize"/>.
    /// </param>
    /// <param name="configure">
    /// Optional delegate to override any default option value after the initial defaults are applied.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resulting <see cref="ConnectionPoolOptions"/> fail validation.
    /// </exception>
    public static IServiceCollection AddSqliteConnectionPool(
        this IServiceCollection services,
        AppSettings appSettings,
        Action<ConnectionPoolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(appSettings);

        var options = new ConnectionPoolOptions
        {
            ConnectionString = $"Data Source={appSettings.DatabasePath};Pooling=False"
        };

        if (appSettings.MaxConcurrentBackups > 0)
            options.MaxPoolSize = appSettings.MaxConcurrentBackups;

        configure?.Invoke(options);

        return RegisterPool(services, options);
    }

    /// <summary>
    /// Registers <see cref="IConnectionPool"/> as a singleton with fully custom options.
    /// Use this overload when the pool is not tied to a single application database path.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// Required delegate to configure all <see cref="ConnectionPoolOptions"/> values,
    /// including <see cref="ConnectionPoolOptions.ConnectionString"/>.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the resulting <see cref="ConnectionPoolOptions"/> fail validation.
    /// </exception>
    public static IServiceCollection AddSqliteConnectionPool(
        this IServiceCollection services,
        Action<ConnectionPoolOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ConnectionPoolOptions();
        configure(options);

        return RegisterPool(services, options);
    }

    private static IServiceCollection RegisterPool(IServiceCollection services, ConnectionPoolOptions options)
    {
        if (!options.IsValid())
            throw new InvalidOperationException(
                "SQLite connection pool options failed validation. " +
                "Ensure ConnectionString is non-empty, MaxPoolSize > 0, and timeout values are positive.");

        services.AddSingleton<IConnectionPool>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqliteConnectionPool>>();
            return new SqliteConnectionPool(options, logger);
        });

        return services;
    }
}
