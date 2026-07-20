#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Represents a manifest file that describes a backup, including metadata about the source database,
/// backup configuration, and file information for verification and tracking purposes.
/// </summary>
public class BackupManifest
{
	/// <summary>
	/// Gets or sets the manifest file format version.
	/// </summary>
	public string Version { get; set; } = "1.0";

	/// <summary>
	/// Gets or sets the unique identifier for this backup manifest.
	/// </summary>
	public Guid Id { get; set; } = Guid.NewGuid();

	/// <summary>
	/// Gets or sets the identifier of the associated backup schedule.
	/// </summary>
	public Guid ScheduleId { get; set; }

	/// <summary>
	/// Gets or sets the identifier of the associated backup job.
	/// </summary>
	public Guid BackupJobId { get; set; }

	/// <summary>
	/// Gets or sets the timestamp when the backup was created (UTC).
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the timestamp when the backup was completed (UTC).
	/// </summary>
	public DateTime CompletedAt { get; set; }

	/// <summary>
	/// Gets or sets the source database file path that was backed up.
	/// </summary>
	public string SourceDatabasePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the database file size in bytes at the time of backup.
	/// </summary>
	public long SourceDatabaseSizeBytes { get; set; }

	/// <summary>
	/// Gets or sets the backup file path where the backup is stored.
	/// </summary>
	public string BackupFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the backup file size in bytes.
	/// </summary>
	public long BackupFileSizeBytes { get; set; }

	/// <summary>
	/// Gets or sets the original backup file size in bytes before compression (if compressed).
	/// </summary>
	public long? OriginalFileSizeBytes { get; set; }

	/// <summary>
	/// Gets or sets the compression ratio (original size / compressed size).
	/// </summary>
	public double? CompressionRatio { get; set; }

	/// <summary>
	/// Gets or sets the SHA256 checksum of the backup file.
	/// </summary>
	public string Checksum { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets whether the backup file is encrypted.
	/// </summary>
	public bool IsEncrypted { get; set; }

	/// <summary>
	/// Gets or sets whether the backup file is compressed.
	/// </summary>
	public bool IsCompressed { get; set; }

	/// <summary>
	/// Gets or sets the backup mode used (Full or Incremental).
	/// </summary>
	public string BackupMode { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the base backup ID if this is an incremental backup.
	/// </summary>
	public Guid? BaseBackupResultId { get; set; }

	/// <summary>
	/// Gets or sets the storage configuration type where the backup is stored.
	/// </summary>
	public string StorageType { get; set; } = "Local";

	/// <summary>
	/// Gets or sets the remote storage key/path if stored remotely (e.g., S3 object key).
	/// </summary>
	public string? RemoteStorageKey { get; set; }

	/// <summary>
	/// Gets or sets additional notes about the backup.
	/// </summary>
	public string Notes { get; set; } = string.Empty;
}
