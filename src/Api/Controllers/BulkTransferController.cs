// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.BulkTransfer;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller exposing v2 bulk import and export endpoints for SQLite backup archives.
/// All export endpoints stream the archive body directly to the caller using
/// <see cref="IBulkTransferService.ExportAsync"/> — no intermediate disk storage is required.
/// </summary>
/// <remarks>
/// Route prefix: <c>/api/v2/bulk</c>
/// </remarks>
public sealed class BulkTransferController
{
    private readonly IBulkTransferService _bulkTransferService;
    private readonly ILogger<BulkTransferController> _logger;

    /// <summary>Initialises the controller with its service dependencies.</summary>
    public BulkTransferController(
        IBulkTransferService bulkTransferService,
        ILogger<BulkTransferController> logger)
    {
        _bulkTransferService = bulkTransferService;
        _logger = logger;
    }

    // ── Export endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Streams a ZIP archive containing backup files that match the supplied filter criteria
    /// directly to <paramref name="responseStream"/>.
    /// </summary>
    /// <remarks>
    /// Route: <c>POST /api/v2/bulk/export/stream</c><br/>
    /// The archive is produced via a <see cref="System.IO.Pipelines.Pipe"/> — data flows from
    /// disk to the response body without loading the full archive into heap memory.
    /// </remarks>
    /// <param name="request">Filter and format options.  Pass <c>null</c> to export all backups.</param>
    /// <param name="responseStream">Writable stream representing the HTTP response body.</param>
    /// <param name="ct">Cancellation token supplied by the HTTP framework.</param>
    /// <returns>Number of bytes written, or <c>-1</c> on failure.</returns>
    public async Task<long> StreamExportAsync(
        BulkExportRequest? request,
        Stream responseStream,
        CancellationToken ct = default)
    {
        var exportRequest = request ?? new BulkExportRequest();

        try
        {
            _logger.LogInformation("Inline export started: format={Format}, schedules={ScheduleCount}",
                exportRequest.Format,
                exportRequest.ScheduleIds?.Count ?? 0);

            var bytesWritten = 0L;
            await foreach (var chunk in _bulkTransferService.ExportAsync(exportRequest, cancellationToken: ct))
            {
                await responseStream.WriteAsync(chunk.Data, ct);
                bytesWritten += chunk.Data.Length;
            }

            _logger.LogInformation("Inline export completed: {Bytes:N0} bytes delivered", bytesWritten);
            return bytesWritten;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Inline export cancelled by client");
            return -1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inline export failed");
            return -1;
        }
    }

