#nullable enable
// Author: Vladyslav Zaiets

using System.IO.Compression;
using System.Security.Cryptography;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Utilities;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

using ArgumentNullException = DockerSqliteBackup.Exceptions.ArgumentNullException;
using ArgumentException = DockerSqliteBackup.Exceptions.ArgumentException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service responsible for executing and managing SQLite backup operations, including
/// scheduling, snapshot creation via the Online Backup API, incremental support,
/// encryption, and storage integration.
/// </summary>
public sealed class BackupService : IBackupService
{
    private readonly IBackupRepository _repository;
    private readonly IStorageService _storageService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<BackupService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupService"/> class.
    /// </summary>
    /// <param name="repository">The backup repository for metadata persistence.</param>
    /// <param name="storageService">The storage service for remote file operations.</param>
    /// <param name="appSettings">Global application settings.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public BackupService(
        IBackupRepository repository,
        IStorageService storageService,
        AppSettings appSettings,
        ILogger<BackupService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates and executes a backup for the specified schedule.
    /// </summary>
    /// <param name="schedule">The backup schedule to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="BackupResult"/> describing the outcome of the backup.</returns>
    /// <exception cref="ArgumentNullException">Thrown when schedule is null.</exception>
    /// <exception cref="InvalidScheduleException">Thrown when the provided schedule is invalid.</exception>
    /// <exception cref="DatabaseAccessException">Thrown when the database cannot be accessed.</exception>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid.</exception>
    public async Task<BackupResult> ExecuteBackupAsync(BackupSchedule schedule, CancellationToken cancellationToken = default)
    {
        ValidateSchedule(schedule);

        var result = new BackupResult
        {
            ScheduleId = schedule.Id,
            StartedAt = DateTime.UtcNow,
            BackupMode = schedule.BackupMode
        };

        try
        {
            _logger.LogInformation("Starting backup for schedule {ScheduleId}: {ScheduleName}", 
                schedule.Id, schedule.Name);

            var timestamp = DateTime.UtcNow;
            var backupPath = GenerateBackupPath(schedule, timestamp);
            PrepareBackupDirectory(backupPath);

            // Choose full vs incremental snapshot strategy.
            if ((Constants.BackupMode)schedule.BackupMode == Constants.BackupMode.Incremental)
            {
                // History is ordered newest-first; scan the whole history because the most
                // recent entries are usually incrementals and the full baseline sits further back.
                var baseBackup = (await _repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue))
                    .FirstOrDefault(b => b.IsSuccess && b.BackupMode == (int)Constants.BackupMode.Full);

                if (baseBackup is not null)
                {
                    await CaptureIncrementalAsync(schedule.DatabasePath, baseBackup.BackupFilePath, backupPath);
                    result.BaseBackupResultId = baseBackup.Id;
                    _logger.LogInformation(
                        "Incremental backup created. Base: {BaseId}, Path: {BackupPath}",
                        baseBackup.Id, backupPath);
                }
                else
                {
                    // No prior full backup — fall back to a full snapshot on this run.
                    _logger.LogInformation(
                        "No previous full backup found for schedule {ScheduleId}; performing full backup as baseline",
                        schedule.Id);
                    await SafeCopyDatabaseAsync(schedule.DatabasePath, backupPath);
                    result.BackupMode = (int)Constants.BackupMode.Full;
                }
            }
            else
            {
                await SafeCopyDatabaseAsync(schedule.DatabasePath, backupPath);
            }

            _logger.LogInformation("Backup file created at {BackupPath}", backupPath);

            backupPath = await EncryptBackupIfNeededAsync(backupPath);

            // Track original file size before compression
            long? originalFileSize = null;
            if (_appSettings.CompressBackups)
            {
                originalFileSize = new FileInfo(backupPath).Length;
            }

            var compressionResult = await CompressBackupIfNeededAsync(backupPath, originalFileSize);
            backupPath = compressionResult.compressedPath;
            result.CompressionRatio = compressionResult.compressionRatio;

            result.BackupFilePath = backupPath;
            result.OriginalFileSizeBytes = originalFileSize;
            try
            {
                result.BackupFileSizeBytes = new FileInfo(backupPath).Length;
                result.Checksum = await CalculateBackupChecksumAsync(backupPath);
            }
            catch (Exception ex) when (ex is not FileNotFoundException and not IOException and not UnauthorizedAccessException)
            {
                throw new BackupException("Failed to read backup file metadata or calculate checksum", ex);
            }
            result.Status = (int)Constants.BackupStatus.Success;

            // Create manifest file next to the backup
            await CreateBackupManifestAsync(result, schedule, backupPath);

            // Upload to the configured remote storage backend, if one is configured.
            // Exceptions here (e.g. AmazonS3Exception for missing s3:PutObject permission)
            // are intentionally NOT caught so they propagate and mark the job as failed.
            if (schedule.StorageConfiguration is not null and not LocalStorageConfiguration)
            {
                _logger.LogInformation("Uploading backup to remote storage backend: {StorageType}",
                    schedule.StorageConfiguration.GetType().Name);
                var remoteKey = await _storageService.UploadBackupAsync(backupPath, schedule.StorageConfiguration);
                _logger.LogInformation("Backup uploaded to remote storage. Key/path: {RemoteKey}", remoteKey);

                if (schedule.StorageConfiguration is S3Configuration)
                {
                    result.IsStoredInS3 = true;
                    result.S3ObjectKey = remoteKey;
                }
            }

            _logger.LogInformation("Backup completed successfully. Size: {Size} bytes, Checksum: {Checksum}",
                result.BackupFileSizeBytes, result.Checksum);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup failed for schedule {ScheduleId}", schedule.Id);
            result.Status = (int)Constants.BackupStatus.Failed;
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
            throw;
        }
        finally
        {
            result.CompletedAt = DateTime.UtcNow;
            result.DurationMilliseconds = (long)(result.CompletedAt.Value - result.StartedAt).TotalMilliseconds;

            // Persist the result so history, rotation, and incremental base lookup can see it.
            // Persistence errors are logged rather than thrown so they never mask the
            // original backup outcome.
            try
            {
                await _repository.CreateBackupResultAsync(result);
            }
            catch (Exception persistEx)
            {
                _logger.LogError(persistEx,
                    "Failed to persist backup result {ResultId} for schedule {ScheduleId}",
                    result.Id, schedule.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// Calculates the SHA256 checksum of a backup file.
    /// </summary>
    /// <param name="filePath">The path to the backup file.</param>
    /// <returns>A hex-encoded SHA256 hash string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when filePath is empty or invalid.</exception>
    public async Task<string> CalculateBackupChecksumAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(nameof(filePath), "File path cannot be null or whitespace.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Backup file not found for checksum calculation", filePath);
        }

        try
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hash = await sha256.ComputeHashAsync(stream);
            return Convert.ToHexStringLower(hash);
        }
        catch (Exception ex) when (ex is not IOException and not UnauthorizedAccessException and not NotSupportedException)
        {
            throw new BackupException("Failed to calculate backup checksum", ex);
        }
    }

