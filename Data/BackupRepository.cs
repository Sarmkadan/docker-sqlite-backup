// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Data.SQLite;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Models;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Implements persistent storage for backup data using SQLite.
/// </summary>
public class BackupRepository : IBackupRepository
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger<BackupRepository> _logger;

    public BackupRepository(ConnectionManager connectionManager, ILogger<BackupRepository> logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Backup Job Operations

    public async Task<BackupJob?> GetBackupJobByIdAsync(string jobId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM BackupJobs WHERE Id = @JobId";
                    command.Parameters.AddWithValue("@JobId", jobId);

                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? MapBackupJob(reader) : null;
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<List<BackupJob>> GetAllBackupJobsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var jobs = new List<BackupJob>();

            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM BackupJobs ORDER BY CreatedAt DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            jobs.Add(MapBackupJob(reader));
                        }
                    }
                }
            }

            return jobs;
        }, cancellationToken);
    }

    public async Task SaveBackupJobAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO BackupJobs (Id, Name, DatabasePath, MaxRetentionCount, MaxRetentionDays,
                            EnableVerification, EnableCompression, Status, CreatedAt, LastBackupAt, Tags)
                        VALUES (@Id, @Name, @DatabasePath, @MaxRetentionCount, @MaxRetentionDays,
                            @EnableVerification, @EnableCompression, @Status, @CreatedAt, @LastBackupAt, @Tags)";

                    command.Parameters.AddWithValue("@Id", job.Id);
                    command.Parameters.AddWithValue("@Name", job.Name);
                    command.Parameters.AddWithValue("@DatabasePath", job.DatabasePath);
                    command.Parameters.AddWithValue("@MaxRetentionCount", job.MaxRetentionCount);
                    command.Parameters.AddWithValue("@MaxRetentionDays", job.MaxRetentionDays);
                    command.Parameters.AddWithValue("@EnableVerification", job.EnableVerification ? 1 : 0);
                    command.Parameters.AddWithValue("@EnableCompression", job.EnableCompression ? 1 : 0);
                    command.Parameters.AddWithValue("@Status", (int)job.Status);
                    command.Parameters.AddWithValue("@CreatedAt", job.CreatedAt.ToString("O"));
                    command.Parameters.AddWithValue("@LastBackupAt", job.LastBackupAt?.ToString("O") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(job.Tags));

                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Backup job {JobId} saved successfully", job.Id);
        }, cancellationToken);
    }

    public async Task UpdateBackupJobAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE BackupJobs SET
                            Name = @Name, DatabasePath = @DatabasePath,
                            MaxRetentionCount = @MaxRetentionCount, MaxRetentionDays = @MaxRetentionDays,
                            EnableVerification = @EnableVerification, EnableCompression = @EnableCompression,
                            Status = @Status, LastBackupAt = @LastBackupAt, Tags = @Tags
                        WHERE Id = @Id";

                    command.Parameters.AddWithValue("@Id", job.Id);
                    command.Parameters.AddWithValue("@Name", job.Name);
                    command.Parameters.AddWithValue("@DatabasePath", job.DatabasePath);
                    command.Parameters.AddWithValue("@MaxRetentionCount", job.MaxRetentionCount);
                    command.Parameters.AddWithValue("@MaxRetentionDays", job.MaxRetentionDays);
                    command.Parameters.AddWithValue("@EnableVerification", job.EnableVerification ? 1 : 0);
                    command.Parameters.AddWithValue("@EnableCompression", job.EnableCompression ? 1 : 0);
                    command.Parameters.AddWithValue("@Status", (int)job.Status);
                    command.Parameters.AddWithValue("@LastBackupAt", job.LastBackupAt?.ToString("O") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(job.Tags));

                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Backup job {JobId} updated successfully", job.Id);
        }, cancellationToken);
    }

    public async Task DeleteBackupJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM BackupJobs WHERE Id = @JobId";
                    command.Parameters.AddWithValue("@JobId", jobId);
                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Backup job {JobId} deleted", jobId);
        }, cancellationToken);
    }

    // Backup Snapshot Operations

    public async Task SaveSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO BackupSnapshots
                            (Id, BackupJobId, CreatedAt, SizeBytes, StoragePath, FileHash, Status,
                             DurationMilliseconds, ErrorMessage, CompressionRatio, OriginalSizeBytes,
                             IsVerified, VerifiedAt, Tags)
                        VALUES (@Id, @BackupJobId, @CreatedAt, @SizeBytes, @StoragePath, @FileHash,
                                @Status, @DurationMilliseconds, @ErrorMessage, @CompressionRatio,
                                @OriginalSizeBytes, @IsVerified, @VerifiedAt, @Tags)";

                    BindSnapshotParameters(command, snapshot);
                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Backup snapshot {SnapshotId} saved", snapshot.Id);
        }, cancellationToken);
    }

    public async Task<BackupSnapshot?> GetSnapshotByIdAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM BackupSnapshots WHERE Id = @SnapshotId";
                    command.Parameters.AddWithValue("@SnapshotId", snapshotId);

                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? MapBackupSnapshot(reader) : null;
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<List<BackupSnapshot>> GetSnapshotsByJobIdAsync(string backupJobId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var snapshots = new List<BackupSnapshot>();

            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM BackupSnapshots WHERE BackupJobId = @JobId ORDER BY CreatedAt DESC";
                    command.Parameters.AddWithValue("@JobId", backupJobId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            snapshots.Add(MapBackupSnapshot(reader));
                        }
                    }
                }
            }

            return snapshots;
        }, cancellationToken);
    }

    public async Task UpdateSnapshotAsync(BackupSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE BackupSnapshots SET
                            Status = @Status, DurationMilliseconds = @DurationMilliseconds,
                            ErrorMessage = @ErrorMessage, IsVerified = @IsVerified,
                            VerifiedAt = @VerifiedAt, Tags = @Tags
                        WHERE Id = @Id";

                    command.Parameters.AddWithValue("@Id", snapshot.Id);
                    command.Parameters.AddWithValue("@Status", (int)snapshot.Status);
                    command.Parameters.AddWithValue("@DurationMilliseconds", snapshot.DurationMilliseconds);
                    command.Parameters.AddWithValue("@ErrorMessage", snapshot.ErrorMessage ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@IsVerified", snapshot.IsVerified ? 1 : 0);
                    command.Parameters.AddWithValue("@VerifiedAt", snapshot.VerifiedAt?.ToString("O") ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(snapshot.Tags));

                    command.ExecuteNonQuery();
                }
            }
        }, cancellationToken);
    }

    public async Task DeleteSnapshotAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM BackupSnapshots WHERE Id = @SnapshotId";
                    command.Parameters.AddWithValue("@SnapshotId", snapshotId);
                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Backup snapshot {SnapshotId} deleted", snapshotId);
        }, cancellationToken);
    }

    // Restore Point Operations

    public async Task SaveRestorePointAsync(RestorePoint restorePoint, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO RestorePoints
                            (Id, BackupSnapshotId, BackupJobId, SnapshotTime, IsAvailable, HasBeenRestored,
                             LastRestoredAt, ExpiresAt, RetentionPriority, Label, Description, RegisteredAt, Tags)
                        VALUES (@Id, @BackupSnapshotId, @BackupJobId, @SnapshotTime, @IsAvailable,
                                @HasBeenRestored, @LastRestoredAt, @ExpiresAt, @RetentionPriority,
                                @Label, @Description, @RegisteredAt, @Tags)";

                    BindRestorePointParameters(command, restorePoint);
                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Restore point {RestorePointId} saved", restorePoint.Id);
        }, cancellationToken);
    }

    public async Task<RestorePoint?> GetRestorePointByIdAsync(string restorePointId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM RestorePoints WHERE Id = @RestorePointId";
                    command.Parameters.AddWithValue("@RestorePointId", restorePointId);

                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read() ? MapRestorePoint(reader) : null;
                    }
                }
            }
        }, cancellationToken);
    }

    public async Task<List<RestorePoint>> GetRestorePointsByJobIdAsync(string backupJobId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var restorePoints = new List<RestorePoint>();

            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM RestorePoints WHERE BackupJobId = @JobId ORDER BY SnapshotTime DESC";
                    command.Parameters.AddWithValue("@JobId", backupJobId);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            restorePoints.Add(MapRestorePoint(reader));
                        }
                    }
                }
            }

            return restorePoints;
        }, cancellationToken);
    }

    public async Task UpdateRestorePointAsync(RestorePoint restorePoint, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        UPDATE RestorePoints SET
                            IsAvailable = @IsAvailable, HasBeenRestored = @HasBeenRestored,
                            LastRestoredAt = @LastRestoredAt, ExpiresAt = @ExpiresAt,
                            RetentionPriority = @RetentionPriority, Label = @Label,
                            Description = @Description, Tags = @Tags
                        WHERE Id = @Id";

                    BindRestorePointParameters(command, restorePoint);
                    command.ExecuteNonQuery();
                }
            }
        }, cancellationToken);
    }

    public async Task DeleteRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            using (var connection = _connectionManager.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM RestorePoints WHERE Id = @RestorePointId";
                    command.Parameters.AddWithValue("@RestorePointId", restorePointId);
                    command.ExecuteNonQuery();
                }
            }

            _logger.LogInformation("Restore point {RestorePointId} deleted", restorePointId);
        }, cancellationToken);
    }

    // Metadata Operations

    public async Task SaveMetadataAsync(BackupMetadata metadata, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            // Implementation would go here
        }, cancellationToken);
    }

    public async Task<BackupMetadata?> GetMetadataBySnapshotIdAsync(string snapshotId, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            // Implementation would go here
            return null as BackupMetadata;
        }, cancellationToken);
    }

    // Helper Methods

    private static BackupJob MapBackupJob(SQLiteDataReader reader)
    {
        return new BackupJob
        {
            Id = reader.GetString("Id"),
            Name = reader.GetString("Name"),
            DatabasePath = reader.GetString("DatabasePath"),
            MaxRetentionCount = reader.GetInt32("MaxRetentionCount"),
            MaxRetentionDays = reader.GetInt32("MaxRetentionDays"),
            EnableVerification = reader.GetInt32("EnableVerification") == 1,
            EnableCompression = reader.GetInt32("EnableCompression") == 1,
            Status = (BackupStatus)reader.GetInt32("Status"),
            CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
            LastBackupAt = reader.IsDBNull("LastBackupAt") ? null : DateTime.Parse(reader.GetString("LastBackupAt")),
            Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString("Tags")) ?? new()
        };
    }

    private static BackupSnapshot MapBackupSnapshot(SQLiteDataReader reader)
    {
        return new BackupSnapshot
        {
            Id = reader.GetString("Id"),
            BackupJobId = reader.GetString("BackupJobId"),
            CreatedAt = DateTime.Parse(reader.GetString("CreatedAt")),
            SizeBytes = reader.GetInt64("SizeBytes"),
            StoragePath = reader.GetString("StoragePath"),
            FileHash = reader.IsDBNull("FileHash") ? null : reader.GetString("FileHash"),
            Status = (BackupStatus)reader.GetInt32("Status"),
            DurationMilliseconds = reader.GetInt64("DurationMilliseconds"),
            ErrorMessage = reader.IsDBNull("ErrorMessage") ? null : reader.GetString("ErrorMessage"),
            IsVerified = reader.GetInt32("IsVerified") == 1,
            VerifiedAt = reader.IsDBNull("VerifiedAt") ? null : DateTime.Parse(reader.GetString("VerifiedAt")),
            Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString("Tags")) ?? new()
        };
    }

    private static RestorePoint MapRestorePoint(SQLiteDataReader reader)
    {
        return new RestorePoint
        {
            Id = reader.GetString("Id"),
            BackupSnapshotId = reader.GetString("BackupSnapshotId"),
            BackupJobId = reader.GetString("BackupJobId"),
            SnapshotTime = DateTime.Parse(reader.GetString("SnapshotTime")),
            IsAvailable = reader.GetInt32("IsAvailable") == 1,
            HasBeenRestored = reader.GetInt32("HasBeenRestored") == 1,
            LastRestoredAt = reader.IsDBNull("LastRestoredAt") ? null : DateTime.Parse(reader.GetString("LastRestoredAt")),
            ExpiresAt = reader.IsDBNull("ExpiresAt") ? null : DateTime.Parse(reader.GetString("ExpiresAt")),
            RetentionPriority = reader.GetInt32("RetentionPriority"),
            Label = reader.IsDBNull("Label") ? null : reader.GetString("Label"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            RegisteredAt = DateTime.Parse(reader.GetString("RegisteredAt")),
            Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString("Tags")) ?? new()
        };
    }

    private static void BindSnapshotParameters(SQLiteCommand command, BackupSnapshot snapshot)
    {
        command.Parameters.AddWithValue("@Id", snapshot.Id);
        command.Parameters.AddWithValue("@BackupJobId", snapshot.BackupJobId);
        command.Parameters.AddWithValue("@CreatedAt", snapshot.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@SizeBytes", snapshot.SizeBytes);
        command.Parameters.AddWithValue("@StoragePath", snapshot.StoragePath);
        command.Parameters.AddWithValue("@FileHash", snapshot.FileHash ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Status", (int)snapshot.Status);
        command.Parameters.AddWithValue("@DurationMilliseconds", snapshot.DurationMilliseconds);
        command.Parameters.AddWithValue("@ErrorMessage", snapshot.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompressionRatio", snapshot.CompressionRatio ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@OriginalSizeBytes", snapshot.OriginalSizeBytes ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@IsVerified", snapshot.IsVerified ? 1 : 0);
        command.Parameters.AddWithValue("@VerifiedAt", snapshot.VerifiedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(snapshot.Tags));
    }

    private static void BindRestorePointParameters(SQLiteCommand command, RestorePoint restorePoint)
    {
        command.Parameters.AddWithValue("@Id", restorePoint.Id);
        command.Parameters.AddWithValue("@BackupSnapshotId", restorePoint.BackupSnapshotId);
        command.Parameters.AddWithValue("@BackupJobId", restorePoint.BackupJobId);
        command.Parameters.AddWithValue("@SnapshotTime", restorePoint.SnapshotTime.ToString("O"));
        command.Parameters.AddWithValue("@IsAvailable", restorePoint.IsAvailable ? 1 : 0);
        command.Parameters.AddWithValue("@HasBeenRestored", restorePoint.HasBeenRestored ? 1 : 0);
        command.Parameters.AddWithValue("@LastRestoredAt", restorePoint.LastRestoredAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@ExpiresAt", restorePoint.ExpiresAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RetentionPriority", restorePoint.RetentionPriority);
        command.Parameters.AddWithValue("@Label", restorePoint.Label ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Description", restorePoint.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@RegisteredAt", restorePoint.RegisteredAt.ToString("O"));
        command.Parameters.AddWithValue("@Tags", JsonSerializer.Serialize(restorePoint.Tags));
    }
}
