#nullable enable

using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Base exception for all DockerSqliteBackup-specific exceptions.
/// Carries the common context (which backup and which database were involved)
/// so every logged exception in the hierarchy exposes it uniformly.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$exceptionType", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
[JsonDerivedType(typeof(DockerSqliteBackupException), "base")]
[JsonDerivedType(typeof(BackupException), "backup")]
[JsonDerivedType(typeof(DatabaseAccessException), "databaseAccess")]
[JsonDerivedType(typeof(BackupTimeoutException), "backupTimeout")]
[JsonDerivedType(typeof(BackupCorruptedException), "backupCorrupted")]
[JsonDerivedType(typeof(StorageException), "storage")]
[JsonDerivedType(typeof(S3StorageException), "s3Storage")]
[JsonDerivedType(typeof(LocalStorageException), "localStorage")]
[JsonDerivedType(typeof(AzureStorageException), "azureStorage")]
[JsonDerivedType(typeof(InsufficientStorageException), "insufficientStorage")]
[JsonDerivedType(typeof(VerificationException), "verification")]
[JsonDerivedType(typeof(IntegrityCheckFailedException), "integrityCheckFailed")]
[JsonDerivedType(typeof(RestoreVerificationFailedException), "restoreVerificationFailed")]
[JsonDerivedType(typeof(ValidationException), "validation")]
[JsonDerivedType(typeof(ArgumentNullException), "argumentNull")]
[JsonDerivedType(typeof(ArgumentException), "argument")]
[JsonDerivedType(typeof(EmptyCollectionException), "emptyCollection")]
[JsonDerivedType(typeof(ConfigurationException), "configuration")]
[JsonDerivedType(typeof(MissingConfigurationException), "missingConfiguration")]
[JsonDerivedType(typeof(InvalidConfigurationException), "invalidConfiguration")]
[JsonDerivedType(typeof(ScheduleException), "schedule")]
[JsonDerivedType(typeof(InvalidCronExpressionException), "invalidCronExpression")]
[JsonDerivedType(typeof(InvalidScheduleException), "invalidSchedule")]
[JsonDerivedType(typeof(RotationException), "rotation")]
public class DockerSqliteBackupException : Exception
{
    /// <summary>
    /// Gets the identifier of the backup that was being processed when the error occurred, if known.
    /// </summary>
    public Guid? BackupId { get; init; }

    /// <summary>
    /// Gets the path of the database that was being processed when the error occurred, if known.
    /// </summary>
    public string? DatabasePath { get; init; }

    /// <summary>
    /// Gets the UTC timestamp at which the exception was created.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class.
    /// </summary>
    public DockerSqliteBackupException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DockerSqliteBackupException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockerSqliteBackupException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DockerSqliteBackupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
