// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Provides diagnostic information about the backup system.
/// </summary>
public class DiagnosticService
{
    private readonly IBackupRepository _repository;
    private readonly ILogger<DiagnosticService> _logger;

    public DiagnosticService(IBackupRepository repository, ILogger<DiagnosticService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a complete health report of the backup system.
    /// </summary>
    public async Task<SystemHealthReport> GetSystemHealthReportAsync(CancellationToken cancellationToken = default)
    {
        var report = new SystemHealthReport
        {
            ReportGeneratedAt = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            DotNetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
        };

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);
            report.TotalBackupJobs = jobs.Count;
            report.ActiveJobs = jobs.Count(j => j.Status == BackupStatus.Completed);

            foreach (var job in jobs)
            {
                var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
                var failedSnapshots = snapshots.Where(s => s.Status == BackupStatus.Failed).ToList();

                if (failedSnapshots.Count > 0)
                {
                    report.FailedJobIds.Add(new JobHealth
                    {
                        JobId = job.Id,
                        JobName = job.Name,
                        FailureCount = failedSnapshots.Count,
                        LastFailureTime = failedSnapshots.OrderByDescending(s => s.CreatedAt).First().CreatedAt
                    });
                }

                var totalSize = snapshots.Sum(s => s.SizeBytes);
                report.TotalStorageUsedBytes += totalSize;
            }

            report.SystemStatus = report.FailedJobIds.Count == 0 ? "Healthy" : "Degraded";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating health report");
            report.SystemStatus = "Error";
            report.LastErrorMessage = ex.Message;
        }

        return report;
    }

    /// <summary>
    /// Gets recent backup activity summary.
    /// </summary>
    public async Task<ActivitySummary> GetRecentActivityAsync(
        int hoursBack = 24,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hoursBack);
        var summary = new ActivitySummary
        {
            PeriodStartTime = cutoffTime,
            PeriodEndTime = DateTime.UtcNow,
            HoursPeriod = hoursBack
        };

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
                var recentSnapshots = snapshots.Where(s => s.CreatedAt >= cutoffTime).ToList();

                summary.TotalBackupAttempts += recentSnapshots.Count;
                summary.SuccessfulBackups += recentSnapshots.Count(s => s.Status == BackupStatus.Completed);
                summary.FailedBackups += recentSnapshots.Count(s => s.Status == BackupStatus.Failed);
                summary.TotalDataProcessedBytes += recentSnapshots.Sum(s => s.SizeBytes);

                if (recentSnapshots.Any(s => s.Status == BackupStatus.Failed))
                {
                    summary.JobsWithIssues.Add(job.Id);
                }
            }

            summary.SuccessRate = summary.TotalBackupAttempts > 0
                ? (summary.SuccessfulBackups * 100.0) / summary.TotalBackupAttempts
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity summary");
        }

        return summary;
    }

    /// <summary>
    /// Gets storage usage breakdown by job.
    /// </summary>
    public async Task<StorageUsageReport> GetStorageUsageAsync(CancellationToken cancellationToken = default)
    {
        var report = new StorageUsageReport { ReportGeneratedAt = DateTime.UtcNow };

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
                var completedSnapshots = snapshots.Where(s => s.Status == BackupStatus.Completed).ToList();

                var jobUsage = new JobStorageUsage
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    BackupCount = completedSnapshots.Count,
                    TotalSizeBytes = completedSnapshots.Sum(s => s.SizeBytes),
                    TotalUncompressedBytes = completedSnapshots.Sum(s => s.OriginalSizeBytes ?? s.SizeBytes),
                    OldestBackupDate = completedSnapshots.OrderBy(s => s.CreatedAt).FirstOrDefault()?.CreatedAt,
                    NewestBackupDate = completedSnapshots.OrderByDescending(s => s.CreatedAt).FirstOrDefault()?.CreatedAt
                };

                jobUsage.AverageCompressionRatio = completedSnapshots.Any(s => s.CompressionRatio.HasValue)
                    ? completedSnapshots.Where(s => s.CompressionRatio.HasValue).Average(s => s.CompressionRatio!.Value)
                    : 0;

                report.JobUsages.Add(jobUsage);
                report.TotalStorageBytes += jobUsage.TotalSizeBytes;
            }

            report.JobUsages = report.JobUsages.OrderByDescending(j => j.TotalSizeBytes).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage usage");
        }

        return report;
    }

    /// <summary>
    /// Checks backup job compliance with retention policies.
    /// </summary>
    public async Task<RetentionComplianceReport> GetRetentionComplianceAsync(CancellationToken cancellationToken = default)
    {
        var report = new RetentionComplianceReport { ReportGeneratedAt = DateTime.UtcNow };

        try
        {
            var jobs = await _repository.GetAllBackupJobsAsync(cancellationToken);

            foreach (var job in jobs)
            {
                var snapshots = await _repository.GetSnapshotsByJobIdAsync(job.Id, cancellationToken);
                var eligibleForDeletion = snapshots.Where(s => s.IsEligibleForDeletion(job.MaxRetentionDays)).ToList();

                var compliance = new JobRetentionCompliance
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    PolicyMaxCount = job.MaxRetentionCount,
                    PolicyMaxAgeDays = job.MaxRetentionDays,
                    CurrentSnapshotCount = snapshots.Count,
                    SnapshotsOverCount = Math.Max(0, snapshots.Count - job.MaxRetentionCount),
                    SnapshotsOverAge = eligibleForDeletion.Count,
                    IsCompliant = snapshots.Count <= job.MaxRetentionCount && eligibleForDeletion.Count == 0
                };

                report.JobCompliances.Add(compliance);
            }

            report.TotalJobsCompliant = report.JobCompliances.Count(c => c.IsCompliant);
            report.TotalJobsNonCompliant = report.JobCompliances.Count(c => !c.IsCompliant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking retention compliance");
        }

        return report;
    }
}

