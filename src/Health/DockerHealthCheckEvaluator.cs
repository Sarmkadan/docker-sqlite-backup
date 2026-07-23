#nullable enable
// Author: Vladyslav Zaiets

using Cronos;
using DockerSqliteBackup.Configuration;

namespace DockerSqliteBackup.Health;

/// <summary>
/// Evaluates a persisted <see cref="HealthStatusSnapshot"/> against the configured backup
/// schedule to decide whether the container should report itself healthy to Docker.
/// </summary>
public static class DockerHealthCheckEvaluator
{
    /// <summary>
    /// Evaluates the given snapshot.
    /// </summary>
    /// <param name="snapshot">The persisted status snapshot, or <see langword="null"/> when no status file exists yet.</param>
    /// <param name="appSettings">The application settings, used for the freshness grace factor and fallback interval.</param>
    /// <returns>An outcome describing whether the container is healthy and why.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="appSettings"/> is <see langword="null"/>.</exception>
    public static HealthCheckOutcome Evaluate(HealthStatusSnapshot? snapshot, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);

        if (snapshot is null)
        {
            // No process has recorded a backup/verification event yet (e.g. fresh container
            // still waiting for its first scheduled run). Treat as healthy during startup.
            return new HealthCheckOutcome(true, "No backup activity recorded yet; awaiting first scheduled run.");
        }

        // (b) Most recent restore verification failed.
        if (snapshot.LastRestoreVerificationPassed == false)
        {
            var message = snapshot.LastRestoreVerificationMessage ?? "restore verification failed";
            return new HealthCheckOutcome(false, $"Last restore verification failed at {snapshot.LastRestoreVerificationAt:O}: {message}");
        }

        // (a) A backup failure occurred more recently than the last successful completion.
        if (snapshot.LastBackupFailedAt is { } failedAt &&
            (snapshot.LastBackupCompletedAt is null || failedAt > snapshot.LastBackupCompletedAt))
        {
            var message = snapshot.LastBackupFailedMessage ?? "backup failed";
            return new HealthCheckOutcome(false, $"Last backup failed at {failedAt:O}: {message}");
        }

        // (c) Freshness: the last successful backup is older than the expected interval plus grace.
        if (snapshot.LastBackupCompletedAt is { } completedAt)
        {
            var expectedInterval = ResolveExpectedInterval(snapshot.LastBackupCompletedCronExpression, appSettings);
            var threshold = TimeSpan.FromTicks((long)(expectedInterval.Ticks * appSettings.HealthCheckGraceFactor));
            var age = DateTime.UtcNow - completedAt;

            if (age > threshold)
            {
                return new HealthCheckOutcome(
                    false,
                    $"Last successful backup at {completedAt:O} is {age.TotalMinutes:F1} minutes old, exceeding the allowed {threshold.TotalMinutes:F1} minutes.");
            }

            return new HealthCheckOutcome(true, $"Last successful backup at {completedAt:O} is within the freshness window.");
        }

        return new HealthCheckOutcome(true, "No successful backup recorded yet; awaiting first scheduled run.");
    }

    /// <summary>
    /// Computes the expected gap between backups from the schedule's cron expression, falling
    /// back to the configured schedule polling interval when the cron expression is unavailable
    /// or unparseable.
    /// </summary>
    private static TimeSpan ResolveExpectedInterval(string? cronExpression, AppSettings appSettings)
    {
        if (!string.IsNullOrWhiteSpace(cronExpression))
        {
            try
            {
                var parsed = CronExpression.Parse(cronExpression);
                var now = DateTime.UtcNow;
                var next = parsed.GetNextOccurrence(now);
                var following = next.HasValue ? parsed.GetNextOccurrence(next.Value) : null;

                if (next.HasValue && following.HasValue)
                {
                    return following.Value - next.Value;
                }
            }
            catch (CronFormatException)
            {
                // Fall through to the configured fallback interval below.
            }
        }

        return TimeSpan.FromSeconds(appSettings.ScheduleCheckIntervalSeconds);
    }
}
