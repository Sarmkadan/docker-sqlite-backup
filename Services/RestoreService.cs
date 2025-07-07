// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Data;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Manages database restoration operations from backup snapshots.
/// </summary>
public class RestoreService
{
    private readonly IBackupRepository _backupRepository;
    private readonly IStorageService _storageService;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<RestoreService> _logger;

    public RestoreService(
        IBackupRepository backupRepository,
        IStorageService storageService,
        IVerificationService verificationService,
        ILogger<RestoreService> logger)
    {
        _backupRepository = backupRepository ?? throw new ArgumentNullException(nameof(backupRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Restores a database from a specific backup snapshot.
    /// </summary>
    public async Task<bool> RestoreFromSnapshotAsync(
        BackupSnapshot snapshot,
        StorageProvider storageProvider,
        string targetDatabasePath,
        bool createBackupOfCurrent = true,
        CancellationToken cancellationToken = default)
    {
        if (snapshot == null)
            throw new ArgumentNullException(nameof(snapshot));

        if (string.IsNullOrWhiteSpace(targetDatabasePath))
            throw new ArgumentNullException(nameof(targetDatabasePath));

        _logger.LogInformation("Starting restore from snapshot {SnapshotId} to {TargetPath}",
            snapshot.Id, targetDatabasePath);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Backup current database if it exists
            string? currentDatabaseBackupPath = null;
            if (createBackupOfCurrent && File.Exists(targetDatabasePath))
            {
                currentDatabaseBackupPath = $"{targetDatabasePath}.backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
                File.Copy(targetDatabasePath, currentDatabaseBackupPath, overwrite: false);
                _logger.LogInformation("Created backup of current database at {BackupPath}", currentDatabaseBackupPath);
            }

            // Download backup from storage
            var tempRestorePath = Path.Combine(Path.GetTempPath(), $"restore-{snapshot.Id}.db");
            await _storageService.DownloadBackupAsync(snapshot.StoragePath, tempRestorePath, storageProvider, cancellationToken);

            // Verify integrity of downloaded file
            var downloadedHash = await CalculateFileHashAsync(tempRestorePath, cancellationToken);
            if (snapshot.FileHash != null && snapshot.FileHash != downloadedHash)
            {
                File.Delete(tempRestorePath);
                throw new RestoreException(
                    $"Downloaded backup hash mismatch. Expected: {snapshot.FileHash}, Got: {downloadedHash}",
                    snapshot.Id, targetDatabasePath);
            }

            // Replace target database with restored backup
            if (File.Exists(targetDatabasePath))
                File.Delete(targetDatabasePath);

            File.Move(tempRestorePath, targetDatabasePath, overwrite: true);

            // Verify restored database integrity
            var verificationResult = await _verificationService.VerifyRestoredDatabaseAsync(targetDatabasePath, cancellationToken);
            if (!verificationResult.IsValid)
            {
                _logger.LogError("Restored database verification failed: {ErrorMessage}", verificationResult.ErrorMessage);

                // Restore original database if backup exists
                if (currentDatabaseBackupPath != null && File.Exists(currentDatabaseBackupPath))
                {
                    File.Copy(currentDatabaseBackupPath, targetDatabasePath, overwrite: true);
                    _logger.LogInformation("Restored original database from backup");
                }

                throw new RestoreException(
                    $"Restored database verification failed: {verificationResult.ErrorMessage}",
                    snapshot.Id, targetDatabasePath);
            }

            stopwatch.Stop();
            _logger.LogInformation("Restore completed successfully in {Duration}ms", stopwatch.ElapsedMilliseconds);

            // Create restore point record
            var restorePoint = CreateRestorePointFromSnapshot(snapshot);
            restorePoint.MarkAsRestored();
            await _backupRepository.SaveRestorePointAsync(restorePoint, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Restore failed for snapshot {SnapshotId} after {Duration}ms", snapshot.Id, stopwatch.ElapsedMilliseconds);
            throw new RestoreException($"Restore operation failed: {ex.Message}", ex, snapshot.Id, targetDatabasePath);
        }
    }

    /// <summary>
    /// Lists all available restore points for a backup job.
    /// </summary>
    public async Task<List<RestorePoint>> GetRestorePointsAsync(string backupJobId, CancellationToken cancellationToken = default)
    {
        return await _backupRepository.GetRestorePointsByJobIdAsync(backupJobId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a specific restore point by ID.
    /// </summary>
    public async Task<RestorePoint?> GetRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default)
    {
        return await _backupRepository.GetRestorePointByIdAsync(restorePointId, cancellationToken);
    }

    /// <summary>
    /// Tests the restore capability of a snapshot without actually restoring it.
    /// </summary>
    public async Task<bool> TestRestoreAsync(
        BackupSnapshot snapshot,
        StorageProvider storageProvider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Testing restore capability for snapshot {SnapshotId}", snapshot.Id);

        var tempTestPath = Path.Combine(Path.GetTempPath(), $"test-restore-{snapshot.Id}.db");

        try
        {
            // Download and verify backup
            await _storageService.DownloadBackupAsync(snapshot.StoragePath, tempTestPath, storageProvider, cancellationToken);

            // Verify integrity
            var verificationResult = await _verificationService.VerifyRestoredDatabaseAsync(tempTestPath, cancellationToken);

            _logger.LogInformation("Restore test for snapshot {SnapshotId} completed with result: {IsValid}",
                snapshot.Id, verificationResult.IsValid);

            return verificationResult.IsValid;
        }
        finally
        {
            if (File.Exists(tempTestPath))
                File.Delete(tempTestPath);
        }
    }

    /// <summary>
    /// Marks a restore point as expired for deletion.
    /// </summary>
    public async Task<bool> ExpireRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default)
    {
        var restorePoint = await _backupRepository.GetRestorePointByIdAsync(restorePointId, cancellationToken);
        if (restorePoint == null)
            return false;

        restorePoint.ExpiresAt = DateTime.UtcNow;
        await _backupRepository.UpdateRestorePointAsync(restorePoint, cancellationToken);

        _logger.LogInformation("Marked restore point {RestorePointId} as expired", restorePointId);
        return true;
    }

    /// <summary>
    /// Calculates SHA256 hash of a file.
    /// </summary>
    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        {
            var hash = await Task.Run(() => sha256.ComputeHash(stream), cancellationToken);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Creates a restore point from a backup snapshot.
    /// </summary>
    private RestorePoint CreateRestorePointFromSnapshot(BackupSnapshot snapshot)
    {
        return new RestorePoint
        {
            Id = Guid.NewGuid().ToString("N"),
            BackupSnapshotId = snapshot.Id,
            BackupJobId = snapshot.BackupJobId,
            SnapshotTime = snapshot.CreatedAt,
            DatabaseMetadata = snapshot.Metadata,
            RegisteredAt = DateTime.UtcNow,
            IsAvailable = true
        };
    }
}
