#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DockerSqliteBackup.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Data;

/// <summary>
/// Repository implementation for backup data persistence using SQLite.
/// </summary>
public class BackupRepository : IBackupRepository
{
    private readonly string _connectionString;
    private readonly ILogger<BackupRepository> _logger;

    public BackupRepository(string connectionString, ILogger<BackupRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the database schema on first run.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var commands = new[]
        {
            CreateSchedulesTable,
            CreateBackupResultsTable,
            CreateRotationPoliciesTable,
            CreateVerificationsTable,
            CreateBackupJobsTable
        };

        foreach (var commandText in commands)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }

        _logger.LogInformation("Database schema initialized successfully");
    }

    /// <summary>
    /// Performs a health check on the database connection.
    /// </summary>
    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return false;
        }
    }

    #region Schedule Operations

    public async Task<BackupSchedule> CreateScheduleAsync(BackupSchedule schedule)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO BackupSchedules 
            (Id, Name, Description, DatabasePath, CronExpression, IsActive, CreatedAt, LastModifiedAt, 
             LastBackupAt, RetentionDays, MaxBackupCount, NotificationEmails, VerifyAfterBackup, StorageType)
            VALUES (@id, @name, @desc, @dbPath, @cron, @active, @created, @modified, @lastBackup, 
                    @retention, @maxCount, @emails, @verify, @storageType)";

        AddParameters(command, schedule);
        await command.ExecuteNonQueryAsync();
        return schedule;
    }

    public async Task<BackupSchedule> UpdateScheduleAsync(BackupSchedule schedule)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE BackupSchedules SET 
            Name=@name, Description=@desc, DatabasePath=@dbPath, CronExpression=@cron, 
            IsActive=@active, LastModifiedAt=@modified, LastBackupAt=@lastBackup, 
            RetentionDays=@retention, MaxBackupCount=@maxCount, NotificationEmails=@emails, 
            VerifyAfterBackup=@verify, StorageType=@storageType
            WHERE Id=@id";

        AddParameters(command, schedule);
        await command.ExecuteNonQueryAsync();
        return schedule;
    }

    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BackupSchedules WHERE Id=@id";
        command.Parameters.AddWithValue("@id", scheduleId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<BackupSchedule?> GetScheduleAsync(Guid scheduleId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BackupSchedules WHERE Id=@id";
        command.Parameters.AddWithValue("@id", scheduleId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows && await reader.ReadAsync() ? ReadSchedule(reader) : null;
    }

    public async Task<IEnumerable<BackupSchedule>> GetAllSchedulesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BackupSchedules ORDER BY CreatedAt DESC";

        var schedules = new List<BackupSchedule>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schedules.Add(ReadSchedule(reader));
        }

        return schedules;
    }

    public async Task<IEnumerable<BackupSchedule>> GetActiveSchedulesAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BackupSchedules WHERE IsActive=1 ORDER BY Name";

        var schedules = new List<BackupSchedule>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            schedules.Add(ReadSchedule(reader));
        }

        return schedules;
    }

    #endregion

    #region Backup Result Operations

    public async Task<BackupResult> CreateBackupResultAsync(BackupResult result)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO BackupResults 
            (Id, ScheduleId, BackupJobId, Status, BackupFilePath, BackupFileSizeBytes, Checksum, 
             StartedAt, CompletedAt, DurationMilliseconds, ErrorMessage, StackTrace, IsVerified, 
             VerifiedAt, Notes, IsStoredInS3, IsStoredLocally, S3ObjectKey)
            VALUES (@id, @scheduleId, @jobId, @status, @filePath, @fileSize, @checksum, 
                    @started, @completed, @duration, @error, @stackTrace, @verified, 
                    @verifiedAt, @notes, @s3Stored, @localStored, @s3Key)";

        AddParameters(command, result);
        await command.ExecuteNonQueryAsync();
        return result;
    }

    public async Task<BackupResult> UpdateBackupResultAsync(BackupResult result)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE BackupResults SET 
            Status=@status, BackupFilePath=@filePath, BackupFileSizeBytes=@fileSize, Checksum=@checksum,
            StartedAt=@started, CompletedAt=@completed, DurationMilliseconds=@duration, 
            ErrorMessage=@error, StackTrace=@stackTrace, IsVerified=@verified, VerifiedAt=@verifiedAt,
            Notes=@notes, IsStoredInS3=@s3Stored, IsStoredLocally=@localStored, S3ObjectKey=@s3Key
            WHERE Id=@id";

        AddParameters(command, result);
        await command.ExecuteNonQueryAsync();
        return result;
    }

    public async Task DeleteBackupResultAsync(Guid resultId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM BackupResults WHERE Id=@id";
        command.Parameters.AddWithValue("@id", resultId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<BackupResult?> GetBackupResultAsync(Guid resultId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BackupResults WHERE Id=@id";
        command.Parameters.AddWithValue("@id", resultId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows && await reader.ReadAsync() ? ReadBackupResult(reader) : null;
    }

    public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT * FROM BackupResults 
            WHERE ScheduleId=@scheduleId 
            ORDER BY StartedAt DESC 
            LIMIT @limit";
        command.Parameters.AddWithValue("@scheduleId", scheduleId.ToString());
        command.Parameters.AddWithValue("@limit", limit);

        var results = new List<BackupResult>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(ReadBackupResult(reader));
        }

        return results;
    }

    #endregion

    #region Rotation Policy Operations

    public async Task<RotationPolicy?> GetRotationPolicyAsync(Guid scheduleId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM RotationPolicies WHERE ScheduleId=@scheduleId";
        command.Parameters.AddWithValue("@scheduleId", scheduleId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows && await reader.ReadAsync() ? ReadRotationPolicy(reader) : null;
    }

    public async Task<RotationPolicy> SaveRotationPolicyAsync(RotationPolicy policy)
    {
        var existing = await GetRotationPolicyAsync(policy.ScheduleId);
        
        if (existing  is not null)
        {
            policy.Id = existing.Id;
            await UpdateRotationPolicyAsync(policy);
        }
        else
        {
            await CreateRotationPolicyAsync(policy);
        }

        return policy;
    }

    private async Task CreateRotationPolicyAsync(RotationPolicy policy)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO RotationPolicies 
            (Id, ScheduleId, Strategy, MaxBackupCount, MaxAgeDays, VerifyBeforeDeletion, 
             MinimumBackupCount, DeleteFailedBackups, CreatedAt, LastModifiedAt, LastRotatedAt)
            VALUES (@id, @scheduleId, @strategy, @maxCount, @maxAge, @verify, 
                    @minCount, @deleteFailed, @created, @modified, @lastRotated)";

        AddParameters(command, policy);
        await command.ExecuteNonQueryAsync();
    }

    private async Task UpdateRotationPolicyAsync(RotationPolicy policy)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE RotationPolicies SET 
            Strategy=@strategy, MaxBackupCount=@maxCount, MaxAgeDays=@maxAge, VerifyBeforeDeletion=@verify,
            MinimumBackupCount=@minCount, DeleteFailedBackups=@deleteFailed, LastModifiedAt=@modified, LastRotatedAt=@lastRotated
            WHERE Id=@id";

        AddParameters(command, policy);
        await command.ExecuteNonQueryAsync();
    }

    #endregion

    #region Verification Operations

    public async Task<RestoreVerification> SaveRestoreVerificationAsync(RestoreVerification verification)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO RestoreVerifications 
            (Id, BackupResultId, IsSuccessful, StatusMessage, StartedAt, CompletedAt, DurationMilliseconds, 
             RecordCount, DatabaseSizeBytes, IntegrityCheckPassed, IntegrityCheckErrors, TemporaryDirectory, ErrorMessage)
            VALUES (@id, @backupId, @successful, @message, @started, @completed, @duration, 
                    @recordCount, @dbSize, @integrityPassed, @integrityErrors, @tempDir, @error)";

        AddParameters(command, verification);
        await command.ExecuteNonQueryAsync();
        return verification;
    }

    public async Task<IEnumerable<RestoreVerification>> GetVerificationHistoryAsync(Guid backupResultId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM RestoreVerifications WHERE BackupResultId=@backupId ORDER BY StartedAt DESC";
        command.Parameters.AddWithValue("@backupId", backupResultId.ToString());

        var verifications = new List<RestoreVerification>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            verifications.Add(ReadRestoreVerification(reader));
        }

        return verifications;
    }

    #endregion

    #region Backup Job Operations

    public async Task<BackupJob> CreateBackupJobAsync(BackupJob job)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO BackupJobs 
            (Id, ScheduleId, Status, CreatedAt, StartedAt, CompletedAt, RetryCount, MaxRetries, IsProcessing)
            VALUES (@id, @scheduleId, @status, @created, @started, @completed, @retryCount, @maxRetries, @processing)";

        AddParameters(command, job);
        await command.ExecuteNonQueryAsync();
        return job;
    }

    public async Task<BackupJob> UpdateBackupJobAsync(BackupJob job)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE BackupJobs SET 
            Status=@status, StartedAt=@started, CompletedAt=@completed, RetryCount=@retryCount, IsProcessing=@processing
            WHERE Id=@id";

        AddParameters(command, job);
        await command.ExecuteNonQueryAsync();
        return job;
    }

    public async Task<BackupJob?> GetBackupJobAsync(Guid jobId)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM BackupJobs WHERE Id=@id";
        command.Parameters.AddWithValue("@id", jobId.ToString());

        using var reader = await command.ExecuteReaderAsync();
        return reader.HasRows && await reader.ReadAsync() ? ReadBackupJob(reader) : null;
    }

    #endregion

    #region Helper Methods

    private static void AddParameters(SqliteCommand command, BackupSchedule schedule)
    {
        command.Parameters.AddWithValue("@id", schedule.Id.ToString());
        command.Parameters.AddWithValue("@name", schedule.Name);
        command.Parameters.AddWithValue("@desc", schedule.Description ?? "");
        command.Parameters.AddWithValue("@dbPath", schedule.DatabasePath);
        command.Parameters.AddWithValue("@cron", schedule.CronExpression);
        command.Parameters.AddWithValue("@active", schedule.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@created", schedule.CreatedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@modified", schedule.LastModifiedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@lastBackup", (object?)schedule.LastBackupAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@retention", schedule.RetentionDays);
        command.Parameters.AddWithValue("@maxCount", schedule.MaxBackupCount);
        command.Parameters.AddWithValue("@emails", schedule.NotificationEmails ?? "");
        command.Parameters.AddWithValue("@verify", schedule.VerifyAfterBackup ? 1 : 0);
        command.Parameters.AddWithValue("@storageType", schedule.StorageType);
    }

    private static void AddParameters(SqliteCommand command, BackupResult result)
    {
        command.Parameters.AddWithValue("@id", result.Id.ToString());
        command.Parameters.AddWithValue("@scheduleId", result.ScheduleId.ToString());
        command.Parameters.AddWithValue("@jobId", result.BackupJobId.ToString());
        command.Parameters.AddWithValue("@status", result.Status);
        command.Parameters.AddWithValue("@filePath", result.BackupFilePath ?? "");
        command.Parameters.AddWithValue("@fileSize", result.BackupFileSizeBytes);
        command.Parameters.AddWithValue("@checksum", result.Checksum ?? "");
        command.Parameters.AddWithValue("@started", result.StartedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@completed", (object?)result.CompletedAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@duration", result.DurationMilliseconds);
        command.Parameters.AddWithValue("@error", result.ErrorMessage ?? "");
        command.Parameters.AddWithValue("@stackTrace", result.StackTrace ?? "");
        command.Parameters.AddWithValue("@verified", result.IsVerified ? 1 : 0);
        command.Parameters.AddWithValue("@verifiedAt", (object?)result.VerifiedAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@notes", result.Notes ?? "");
        command.Parameters.AddWithValue("@s3Stored", result.IsStoredInS3 ? 1 : 0);
        command.Parameters.AddWithValue("@localStored", result.IsStoredLocally ? 1 : 0);
        command.Parameters.AddWithValue("@s3Key", result.S3ObjectKey ?? "");
    }

    private static void AddParameters(SqliteCommand command, RotationPolicy policy)
    {
        command.Parameters.AddWithValue("@id", policy.Id.ToString());
        command.Parameters.AddWithValue("@scheduleId", policy.ScheduleId.ToString());
        command.Parameters.AddWithValue("@strategy", policy.Strategy);
        command.Parameters.AddWithValue("@maxCount", policy.MaxBackupCount);
        command.Parameters.AddWithValue("@maxAge", policy.MaxAgeDays);
        command.Parameters.AddWithValue("@verify", policy.VerifyBeforeDeletion ? 1 : 0);
        command.Parameters.AddWithValue("@minCount", policy.MinimumBackupCount);
        command.Parameters.AddWithValue("@deleteFailed", policy.DeleteFailedBackups ? 1 : 0);
        command.Parameters.AddWithValue("@created", policy.CreatedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@modified", policy.LastModifiedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@lastRotated", (object?)policy.LastRotatedAt?.ToUniversalTime() ?? DBNull.Value);
    }

    private static void AddParameters(SqliteCommand command, RestoreVerification verification)
    {
        command.Parameters.AddWithValue("@id", verification.Id.ToString());
        command.Parameters.AddWithValue("@backupId", verification.BackupResultId.ToString());
        command.Parameters.AddWithValue("@successful", verification.IsSuccessful ? 1 : 0);
        command.Parameters.AddWithValue("@message", verification.StatusMessage ?? "");
        command.Parameters.AddWithValue("@started", verification.StartedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@completed", (object?)verification.CompletedAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@duration", verification.DurationMilliseconds);
        command.Parameters.AddWithValue("@recordCount", verification.RecordCount);
        command.Parameters.AddWithValue("@dbSize", verification.DatabaseSizeBytes);
        command.Parameters.AddWithValue("@integrityPassed", verification.IntegrityCheckPassed ? 1 : 0);
        command.Parameters.AddWithValue("@integrityErrors", verification.IntegrityCheckErrors ?? "");
        command.Parameters.AddWithValue("@tempDir", verification.TemporaryDirectory ?? "");
        command.Parameters.AddWithValue("@error", verification.ErrorMessage ?? "");
    }

    private static void AddParameters(SqliteCommand command, BackupJob job)
    {
        command.Parameters.AddWithValue("@id", job.Id.ToString());
        command.Parameters.AddWithValue("@scheduleId", job.ScheduleId.ToString());
        command.Parameters.AddWithValue("@status", job.Status);
        command.Parameters.AddWithValue("@created", job.CreatedAt.ToUniversalTime());
        command.Parameters.AddWithValue("@started", (object?)job.StartedAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@completed", (object?)job.CompletedAt?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("@retryCount", job.RetryCount);
        command.Parameters.AddWithValue("@maxRetries", job.MaxRetries);
        command.Parameters.AddWithValue("@processing", job.IsProcessing ? 1 : 0);
    }

    private static BackupSchedule ReadSchedule(SqliteDataReader reader)
    {
        return new BackupSchedule
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            Description = reader.GetString(2),
            DatabasePath = reader.GetString(3),
            CronExpression = reader.GetString(4),
            IsActive = reader.GetInt32(5) == 1,
            CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc),
            LastModifiedAt = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
            LastBackupAt = reader.IsDBNull(8) ? null : DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc),
            RetentionDays = reader.GetInt32(9),
            MaxBackupCount = reader.GetInt32(10),
            NotificationEmails = reader.GetString(11),
            VerifyAfterBackup = reader.GetInt32(12) == 1,
            StorageType = reader.GetInt32(13)
        };
    }

    private static BackupResult ReadBackupResult(SqliteDataReader reader)
    {
        return new BackupResult
        {
            Id = Guid.Parse(reader.GetString(0)),
            ScheduleId = Guid.Parse(reader.GetString(1)),
            BackupJobId = Guid.Parse(reader.GetString(2)),
            Status = reader.GetInt32(3),
            BackupFilePath = reader.GetString(4),
            BackupFileSizeBytes = reader.GetInt64(5),
            Checksum = reader.GetString(6),
            StartedAt = DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc),
            CompletedAt = reader.IsDBNull(8) ? null : DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc),
            DurationMilliseconds = reader.GetInt64(9),
            ErrorMessage = reader.GetString(10),
            StackTrace = reader.GetString(11),
            IsVerified = reader.GetInt32(12) == 1,
            VerifiedAt = reader.IsDBNull(13) ? null : DateTime.SpecifyKind(reader.GetDateTime(13), DateTimeKind.Utc),
            Notes = reader.GetString(14),
            IsStoredInS3 = reader.GetInt32(15) == 1,
            IsStoredLocally = reader.GetInt32(16) == 1,
            S3ObjectKey = reader.GetString(17)
        };
    }

    private static RotationPolicy ReadRotationPolicy(SqliteDataReader reader)
    {
        return new RotationPolicy
        {
            Id = Guid.Parse(reader.GetString(0)),
            ScheduleId = Guid.Parse(reader.GetString(1)),
            Strategy = reader.GetInt32(2),
            MaxBackupCount = reader.GetInt32(3),
            MaxAgeDays = reader.GetInt32(4),
            VerifyBeforeDeletion = reader.GetInt32(5) == 1,
            MinimumBackupCount = reader.GetInt32(6),
            DeleteFailedBackups = reader.GetInt32(7) == 1,
            CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc),
            LastModifiedAt = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc),
            LastRotatedAt = reader.IsDBNull(10) ? null : DateTime.SpecifyKind(reader.GetDateTime(10), DateTimeKind.Utc)
        };
    }

    private static RestoreVerification ReadRestoreVerification(SqliteDataReader reader)
    {
        return new RestoreVerification
        {
            Id = Guid.Parse(reader.GetString(0)),
            BackupResultId = Guid.Parse(reader.GetString(1)),
            IsSuccessful = reader.GetInt32(2) == 1,
            StatusMessage = reader.GetString(3),
            StartedAt = DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            CompletedAt = reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            DurationMilliseconds = reader.GetInt64(6),
            RecordCount = reader.GetInt64(7),
            DatabaseSizeBytes = reader.GetInt64(8),
            IntegrityCheckPassed = reader.GetInt32(9) == 1,
            IntegrityCheckErrors = reader.GetString(10),
            TemporaryDirectory = reader.GetString(11),
            ErrorMessage = reader.GetString(12)
        };
    }

    private static BackupJob ReadBackupJob(SqliteDataReader reader)
    {
        return new BackupJob
        {
            Id = Guid.Parse(reader.GetString(0)),
            ScheduleId = Guid.Parse(reader.GetString(1)),
            Status = reader.GetInt32(2),
            CreatedAt = DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
            StartedAt = reader.IsDBNull(4) ? null : DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
            CompletedAt = reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc),
            RetryCount = reader.GetInt32(6),
            MaxRetries = reader.GetInt32(7),
            IsProcessing = reader.GetInt32(8) == 1
        };
    }

    #endregion

    #region SQL Schema Definitions

    private const string CreateSchedulesTable = @"
        CREATE TABLE IF NOT EXISTS BackupSchedules (
            Id TEXT PRIMARY KEY,
            Name TEXT NOT NULL UNIQUE,
            Description TEXT,
            DatabasePath TEXT NOT NULL,
            CronExpression TEXT NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            LastModifiedAt TEXT NOT NULL,
            LastBackupAt TEXT,
            RetentionDays INTEGER NOT NULL,
            MaxBackupCount INTEGER NOT NULL,
            NotificationEmails TEXT,
            VerifyAfterBackup INTEGER NOT NULL DEFAULT 1,
            StorageType INTEGER NOT NULL DEFAULT 0
        )";

    private const string CreateBackupResultsTable = @"
        CREATE TABLE IF NOT EXISTS BackupResults (
            Id TEXT PRIMARY KEY,
            ScheduleId TEXT NOT NULL,
            BackupJobId TEXT NOT NULL,
            Status INTEGER NOT NULL,
            BackupFilePath TEXT,
            BackupFileSizeBytes INTEGER NOT NULL DEFAULT 0,
            Checksum TEXT,
            StartedAt TEXT NOT NULL,
            CompletedAt TEXT,
            DurationMilliseconds INTEGER NOT NULL DEFAULT 0,
            ErrorMessage TEXT,
            StackTrace TEXT,
            IsVerified INTEGER NOT NULL DEFAULT 0,
            VerifiedAt TEXT,
            Notes TEXT,
            IsStoredInS3 INTEGER NOT NULL DEFAULT 0,
            IsStoredLocally INTEGER NOT NULL DEFAULT 1,
            S3ObjectKey TEXT,
            FOREIGN KEY (ScheduleId) REFERENCES BackupSchedules(Id)
        )";

    private const string CreateRotationPoliciesTable = @"
        CREATE TABLE IF NOT EXISTS RotationPolicies (
            Id TEXT PRIMARY KEY,
            ScheduleId TEXT NOT NULL UNIQUE,
            Strategy INTEGER NOT NULL,
            MaxBackupCount INTEGER NOT NULL,
            MaxAgeDays INTEGER NOT NULL,
            VerifyBeforeDeletion INTEGER NOT NULL DEFAULT 0,
            MinimumBackupCount INTEGER NOT NULL,
            DeleteFailedBackups INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            LastModifiedAt TEXT NOT NULL,
            LastRotatedAt TEXT,
            FOREIGN KEY (ScheduleId) REFERENCES BackupSchedules(Id)
        )";

    private const string CreateVerificationsTable = @"
        CREATE TABLE IF NOT EXISTS RestoreVerifications (
            Id TEXT PRIMARY KEY,
            BackupResultId TEXT NOT NULL,
            IsSuccessful INTEGER NOT NULL,
            StatusMessage TEXT,
            StartedAt TEXT NOT NULL,
            CompletedAt TEXT,
            DurationMilliseconds INTEGER NOT NULL,
            RecordCount INTEGER NOT NULL DEFAULT 0,
            DatabaseSizeBytes INTEGER NOT NULL DEFAULT 0,
            IntegrityCheckPassed INTEGER NOT NULL DEFAULT 0,
            IntegrityCheckErrors TEXT,
            TemporaryDirectory TEXT,
            ErrorMessage TEXT,
            FOREIGN KEY (BackupResultId) REFERENCES BackupResults(Id)
        )";

    private const string CreateBackupJobsTable = @"
        CREATE TABLE IF NOT EXISTS BackupJobs (
            Id TEXT PRIMARY KEY,
            ScheduleId TEXT NOT NULL,
            Status INTEGER NOT NULL,
            CreatedAt TEXT NOT NULL,
            StartedAt TEXT,
            CompletedAt TEXT,
            RetryCount INTEGER NOT NULL DEFAULT 0,
            MaxRetries INTEGER NOT NULL DEFAULT 3,
            IsProcessing INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY (ScheduleId) REFERENCES BackupSchedules(Id)
        )";

    #endregion
}
