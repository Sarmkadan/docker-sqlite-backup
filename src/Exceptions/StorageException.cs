#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for storage-related errors.
/// </summary>
public class StorageException : DockerSqliteBackupException
{
    /// <summary>Gets a value indicating whether the error is transient and can be retried.</summary>
    public bool IsTransient { get; }

    /// <summary>Gets the storage type that failed.</summary>
    public string? StorageType { get; }

    /// <summary>
    /// Initializes a new instance of the StorageException class.
    /// </summary>
    public StorageException(string message, bool isTransient = false) : base(message)
    {
        IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance with storage type information.
    /// </summary>
    public StorageException(string message, string storageType, bool isTransient = false) : base(message)
    {
        StorageType = storageType;
        IsTransient = isTransient;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public StorageException(string message, Exception innerException, bool isTransient = false)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}


/// <summary>
/// Exception thrown when S3 operation fails.
/// </summary>
public class S3StorageException : StorageException
{
    public S3StorageException(string message, bool isTransient = false) : base(message, "S3", isTransient) { }
    public S3StorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when local storage operation fails.
/// </summary>
public class LocalStorageException : StorageException
{
    public LocalStorageException(string message, bool isTransient = false) : base(message, "Local", isTransient) { }
    public LocalStorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when Azure Blob Storage operation fails.
/// </summary>
public class AzureStorageException : StorageException
{
    public AzureStorageException(string message, bool isTransient = false) : base(message, "Azure", isTransient) { }
    public AzureStorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when insufficient disk space is available.
/// </summary>
public class InsufficientStorageException : StorageException
{
    public InsufficientStorageException(long requiredBytes, long availableBytes)
        : base($"Insufficient storage. Required: {requiredBytes} bytes, Available: {availableBytes} bytes", "Local", false) { }
}
