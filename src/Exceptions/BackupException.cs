// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Base exception for backup-related errors.
/// </summary>
public class BackupException : Exception
{
    /// <summary>Gets or sets the backup ID associated with the error.</summary>
    public Guid? BackupId { get; }

    /// <summary>Gets or sets the schedule ID associated with the error.</summary>
    public Guid? ScheduleId { get; }

    /// <summary>
    /// Initializes a new instance of the BackupException class.
    /// </summary>
    public BackupException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the BackupException class with a backup ID.
    /// </summary>
    public BackupException(string message, Guid backupId) : base(message)
    {
        BackupId = backupId;
    }

    /// <summary>
    /// Initializes a new instance of the BackupException class with inner exception.
    /// </summary>
    public BackupException(string message, Exception innerException) 
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with schedule and backup IDs.
    /// </summary>
    public BackupException(string message, Guid scheduleId, Guid backupId) : base(message)
    {
        ScheduleId = scheduleId;
        BackupId = backupId;
    }
}

/// <summary>
/// Exception thrown when a database cannot be accessed.
/// </summary>
public class DatabaseAccessException : BackupException
{
    public DatabaseAccessException(string databasePath, Exception innerException)
        : base($"Failed to access database: {databasePath}", innerException) { }
}

/// <summary>
/// Exception thrown when backup operation times out.
/// </summary>
public class BackupTimeoutException : BackupException
{
    public BackupTimeoutException(string message, TimeSpan timeout)
        : base($"{message}. Timeout: {timeout.TotalSeconds} seconds") { }
}

/// <summary>
/// Exception thrown when backup file becomes corrupted.
/// </summary>
public class BackupCorruptedException : BackupException
{
    public BackupCorruptedException(string message, Guid backupId)
        : base(message, backupId) { }
}
