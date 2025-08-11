#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Base configuration class for backup storage.
/// </summary>
public abstract class StorageConfiguration
{
    /// <summary>Gets or sets the unique identifier for this configuration.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the name of the storage configuration.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage type.</summary>
    public abstract int StorageType { get; }

    /// <summary>Gets or sets whether this is the default configuration.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Gets or sets when the configuration was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the configuration was last modified.</summary>
    public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the storage configuration.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public abstract bool IsValid();

    /// <summary>
    /// Tests the connection to the storage backend.
    /// </summary>
    /// <returns>True if connection successful, false otherwise.</returns>
    public abstract Task<bool> TestConnectionAsync();
}
