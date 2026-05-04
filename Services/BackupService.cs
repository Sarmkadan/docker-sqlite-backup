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
/// Orchestrates backup operations and manages backup lifecycle.
/// </summary>
public class BackupService
{
    private readonly IBackupRepository _backupRepository;
    private readonly IStorageService _storageService;
    private readonly IVerificationService _verificationService;
    private readonly ILogger<BackupService> _logger;

    public BackupService(
        IBackupRepository backupRepository,
        IStorageService storageService,
        IVerificationService verificationService,
        ILogger<BackupService> logger)
    {
        _backupRepository = backupRepository ?? throw new ArgumentNullException(nameof(backupRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes a full backup for the specified backup job.
    /// </summary>
    public async Task<BackupSnapshot> ExecuteBackupAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        var validationErrors = job.Validate();
        if (validationErrors.Count > 0)
            throw new BackupException($"Invalid backup job configuration: {string.Join(", ", validationErrors)}", job.Id, null);

        var snapshot = new BackupSnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            BackupJobId = job.Id,
            CreatedAt = DateTime.UtcNow,
            Status = BackupStatus.InProgress
        };

        _logger.LogInformation("Starting backup for job {JobId} into snapshot {SnapshotId}", job.Id, snapshot.Id);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Copy the database file to temporary location
            var tempBackupPath = Path.Combine(Path.GetTempPath(), $"backup-{snapshot.Id}.db");
            File.Copy(job.DatabasePath, tempBackupPath, overwrite: true);

            // Calculate file hash before storage
            snapshot.FileHash = await CalculateFileHashAsync(tempBackupPath, cancellationToken);
            snapshot.SizeBytes = new FileInfo(tempBackupPath).Length;
            snapshot.OriginalSizeBytes = snapshot.SizeBytes;

            // Upload to storage
            var storagePath = job.StorageProvider.ConstructBackupPath($"{snapshot.Id}.db");
            await _storageService.UploadBackupAsync(tempBackupPath, storagePath, job.StorageProvider, cancellationToken);
            snapshot.StoragePath = storagePath;

            // Apply compression if enabled
            if (job.EnableCompression)
            {
                var compressedPath = $"{tempBackupPath}.gz";
                CompressFile(tempBackupPath, compressedPath);
                var compressedSize = new FileInfo(compressedPath).Length;
                snapshot.CompressionRatio = (double)compressedSize / snapshot.SizeBytes;
                snapshot.SizeBytes = compressedSize;
                File.Delete(compressedPath);
            }

            // Capture metadata
            snapshot.Metadata = await CaptureMetadataAsync(job.DatabasePath, cancellationToken);

            // Verify backup if enabled
            if (job.EnableVerification)
            {
                var verificationResult = await _verificationService.VerifyBackupAsync(snapshot, job, cancellationToken);
                if (verificationResult.IsValid)
                {
                    snapshot.MarkAsVerified();
                }
                else
                {
                    snapshot.Status = BackupStatus.VerificationFailed;
                    snapshot.ErrorMessage = $"Verification failed: {verificationResult.ErrorMessage}";
                }
            }
            else
            {
                snapshot.Status = BackupStatus.Completed;
            }

            stopwatch.Stop();
            snapshot.DurationMilliseconds = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("Backup completed for job {JobId} snapshot {SnapshotId} in {Duration}ms",
                job.Id, snapshot.Id, stopwatch.ElapsedMilliseconds);

            // Clean up temporary files
            File.Delete(tempBackupPath);

            // Save snapshot to repository
            await _backupRepository.SaveSnapshotAsync(snapshot, cancellationToken);

            return snapshot;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            snapshot.DurationMilliseconds = stopwatch.ElapsedMilliseconds;
            snapshot.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Backup failed for job {JobId} snapshot {SnapshotId}", job.Id, snapshot.Id);
            throw new BackupException($"Backup execution failed: {ex.Message}", ex, job.Id, snapshot.Id);
        }
    }

    /// <summary>
    /// Retrieves all snapshots for a specific backup job.
    /// </summary>
    public async Task<List<BackupSnapshot>> GetJobSnapshotsAsync(string backupJobId, CancellationToken cancellationToken = default)
    {
        return await _backupRepository.GetSnapshotsByJobIdAsync(backupJobId, cancellationToken);
    }

    /// <summary>
    /// Deletes old snapshots based on retention policies.
    /// </summary>
    public async Task<int> ApplyRetentionPoliciesAsync(BackupJob job, CancellationToken cancellationToken = default)
    {
        var snapshots = await GetJobSnapshotsAsync(job.Id, cancellationToken);
        var eligibleForDeletion = snapshots
            .Where(s => s.IsEligibleForDeletion(job.MaxRetentionDays))
            .OrderByDescending(s => s.CreatedAt)
            .Skip(job.MaxRetentionCount)
            .ToList();

        int deletedCount = 0;
        foreach (var snapshot in eligibleForDeletion)
        {
            try
            {
                await _storageService.DeleteBackupAsync(snapshot.StoragePath, job.StorageProvider, cancellationToken);
                await _backupRepository.DeleteSnapshotAsync(snapshot.Id, cancellationToken);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old snapshot {SnapshotId}", snapshot.Id);
            }
        }

        return deletedCount;
    }

    /// <summary>
    /// Calculates the SHA256 hash of a file.
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
    /// Compresses a file using GZIP compression.
    /// </summary>
    private void CompressFile(string sourcePath, string destinationPath)
    {
        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
        using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
        using (var gzipStream = new System.IO.Compression.GZipStream(destinationStream, System.IO.Compression.CompressionMode.Compress))
        {
            sourceStream.CopyTo(gzipStream);
        }
    }

    /// <summary>
    /// Captures metadata about the SQLite database.
    /// </summary>
    private async Task<BackupMetadata> CaptureMetadataAsync(string databasePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            var metadata = new BackupMetadata
            {
                CapturedAt = DateTime.UtcNow,
                HostName = Environment.MachineName,
                ToolVersion = "1.0.0",
                SqliteVersion = "3.40.0" // Should query actual version
            };

            // Metadata capture logic would go here
            return metadata;
        }, cancellationToken);
    }
}
