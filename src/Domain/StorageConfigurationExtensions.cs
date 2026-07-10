#nullable enable

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="StorageConfiguration"/> providing common operations and validations.
/// </summary>
public static class StorageConfigurationExtensions
{
    /// <summary>
    /// Creates a deep copy of the storage configuration.
    /// </summary>
    /// <param name="configuration">The configuration to copy.</param>
    /// <returns>A new instance with the same property values.</returns>
    public static StorageConfiguration DeepCopy(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        StorageConfiguration copy;
        switch (configuration)
        {
            case LocalStorageConfiguration local:
                copy = new LocalStorageConfiguration
                {
                    Id = local.Id,
                    Name = local.Name,
                    IsDefault = local.IsDefault,
                    CreatedAt = local.CreatedAt,
                    LastModifiedAt = local.LastModifiedAt,
                    BaseDirectory = local.BaseDirectory,
                    CreateSubdirectoriesBySchedule = local.CreateSubdirectoriesBySchedule,
                    FilePermissions = local.FilePermissions,
                    CompressBackups = local.CompressBackups,
                    MinimumFreeSpaceBytes = local.MinimumFreeSpaceBytes,
                    PreserveFileTimestamp = local.PreserveFileTimestamp
                };
                break;
            case S3Configuration s3:
                copy = new S3Configuration
                {
                    Id = s3.Id,
                    Name = s3.Name,
                    IsDefault = s3.IsDefault,
                    CreatedAt = s3.CreatedAt,
                    LastModifiedAt = s3.LastModifiedAt,
                    AccessKeyId = s3.AccessKeyId,
                    SecretAccessKey = s3.SecretAccessKey,
                    BucketName = s3.BucketName,
                    RegionName = s3.RegionName,
                    ObjectKeyPrefix = s3.ObjectKeyPrefix,
                    UseSSL = s3.UseSSL,
                    EnableServerSideEncryption = s3.EnableServerSideEncryption,
                    StorageClass = s3.StorageClass,
                    CustomEndpoint = s3.CustomEndpoint,
                    TransitionToGlacierDays = s3.TransitionToGlacierDays
                };
                break;
            case AzureConfiguration azure:
                copy = new AzureConfiguration
                {
                    Id = azure.Id,
                    Name = azure.Name,
                    IsDefault = azure.IsDefault,
                    CreatedAt = azure.CreatedAt,
                    LastModifiedAt = azure.LastModifiedAt,
                    ConnectionString = azure.ConnectionString,
                    SasUri = azure.SasUri,
                    ContainerName = azure.ContainerName,
                    BlobPrefix = azure.BlobPrefix,
                    AccessTier = azure.AccessTier,
                    EnableImmutability = azure.EnableImmutability,
                    SoftDeleteRetentionDays = azure.SoftDeleteRetentionDays
                };
                break;
            default:
                throw new InvalidOperationException("Unknown StorageConfiguration type");
        }

        return copy;
    }

    /// <summary>
    /// Updates the LastModifiedAt timestamp to the current UTC time.
    /// </summary>
    /// <param name="configuration">The configuration to update.</param>
    /// <returns>The same configuration instance for method chaining.</returns>
    public static StorageConfiguration Touch(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        configuration.LastModifiedAt = DateTime.UtcNow;
        return configuration;
    }

    /// <summary>
    /// Determines whether the storage configuration is a cloud-based storage (S3, Azure).
    /// </summary>
    /// <param name="configuration">The configuration to check.</param>
    /// <returns>True if cloud storage, false otherwise.</returns>
    public static bool IsCloudStorage(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration switch
        {
            S3Configuration => true,
            AzureConfiguration => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines whether the storage configuration is a local storage.
    /// </summary>
    /// <param name="configuration">The configuration to check.</param>
    /// <returns>True if local storage, false otherwise.</returns>
    public static bool IsLocalStorage(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration is LocalStorageConfiguration;
    }

    /// <summary>
    /// Gets a display name for the storage configuration based on its type.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>A user-friendly display name.</returns>
    public static string GetDisplayName(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return configuration switch
        {
            LocalStorageConfiguration local => $"Local Storage: {local.Name}",
            S3Configuration s3 => $"S3: {s3.Name}",
            AzureConfiguration azure => $"Azure: {azure.Name}",
            _ => configuration.Name
        };
    }

    /// <summary>
    /// Validates that the configuration name is not null or whitespace.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <returns>True if name is valid, false otherwise.</returns>
    public static bool ValidateName(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return !string.IsNullOrWhiteSpace(configuration.Name);
    }

    /// <summary>
    /// Gets the age of the configuration in days.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The age in days, or 0 if CreatedAt is in the future.</returns>
    public static int GetAgeInDays(this StorageConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var now = DateTime.UtcNow;
        var createdAt = configuration.CreatedAt > now ? now : configuration.CreatedAt;
        return (int)(now - createdAt).TotalDays;
    }
}
