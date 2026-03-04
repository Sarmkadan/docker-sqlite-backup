#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Events;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DockerSqliteBackup.BulkTransfer;

/// <summary>
/// Production implementation of <see cref="IBulkTransferService"/>.
/// Uses <see cref="System.IO.Pipelines"/> to stream ZIP archives without loading the full
/// archive into heap memory, and <see cref="Channel{T}"/> for lock-free progress broadcasting.
/// </summary>
public sealed class BulkTransferService : IBulkTransferService, IDisposable
{
    private readonly IBackupRepository _repository;
    private readonly IBackupService _backupService;
    private readonly IVerificationService _verificationService;
    private readonly IBackupEventPublisher _eventPublisher;
    private readonly BulkTransferOptions _options;
    private readonly ILogger<BulkTransferService> _logger;

    private readonly ConcurrentDictionary<Guid, TransferJob> _jobs = new();
    private readonly ConcurrentDictionary<Guid, Channel<TransferProgress>> _progressChannels = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellations = new();
    private bool _disposed;

    /// <summary>Initialises the service with its dependencies.</summary>
    public BulkTransferService(
        IBackupRepository repository,
        IBackupService backupService,
        IVerificationService verificationService,
        IBackupEventPublisher eventPublisher,
        IOptions<BulkTransferOptions> options,
        ILogger<BulkTransferService> logger)
    {
        _repository = repository;
        _backupService = backupService;
        _verificationService = verificationService;
        _eventPublisher = eventPublisher;
        _options = options.Value;
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async IAsyncEnumerable<BulkTransferChunk> ExportAsync(
        BulkExportRequest request,
        IProgress<TransferProgress>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var backups = await ResolveExportCandidatesAsync(request, cancellationToken);
        var estimatedBytes = backups.Sum(b => b.BackupFileSizeBytes);

        _logger.LogInformation("Export [{JobId}]: {Count} backups, ~{Bytes:N0} bytes", jobId, backups.Count, estimatedBytes);

        await _eventPublisher.PublishAsync(new BulkTransferStartedEvent
        {
            JobId = jobId, Direction = "export", ItemCount = backups.Count, EstimatedBytes = estimatedBytes
        }, cancellationToken);

        var pipeOptions = new PipeOptions(pauseWriterThreshold: _options.PipePauseWriterThreshold);
        var pipe = new Pipe(pipeOptions);
        using var writeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var writeTask = WriteArchiveToPipeAsync(pipe.Writer, backups, request, progress, jobId, estimatedBytes, startedAt, writeCts.Token);

        long sequenceNumber = 0;
        long byteOffset = 0;

        try
        {
            while (true)
            {
                var readResult = await pipe.Reader.ReadAsync(cancellationToken);
                var buffer = readResult.Buffer;

                var pos = buffer.Start;
                while (buffer.TryGet(ref pos, out var segment))
                {
                    for (var offset = 0; offset < segment.Length; offset += _options.ChunkSizeBytes)
                    {
                        var length = Math.Min(_options.ChunkSizeBytes, segment.Length - offset);
                        var chunkData = segment.Slice(offset, length).ToArray();
                        yield return new BulkTransferChunk(sequenceNumber++, chunkData.AsMemory(), false, byteOffset);
                        byteOffset += length;
                    }
                }

                pipe.Reader.AdvanceTo(buffer.End);
                if (readResult.IsCompleted) break;
            }
        }
        finally
        {
            await writeCts.CancelAsync();
            await pipe.Reader.CompleteAsync();
        }

        await writeTask;

        _logger.LogInformation("Export [{JobId}] complete: {Chunks} chunks, {Bytes:N0} bytes", jobId, sequenceNumber, byteOffset);
        await _eventPublisher.PublishAsync(new BulkTransferCompletedEvent
        {
            JobId = jobId, Direction = "export", ItemCount = backups.Count,
            TotalBytes = byteOffset, Duration = DateTime.UtcNow - startedAt
        }, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task<BulkImportResult> ImportAsync(
        Stream source,
        BulkImportRequest request,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var errors = new List<ImportEntryError>();
        var importedIds = new List<Guid>();
        int importedCount = 0, skippedCount = 0, failedCount = 0;
        long totalBytesRead = 0;

        _logger.LogInformation("Import [{JobId}] starting, dry-run={DryRun}", jobId, request.DryRun);
        await _eventPublisher.PublishAsync(new BulkTransferStartedEvent { JobId = jobId, Direction = "import" }, cancellationToken);

        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var entries = archive.Entries
            .Where(e => !e.Name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

        using var semaphore = new SemaphoreSlim(_options.MaxConcurrentImports);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(BuildProgress(jobId, entries.Count, importedCount, skippedCount, failedCount,
                totalBytesRead, 0, entry.Name, startedAt));

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var (outcome, backupId, errorMessage) = await ProcessImportEntryAsync(entry, request, cancellationToken);
                switch (outcome)
                {
                    case ImportOutcome.Imported when backupId.HasValue:
                        importedCount++;
                        importedIds.Add(backupId.Value);
                        break;
                    case ImportOutcome.Imported:
                        importedCount++;
                        break;
                    case ImportOutcome.Skipped:
                        skippedCount++;
                        break;
                    default:
                        failedCount++;
                        errors.Add(new ImportEntryError(entry.Name, "IMPORT_FAILED", errorMessage ?? "Unknown error"));
                        break;
                }
                totalBytesRead += entry.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing entry {Entry}", entry.Name);
                failedCount++;
                errors.Add(new ImportEntryError(entry.Name, "UNHANDLED_EXCEPTION", ex.Message));
            }
            finally
            {
                semaphore.Release();
            }
        }

        var result = new BulkImportResult
        {
            TotalEntries = entries.Count,
            ImportedCount = importedCount,
            SkippedCount = skippedCount,
            FailedCount = failedCount,
            TotalBytesRead = totalBytesRead,
            Duration = DateTime.UtcNow - startedAt,
            WasDryRun = request.DryRun,
            Errors = errors,
            ImportedBackupIds = importedIds
        };

        _logger.LogInformation("Import [{JobId}] done: {I} imported, {S} skipped, {F} failed in {D:N0}ms",
            jobId, importedCount, skippedCount, failedCount, result.Duration.TotalMilliseconds);

        await _eventPublisher.PublishAsync(new BulkTransferCompletedEvent
        {
            JobId = jobId, Direction = "import", ItemCount = importedCount,
            TotalBytes = totalBytesRead, Duration = result.Duration
        }, CancellationToken.None);

        return result;
    }

    /// <inheritdoc/>
    public Task<TransferJob> StartExportJobAsync(BulkExportRequest request, CancellationToken cancellationToken = default)
    {
        var job = CreateAndRegisterJob("export");
        _ = RunJobAsync(job, async cts =>
        {
            await foreach (var _ in ExportAsync(request, CreateJobProgressReporter(job), cts.Token)) { }
            job.ImportResult = null;
        });
        return Task.FromResult(job);
    }

    /// <inheritdoc/>
    public Task<TransferJob> StartImportJobAsync(Stream source, BulkImportRequest request, CancellationToken cancellationToken = default)
    {
        var job = CreateAndRegisterJob("import");
        _ = RunJobAsync(job, async cts =>
        {
            job.ImportResult = await ImportAsync(source, request, CreateJobProgressReporter(job), cts.Token);
        });
        return Task.FromResult(job);
    }

    /// <inheritdoc/>
    public Task<TransferJob?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TransferProgress> WatchJobProgressAsync(
        Guid jobId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_progressChannels.TryGetValue(jobId, out var channel)) yield break;
        await foreach (var snapshot in channel.Reader.ReadAllAsync(cancellationToken))
            yield return snapshot;
    }

    /// <inheritdoc/>
    public IReadOnlyList<TransferJob> GetActiveJobs() => [.. _jobs.Values];

    /// <inheritdoc/>
    public async Task<bool> CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobCancellations.TryGetValue(jobId, out var cts)) return false;
        await cts.CancelAsync();
        return true;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        foreach (var cts in _jobCancellations.Values) cts.Dispose();
        _disposed = true;
    }

    // ── Private: ZIP archive writer ───────────────────────────────────────────

    private async Task WriteArchiveToPipeAsync(
        PipeWriter writer,
        IReadOnlyList<BackupResult> backups,
        BulkExportRequest request,
        IProgress<TransferProgress>? progress,
        Guid jobId,
        long estimatedBytes,
        DateTime startedAt,
        CancellationToken ct)
    {
        try
        {
            var pipeStream = writer.AsStream(leaveOpen: true);
            using (var archive = new ZipArchive(pipeStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                if (_options.EmbedManifest && request.IncludeManifest)
                    await WriteManifestEntryAsync(archive, backups, ct);

                long bytesProcessed = 0;
                var processedCount = 0;

                foreach (var backup in backups)
                {
                    ct.ThrowIfCancellationRequested();

                    if (!File.Exists(backup.BackupFilePath))
                    {
                        _logger.LogWarning("Export [{JobId}]: skipping missing file {Path}", jobId, backup.BackupFilePath);
                        continue;
                    }

                    var entryName = $"{backup.ScheduleId}/{backup.Id}/{Path.GetFileName(backup.BackupFilePath)}";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

                    await using (var entryStream = entry.Open())
                    await using (var fileStream = new FileStream(backup.BackupFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81_920, useAsync: true))
                        await fileStream.CopyToAsync(entryStream, ct);

                    bytesProcessed += backup.BackupFileSizeBytes;
                    processedCount++;
                    progress?.Report(BuildProgress(jobId, backups.Count, processedCount, 0, 0,
                        bytesProcessed, estimatedBytes, entryName, startedAt));
                }
            }

            await pipeStream.FlushAsync(ct);
            await writer.CompleteAsync();
        }
        catch (Exception ex)
        {
            await writer.CompleteAsync(ex);
        }
    }

    private static async Task WriteManifestEntryAsync(ZipArchive archive, IEnumerable<BackupResult> backups, CancellationToken ct)
    {
        var manifest = backups.Select(b => new
        {
            b.Id, b.ScheduleId, b.Checksum, b.BackupFileSizeBytes, b.StartedAt, b.Status,
            FileName = Path.GetFileName(b.BackupFilePath)
        });

        var entry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
        await using var writer = new StreamWriter(entry.Open());
        await writer.WriteAsync(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
    }

    // ── Private: import entry processor ──────────────────────────────────────

    private async Task<(ImportOutcome Outcome, Guid? BackupId, string? ErrorMessage)> ProcessImportEntryAsync(
        ZipArchiveEntry entry,
        BulkImportRequest request,
        CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"bulk_import_{Guid.NewGuid():N}.sqlite");

        try
        {
            await using (var src = entry.Open())
            await using (var dst = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81_920, useAsync: true))
                await src.CopyToAsync(dst, ct);

            var checksum = await _backupService.CalculateBackupChecksumAsync(tempPath);

            if (request.ValidateChecksums)
            {
                var (isValid, _) = await _verificationService.PerformIntegrityCheckAsync(tempPath);
                if (!isValid)
                    return (ImportOutcome.Failed, null, "SQLite integrity check failed");
            }

            if (request.DryRun)
                return (ImportOutcome.Imported, null, null);

            var result = new BackupResult
            {
                ScheduleId = request.TargetScheduleId ?? Guid.Empty,
                BackupFilePath = tempPath,
                BackupFileSizeBytes = entry.Length,
                Checksum = checksum,
                Status = (int)BackupStatus.Success,
                StartedAt = entry.LastWriteTime.UtcDateTime,
                CompletedAt = DateTime.UtcNow,
                IsStoredLocally = true,
                Notes = $"Bulk imported from archive: {entry.FullName}"
            };

            var created = await _repository.CreateBackupResultAsync(result);

            if (request.RunVerification)
                await _verificationService.VerifyBackupAsync(created, ct);

            return (ImportOutcome.Imported, created.Id, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import entry failed: {Name}", entry.Name);
            if (File.Exists(tempPath)) File.Delete(tempPath);
            return (ImportOutcome.Failed, null, ex.Message);
        }
    }

    // ── Private: job infrastructure ───────────────────────────────────────────

    private TransferJob CreateAndRegisterJob(string direction)
    {
        var job = new TransferJob { Id = Guid.NewGuid(), Direction = direction, CreatedAt = DateTime.UtcNow };
        _jobs[job.Id] = job;
        _jobCancellations[job.Id] = new CancellationTokenSource();
        _progressChannels[job.Id] = Channel.CreateBounded<TransferProgress>(
            new BoundedChannelOptions(_options.ProgressChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = true,
                SingleReader = false
            });
        return job;
    }

    private async Task RunJobAsync(TransferJob job, Func<CancellationTokenSource, Task> work)
    {
        var cts = _jobCancellations[job.Id];
        job.State = TransferJobState.Running;
        job.StartedAt = DateTime.UtcNow;

        try
        {
            await work(cts);
            job.State = TransferJobState.Completed;
        }
        catch (OperationCanceledException)
        {
            job.State = TransferJobState.Cancelled;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} ({Direction}) failed", job.Id, job.Direction);
            job.State = TransferJobState.Failed;
            job.ErrorMessage = ex.Message;

            await _eventPublisher.PublishAsync(new BulkTransferFailedEvent
            {
                JobId = job.Id, Direction = job.Direction,
                ErrorMessage = ex.Message, StackTrace = ex.StackTrace
            }, CancellationToken.None);
        }
        finally
        {
            job.CompletedAt = DateTime.UtcNow;
            if (_progressChannels.TryGetValue(job.Id, out var ch)) ch.Writer.TryComplete();
        }
    }

    private IProgress<TransferProgress> CreateJobProgressReporter(TransferJob job) =>
        new Progress<TransferProgress>(snapshot =>
        {
            job.LatestProgress = snapshot;
            if (_progressChannels.TryGetValue(job.Id, out var ch)) ch.Writer.TryWrite(snapshot);
        });

    private async Task<List<BackupResult>> ResolveExportCandidatesAsync(
        BulkExportRequest request,
        CancellationToken ct)
    {
        IEnumerable<Guid> scheduleIds = request.ScheduleIds?.Count > 0
            ? request.ScheduleIds
            : (await _repository.GetAllSchedulesAsync()).Select(s => s.Id);

        var limit = request.MaxItems > 0 ? request.MaxItems : 10_000;
        var all = new List<BackupResult>();

        foreach (var id in scheduleIds)
        {
            ct.ThrowIfCancellationRequested();
            var history = await _repository.GetBackupHistoryAsync(id, limit);
            all.AddRange(history);
        }

        IEnumerable<BackupResult> filtered = all;
        if (request.From.HasValue) filtered = filtered.Where(b => b.StartedAt >= request.From.Value);
        if (request.To.HasValue) filtered = filtered.Where(b => b.StartedAt <= request.To.Value);
        if (request.StatusFilter?.Count > 0) filtered = filtered.Where(b => request.StatusFilter.Contains(b.Status));
        if (request.MaxItems > 0) filtered = filtered.Take(request.MaxItems);

        return [.. filtered.Where(b => !string.IsNullOrWhiteSpace(b.BackupFilePath))];
    }

    private static TransferProgress BuildProgress(
        Guid jobId, int total, int processed, int skipped, int failed,
        long bytesTransferred, long totalBytes, string currentItem, DateTime startedAt)
    {
        var elapsed = (DateTime.UtcNow - startedAt).TotalSeconds;
        var rate = elapsed > 0 ? bytesTransferred / elapsed : 0;
        var eta = rate > 0 && totalBytes > 0 ? (totalBytes - bytesTransferred) / rate : (double?)null;
        return new TransferProgress(jobId, total, processed, skipped, failed,
            bytesTransferred, totalBytes, currentItem, rate, eta, DateTime.UtcNow);
    }

    private enum ImportOutcome { Imported, Skipped, Failed }
}