    /// <summary>
    /// Retrieves backup history for a schedule.
    /// </summary>
    /// <param name="scheduleId">The ID of the schedule.</param>
    /// <param name="limit">The maximum number of results to return.</param>
    /// <returns>An enumerable of <see cref="BackupResult"/> objects.</returns>
    public async Task<IEnumerable<BackupResult>> GetBackupHistoryAsync(Guid scheduleId, int limit = 10)
    {
        return await _repository.GetBackupHistoryAsync(scheduleId, limit);
    }

    /// <summary>
    /// Deletes a backup file and removes it from storage.
    /// </summary>
    /// <param name="backupResultId">The ID of the backup result to delete.</param>
    /// <exception cref="BackupException">Thrown when the backup result is not found.</exception>
    public async Task DeleteBackupAsync(Guid backupResultId)
    {
        var backup = await _repository.GetBackupResultAsync(backupResultId);
        if (backup  is null)
        {
            throw new BackupException("Backup not found", backupResultId);
        }

        if (!string.IsNullOrEmpty(backup.BackupFilePath) && File.Exists(backup.BackupFilePath))
        {
            File.Delete(backup.BackupFilePath);
            _logger.LogInformation("Deleted backup file: {FilePath}", backup.BackupFilePath);
        }

        await _repository.DeleteBackupResultAsync(backupResultId);
    }

    /// <summary>
    /// Gets a specific backup result by ID.
    /// </summary>
    /// <param name="backupResultId">The ID of the backup result.</param>
    /// <returns>The <see cref="BackupResult"/> object, or null if not found.</returns>
    public async Task<BackupResult?> GetBackupResultAsync(Guid backupResultId)
    {
        return await _repository.GetBackupResultAsync(backupResultId);
    }

