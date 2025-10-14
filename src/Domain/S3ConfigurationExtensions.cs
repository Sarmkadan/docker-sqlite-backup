using System;
using System.Globalization;

namespace DockerSqliteBackup.Domain;

public static class S3ConfigurationExtensions
{
    /// <summary>
    /// Creates a well-formed S3 object key by combining the <see cref="S3Configuration.ObjectKeyPrefix"/> 
    /// and the provided <paramref name="fileName"/>.
    /// </summary>
    /// <param name="configuration">The S3 configuration.</param>
    /// <param name="fileName">The name of the file to be stored.</param>
    /// <returns>A well-formed S3 object key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> or <paramref name="fileName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is empty.</exception>
    public static string GetS3ObjectKey(this S3Configuration configuration, string fileName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var objectKey = configuration.ObjectKeyPrefix?.TrimEnd('/') ?? string.Empty;
        if (!string.IsNullOrEmpty(objectKey))
        {
            objectKey += '/';
        }

        objectKey += fileName.TrimStart('/');

        return objectKey;
    }

    /// <summary>
    /// Determines whether the <see cref="S3Configuration.StorageClass"/> is a valid S3 storage class.
    /// </summary>
    /// <param name="configuration">The S3 configuration.</param>
    /// <returns>True if the storage class is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
    public static bool IsValidStorageClass(this S3Configuration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var validStorageClasses = new[] { "STANDARD", "STANDARD_IA", "ONEZONE_IA", "GLACIER", "DEEP_ARCHIVE" };
        return Array.Exists(validStorageClasses, x => string.Equals(x, configuration.StorageClass, StringComparison.OrdinalIgnoreCase));
    }
}
