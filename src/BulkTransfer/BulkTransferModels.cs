#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Events;

namespace DockerSqliteBackup.BulkTransfer;

// ── Enumerations ──────────────────────────────────────────────────────────────

/// <summary>Output archive format used during bulk export operations.</summary>
public enum BulkExportFormat
{
    /// <summary>
    /// All backup files are bundled into a single ZIP archive.
    /// Each entry follows the path pattern <c>{scheduleId}/{backupId}/{filename}</c>.
    /// A <c>manifest.json</c> is embedded when <see cref="BulkExportRequest.IncludeManifest"/> is set.
    /// </summary>
    Zip,

    /// <summary>
    /// Entries are compressed individually with GZip then packed into a POSIX tar archive.
    /// Preserves Unix file attributes and supports files larger than 4 GiB.
    /// </summary>
    TarGzip,

    /// <summary>
    /// Backup files are written raw (no container), separated by length-prefixed framing.
    /// Intended for direct pipe consumption, not human-readable tools.
    /// </summary>
    RawStream
}

/// <summary>Determines how a bulk import handles an entry that conflicts with an existing record.</summary>
public enum ImportConflictPolicy
{
    /// <summary>Leave the existing record untouched and skip the incoming entry.</summary>
    Skip,

    /// <summary>Replace the existing record and file with the imported data.</summary>
    Overwrite,

    /// <summary>Abort the entire import operation immediately on first conflict.</summary>
    Fail
}

/// <summary>Lifecycle state of a background transfer job.</summary>
public enum TransferJobState
{
    /// <summary>Job has been registered but the transfer has not yet started.</summary>
    Queued,

    /// <summary>The transfer is actively reading or writing data.</summary>
    Running,

    /// <summary>The transfer completed without error.</summary>
    Completed,

    /// <summary>The transfer stopped due to an unhandled exception.</summary>
    Failed,

    /// <summary>The transfer was stopped by an explicit cancellation request.</summary>
    Cancelled
}

// ── Request records ───────────────────────────────────────────────────────────

/// <summary>
/// Parameters controlling which backups are included in a bulk export and how the archive
/// is formatted.
/// </summary>
/// <param name="ScheduleIds">
/// Restrict the export to backups belonging to these schedules.
/// <c>null</c> or empty means all schedules are included.
/// </param>
/// <param name="From">
/// Include only backups whose <c>StartedAt</c> timestamp is on or after this UTC value.
/// </param>
/// <param name="To">
/// Include only backups whose <c>StartedAt</c> timestamp is on or before this UTC value.
/// </param>
/// <param name="StatusFilter">
/// Include only backups with these raw status codes.  Pass an empty list to include all statuses.
/// </param>
/// <param name="Format">The output archive format.  Defaults to <see cref="BulkExportFormat.Zip"/>.</param>
/// <param name="IncludeManifest">
/// Embed a <c>manifest.json</c> entry describing all included backups.  Defaults to <c>true</c>.
/// </param>
/// <param name="MaxItems">
/// Cap the number of backup files exported.  <c>0</c> means no cap is applied.
/// </param>
public record BulkExportRequest(
    IReadOnlyList<Guid>? ScheduleIds = null,
    DateTime? From = null,
    DateTime? To = null,
    IReadOnlyList<int>? StatusFilter = null,
    BulkExportFormat Format = BulkExportFormat.Zip,
    bool IncludeManifest = true,
    int MaxItems = 0);

/// <summary>
/// Parameters controlling how a stream of backup files is imported into the system.
/// </summary>
/// <param name="TargetScheduleId">
/// Associate all imported backups with this schedule.
/// When <c>null</c> the schedule ID is read from the embedded manifest; entries with no
/// resolvable schedule ID are imported with <see cref="Guid.Empty"/>.
/// </param>
/// <param name="ConflictPolicy">
/// How to react when an imported backup ID already exists.
/// Defaults to <see cref="ImportConflictPolicy.Skip"/>.
/// </param>
/// <param name="ValidateChecksums">
/// Compute and verify the SHA-256 checksum of each imported file before persisting.
/// </param>
/// <param name="RunVerification">
/// Run a full SQLite <c>PRAGMA integrity_check</c> on each imported database after writing.
/// </param>
/// <param name="DryRun">
/// Parse and validate all entries without persisting any records or files.
/// </param>
public record BulkImportRequest(
    Guid? TargetScheduleId = null,
    ImportConflictPolicy ConflictPolicy = ImportConflictPolicy.Skip,
    bool ValidateChecksums = true,
    bool RunVerification = false,
    bool DryRun = false);

// ── Progress & streaming records ──────────────────────────────────────────────

