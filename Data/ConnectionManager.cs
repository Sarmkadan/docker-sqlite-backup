// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Manages SQLite database connections and initialization.
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<ConnectionManager> _logger;
    private readonly object _syncLock = new object();
    private bool _initialized;

    public ConnectionManager(string databasePath, ILogger<ConnectionManager> logger)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentNullException(nameof(databasePath));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = $"Data Source={databasePath};";
    }

    /// <summary>
    /// Initializes the SQLite database schema on first connection.
    /// </summary>
    public void Initialize()
    {
        lock (_syncLock)
        {
            if (_initialized)
                return;

            _logger.LogInformation("Initializing backup database");

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Create tables for backup jobs
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS BackupJobs (
                        Id TEXT PRIMARY KEY,
                        Name TEXT NOT NULL,
                        DatabasePath TEXT NOT NULL,
                        MaxRetentionCount INTEGER DEFAULT 30,
                        MaxRetentionDays INTEGER DEFAULT 90,
                        EnableVerification INTEGER DEFAULT 1,
                        EnableCompression INTEGER DEFAULT 1,
                        Status INTEGER DEFAULT 0,
                        CreatedAt TEXT NOT NULL,
                        LastBackupAt TEXT,
                        Tags TEXT
                    );
                ");

                // Create table for backup snapshots
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS BackupSnapshots (
                        Id TEXT PRIMARY KEY,
                        BackupJobId TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        SizeBytes INTEGER NOT NULL,
                        StoragePath TEXT NOT NULL,
                        FileHash TEXT,
                        Status INTEGER NOT NULL,
                        DurationMilliseconds INTEGER,
                        ErrorMessage TEXT,
                        CompressionRatio REAL,
                        OriginalSizeBytes INTEGER,
                        IsVerified INTEGER DEFAULT 0,
                        VerifiedAt TEXT,
                        Tags TEXT,
                        FOREIGN KEY (BackupJobId) REFERENCES BackupJobs(Id)
                    );
                ");

                // Create table for restore points
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS RestorePoints (
                        Id TEXT PRIMARY KEY,
                        BackupSnapshotId TEXT NOT NULL,
                        BackupJobId TEXT NOT NULL,
                        SnapshotTime TEXT NOT NULL,
                        IsAvailable INTEGER DEFAULT 1,
                        HasBeenRestored INTEGER DEFAULT 0,
                        LastRestoredAt TEXT,
                        ExpiresAt TEXT,
                        RetentionPriority INTEGER DEFAULT 1,
                        Label TEXT,
                        Description TEXT,
                        RegisteredAt TEXT NOT NULL,
                        Tags TEXT,
                        FOREIGN KEY (BackupJobId) REFERENCES BackupJobs(Id)
                    );
                ");

                // Create table for backup metadata
                ExecuteNonQuery(connection, @"
                    CREATE TABLE IF NOT EXISTS BackupMetadata (
                        Id TEXT PRIMARY KEY,
                        SnapshotId TEXT NOT NULL,
                        SqliteVersion TEXT,
                        TableCount INTEGER,
                        TableNames TEXT,
                        IndexCount INTEGER,
                        HasViews INTEGER DEFAULT 0,
                        ViewCount INTEGER DEFAULT 0,
                        IsWalMode INTEGER DEFAULT 0,
                        PageSize INTEGER,
                        PageCount INTEGER,
                        FreePages INTEGER,
                        UserVersion INTEGER,
                        ApplicationId INTEGER,
                        IsEncrypted INTEGER DEFAULT 0,
                        IntegrityCheckPassed INTEGER DEFAULT 1,
                        IntegrityCheckOutput TEXT,
                        CapturedAt TEXT NOT NULL,
                        HostName TEXT,
                        ToolVersion TEXT,
                        FOREIGN KEY (SnapshotId) REFERENCES BackupSnapshots(Id)
                    );
                ");

                // Create indexes
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_snapshots_job ON BackupSnapshots(BackupJobId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_snapshots_created ON BackupSnapshots(CreatedAt);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_restorepoints_job ON RestorePoints(BackupJobId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_restorepoints_snapshot ON RestorePoints(BackupSnapshotId);");
                ExecuteNonQuery(connection, "CREATE INDEX IF NOT EXISTS idx_metadata_snapshot ON BackupMetadata(SnapshotId);");

                connection.Close();
            }

            _initialized = true;
            _logger.LogInformation("Database initialization completed");
        }
    }

    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    public SQLiteConnection CreateConnection()
    {
        return new SQLiteConnection(_connectionString);
    }

    /// <summary>
    /// Executes a non-query SQL command.
    /// </summary>
    private static void ExecuteNonQuery(SQLiteConnection connection, string commandText)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        // Nothing specific to dispose for SQLite
    }
}
