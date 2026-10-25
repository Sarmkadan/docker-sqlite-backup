#nullable enable
// Author: Vladyslav Zaiets

using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Events;

/// <summary>
/// Event listener that collects metrics and statistics from backup events.
/// Tracks success rates, average durations, data transferred, and failure counts.
/// </summary>
public class MetricsEventListener : IBackupEventListener
{
    private readonly object _metricsLock = new();
    private long _totalBackups;
    private long _successfulBackups;
    private long _failedBackups;
    private long _totalBytes;
    private double _totalDurationSeconds;
    private long _restoreVerificationFailures;
    private readonly Dictionary<string, int> _failureReasons = [];
    private readonly ILogger<MetricsEventListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsEventListener"/> class.
    /// </summary>
    /// <param name="logger">Logger used to report metric updates and failures.</param>
    public MetricsEventListener(ILogger<MetricsEventListener> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of restore verifications that have failed, tracked separately from
    /// backup failures so monitoring can distinguish "backup never ran" from "backup ran but
    /// the restore verification failed".
    /// </summary>
    public long RestoreVerificationFailures
    {
        get
        {
            lock (_metricsLock)
            {
                return _restoreVerificationFailures;
            }
        }
    }

    /// <summary>
    /// Handles backup events and updates metrics.
    /// </summary>
    public async Task HandleAsync(BackupEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        switch (@event)
        {
            case BackupCompletedEvent completedEvent:
                RecordBackupSuccess(completedEvent);
                break;
            case BackupFailedEvent failedEvent:
                RecordBackupFailure(failedEvent);
                break;
            case RestoreVerificationFailedEvent verificationFailedEvent:
                RecordRestoreVerificationFailure(verificationFailedEvent);
                break;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets the event types this listener handles.
    /// </summary>
    public IEnumerable<string> GetSupportedEventTypes()
    {
        yield return "backup.completed";
        yield return "backup.failed";
        yield return "restore.verification.failed";
    }

    /// <summary>
    /// Checks if this listener can handle the given event type.
    /// </summary>
    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return GetSupportedEventTypes().Contains(eventType);
    }

    /// <summary>
    /// Gets current metrics.
    /// </summary>
    public BackupMetrics GetMetrics()
    {
        lock (_metricsLock)
        {
            var avgDuration = _totalBackups > 0 ? _totalDurationSeconds / _totalBackups : 0;
            var successRate = _totalBackups > 0 ? (_successfulBackups * 100.0) / _totalBackups : 0;

            return new BackupMetrics
            {
                TotalBackups = _totalBackups,
                SuccessfulBackups = _successfulBackups,
                FailedBackups = _failedBackups,
                SuccessRate = successRate,
                TotalBytesTransferred = _totalBytes,
                AverageDurationSeconds = avgDuration,
                FailureReasons = new Dictionary<string, int>(_failureReasons),
                RestoreVerificationFailures = _restoreVerificationFailures,
                CapturedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Resets all metrics to zero.
    /// </summary>
    public void ResetMetrics()
    {
        lock (_metricsLock)
        {
            _totalBackups = 0;
            _successfulBackups = 0;
            _failedBackups = 0;
            _totalBytes = 0;
            _totalDurationSeconds = 0;
            _restoreVerificationFailures = 0;
            _failureReasons.Clear();
            _logger.LogInformation("Metrics have been reset");
        }
    }

    private void RecordBackupSuccess(BackupCompletedEvent @event)
    {
        lock (_metricsLock)
        {
            _totalBackups++;
            _successfulBackups++;
            _totalBytes += @event.Result.BackupFileSizeBytes;
            _totalDurationSeconds += @event.Duration.TotalSeconds;

            _logger.LogInformation(
                "Backup metrics updated: Total={Total}, Success={Success}, Failed={Failed}, Rate={Rate:F2}%",
                _totalBackups,
                _successfulBackups,
                _failedBackups,
                (_successfulBackups * 100.0) / _totalBackups);
        }
    }

    private void RecordBackupFailure(BackupFailedEvent @event)
    {
        lock (_metricsLock)
        {
            _totalBackups++;
            _failedBackups++;

            var reason = ExtractErrorReason(@event.ErrorMessage);
            if (_failureReasons.ContainsKey(reason))
                _failureReasons[reason]++;
            else
                _failureReasons[reason] = 1;

            _logger.LogWarning(
                "Backup failed: {Reason} [{ScheduleId}]",
                reason,
                @event.ScheduleId);
        }
    }

    private void RecordRestoreVerificationFailure(RestoreVerificationFailedEvent @event)
    {
        lock (_metricsLock)
        {
            _restoreVerificationFailures++;

            _logger.LogWarning(
                "Restore verification failed: {Stage} for backup {BackupResultId}: {ExceptionMessage}",
                @event.FailureStage,
                @event.BackupResultId,
                @event.ExceptionMessage);
        }
    }

    private static string ExtractErrorReason(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return "Unknown";

        // Extract first line or first 50 chars as reason
        var lines = errorMessage.Split('\n');
        var reason = lines[0].Length > 50 ? lines[0][..50] : lines[0];

        return string.IsNullOrEmpty(reason) ? "Unknown" : reason;
    }
}

/// <summary>
/// Represents backup metrics at a point in time.
/// </summary>
public class BackupMetrics
{
    /// <summary>
    /// Gets or sets the total number of backups recorded, both successful and failed.
    /// </summary>
    public long TotalBackups { get; set; }

    /// <summary>
    /// Gets or sets the number of backups that completed successfully.
    /// </summary>
    public long SuccessfulBackups { get; set; }

    /// <summary>
    /// Gets or sets the number of backups that failed.
    /// </summary>
    public long FailedBackups { get; set; }

    /// <summary>
    /// Gets or sets the percentage of successful backups, from 0 to 100.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total size in bytes of backup files produced by successful backups.
    /// </summary>
    public long TotalBytesTransferred { get; set; }

    /// <summary>
    /// Gets or sets the average backup duration in seconds. The accumulated duration of
    /// successful backups is divided by <see cref="TotalBackups"/>.
    /// </summary>
    public double AverageDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of failures per failure reason. Each reason is the first line
    /// of the error message, truncated to 50 characters, or "Unknown" when no message is available.
    /// </summary>
    public Dictionary<string, int> FailureReasons { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of restore verifications that have failed.
    /// </summary>
    public long RestoreVerificationFailures { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which this metrics snapshot was captured.
    /// </summary>
    public DateTime CapturedAt { get; set; }

    /// <summary>
    /// Formats metrics as a readable string.
    /// </summary>
    public override string ToString()
    {
        return $"Metrics: {TotalBackups} total, {SuccessfulBackups} successful ({SuccessRate:F2}%), " +
               $"{FailedBackups} failed, {TotalBytesTransferred / (1024 * 1024)} MB transferred, " +
               $"Avg {AverageDurationSeconds:F2}s";
    }
}