/// <summary>
/// Immutable point-in-time snapshot of a bulk transfer operation's progress.
/// Reported at most once per <see cref="BulkTransferOptions.ProgressReportIntervalMs"/> milliseconds.
/// </summary>
/// <param name="JobId">Owning job identifier, or <see cref="Guid.Empty"/> for inline operations.</param>
/// <param name="TotalItems">Total items expected.  Zero if not yet resolved.</param>
/// <param name="ProcessedItems">Items fully transferred so far.</param>
/// <param name="SkippedItems">Items skipped due to conflict policy or missing files.</param>
/// <param name="FailedItems">Items that encountered errors during transfer.</param>
/// <param name="BytesTransferred">Cumulative bytes moved so far.</param>
/// <param name="TotalBytes">Estimated total bytes.  Zero if unknown.</param>
/// <param name="CurrentItemName">Name of the entry currently being transferred.</param>
/// <param name="TransferRateBytesPerSecond">Instantaneous byte throughput.</param>
/// <param name="EstimatedSecondsRemaining">ETA in seconds based on current rate; <c>null</c> if unknown.</param>
/// <param name="RecordedAt">UTC timestamp of this snapshot.</param>
public record TransferProgress(
    Guid JobId,
    int TotalItems,
    int ProcessedItems,
    int SkippedItems,
    int FailedItems,
    long BytesTransferred,
    long TotalBytes,
    string CurrentItemName,
    double TransferRateBytesPerSecond,
    double? EstimatedSecondsRemaining,
    DateTime RecordedAt)
{
    /// <summary>Transfer completion percentage in the range 0–100, computed from bytes when available.</summary>
    public double PercentComplete => TotalBytes > 0
        ? Math.Clamp(BytesTransferred * 100.0 / TotalBytes, 0, 100)
        : TotalItems > 0
            ? Math.Clamp(ProcessedItems * 100.0 / TotalItems, 0, 100)
            : 0;

    /// <summary>Human-readable ETA string, e.g. <c>"2m 30s"</c> or <c>"unknown"</c>.</summary>
    public string EtaFormatted => EstimatedSecondsRemaining switch
    {
        null             => "unknown",
        < 60             => $"{EstimatedSecondsRemaining:0}s",
        < 3_600          => $"{EstimatedSecondsRemaining / 60:0}m {EstimatedSecondsRemaining % 60:0}s",
        _                => $"{EstimatedSecondsRemaining / 3_600:0}h {EstimatedSecondsRemaining % 3_600 / 60:0}m"
    };

    /// <summary>Human-readable throughput string, e.g. <c>"3.2 MB/s"</c>.</summary>
    public string ThroughputFormatted => TransferRateBytesPerSecond switch
    {
        >= 1_073_741_824 => $"{TransferRateBytesPerSecond / 1_073_741_824:F1} GB/s",
        >= 1_048_576     => $"{TransferRateBytesPerSecond / 1_048_576:F1} MB/s",
        >= 1_024         => $"{TransferRateBytesPerSecond / 1_024:F0} KB/s",
        _                => $"{TransferRateBytesPerSecond:F0} B/s"
    };
}

/// <summary>
/// A single binary chunk yielded during a streaming export.  Concatenating all chunks in
/// <see cref="BulkTransferChunk.SequenceNumber"/> order reconstructs the complete archive.
/// </summary>
/// <param name="SequenceNumber">Zero-based position index within the stream.</param>
/// <param name="Data">Raw bytes for this chunk; valid for the lifetime of the current iteration step.</param>
/// <param name="IsLast">
/// <c>true</c> for the final chunk in the sequence; consumers may use this to finalize writes
/// without awaiting stream completion.
/// </param>
/// <param name="ByteOffset">Absolute byte offset within the full archive stream.</param>
public record BulkTransferChunk(
    long SequenceNumber,
    ReadOnlyMemory<byte> Data,
    bool IsLast,
    long ByteOffset);

// ── Result records ────────────────────────────────────────────────────────────

/// <summary>Summary returned at the end of a completed bulk import operation.</summary>
public record BulkImportResult
{
    /// <summary>Total number of archive entries examined (excluding the manifest).</summary>
    public required int TotalEntries { get; init; }

    /// <summary>Entries that were successfully written and registered.</summary>
    public required int ImportedCount { get; init; }

    /// <summary>Entries skipped by the active <see cref="ImportConflictPolicy"/>.</summary>
    public required int SkippedCount { get; init; }

    /// <summary>Entries that failed checksum validation, integrity checks, or persistence.</summary>
    public required int FailedCount { get; init; }

