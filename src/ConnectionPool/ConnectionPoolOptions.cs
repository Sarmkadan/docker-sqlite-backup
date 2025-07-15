#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.ConnectionPool;

/// <summary>
/// Configuration options for the SQLite connection pool.
/// Bind this class from <c>appsettings.json</c> or configure it programmatically
/// via <see cref="ConnectionPoolExtensions.AddSqliteConnectionPool"/>.
/// </summary>
public class ConnectionPoolOptions
{
    /// <summary>Gets or sets the minimum number of connections to keep alive in the pool.</summary>
    public int MinPoolSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of connections the pool will hold simultaneously
    /// (idle + in-use combined). Callers that exceed this limit block until a connection
    /// is returned or <see cref="ConnectionTimeoutSeconds"/> elapses.
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of seconds an idle connection may remain in the pool
    /// before the eviction sweep closes it.
    /// </summary>
    public int IdleTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the number of seconds a caller will wait for an available connection
    /// before a <see cref="TimeoutException"/> is thrown.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the interval in seconds between background idle-eviction sweeps.
    /// </summary>
    public int EvictionIntervalSeconds { get; set; } = 60;

    /// <summary>Gets or sets the SQLite connection string used to open new connections.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Returns <see langword="true"/> when all required options are within acceptable bounds.
    /// </summary>
    public bool IsValid() =>
        MaxPoolSize > 0 &&
        MinPoolSize >= 0 &&
        MinPoolSize <= MaxPoolSize &&
        IdleTimeoutSeconds > 0 &&
        ConnectionTimeoutSeconds > 0 &&
        EvictionIntervalSeconds > 0 &&
        !string.IsNullOrWhiteSpace(ConnectionString);
}

/// <summary>
/// Point-in-time snapshot of connection pool health metrics.
/// </summary>
/// <param name="Total">Sum of idle and in-use connections currently held by the pool.</param>
/// <param name="Available">Connections sitting idle and ready for immediate acquisition.</param>
/// <param name="InUse">Connections currently checked out by callers.</param>
/// <param name="Evicted">Cumulative count of connections discarded by the idle-eviction sweep since pool start.</param>
public record PoolStatistics(int Total, int Available, int InUse, long Evicted);