    /// <summary>
    /// Copies a SQLite database using the Online Backup API, which guarantees a consistent
    /// snapshot even when the source database has active writers in WAL mode.
    /// Falls back to File.Copy only if the backup API call fails (e.g., non-SQLite file).
    /// </summary>
    private async Task SafeCopyDatabaseAsync(string sourcePath, string destinationPath)
    {
        try
        {
            using var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
            using var destination = new SqliteConnection($"Data Source={destinationPath}");
            await source.OpenAsync();
            await destination.OpenAsync();
            source.BackupDatabase(destination);
            _logger.LogDebug("Database backed up using SQLite Online Backup API");
        }
        catch (SqliteException ex)
        {
            _logger.LogWarning(ex, "SQLite backup API failed, falling back to file copy");
            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static string GenerateBackupPath(BackupSchedule schedule, DateTime timestamp)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "backups");
        var scheduleDir = Path.Combine(baseDir, SanitizeFileName(schedule.Name));
        var fileName = $"backup_{timestamp:yyyy-MM-dd_HH-mm-ss}.sqlite";
        return Path.Combine(scheduleDir, fileName);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Split(invalidChars));
    }

    /// <summary>
    /// Captures an incremental backup by copying WAL-checkpointed pages that differ
    /// from the base snapshot. The approach:
    /// 1. Open the source database and issue PRAGMA wal_checkpoint(TRUNCATE) to flush
    ///    all WAL data into the main database file, ensuring the live DB reflects all commits.
    /// 2. Copy the updated source database to the incremental path — because the checkpoint
    ///    has already merged changed pages, this file contains only the current state.
    ///    The caller is responsible for tracking the base backup ID so that older snapshots
    ///    can be safely pruned only when no incremental depends on them.
    /// </summary>
    private async Task CaptureIncrementalAsync(string sourcePath, string baseBackupPath, string incrementalPath)
    {
        // Flush all WAL frames into the main database file so that SafeCopyDatabaseAsync
        // captures a fully up-to-date snapshot without needing the WAL file.
        await CheckpointDatabaseAsync(sourcePath);
        await SafeCopyDatabaseAsync(sourcePath, incrementalPath);
    }

    /// <summary>
    /// Issues a WAL_CHECKPOINT(TRUNCATE) pragma on the source database to merge all
    /// pending WAL frames back into the main database file and truncate the WAL.
    /// </summary>
    private async Task CheckpointDatabaseAsync(string sourcePath)
    {
        try
        {
            using var connection = new SqliteConnection($"Data Source={sourcePath}");
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
            await command.ExecuteNonQueryAsync();
            _logger.LogDebug("WAL checkpoint completed for {SourcePath}", sourcePath);
        }
        catch (SqliteException ex)
        {
            // Non-WAL databases silently ignore the checkpoint — this is acceptable.
            _logger.LogDebug(ex, "WAL checkpoint skipped for {SourcePath} (database may not be in WAL mode)", sourcePath);
        }
    }

    /// <summary>
    /// Creates a manifest file next to the backup file with metadata about the backup.
    /// </summary>
    /// <param name="backupResult">The backup result containing metadata about the backup.</param>
    /// <param name="schedule">The backup schedule that was executed.</param>
    /// <param name="backupFilePath">The path to the backup file.</param>
    private async Task CreateBackupManifestAsync(BackupResult backupResult, BackupSchedule schedule, string backupFilePath)
    {
        try
        {
            var manifest = new BackupManifest
            {
                ScheduleId = backupResult.ScheduleId,
                BackupJobId = backupResult.BackupJobId,
                CreatedAt = backupResult.StartedAt,
                CompletedAt = backupResult.CompletedAt ?? DateTime.UtcNow,
                SourceDatabasePath = schedule.DatabasePath,
                SourceDatabaseSizeBytes = new FileInfo(schedule.DatabasePath).Length,
                BackupFilePath = backupResult.BackupFilePath,
                BackupFileSizeBytes = backupResult.BackupFileSizeBytes,
                OriginalFileSizeBytes = backupResult.OriginalFileSizeBytes,
                CompressionRatio = backupResult.CompressionRatio,
                Checksum = backupResult.Checksum,
                IsEncrypted = backupFilePath.EndsWith(".enc", StringComparison.OrdinalIgnoreCase),
                IsCompressed = backupFilePath.EndsWith(BackupConstants.CompressedBackupExtension, StringComparison.OrdinalIgnoreCase),
                BackupMode = ((Constants.BackupMode)backupResult.BackupMode).ToString(),
                BaseBackupResultId = backupResult.BaseBackupResultId,
                Notes = $"Backup created by docker-sqlite-backup v{BackupConstants.ApplicationVersion}"
            };

            // Set storage type based on configuration
            if (schedule.StorageConfiguration is null or LocalStorageConfiguration)
            {
                manifest.StorageType = "Local";
            }
            else if (schedule.StorageConfiguration is S3Configuration)
            {
                manifest.StorageType = "S3";
                manifest.RemoteStorageKey = backupResult.S3ObjectKey;
            }
            else
            {
                manifest.StorageType = schedule.StorageConfiguration.GetType().Name.Replace("Configuration", "");
            }

            // Write manifest file next to the backup
            var manifestPath = backupFilePath + BackupConstants.BackupMetadataExtension;
            manifest.WriteToFile(manifestPath);

            _logger.LogInformation("Backup manifest created: {ManifestPath}", manifestPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create backup manifest, continuing with backup operation");
            // Don't throw - manifest creation is optional
        }
    }

    private void ValidateSchedule(BackupSchedule? schedule)
    {
        if (schedule == null)
        {
            throw new ArgumentNullException(nameof(schedule));
        }

        if (!schedule.IsValid())
        {
            throw new InvalidScheduleException("Schedule is not valid", schedule.Id);
        }

        if (!schedule.ValidateDatabasePath())
        {
            throw new DatabaseAccessException(schedule.DatabasePath,
                new FileNotFoundException($"Database file not found: {schedule.DatabasePath}"));
        }
    }

    private async Task<string> EncryptBackupIfNeededAsync(string backupPath)
    {
        var encryptionKey = ResolveEncryptionKey();
        if (encryptionKey is null)
            return backupPath;

        try
        {
            var encryptedPath = backupPath + ".enc";
            await EncryptionUtility.EncryptFileAsync(backupPath, encryptedPath, encryptionKey);
            File.Delete(backupPath);
            _logger.LogInformation("Backup archive encrypted with AES-256: {EncryptedPath}", encryptedPath);
            return encryptedPath;
        }
        catch (Exception ex) when (ex is not BackupException and not ArgumentException)
        {
            throw new BackupException("Failed to encrypt backup file", ex);
        }
    }

    private void PrepareBackupDirectory(string backupPath)
    {
        var backupDir = Path.GetDirectoryName(backupPath);
        if (string.IsNullOrEmpty(backupDir) || Directory.Exists(backupDir))
            return;

        try
        {
            Directory.CreateDirectory(backupDir);
        }
        catch (Exception dirEx) when (dirEx is not IOException and not UnauthorizedAccessException)
        {
            throw new BackupException($"Failed to create backup directory: {backupDir}", dirEx);
        }
    }

    /// <summary>
    /// Returns the AES-256 encryption key if encryption is enabled, or null otherwise.
    /// The key is sourced from the <c>BACKUP_ENCRYPTION_KEY</c> environment variable first,
    /// then from <see cref="AppSettings.EncryptionKey"/> in configuration.
    /// </summary>
    private string? ResolveEncryptionKey()
    {
        if (!_appSettings.EnableEncryption)
            return null;

        var key = Environment.GetEnvironmentVariable("BACKUP_ENCRYPTION_KEY")
                  ?? _appSettings.EncryptionKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning(
                "Encryption is enabled but no key was found. " +
                "Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey. Skipping encryption.");
            return null;
        }

        if (!EncryptionUtility.IsValidKey(key))
        {
            _logger.LogError(
                "Encryption key is invalid (must be a Base64-encoded 32-byte value). Skipping encryption.");
            return null;
        }

        return key;
    }

    /// <summary>
    /// Compresses a backup file using gzip if compression is enabled in settings.
    /// Returns the path to the compressed file (or original if compression is disabled).
    /// </summary>
    /// <param name="backupPath">The path to the backup file to compress.</param>
    /// <param name="originalFileSize">The original file size before compression (for calculating ratio).</param>
    /// <returns>The path to the compressed file (with .gz extension) or original path if compression disabled.</returns>
    private async Task<(string compressedPath, double? compressionRatio)> CompressBackupIfNeededAsync(string backupPath, long? originalFileSize = null)
    {
        if (!_appSettings.CompressBackups)
        {
            _logger.LogDebug("Compression is disabled, skipping backup compression");
            return (backupPath, null);
        }

        try
        {
            var compressedPath = backupPath + BackupConstants.CompressedBackupExtension;
            long compressedFileSize = 0;

            await using (var sourceStream = File.OpenRead(backupPath))
            await using (var compressedStream = File.Create(compressedPath))
            await using (var gzipStream = new GZipStream(compressedStream, (CompressionLevel)_appSettings.CompressionLevel))
            {
                await sourceStream.CopyToAsync(gzipStream);
            }

            // Get compressed file size
            compressedFileSize = new FileInfo(compressedPath).Length;

            // Delete the original uncompressed file
            File.Delete(backupPath);

            double? compressionRatio = null;
            if (originalFileSize.HasValue && originalFileSize > 0 && compressedFileSize > 0)
            {
                compressionRatio = (double)originalFileSize.Value / compressedFileSize;
            }

            _logger.LogInformation("Backup compressed with gzip: {CompressedPath} (Original: {OriginalSize} bytes, Compressed: {CompressedSize} bytes, Ratio: {Ratio:F2}x)",
                compressedPath, originalFileSize, compressedFileSize, compressionRatio);
            return (compressedPath, compressionRatio);
        }
        catch (Exception ex) when (ex is not BackupException and not ArgumentException)
        {
            _logger.LogWarning(ex, "Failed to compress backup file {BackupPath}, continuing with uncompressed version", backupPath);
            return (backupPath, null); // Return original path if compression fails
        }
    }
}
