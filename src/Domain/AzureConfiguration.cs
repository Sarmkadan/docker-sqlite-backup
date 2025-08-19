#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Azure.Storage.Blobs;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Configuration for Azure Blob Storage backend.
/// Supports both connection-string and SAS-URI authentication.
/// </summary>
public class AzureConfiguration : StorageConfiguration
{
    /// <summary>Gets the storage type for Azure Blob Storage.</summary>
    public override int StorageType => (int)Constants.StorageType.Azure;

    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// Mutually exclusive with <see cref="SasUri"/>.
    /// Example: <c>DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net</c>
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a SAS URI scoped to the container.
    /// Used when a full connection string is not available (e.g., least-privilege deployments).
    /// Mutually exclusive with <see cref="ConnectionString"/>.
    /// </summary>
    public string? SasUri { get; set; }

    /// <summary>Gets or sets the name of the blob container to store backups in.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the blob name prefix (virtual folder path) for backup objects.</summary>
    public string BlobPrefix { get; set; } = "backups/";

    /// <summary>
    /// Gets or sets the access tier for uploaded blobs.
    /// Accepted values: <c>Hot</c>, <c>Cool</c>, <c>Archive</c>.
    /// Defaults to <c>Cool</c> which is cost-effective for backup scenarios.
    /// </summary>
    public string AccessTier { get; set; } = "Cool";

    /// <summary>
    /// Gets or sets whether blobs should be immutable (soft-delete + versioning) after upload.
    /// When <c>true</c>, the service uses blob versioning to protect against accidental deletion.
    /// </summary>
    public bool EnableImmutability { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of days the soft-delete retention policy should keep deleted blobs.
    /// 0 disables soft-delete metadata. Requires the storage account to have soft-delete enabled.
    /// </summary>
    public int SoftDeleteRetentionDays { get; set; } = 0;

    /// <inheritdoc />
    public override bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (string.IsNullOrWhiteSpace(ContainerName))
            return false;

        var hasConnectionString = !string.IsNullOrWhiteSpace(ConnectionString);
        var hasSasUri = !string.IsNullOrWhiteSpace(SasUri);

        if (!hasConnectionString && !hasSasUri)
            return false;

        if (!IsValidAccessTier(AccessTier))
            return false;

        return true;
    }

    /// <inheritdoc />
    public override async Task<bool> TestConnectionAsync()
    {
        try
        {
            var containerClient = CreateContainerClient();
            return await containerClient.ExistsAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates and returns a <see cref="BlobContainerClient"/> for this configuration.
    /// </summary>
    internal BlobContainerClient CreateContainerClient()
    {
        if (!string.IsNullOrWhiteSpace(ConnectionString))
            return new BlobContainerClient(ConnectionString, ContainerName);

        if (!string.IsNullOrWhiteSpace(SasUri))
            return new BlobContainerClient(new Uri(SasUri));

        throw new InvalidOperationException(
            "Azure storage requires either ConnectionString or SasUri to be configured.");
    }

    private static bool IsValidAccessTier(string tier) =>
        tier is "Hot" or "Cool" or "Archive";
}
