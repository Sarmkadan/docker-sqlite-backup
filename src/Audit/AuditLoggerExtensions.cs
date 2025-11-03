namespace DockerSqliteBackup.Audit;

/// <summary>
/// Extension methods for <see cref="AuditLogger"/>.
/// </summary>
public static class AuditLoggerExtensions
{
    /// <summary>
    /// Logs an entry with a specific category and action, indicating whether the operation was successful.
    /// </summary>
    /// <param name="auditLogger">The <see cref="AuditLogger"/> instance.</param>
    /// <param name="category">The category of the log entry.</param>
    /// <param name="action">The action being logged.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="auditLogger"/> is null.</exception>
    public static void LogSuccess(this AuditLogger auditLogger, string category, string action, bool success)
    {
        ArgumentNullException.ThrowIfNull(auditLogger);
        ArgumentException.ThrowIfNullOrEmpty(category);
        ArgumentException.ThrowIfNullOrEmpty(action);

        auditLogger.LogEntry(new AuditEntry
        {
            Category = category,
            Action = action,
            Success = success,
            Timestamp = DateTime.UtcNow,
        });
    }

    /// <summary>
    /// Logs a backup operation with a specific target ID and user ID, indicating whether the operation was successful.
    /// </summary>
    /// <param name="auditLogger">The <see cref="AuditLogger"/> instance.</param>
    /// <param name="targetId">The ID of the target being backed up.</param>
    /// <param name="userId">The ID of the user performing the backup.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="auditLogger"/> is null.</exception>
    public static void LogBackupResult(this AuditLogger auditLogger, string targetId, string? userId, bool success)
    {
        ArgumentNullException.ThrowIfNull(auditLogger);
        ArgumentException.ThrowIfNullOrEmpty(targetId);

        auditLogger.LogEntry(new AuditEntry
        {
            Category = "Backup",
            Action = "Operation",
            TargetId = targetId,
            UserId = userId,
            Success = success,
            Timestamp = DateTime.UtcNow,
        });
    }
}
