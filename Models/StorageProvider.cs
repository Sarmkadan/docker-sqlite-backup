// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Encapsulates storage provider configuration for backup destinations.
/// </summary>
public class StorageProvider
{
    /// <summary>
    /// Type of storage backend (Local, S3, GCS, Azure).
    /// </summary>
    public StorageType Type { get; set; } = StorageType.Local;

    /// <summary>
    /// Display name for this storage provider.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// For Local storage: the base directory path for backups.
    /// For S3: the bucket name.
    /// For GCS: the bucket name.
    /// </summary>
    public string Location { get; set; } = null!;

    /// <summary>
    /// Optional prefix or folder path within the storage location.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// AWS region for S3 storage (e.g., "us-east-1").
    /// </summary>
    public string? AwsRegion { get; set; }

    /// <summary>
    /// AWS Access Key ID for S3 authentication.
    /// </summary>
    public string? AwsAccessKeyId { get; set; }

    /// <summary>
    /// AWS Secret Access Key for S3 authentication.
    /// </summary>
    public string? AwsSecretAccessKey { get; set; }

    /// <summary>
    /// Whether to use server-side encryption for S3 uploads.
    /// </summary>
    public bool UseEncryption { get; set; } = true;

    /// <summary>
    /// Storage class for S3 (STANDARD, STANDARD_IA, GLACIER, etc.).
    /// </summary>
    public string? StorageClass { get; set; } = "STANDARD";

    /// <summary>
    /// Whether to enable versioning in the storage backend.
    /// </summary>
    public bool EnableVersioning { get; set; } = false;

    /// <summary>
    /// Whether this storage provider is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds for storage operations.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retries for failed storage operations.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Validates the storage provider configuration.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Storage provider name is required.");

        if (string.IsNullOrWhiteSpace(Location))
            errors.Add("Storage location is required.");

        switch (Type)
        {
            case StorageType.Local:
                if (!System.IO.Directory.Exists(Location) && !System.IO.Path.IsPathRooted(Location))
                    errors.Add("Local storage path must be a valid directory or absolute path.");
                break;

            case StorageType.S3:
                if (string.IsNullOrWhiteSpace(AwsRegion))
                    errors.Add("AWS region is required for S3 storage.");
                if (string.IsNullOrWhiteSpace(AwsAccessKeyId))
                    errors.Add("AWS Access Key ID is required for S3 storage.");
                if (string.IsNullOrWhiteSpace(AwsSecretAccessKey))
                    errors.Add("AWS Secret Access Key is required for S3 storage.");
                break;

            case StorageType.GCS:
                // GCS validation would go here
                break;

            case StorageType.AzureBlob:
                // Azure validation would go here
                break;
        }

        if (ConnectionTimeoutSeconds < 5)
            errors.Add("Connection timeout must be at least 5 seconds.");

        if (MaxRetries < 0 || MaxRetries > 10)
            errors.Add("Max retries must be between 0 and 10.");

        return errors;
    }

    /// <summary>
    /// Constructs the full path for a backup file in this storage provider.
    /// </summary>
    public string ConstructBackupPath(string fileName)
    {
        if (Type == StorageType.Local)
        {
            var basePath = Prefix ?? Location;
            return System.IO.Path.Combine(basePath, fileName);
        }

        // For cloud storage
        var prefix = string.IsNullOrWhiteSpace(Prefix) ? "" : $"{Prefix.TrimEnd('/')}/";
        return $"{prefix}{fileName}";
    }
}
