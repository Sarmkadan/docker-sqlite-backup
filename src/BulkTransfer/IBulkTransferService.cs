#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.BulkTransfer;

/// <summary>
/// Service interface for async bulk import and export of SQLite backup archives
/// with streaming delivery and real-time progress reporting.
/// </summary>
/// <remarks>
/// The service supports two interaction modes:
/// <list type="bullet">
///   <item><description>
///     <b>Inline streaming</b> – <see cref="ExportAsync"/> / <see cref="ImportAsync"/> run on the
///     caller's logical thread and stream data directly via <see cref="IAsyncEnumerable{T}"/> or
///     <see cref="IProgress{T}"/>.
///   </description></item>
///   <item><description>
///     <b>Background jobs</b> – <see cref="StartExportJobAsync"/> / <see cref="StartImportJobAsync"/>
///     launch a background task and return a <see cref="TransferJob"/> handle.  Progress is polled
///     via <see cref="GetJobStatusAsync"/> or streamed via <see cref="WatchJobProgressAsync"/>.
///   </description></item>
/// </list>
/// </remarks>
public interface IBulkTransferService
{
    /// <summary>
    /// Exports backup files matching the given criteria as an asynchronous stream of binary chunks
    /// forming a ZIP archive.  Chunks are produced as files are read, keeping memory usage bounded
    /// regardless of total archive size.
    /// </summary>
    /// <param name="request">Filter and format options for the export operation.</param>
    /// <param name="progress">
    /// Optional callback invoked after each backup file is added to the archive.
    /// Snapshots are throttled to <see cref="BulkTransferOptions.ProgressReportIntervalMs"/>.
    /// </param>
    /// <param name="cancellationToken">Cancels both reading and the background ZIP writer.</param>
    /// <returns>
    /// An async sequence of <see cref="BulkTransferChunk"/> values that together form the
    /// complete archive byte stream.
    /// </returns>
    IAsyncEnumerable<BulkTransferChunk> ExportAsync(
        BulkExportRequest request,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports backup files from a ZIP archive stream, validating checksums and registering
    /// each entry with the backup system.
    /// </summary>
    /// <param name="source">
    /// Readable stream positioned at the start of a ZIP archive previously created by
    /// <see cref="ExportAsync"/>.
    /// </param>
    /// <param name="request">Import settings controlling validation and conflict resolution.</param>
    /// <param name="progress">Optional callback invoked after each entry is processed.</param>
    /// <param name="cancellationToken">Cancels the import mid-flight.</param>
    /// <returns>
    /// A <see cref="BulkImportResult"/> summary with counts of imported, skipped, and
    /// failed entries, plus per-entry error details.
    /// </returns>
    Task<BulkImportResult> ImportAsync(
        Stream source,
        BulkImportRequest request,
        IProgress<TransferProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a background export job and returns a <see cref="TransferJob"/> handle immediately.
    /// The export runs concurrently; use <see cref="WatchJobProgressAsync"/> or
    /// <see cref="GetJobStatusAsync"/> to observe progress.
    /// </summary>
    /// <param name="request">Filter and format options for the export.</param>
    /// <param name="cancellationToken">Cancels job registration (does not cancel the job itself).</param>
    Task<TransferJob> StartExportJobAsync(
        BulkExportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a background import job and returns a <see cref="TransferJob"/> handle immediately.
    /// The source stream must remain readable for the lifetime of the background task.
    /// </summary>
    /// <param name="source">Readable stream containing a ZIP archive to import.</param>
    /// <param name="request">Import settings controlling validation and conflict resolution.</param>
    /// <param name="cancellationToken">Cancels job registration (does not cancel the job itself).</param>
    Task<TransferJob> StartImportJobAsync(
        Stream source,
        BulkImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot of the job's current state, or <c>null</c> if the job is unknown
    /// or has been evicted from the retention window.
    /// </summary>
    Task<TransferJob?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams live <see cref="TransferProgress"/> snapshots for the specified job until it
    /// reaches a terminal state (<see cref="TransferJobState.Completed"/>,
    /// <see cref="TransferJobState.Failed"/>, or <see cref="TransferJobState.Cancelled"/>).
    /// Completes normally when the job finishes; throws <see cref="OperationCanceledException"/>
    /// if <paramref name="cancellationToken"/> is triggered first.
    /// </summary>
    IAsyncEnumerable<TransferProgress> WatchJobProgressAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an immutable snapshot of all currently tracked transfer jobs, including
    /// recently completed ones within the configured retention window.
    /// </summary>
    IReadOnlyList<TransferJob> GetActiveJobs();

    /// <summary>
    /// Requests cooperative cancellation of a running transfer job.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the job was found and cancellation was signalled;
    /// <c>false</c> if the job is unknown or already in a terminal state.
    /// </returns>
    Task<bool> CancelJobAsync(Guid jobId, CancellationToken cancellationToken = default);
}
