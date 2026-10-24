#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown for backup verification errors.
/// </summary>
public class VerificationException : DockerSqliteBackupException
{
    /// <summary>
    /// Initializes a new instance of the VerificationException class.
    /// </summary>
    public VerificationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with backup ID.
    /// </summary>
    public VerificationException(string message, Guid backupId) : base(message)
    {
        BackupId = backupId;
    }

    /// <summary>
    /// Initializes a new instance with inner exception.
    /// </summary>
    public VerificationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when database integrity check fails.
/// </summary>
public class IntegrityCheckFailedException : VerificationException
{
    /// <summary>
    /// Gets the raw error output reported by the integrity check, or <c>null</c> if none was captured.
    /// </summary>
    public string? Errors { get; }

    /// <summary>
    /// Initializes a new instance of the IntegrityCheckFailedException class with an error message only.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public IntegrityCheckFailedException(string message) : base(message)
    {
        System.ArgumentNullException.ThrowIfNull(message);
    }

    /// <summary>
    /// Initializes a new instance of the IntegrityCheckFailedException class with an error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="innerException">The exception that caused the integrity check failure.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="innerException"/> is <c>null</c>.</exception>
    public IntegrityCheckFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        System.ArgumentNullException.ThrowIfNull(message);
        System.ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// Initializes a new instance of the IntegrityCheckFailedException class.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="backupId">The identifier of the backup that failed the integrity check.</param>
    /// <param name="errors">The raw error output from the integrity check, if available.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public IntegrityCheckFailedException(string message, Guid backupId, string? errors = null)
        : base(message, backupId)
    {
        System.ArgumentNullException.ThrowIfNull(message);

        Errors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the IntegrityCheckFailedException class with every contextual value.
    /// </summary>
    /// <param name="message">The error message describing the failure.</param>
    /// <param name="backupId">The identifier of the backup that failed the integrity check.</param>
    /// <param name="errors">The raw error output from the integrity check, if available.</param>
    /// <param name="innerException">The exception that caused the integrity check failure.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="innerException"/> is <c>null</c>.</exception>
    public IntegrityCheckFailedException(string message, Guid backupId, string? errors, Exception innerException)
        : base(message, innerException)
    {
        System.ArgumentNullException.ThrowIfNull(message);
        System.ArgumentNullException.ThrowIfNull(innerException);

        BackupId = backupId;
        Errors = errors;
    }
}

/// <summary>
/// Exception thrown when restore verification fails.
/// </summary>
public class RestoreVerificationFailedException : VerificationException
{
    /// <summary>
    /// Gets the path of the temporary database produced by restoring the backup, or <c>null</c> if the restore did not reach that step.
    /// </summary>
    public string? RestoredDatabasePath { get; }

    /// <summary>
    /// Initializes a new instance of the RestoreVerificationFailedException class with an error message only.
    /// </summary>
    /// <param name="message">The error message describing the failed restore verification.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public RestoreVerificationFailedException(string message) : base(message)
    {
        System.ArgumentNullException.ThrowIfNull(message);
    }

    /// <summary>
    /// Initializes a new instance of the RestoreVerificationFailedException class with an error message and an inner exception.
    /// </summary>
    /// <param name="message">The error message describing the failed restore verification.</param>
    /// <param name="innerException">The exception that caused the restore verification failure.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="innerException"/> is <c>null</c>.</exception>
    public RestoreVerificationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
        System.ArgumentNullException.ThrowIfNull(message);
        System.ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// Initializes a new instance of the RestoreVerificationFailedException class.
    /// </summary>
    /// <param name="message">The error message describing the failed restore verification.</param>
    /// <param name="backupId">The identifier of the backup whose restore verification failed.</param>
    /// <param name="restoredDatabasePath">The path of the temporary database produced by the restore, if available.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public RestoreVerificationFailedException(string message, Guid backupId, string? restoredDatabasePath = null)
        : base(message, backupId)
    {
        System.ArgumentNullException.ThrowIfNull(message);

        RestoredDatabasePath = restoredDatabasePath;
    }

    /// <summary>
    /// Initializes a new instance of the RestoreVerificationFailedException class with every contextual value.
    /// </summary>
    /// <param name="message">The error message describing the failed restore verification.</param>
    /// <param name="backupId">The identifier of the backup whose restore verification failed.</param>
    /// <param name="restoredDatabasePath">The path of the temporary database produced by the restore, if available.</param>
    /// <param name="innerException">The exception that caused the restore verification failure.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="message"/> or <paramref name="innerException"/> is <c>null</c>.</exception>
    public RestoreVerificationFailedException(string message, Guid backupId, string? restoredDatabasePath, Exception innerException)
        : base(message, innerException)
    {
        System.ArgumentNullException.ThrowIfNull(message);
        System.ArgumentNullException.ThrowIfNull(innerException);

        BackupId = backupId;
        RestoredDatabasePath = restoredDatabasePath;
    }
}
