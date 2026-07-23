#nullable enable
// Author: Vladyslav Zaiets

using Amazon.S3;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Configuration for AWS S3 storage backend.
/// </summary>
public class S3Configuration : StorageConfiguration
{
    /// <summary>Gets the storage type for S3.</summary>
    public override int StorageType => (int)Constants.StorageType.S3;

    /// <summary>Gets or sets the AWS access key ID.</summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>Gets or sets the AWS secret access key.</summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the S3 bucket name.</summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>Gets or sets the AWS region name.</summary>
    public string RegionName { get; set; } = "us-east-1";

    /// <summary>Gets or sets the prefix for backup objects in S3.</summary>
    public string ObjectKeyPrefix { get; set; } = "backups/";

    /// <summary>Gets or sets whether to use SSL/TLS for the connection.</summary>
    public bool UseSSL { get; set; } = true;

    /// <summary>Gets or sets whether server-side encryption should be used.</summary>
    public bool EnableServerSideEncryption { get; set; } = true;

    /// <summary>Gets or sets the storage class for S3 objects (e.g., STANDARD, GLACIER).</summary>
    public string StorageClass { get; set; } = "STANDARD";

    /// <summary>Gets or sets the custom endpoint URL if using S3-compatible service.</summary>
    public string? CustomEndpoint { get; set; }

    /// <summary>Gets or sets the number of days before transitioning to Glacier.</summary>
    public int? TransitionToGlacierDays { get; set; }

	/// <summary>
	/// Enables streaming uploads for this S3 configuration using multipart upload.
	/// When true, large backups are uploaded in chunks to avoid loading the entire file into memory.
	/// </summary>
	public bool EnableStreamingUploads { get; set; } = true;

	/// <summary>
	/// The maximum size of each multipart upload part in bytes for this S3 configuration.
	/// Default is 16MB (16 * 1024 * 1024), which is the recommended minimum for S3.
	/// </summary>
	public int MultipartPartSizeBytes { get; set; } = 16 * 1024 * 1024; // 16MB

    /// <summary>
    /// Validates the S3 configuration.
    /// </summary>
    public override bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (string.IsNullOrWhiteSpace(AccessKeyId) || AccessKeyId.Length < 16)
            return false;

        if (string.IsNullOrWhiteSpace(SecretAccessKey) || SecretAccessKey.Length < 16)
            return false;

        if (string.IsNullOrWhiteSpace(BucketName))
            return false;

        if (string.IsNullOrWhiteSpace(RegionName))
            return false;

        if (!IsValidStorageClass(StorageClass))
            return false;

        return true;
    }

    /// <summary>
    /// Tests the S3 connection by listing buckets.
    /// </summary>
    public override async Task<bool> TestConnectionAsync()
    {
        try
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(RegionName),
                UseAccelerateEndpoint = false
            };

            if (!string.IsNullOrEmpty(CustomEndpoint))
            {
                config.ServiceURL = CustomEndpoint;
            }

            using var client = new AmazonS3Client(AccessKeyId, SecretAccessKey, config);
            var response = await client.ListBucketsAsync();
            return response?.Buckets?.Any(b => b.BucketName == BucketName) ?? false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if the storage class is one of the supported S3 classes.
    /// </summary>
    /// <param name="storageClass">The storage class to validate.</param>
    /// <returns>True if the storage class is valid; otherwise, false.</returns>
    public static bool IsValidStorageClass(string storageClass)
    {
        var validClasses = new[] { "STANDARD", "REDUCED_REDUNDANCY", "STANDARD_IA", "ONEZONE_IA", "INTELLIGENT_TIERING", "GLACIER", "DEEP_ARCHIVE" };
        return validClasses.Contains(storageClass, StringComparer.OrdinalIgnoreCase);
    }
}