    /// <summary>
    /// Enqueues a background export job and returns the job handle immediately.
    /// Poll progress via <see cref="GetJobStatusAsync"/> or watch it with
    /// <see cref="WatchJobProgressAsync"/>.
    /// </summary>
    /// <remarks>Route: <c>POST /api/v2/bulk/export/job</c></remarks>
    /// <param name="request">Filter and format options.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ApiResponse<BulkTransferJobDto>> StartExportJobAsync(
        BulkExportRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var job = await _bulkTransferService.StartExportJobAsync(request, ct);
            _logger.LogInformation("Export job enqueued: {JobId}", job.Id);
            return ApiResponse<BulkTransferJobDto>.SuccessResponse(MapJobToDto(job), "Export job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue export job");
            return ApiResponse<BulkTransferJobDto>.ErrorResponse("EXPORT_JOB_FAILED", ex.Message);
        }
    }

    // ── Import endpoints ──────────────────────────────────────────────────────

    /// <summary>
    /// Imports backup files from the ZIP archive provided in <paramref name="sourceStream"/>.
    /// The operation is synchronous from the caller's perspective — it returns only after
    /// all entries have been processed.
    /// </summary>
    /// <remarks>Route: <c>POST /api/v2/bulk/import</c></remarks>
    /// <param name="sourceStream">Readable stream positioned at the start of a ZIP archive.</param>
    /// <param name="request">Import settings; defaults to skip-on-conflict with checksum validation.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ApiResponse<BulkImportResultDto>> ImportAsync(
        Stream sourceStream,
        BulkImportRequest? request = null,
        CancellationToken ct = default)
    {
        var importRequest = request ?? new BulkImportRequest();

        try
        {
            _logger.LogInformation("Bulk import started: dry-run={DryRun}, conflict={Policy}",
                importRequest.DryRun, importRequest.ConflictPolicy);

            var result = await _bulkTransferService.ImportAsync(sourceStream, importRequest, cancellationToken: ct);

            var dto = MapResultToDto(result);
            var message = result.WasDryRun
                ? $"Dry run complete: {result.TotalEntries} entries validated, no records persisted"
                : $"Import complete: {result.ImportedCount} imported, {result.SkippedCount} skipped, {result.FailedCount} failed";

            _logger.LogInformation(message);
            return ApiResponse<BulkImportResultDto>.SuccessResponse(dto, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import failed");
            return ApiResponse<BulkImportResultDto>.ErrorResponse("IMPORT_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Enqueues a background import job from the provided stream and returns the job handle.
    /// The source stream must remain readable for the full lifetime of the background task.
    /// </summary>
    /// <remarks>Route: <c>POST /api/v2/bulk/import/job</c></remarks>
    /// <param name="sourceStream">Readable stream containing the ZIP archive to import.</param>
    /// <param name="request">Import settings.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ApiResponse<BulkTransferJobDto>> StartImportJobAsync(
        Stream sourceStream,
        BulkImportRequest? request = null,
        CancellationToken ct = default)
    {
        var importRequest = request ?? new BulkImportRequest();

        try
        {
            var job = await _bulkTransferService.StartImportJobAsync(sourceStream, importRequest, ct);
            _logger.LogInformation("Import job enqueued: {JobId}", job.Id);
            return ApiResponse<BulkTransferJobDto>.SuccessResponse(MapJobToDto(job), "Import job enqueued");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue import job");
            return ApiResponse<BulkTransferJobDto>.ErrorResponse("IMPORT_JOB_FAILED", ex.Message);
        }
    }

    // ── Job management endpoints ──────────────────────────────────────────────

    /// <summary>
    /// Returns a snapshot of the requested job's current state.
    /// </summary>
    /// <remarks>Route: <c>GET /api/v2/bulk/jobs/{jobId}</c></remarks>
    /// <param name="jobId">The job identifier returned by a start-job endpoint.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ApiResponse<BulkTransferJobDto>> GetJobStatusAsync(Guid jobId, CancellationToken ct = default)
    {
        try
        {
            var job = await _bulkTransferService.GetJobStatusAsync(jobId, ct);
            if (job is null)
                return ApiResponse<BulkTransferJobDto>.ErrorResponse("JOB_NOT_FOUND", $"Job {jobId} not found");

            return ApiResponse<BulkTransferJobDto>.SuccessResponse(MapJobToDto(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve job status for {JobId}", jobId);
            return ApiResponse<BulkTransferJobDto>.ErrorResponse("JOB_STATUS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Returns all transfer jobs currently tracked by the service.
    /// Includes active, queued, and recently completed jobs within the configured retention window.
    /// </summary>
    /// <remarks>Route: <c>GET /api/v2/bulk/jobs</c></remarks>
    /// <param name="ct">Cancellation token.</param>
    public Task<ApiResponse<IReadOnlyList<BulkTransferJobDto>>> ListJobsAsync(CancellationToken ct = default)
    {
        try
        {
            var jobs = _bulkTransferService.GetActiveJobs()
                .Select(MapJobToDto)
                .ToList()
                .AsReadOnly();

            return Task.FromResult(ApiResponse<IReadOnlyList<BulkTransferJobDto>>.SuccessResponse(
                jobs, $"{jobs.Count} job(s) tracked"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list jobs");
            return Task.FromResult(ApiResponse<IReadOnlyList<BulkTransferJobDto>>.ErrorResponse("LIST_JOBS_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Streams live <see cref="TransferProgress"/> snapshots for a running job until it
    /// reaches a terminal state.  Intended for use with Server-Sent Events or long-polling.
    /// </summary>
    /// <remarks>Route: <c>GET /api/v2/bulk/jobs/{jobId}/progress</c></remarks>
    /// <param name="jobId">The job to watch.</param>
    /// <param name="onProgress">Callback invoked for each incoming progress snapshot.</param>
    /// <param name="ct">Cancellation token; used to stop watching before job completion.</param>
    public async Task WatchJobProgressAsync(
        Guid jobId,
        Func<TransferProgress, Task> onProgress,
        CancellationToken ct = default)
    {
        try
        {
            await foreach (var snapshot in _bulkTransferService.WatchJobProgressAsync(jobId, ct))
                await onProgress(snapshot);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Progress watch for job {JobId} cancelled", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error watching progress for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Requests cooperative cancellation of a running transfer job.
    /// </summary>
    /// <remarks>Route: <c>DELETE /api/v2/bulk/jobs/{jobId}</c></remarks>
    /// <param name="jobId">The job to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<ApiResponse> CancelJobAsync(Guid jobId, CancellationToken ct = default)
    {
        try
        {
            var cancelled = await _bulkTransferService.CancelJobAsync(jobId, ct);
            return cancelled
                ? ApiResponse.SuccessResponse($"Cancellation requested for job {jobId}")
                : ApiResponse.ErrorResponse("JOB_NOT_FOUND", $"Job {jobId} not found or already complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
            return ApiResponse.ErrorResponse("CANCEL_FAILED", ex.Message);
        }
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    private static BulkTransferJobDto MapJobToDto(TransferJob job) => new(
        Id: job.Id,
        Direction: job.Direction,
        State: job.State.ToDisplayString(),
        CreatedAt: job.CreatedAt,
        StartedAt: job.StartedAt,
        CompletedAt: job.CompletedAt,
        ElapsedSeconds: job.ElapsedTime?.TotalSeconds,
        PercentComplete: job.LatestProgress?.PercentComplete,
        CurrentItem: job.LatestProgress?.CurrentItemName,
        Throughput: job.LatestProgress?.ThroughputFormatted,
        Eta: job.LatestProgress?.EtaFormatted,
        ErrorMessage: job.ErrorMessage,
        ImportResult: job.ImportResult is not null ? MapResultToDto(job.ImportResult) : null,
        CorrelationId: job.CorrelationId);

    private static BulkImportResultDto MapResultToDto(BulkImportResult r) => new(
        TotalEntries: r.TotalEntries,
        ImportedCount: r.ImportedCount,
        SkippedCount: r.SkippedCount,
        FailedCount: r.FailedCount,
        TotalBytesRead: r.TotalBytesRead,
        DurationMs: (long)r.Duration.TotalMilliseconds,
        WasDryRun: r.WasDryRun,
        ImportedBackupIds: r.ImportedBackupIds,
        Errors: r.Errors.Select(e => new ImportEntryErrorDto(e.EntryName, e.ErrorCode, e.Message)).ToList());
}

// ── Response DTOs ─────────────────────────────────────────────────────────────

/// <summary>
/// Projection of <see cref="TransferJob"/> for API responses.
/// All fields are nullable to handle jobs in different lifecycle stages.
/// </summary>
/// <param name="Id">Unique job identifier.</param>
/// <param name="Direction"><c>"export"</c> or <c>"import"</c>.</param>
/// <param name="State">Human-readable lifecycle state, e.g. <c>"Running"</c>.</param>
/// <param name="CreatedAt">UTC timestamp when the job was registered.</param>
/// <param name="StartedAt">UTC timestamp when processing began; <c>null</c> if still queued.</param>
/// <param name="CompletedAt">UTC timestamp when the job finished; <c>null</c> if still active.</param>
/// <param name="ElapsedSeconds">Total seconds elapsed; <c>null</c> if not yet started.</param>
/// <param name="PercentComplete">Transfer completion 0–100; <c>null</c> if unknown.</param>
/// <param name="CurrentItem">Name of the file currently being transferred.</param>
/// <param name="Throughput">Human-readable throughput string, e.g. <c>"3.2 MB/s"</c>.</param>
/// <param name="Eta">Human-readable ETA string, e.g. <c>"2m 30s"</c>.</param>
/// <param name="ErrorMessage">Failure description; populated only when <paramref name="State"/> is <c>"Failed"</c>.</param>
/// <param name="ImportResult">Final import summary; <c>null</c> for export jobs or incomplete imports.</param>
/// <param name="CorrelationId">Distributed trace correlation identifier.</param>
public record BulkTransferJobDto(
    Guid Id,
    string Direction,
    string State,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? ElapsedSeconds,
    double? PercentComplete,
    string? CurrentItem,
    string? Throughput,
    string? Eta,
    string? ErrorMessage,
    BulkImportResultDto? ImportResult,
    string CorrelationId);

/// <summary>Projection of <see cref="BulkImportResult"/> for API responses.</summary>
/// <param name="TotalEntries">Archive entries examined (excluding manifest).</param>
/// <param name="ImportedCount">Entries successfully imported.</param>
/// <param name="SkippedCount">Entries skipped due to conflict policy.</param>
/// <param name="FailedCount">Entries that failed validation or persistence.</param>
/// <param name="TotalBytesRead">Total bytes consumed from the source stream.</param>
/// <param name="DurationMs">Wall-clock duration in milliseconds.</param>
/// <param name="WasDryRun"><c>true</c> when no records were persisted.</param>
/// <param name="ImportedBackupIds">IDs of <c>BackupResult</c> records created during the import.</param>
/// <param name="Errors">Per-entry error details.</param>
public record BulkImportResultDto(
    int TotalEntries,
    int ImportedCount,
    int SkippedCount,
    int FailedCount,
    long TotalBytesRead,
    long DurationMs,
    bool WasDryRun,
    IReadOnlyList<Guid> ImportedBackupIds,
    IReadOnlyList<ImportEntryErrorDto> Errors);

/// <summary>Per-entry error detail surfaced in the import result.</summary>
/// <param name="EntryName">Archive path of the failed entry.</param>
/// <param name="ErrorCode">Machine-readable error code.</param>
/// <param name="Message">Human-readable failure description.</param>
public record ImportEntryErrorDto(string EntryName, string ErrorCode, string Message);