public class SystemHealthReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public string? MachineName { get; set; }
    public string? DotNetVersion { get; set; }
    public int TotalBackupJobs { get; set; }
    public int ActiveJobs { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public string? SystemStatus { get; set; }
    public List<JobHealth> FailedJobIds { get; set; } = new();
    public string? LastErrorMessage { get; set; }

    public string GetStorageUsageFormatted() => FileUtilities.FormatFileSize(TotalStorageUsedBytes);
}

public class JobHealth
{
    public string? JobId { get; set; }
    public string? JobName { get; set; }
    public int FailureCount { get; set; }
    public DateTime? LastFailureTime { get; set; }
}

public class ActivitySummary
{
    public DateTime PeriodStartTime { get; set; }
    public DateTime PeriodEndTime { get; set; }
    public int HoursPeriod { get; set; }
    public int TotalBackupAttempts { get; set; }
    public int SuccessfulBackups { get; set; }
    public int FailedBackups { get; set; }
    public long TotalDataProcessedBytes { get; set; }
    public double SuccessRate { get; set; }
    public List<string> JobsWithIssues { get; set; } = new();

    public string GetDataProcessedFormatted() => FileUtilities.FormatFileSize(TotalDataProcessedBytes);
}

public class StorageUsageReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public List<JobStorageUsage> JobUsages { get; set; } = new();
    public long TotalStorageBytes { get; set; }

    public string GetTotalStorageFormatted() => FileUtilities.FormatFileSize(TotalStorageBytes);
}

public class JobStorageUsage
{
    public string? JobId { get; set; }
    public string? JobName { get; set; }
    public int BackupCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public long TotalUncompressedBytes { get; set; }
    public double AverageCompressionRatio { get; set; }
    public DateTime? OldestBackupDate { get; set; }
    public DateTime? NewestBackupDate { get; set; }

    public string GetTotalSizeFormatted() => FileUtilities.FormatFileSize(TotalSizeBytes);
    public string GetUncompressedSizeFormatted() => FileUtilities.FormatFileSize(TotalUncompressedBytes);
    public double GetCompressionPercentage() => (1 - AverageCompressionRatio) * 100;
}

public class RetentionComplianceReport
{
    public DateTime ReportGeneratedAt { get; set; }
    public List<JobRetentionCompliance> JobCompliances { get; set; } = new();
    public int TotalJobsCompliant { get; set; }
    public int TotalJobsNonCompliant { get; set; }
}

public class JobRetentionCompliance
{
    public string? JobId { get; set; }
    public string? JobName { get; set; }
    public int PolicyMaxCount { get; set; }
    public int PolicyMaxAgeDays { get; set; }
    public int CurrentSnapshotCount { get; set; }
    public int SnapshotsOverCount { get; set; }
    public int SnapshotsOverAge { get; set; }
    public bool IsCompliant { get; set; }
}
