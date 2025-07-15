#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.ConnectionPool;

/// <summary>
/// Manages a bounded pool of reusable SQLite connections with configurable size,
/// idle timeout, and background eviction.  Safe to use as a singleton.
/// </summary>
public interface IConnectionPool : IAsyncDisposable
{
    /// <summary>
    /// Leases a connection from the pool, opening a new one if no idle connection is available
    /// and the pool has not reached <see cref="ConnectionPoolOptions.MaxPoolSize"/>.
    /// Dispose the returned <see cref="PooledConnection"/> to return it to the pool.
    /// </summary>
    /// <exception cref="TimeoutException">
    /// Thrown when no connection becomes available within
    /// <see cref="ConnectionPoolOptions.ConnectionTimeoutSeconds"/>.
    /// </exception>
    Task<PooledConnection> AcquireAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a point-in-time snapshot of pool metrics.</summary>
    Task<PoolStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes all idle connections and waits for in-use connections to be returned before closing them.
    /// </summary>
    Task DrainAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A leased <see cref="SqliteConnection"/> that returns itself to the pool when disposed
/// instead of closing the underlying connection.
/// </summary>
public sealed class PooledConnection : IAsyncDisposable
{
    private readonly SqliteConnectionPool _pool;
    private bool _disposed;

    /// <summary>Gets the underlying open <see cref="SqliteConnection"/>.</summary>
    public SqliteConnection Connection { get; }

    internal PooledConnection(SqliteConnection connection, SqliteConnectionPool pool)
    {
        Connection = connection;
        _pool = pool;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _pool.Release(Connection);
        }
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// SQLite connection pool that limits total connections to
/// <see cref="ConnectionPoolOptions.MaxPoolSize"/> and evicts idle connections
/// that exceed <see cref="ConnectionPoolOptions.IdleTimeoutSeconds"/>.
/// </summary>
public sealed class SqliteConnectionPool : IConnectionPool
{
    private readonly ConnectionPoolOptions _options;
    private readonly ILogger<SqliteConnectionPool> _logger;

    // Each slot in the semaphore represents permission to hold one connection alive
    // (whether idle or in-use).  A slot is consumed on first open and freed on eviction.
    private readonly SemaphoreSlim _creationGate;

    // FIFO queue so the eviction sweep always hits the oldest idle connections first.
    private readonly ConcurrentQueue<PooledEntry> _idle = new();
    private readonly Timer _evictionTimer;

    private int _inUse;
    private long _totalEvicted;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new <see cref="SqliteConnectionPool"/> with the supplied options.
    /// </summary>
    public SqliteConnectionPool(ConnectionPoolOptions options, ILogger<SqliteConnectionPool> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!options.IsValid())
            throw new ArgumentException("Connection pool options failed validation.", nameof(options));

        _options = options;
        _logger = logger;
        _creationGate = new SemaphoreSlim(options.MaxPoolSize, options.MaxPoolSize);

        var evictInterval = TimeSpan.FromSeconds(options.EvictionIntervalSeconds);
        _evictionTimer = new Timer(_ => EvictIdleConnections(), null, evictInterval, evictInterval);

        _logger.LogInformation(
            "SQLite connection pool started. MaxPoolSize={Max}, IdleTimeout={Idle}s",
            options.MaxPoolSize, options.IdleTimeoutSeconds);
    }

    /// <inheritdoc/>
    public async Task<PooledConnection> AcquireAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Fast path: reuse the oldest idle connection that is still healthy.
        while (_idle.TryDequeue(out var entry))
        {
            if (!entry.IsExpired(_options.IdleTimeoutSeconds) && entry.Connection.State == ConnectionState.Open)
            {
                Interlocked.Increment(ref _inUse);
                _logger.LogDebug("Reused idle connection. InUse={InUse}", _inUse);
                return new PooledConnection(entry.Connection, this);
            }

            // Connection expired or broken while idle — discard and free its slot.
            CloseAndFreeSlot(entry.Connection);
        }