    /// <summary>Total bytes read from the source archive stream.</summary>
    public required long TotalBytesRead { get; init; }

    /// <summary>Wall-clock duration of the full import operation.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary><c>true</c> when the operation ran in dry-run mode and no records were persisted.</summary>
    public required bool WasDryRun { get; init; }

    /// <summary>Per-entry error details for every failed import entry.</summary>
    public IReadOnlyList<ImportEntryError> Errors { get; init; } = [];

    /// <summary>Database IDs of <c>BackupResult</c> records created during this import.</summary>
    public IReadOnlyList<Guid> ImportedBackupIds { get; init; } = [];
}

/// <summary>Error detail captured for a single archive entry that failed to import.</summary>
/// <param name="EntryName">The archive path of the entry that failed.</param>
/// <param name="ErrorCode">Machine-readable error code, e.g. <c>"CHECKSUM_MISMATCH"</c>.</param>
/// <param name="Message">Human-readable description of the failure.</param>
public record ImportEntryError(string EntryName, string ErrorCode, string Message);

// ── Job model ─────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a long-running background transfer job tracked by
/// <see cref="IBulkTransferService"/>.  Properties are mutated only by the service
/// implementation; external code should treat instances as read-only snapshots.
/// </summary>
public sealed class TransferJob
{
    /// <summary>Unique identifier for this job.</summary>
    public required Guid Id { get; init; }

    /// <summary><c>"export"</c> or <c>"import"</c>.</summary>
    public required string Direction { get; init; }

    /// <summary>UTC timestamp when the job was registered.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Current lifecycle state of the job.</summary>
    public TransferJobState State { get; set; } = TransferJobState.Queued;

    /// <summary>UTC timestamp when the transfer began processing the first item.</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>UTC timestamp when the job reached a terminal state.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Most recent progress snapshot; <c>null</c> if the job has not yet started.</summary>
    public TransferProgress? LatestProgress { get; set; }

    /// <summary>Final result for a completed import job; always <c>null</c> for export jobs.</summary>
    public BulkImportResult? ImportResult { get; set; }

    /// <summary>Error message populated when <see cref="State"/> is <see cref="TransferJobState.Failed"/>.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Correlation ID for linking distributed trace spans to this job.</summary>
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Wall-clock elapsed time since the job started, or total duration if finished.</summary>
    public TimeSpan? ElapsedTime => StartedAt.HasValue
        ? (CompletedAt ?? DateTime.UtcNow) - StartedAt
        : null;
}

// ── Domain events ─────────────────────────────────────────────────────────────

/// <summary>
/// Event published at the start of a bulk transfer operation (import or export).
/// </summary>
public sealed class BulkTransferStartedEvent : BackupEvent
{
    /// <summary>The job identifier for background operations; <see cref="Guid.Empty"/> for inline streaming.</summary>
    public Guid JobId { get; set; }

    /// <summary><c>"export"</c> or <c>"import"</c>.</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Number of items included in this transfer.</summary>
    public int ItemCount { get; set; }

    /// <summary>Estimated total bytes to transfer; zero if not yet determined.</summary>
    public long EstimatedBytes { get; set; }

    /// <summary>Initialises with the canonical event type key.</summary>
    public BulkTransferStartedEvent() : base("bulk_transfer.started") { }
}

/// <summary>
/// Event published when a bulk transfer operation completes successfully.
/// </summary>
public sealed class BulkTransferCompletedEvent : BackupEvent
{
    /// <summary>The job identifier for background operations; <see cref="Guid.Empty"/> for inline streaming.</summary>
    public Guid JobId { get; set; }

    /// <summary><c>"export"</c> or <c>"import"</c>.</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Total number of items processed.</summary>
    public int ItemCount { get; set; }

    /// <summary>Actual bytes transferred (post-compression for exports).</summary>
    public long TotalBytes { get; set; }

    /// <summary>Wall-clock duration of the entire operation.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>Initialises with the canonical event type key.</summary>
    public BulkTransferCompletedEvent() : base("bulk_transfer.completed") { }
}

/// <summary>
/// Event published when a bulk transfer operation terminates with an error.
/// </summary>
public sealed class BulkTransferFailedEvent : BackupEvent
{
    /// <summary>The job identifier; <see cref="Guid.Empty"/> for inline operations.</summary>
    public Guid JobId { get; set; }

    /// <summary><c>"export"</c> or <c>"import"</c>.</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>Human-readable description of the failure.</summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Stack trace of the originating exception, if available.</summary>
    public string? StackTrace { get; set; }

    /// <summary>Initialises with the canonical event type key.</summary>
    public BulkTransferFailedEvent() : base("bulk_transfer.failed") { }
}
