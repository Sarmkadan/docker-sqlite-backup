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
    /// <summary>Initializes a new instance of the S3StorageException class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public S3StorageException(string message, bool isTransient = false) : base(message, "S3", isTransient) { }

    /// <summary>Initializes a new instance of the S3StorageException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public S3StorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when local storage operation fails.
/// </summary>
public class LocalStorageException : StorageException
{
    /// <summary>Initializes a new instance of the LocalStorageException class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public LocalStorageException(string message, bool isTransient = false) : base(message, "Local", isTransient) { }

    /// <summary>Initializes a new instance of the LocalStorageException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public LocalStorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when Azure Blob Storage operation fails.
/// </summary>
public class AzureStorageException : StorageException
{
    /// <summary>Initializes a new instance of the AzureStorageException class.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public AzureStorageException(string message, bool isTransient = false) : base(message, "Azure", isTransient) { }

    /// <summary>Initializes a new instance of the AzureStorageException class with an inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused this error.</param>
    /// <param name="isTransient">Whether the error is transient and can be retried.</param>
    public AzureStorageException(string message, Exception innerException, bool isTransient = false) : base(message, innerException, isTransient) { }
}

/// <summary>
/// Exception thrown when insufficient disk space is available.
/// </summary>
public class InsufficientStorageException : StorageException
{
    /// <summary>Initializes a new instance of the InsufficientStorageException class.</summary>
    /// <param name="requiredBytes">The number of bytes required.</param>
    /// <param name="availableBytes">The number of bytes available.</param>
    public InsufficientStorageException(long requiredBytes, long availableBytes)
        : base($"Insufficient storage. Required: {requiredBytes} bytes, Available: {availableBytes} bytes", "Local", false) { }
}