        // Slow path: open a new connection, bounded by MaxPoolSize.
        var timeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds);
        if (!await _creationGate.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
        {
            throw new TimeoutException(
                $"A pooled connection was not available within {_options.ConnectionTimeoutSeconds} seconds. " +
                $"Consider increasing MaxPoolSize (current: {_options.MaxPoolSize}).");
        }

        try
        {
            var connection = new SqliteConnection(_options.ConnectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _inUse);

            _logger.LogDebug("Opened new pooled connection. InUse={InUse}", _inUse);
            return new PooledConnection(connection, this);
        }
        catch (Exception ex)
        {
            _creationGate.Release();
            _logger.LogError(ex, "Failed to open a new SQLite connection.");
            throw;
        }
    }

    /// <summary>
    /// Returns a connection to the idle queue.  Called by <see cref="PooledConnection.DisposeAsync"/>.
    /// </summary>
    internal void Release(SqliteConnection connection)
    {
        Interlocked.Decrement(ref _inUse);

        if (connection.State == ConnectionState.Open)
        {
            _idle.Enqueue(new PooledEntry(connection));
            _logger.LogDebug("Connection returned to pool. Idle={Idle}, InUse={InUse}", _idle.Count, _inUse);
        }
        else
        {
            // Broken connection — free the creation slot so a healthy one can take its place.
            _creationGate.Release();
        }
    }

    /// <inheritdoc/>
    public Task<PoolStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var available = _idle.Count;
        var inUse = _inUse;
        return Task.FromResult(new PoolStatistics(available + inUse, available, inUse, _totalEvicted));
    }

    /// <inheritdoc/>
    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Draining SQLite connection pool.");

        while (_idle.TryDequeue(out var entry))
            CloseAndFreeSlot(entry.Connection);

        // Wait for in-use connections to be returned, then discard them.
        var deadline = DateTime.UtcNow.AddSeconds(_options.ConnectionTimeoutSeconds);
        while (_inUse > 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            while (_idle.TryDequeue(out var entry))
                CloseAndFreeSlot(entry.Connection);
        }

        if (_inUse > 0)
            _logger.LogWarning("Pool drained with {InUse} connection(s) still checked out.", _inUse);
        else
            _logger.LogInformation("Pool drained cleanly.");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _evictionTimer.DisposeAsync().ConfigureAwait(false);
        await DrainAsync().ConfigureAwait(false);
        _creationGate.Dispose();
    }

    private void EvictIdleConnections()
    {
        var evicted = 0;
        var totalInPool = _idle.Count + _inUse;

        // Walk from the head of the FIFO queue (oldest connections first).
        // Stop evicting if doing so would shrink the pool below MinPoolSize.
        while (totalInPool - evicted > _options.MinPoolSize &&
               _idle.TryPeek(out var head) &&
               head.IsExpired(_options.IdleTimeoutSeconds))
        {
            if (_idle.TryDequeue(out var expired))
            {
                CloseAndFreeSlot(expired.Connection);
                evicted++;
            }
        }

        if (evicted > 0)
        {
            Interlocked.Add(ref _totalEvicted, evicted);
            _logger.LogDebug("Evicted {Count} idle connection(s). Remaining idle: {Idle}", evicted, _idle.Count);
        }
    }

    private void CloseAndFreeSlot(SqliteConnection connection)
    {
        try
        {
            connection.Close();
            connection.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing a pooled connection.");
        }
        finally
        {
            _creationGate.Release();
        }
    }

    private sealed class PooledEntry
    {
        private readonly DateTime _idleSince = DateTime.UtcNow;

        public PooledEntry(SqliteConnection connection) => Connection = connection;

        public SqliteConnection Connection { get; }

        public bool IsExpired(int idleTimeoutSeconds) =>
            (DateTime.UtcNow - _idleSince).TotalSeconds >= idleTimeoutSeconds;
    }
}
