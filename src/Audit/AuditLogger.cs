#nullable enable
// Author: Vladyslav Zaiets

using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Audit;

/// <summary>
/// Audit logger for recording important operations and state changes.
/// Provides structured logging of backup operations, schedule changes, and data access.
/// </summary>
public class AuditLogger
{
    private readonly ILogger<AuditLogger> _logger;
    private readonly string _auditLogPath;

    public AuditLogger(
        ILogger<AuditLogger> logger,
        string? auditLogPath = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _auditLogPath = auditLogPath ?? Path.Combine(AppContext.BaseDirectory, "audit.log");
    }

    /// <summary>
    /// Logs a backup operation.
    /// </summary>
    public void LogBackupOperation(Guid scheduleId, string operation, bool success, string? details = null)
    {
        _logger.LogInformation("LogBackupOperation called for {ScheduleId} with operation {Operation}", scheduleId, operation);
        ArgumentException.ThrowIfNullOrEmpty(operation);
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = "BACKUP",
            Action = operation,
            TargetId = scheduleId.ToString(),
            Success = success,
            Details = details
        };

        LogAuditEntry(entry);
        _logger.LogInformation("LogBackupOperation completed for {ScheduleId}", scheduleId);
    }

    /// <summary>
    /// Logs a schedule change.
    /// </summary>
    public void LogScheduleChange(Guid scheduleId, string action, Dictionary<string, string> changes)
    {
        _logger.LogInformation("LogScheduleChange called for {ScheduleId} with action {Action}", scheduleId, action);
        ArgumentException.ThrowIfNullOrEmpty(action);
        ArgumentNullException.ThrowIfNull(changes);
        var changeDetails = string.Join("; ", changes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = "SCHEDULE",
            Action = action,
            TargetId = scheduleId.ToString(),
            Success = true,
            Details = changeDetails
        };

        LogAuditEntry(entry);
        _logger.LogInformation("LogScheduleChange completed for {ScheduleId}", scheduleId);
    }

    /// <summary>
    /// Logs a configuration change.
    /// </summary>
    public void LogConfigChange(string setting, string oldValue, string newValue, string? reason = null)
    {
        _logger.LogInformation("LogConfigChange called for {Setting}", setting);
        ArgumentException.ThrowIfNullOrEmpty(setting);
        ArgumentException.ThrowIfNullOrEmpty(oldValue);
        ArgumentException.ThrowIfNullOrEmpty(newValue);
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = "CONFIG",
            Action = "CHANGED",
            TargetId = setting,
            Success = true,
            Details = $"{oldValue} -> {newValue}" + (reason  is not null ? $" ({reason})" : "")
        };

        LogAuditEntry(entry);
        _logger.LogInformation("LogConfigChange completed for {Setting}", setting);
    }

    /// <summary>
    /// Logs a data access event.
    /// </summary>
    public void LogDataAccess(string resource, string action, string? userId = null, bool success = true)
    {
        _logger.LogInformation("LogDataAccess called for {Resource} with action {Action}", resource, action);
        ArgumentException.ThrowIfNullOrEmpty(resource);
        ArgumentException.ThrowIfNullOrEmpty(action);
        var entry = new AuditEntry
        {
            Timestamp = DateTime.UtcNow,
            Category = "DATA_ACCESS",
            Action = action,
            TargetId = resource,
            UserId = userId,
            Success = success
        };

        LogAuditEntry(entry);
        _logger.LogInformation("LogDataAccess completed for {Resource}", resource);
    }

    /// <summary>
    /// Logs a generic audit entry.
    /// </summary>
    public void LogEntry(AuditEntry entry)
    {
        _logger.LogInformation("LogEntry called for category {Category}", entry.Category);
        ArgumentNullException.ThrowIfNull(entry);
        LogAuditEntry(entry);
        _logger.LogInformation("LogEntry completed for category {Category}", entry.Category);
    }

    private void LogAuditEntry(AuditEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry);
            _logger.LogInformation("AUDIT: {AuditEntry}", json);

            // Optionally write to file
            if (!string.IsNullOrEmpty(_auditLogPath))
            {
                File.AppendAllText(_auditLogPath, json + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log entry");
        }
    }
}

/// <summary>
/// Represents a single audit log entry.
/// </summary>
public class AuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool Success { get; set; }
    public string? Details { get; set; }
    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets a human-readable summary of the audit entry.
    /// </summary>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "FAILED";
        var summary = $"[{Timestamp:O}] {Category}/{Action} on {TargetId} - {status}";

        if (!string.IsNullOrEmpty(Details))
            summary += $": {Details}";

        if (!string.IsNullOrEmpty(UserId))
            summary += $" (User: {UserId})";

        return summary;
    }
}
