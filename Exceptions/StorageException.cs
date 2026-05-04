// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Thrown when a storage operation (upload, download, delete) fails.
/// </summary>
public class StorageException : Exception
{
    /// <summary>
    /// The storage provider name where the operation failed.
    /// </summary>
    public string? StorageProvider { get; set; }

    /// <summary>
    /// The path or object key that was being accessed.
    /// </summary>
    public string? ObjectPath { get; set; }

    /// <summary>
    /// The operation that was being performed (Upload, Download, Delete, etc.).
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// Initializes a new instance of StorageException with a message.
    /// </summary>
    public StorageException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of StorageException with a message and inner exception.
    /// </summary>
    public StorageException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with message and storage context.
    /// </summary>
    public StorageException(string message, string? provider, string? objectPath, string? operation)
        : base(message)
    {
        StorageProvider = provider;
        ObjectPath = objectPath;
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and storage context.
    /// </summary>
    public StorageException(string message, Exception innerException, string? provider, string? objectPath, string? operation)
        : base(message, innerException)
    {
        StorageProvider = provider;
        ObjectPath = objectPath;
        Operation = operation;
    }
}
