// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

#nullable enable

using Cronos;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup;

/// <summary>
/// Background worker service that manages backup scheduling and execution.
/// </summary>
public class BackupWorker : BackgroundService
{
    private readonly IScheduleService _scheduleService;
    private readonly IBackupService _backupService;
    private readonly IVerificationService _verificationService;
    private readonly IRotationService _rotationService;
    private readonly AppSettings _appSettings;
    private readonly ILogger<BackupWorker> _logger;

    private readonly Dictionary<Guid, DateTime> _scheduleLastRun = new();
    private readonly Dictionary<Guid, CronExpression> _cronExpressions = new();
    private int _activeBackups;

    public BackupWorker(
        IScheduleService scheduleService,
        IBackupService backupService,
        IVerificationService verificationService,
        IRotationService rotationService,
        AppSettings appSettings,
        ILogger<BackupWorker> logger)
    {
        _scheduleService = scheduleService;
        _backupService = backupService;
        _verificationService = verificationService;
        _rotationService = rotationService;
        _appSettings = appSettings;
        _logger = logger;
    }

    /// <summary>
    /// Starts the background worker service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Backup Worker service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Load all active schedules
                var schedules = await _scheduleService.GetActiveSchedulesAsync();

                foreach (var schedule in schedules)
                {
                    // Check if this schedule should run
                    if (ShouldExecuteSchedule(schedule))
                    {
                        _ = ExecuteScheduleAsync(schedule, stoppingToken);
                    }
                }

                // Wait before checking schedules again
                await Task.Delay(
                    _appSettings.ScheduleCheckIntervalSeconds * 1000,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Backup Worker service is shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in backup worker loop");
                await Task.Delay(10000, stoppingToken);
            }
        }

        _logger.LogInformation("Backup Worker service stopped");
    }

    /// <summary>
    /// Determines if a schedule should be executed based on its cron expression.
    /// </summary>
    private bool ShouldExecuteSchedule(BackupSchedule schedule)
    {
        if (!_cronExpressions.TryGetValue(schedule.Id, out var cronExpression))
        {
            try
            {
                cronExpression = CronExpression.Parse(schedule.CronExpression);
                _cronExpressions[schedule.Id] = cronExpression;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid cron expression for schedule {ScheduleId}: {Cron}",
                    schedule.Id, schedule.CronExpression);
                return false;
            }
        }

        if (!_scheduleLastRun.TryGetValue(schedule.Id, out var lastRun))
        {
            lastRun = DateTime.MinValue;
        }

        var nextOccurrence = cronExpression.GetNextOccurrence(lastRun);
        return nextOccurrence.HasValue && nextOccurrence.Value <= DateTime.UtcNow;
    }

    /// <summary>
    /// Executes a backup for the specified schedule.
    /// </summary>
    private async Task ExecuteScheduleAsync(BackupSchedule schedule, CancellationToken cancellationToken)
    {
        // Rate limiting: don't exceed max concurrent backups
        if (_activeBackups >= _appSettings.MaxConcurrentBackups)
        {
            _logger.LogWarning(
                "Skipping backup for schedule {ScheduleId} - max concurrent backups reached ({Count})",
                schedule.Id, _appSettings.MaxConcurrentBackups);
            return;
        }

        Interlocked.Increment(ref _activeBackups);

        try
        {
            _logger.LogInformation("Starting backup for schedule {ScheduleId}: {ScheduleName}",
                schedule.Id, schedule.Name);

            // Create backup job
            var job = new BackupJob { ScheduleId = schedule.Id };
            job.MarkStarted();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_appSettings.BackupTimeoutSeconds));

            BackupResult? result = null;
            try
            {
                // Execute backup
                result = await _backupService.ExecuteBackupAsync(schedule, cts.Token);
                job.Result = result;

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Backup successful for schedule {ScheduleId}: {BackupPath}",
                        schedule.Id, result.BackupFilePath);

                    // Verify if enabled
                    if (schedule.VerifyAfterBackup)
                    {
                        _logger.LogInformation("Starting verification for backup {BackupId}",
                            result.Id);
                        var verification = await _verificationService.VerifyBackupAsync(result, cts.Token);

                        if (verification.IsSuccessful)
                        {
                            result.Status = (int)BackupStatus.VerifiedSuccess;
                            result.IsVerified = true;
                            result.VerifiedAt = DateTime.UtcNow;
                            _logger.LogInformation("Verification successful for backup {BackupId}",
                                result.Id);
                        }
                        else
                        {
                            result.Status = (int)BackupStatus.VerificationFailed;
                            _logger.LogError("Verification failed for backup {BackupId}: {Reason}",
                                result.Id, verification.StatusMessage);
                        }
                    }

                    // Execute rotation
                    var deletedCount = await _rotationService.ExecuteRotationAsync(schedule.Id);
                    if (deletedCount > 0)
                    {
                        _logger.LogInformation("Rotation deleted {DeletedCount} old backups for schedule {ScheduleId}",
                            deletedCount, schedule.Id);
                    }

                    // Update schedule's last backup time
                    schedule.LastBackupAt = DateTime.UtcNow;
                    await _scheduleService.UpdateScheduleAsync(schedule);
                    _scheduleLastRun[schedule.Id] = DateTime.UtcNow;

                    job.MarkCompleted((int)BackupStatus.Success);
                }
                else
                {
                    job.MarkCompleted((int)BackupStatus.Failed);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Backup timeout for schedule {ScheduleId} after {TimeoutSeconds} seconds",
                    schedule.Id, _appSettings.BackupTimeoutSeconds);
                job.MarkCompleted((int)BackupStatus.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Backup execution failed for schedule {ScheduleId}",
                    schedule.Id);
                job.MarkCompleted((int)BackupStatus.Failed);
            }
        }
        finally
        {
            Interlocked.Decrement(ref _activeBackups);
        }
    }
}
