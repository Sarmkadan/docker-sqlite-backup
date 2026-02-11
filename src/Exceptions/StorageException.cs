#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for storage-related errors.
/// </summary>
public class StorageException : Exception
{
    /// <summary>Gets the storage type that failed.</summary>
    public string? StorageType { get; }

    /// <summary>
    /// Initializes a new instance of the StorageException class.
    /// </summary>
    public StorageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with storage type information.
    /// </summary>
    public StorageException(string message, string storageType) : base(message)
    {
        StorageType = storageType;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public StorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when S3 operation fails.
/// </summary>
public class S3StorageException : StorageException
{
    public S3StorageException(string message) : base(message, "S3") { }
    public S3StorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when local storage operation fails.
/// </summary>
public class LocalStorageException : StorageException
{
    public LocalStorageException(string message) : base(message, "Local") { }
    public LocalStorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when insufficient disk space is available.
/// </summary>
public class InsufficientStorageException : StorageException
{
    public InsufficientStorageException(long requiredBytes, long availableBytes)
        : base($"Insufficient storage. Required: {requiredBytes} bytes, Available: {availableBytes} bytes", "Local") { }
}
