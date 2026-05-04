// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Models;

/// <summary>
/// Defines the schedule for automatic backup execution using CRON expressions.
/// </summary>
public class BackupSchedule
{
    /// <summary>
    /// CRON expression defining when backups should run (e.g., "0 2 * * *" for 2 AM daily).
    /// </summary>
    public string CronExpression { get; set; } = null!;

    /// <summary>
    /// Human-readable description of the schedule.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Whether the schedule is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Timezone identifier for CRON evaluation (e.g., "UTC", "America/New_York").
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    /// <summary>
    /// Maximum allowed execution time in seconds before the backup is marked as failed.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3600; // 1 hour default

    /// <summary>
    /// Date and time when the schedule was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of the next scheduled backup execution.
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Validates the backup schedule configuration.
    /// </summary>
    /// <returns>List of validation error messages, empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(CronExpression))
            errors.Add("CRON expression is required for the backup schedule.");

        if (!IsValidCronExpression(CronExpression))
            errors.Add($"Invalid CRON expression: {CronExpression}");

        if (string.IsNullOrWhiteSpace(Description))
            errors.Add("Schedule description is required.");

        if (string.IsNullOrWhiteSpace(TimeZone))
            errors.Add("TimeZone is required.");

        if (!IsValidTimeZone(TimeZone))
            errors.Add($"Invalid timezone: {TimeZone}");

        if (TimeoutSeconds < 60)
            errors.Add("Timeout must be at least 60 seconds.");

        if (TimeoutSeconds > 86400)
            errors.Add("Timeout cannot exceed 24 hours (86400 seconds).");

        return errors;
    }

    /// <summary>
    /// Validates if the provided CRON expression is properly formatted.
    /// </summary>
    private static bool IsValidCronExpression(string cron)
    {
        if (string.IsNullOrWhiteSpace(cron))
            return false;

        try
        {
            // CRON should have 5 or 6 fields (minute, hour, day, month, day-of-week, [second])
            var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length is 5 or 6;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if the provided timezone identifier is valid.
    /// </summary>
    private static bool IsValidTimeZone(string tzId)
    {
        if (string.IsNullOrWhiteSpace(tzId))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(tzId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
