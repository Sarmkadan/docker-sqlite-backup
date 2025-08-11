#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.BulkTransfer;

/// <summary>
/// Configuration options for the bulk transfer subsystem.
/// Bind this class from <c>appsettings.json</c> (or environment variables) using
/// the key <see cref="SectionKey"/>.
/// </summary>
/// <example>
/// <code language="json">
/// {
///   "BulkTransfer": {
///     "ChunkSizeBytes": 65536,
///     "MaxConcurrentExports": 4,
///     "ValidateChecksumsOnExport": true,
///     "EmbedManifest": true
///   }
/// }
/// </code>
/// </example>
public sealed class BulkTransferOptions
{
    /// <summary>Configuration section key for use with <c>IConfiguration.GetSection</c>.</summary>
    public const string SectionKey = "BulkTransfer";

    /// <summary>
    /// Size of each <see cref="BulkTransferChunk"/> yielded by <c>ExportAsync</c>, in bytes.
    /// Increasing this value raises throughput at the cost of first-chunk latency and heap pressure.
    /// Default: <c>65 536</c> (64 KiB).
    /// </summary>
    public int ChunkSizeBytes { get; set; } = 65_536;

    /// <summary>
    /// Maximum number of backup files that may have their source streams open simultaneously
    /// during a single export operation.
    /// Default: <c>4</c>.
    /// </summary>
    public int MaxConcurrentExports { get; set; } = 4;

    /// <summary>
    /// Maximum number of archive entries that may be validated and persisted concurrently
    /// during a single import operation.
    /// Keeping this value low prevents excessive SQLite connection contention.
    /// Default: <c>2</c>.
    /// </summary>
    public int MaxConcurrentImports { get; set; } = 2;

    /// <summary>
    /// When <c>true</c>, the SHA-256 checksum stored in each <c>BackupResult</c> record is
    /// recomputed and compared before the file is added to the export archive.
    /// Tampered or corrupted files cause an <see cref="InvalidDataException"/>.
    /// Default: <c>true</c>.
    /// </summary>
    public bool ValidateChecksumsOnExport { get; set; } = true;

    /// <summary>
    /// Duration for which completed or failed jobs remain queryable via
    /// <see cref="IBulkTransferService.GetJobStatusAsync"/>.
    /// Jobs older than this window are evicted from the in-memory registry.
    /// Default: <c>24 hours</c>.
    /// </summary>
    public TimeSpan JobRetentionPeriod { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Minimum interval between consecutive <see cref="IProgress{T}"/> callbacks, in milliseconds.
    /// Reducing this value increases reporting granularity but adds lock contention on the progress reporter.
    /// Default: <c>250</c> ms.
    /// </summary>
    public int ProgressReportIntervalMs { get; set; } = 250;

    /// <summary>
    /// Hard upper limit on the total compressed size of a single export archive, in bytes.
    /// Exports that exceed this limit are aborted with a <see cref="InvalidOperationException"/>.
    /// Default: <c>10 GiB</c>.
    /// </summary>
    public long MaxExportSizeBytes { get; set; } = 10L * 1_024 * 1_024 * 1_024;

    /// <summary>
    /// When <c>true</c>, a <c>manifest.json</c> entry is embedded in each exported archive
    /// containing metadata (checksum, schedule ID, timestamps) for every included backup file.
    /// The manifest is read automatically during import to resolve schedule associations.
    /// Default: <c>true</c>.
    /// </summary>
    public bool EmbedManifest { get; set; } = true;

    /// <summary>
    /// Capacity of the bounded <see cref="System.Threading.Channels.Channel{T}"/> used to
    /// buffer <see cref="TransferProgress"/> snapshots for <see cref="IBulkTransferService.WatchJobProgressAsync"/>.
    /// When the channel is full, the oldest snapshot is dropped rather than blocking the producer.
    /// Default: <c>128</c>.
    /// </summary>
    public int ProgressChannelCapacity { get; set; } = 128;

    /// <summary>
    /// Pipe writer pause threshold in bytes for the internal <see cref="System.IO.Pipelines.Pipe"/>
    /// used during streaming exports.  The ZIP writer pauses when unread data in the pipe exceeds
    /// this value, providing natural backpressure against slow consumers.
    /// Defaults to four times <see cref="ChunkSizeBytes"/>.
    /// </summary>
    public long PipePauseWriterThreshold => ChunkSizeBytes * 4L;

    /// <summary>
    /// Returns the options in a human-readable format for diagnostic logging.
    /// </summary>
    public override string ToString() =>
        $"BulkTransferOptions {{ ChunkSize={ChunkSizeBytes / 1024}KiB, " +
        $"MaxExports={MaxConcurrentExports}, MaxImports={MaxConcurrentImports}, " +
        $"MaxArchive={MaxExportSizeBytes / (1024 * 1024)}MiB, " +
        $"JobRetention={JobRetentionPeriod} }}";
}
