#nullable enable

using System.Globalization;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using ArgumentNullException = System.ArgumentNullException;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Provides validation helpers for <see cref="BackupService"/> instances.
/// </summary>
public static class BackupServiceValidation
{
    /// <summary>
    /// Validates a <see cref="BackupService"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The backup service instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BackupService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate public members that would be used in operations
        // Note: BackupService is a service class with injected dependencies, so we validate its configuration
        // rather than its internal state, which is managed by DI

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BackupService"/> instance is valid.
    /// </summary>
    /// <param name="value">The backup service instance to check.</param>
    /// <returns>True if the service is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this BackupService? value)
    {
        return value is not null && !value.Validate().Any();
    }

    /// <summary>
    /// Ensures that the specified <see cref="BackupService"/> instance is valid, throwing an exception
    /// with detailed validation messages if it is not.
    /// </summary>
    /// <param name="value">The backup service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the service is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this BackupService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            "BackupService is not valid. " +
            string.Join(" ", problems),
            nameof(value),
            new InvalidOperationException(string.Join("\n", problems)));
    }

    /// <summary>
    /// Validates a <see cref="BackupResult"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="result">The backup result to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BackupResult? result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var problems = new List<string>();

        // Validate required fields
        if (result.ScheduleId == Guid.Empty)
        {
            problems.Add("ScheduleId must be a non-empty GUID.");
        }

        // Validate status
        if (result.Status < 0 || result.Status > 6) // BackupStatus enum range
        {
            problems.Add("Status must be a valid BackupStatus value (0-6).");
        }

        // Validate file path if present
        if (!string.IsNullOrWhiteSpace(result.BackupFilePath))
        {
            if (string.IsNullOrWhiteSpace(result.BackupFilePath.Trim()))
            {
                problems.Add("BackupFilePath cannot be whitespace.");
            }
            else if (result.BackupFilePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                problems.Add("BackupFilePath contains invalid path characters.");
            }
        }

        // Validate file size (should be non-negative if set)
        if (result.BackupFileSizeBytes < 0)
        {
            problems.Add("BackupFileSizeBytes cannot be negative.");
        }

        // Validate checksum format if present
        if (!string.IsNullOrWhiteSpace(result.Checksum))
        {
            if (string.IsNullOrWhiteSpace(result.Checksum.Trim()))
            {
                problems.Add("Checksum cannot be whitespace.");
            }
            else if (result.Checksum.Length != 64) // SHA256 is 64 hex chars
            {
                problems.Add("Checksum must be a 64-character hexadecimal string (SHA256).");
            }
            else if (!IsHexadecimal(result.Checksum))
            {
                problems.Add("Checksum must contain only hexadecimal characters (0-9, a-f).");
            }
        }

        // Validate timestamps
        if (result.StartedAt == default)
        {
            problems.Add("StartedAt must be set to a valid DateTime.");
        }
        else if (result.StartedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("StartedAt must be in UTC.");
        }

        if (result.CompletedAt.HasValue)
        {
            if (result.CompletedAt.Value == default)
            {
                problems.Add("CompletedAt cannot be default DateTime.");
            }
            else if (result.CompletedAt.Value.Kind != DateTimeKind.Utc)
            {
                problems.Add("CompletedAt must be in UTC.");
            }
            else if (result.CompletedAt.Value < result.StartedAt)
            {
                problems.Add("CompletedAt cannot be before StartedAt.");
            }
        }

        if (result.DurationMilliseconds < 0)
        {
            problems.Add("DurationMilliseconds cannot be negative.");
        }

        // Validate backup mode
        if (result.BackupMode < 0 || result.BackupMode > 1) // BackupMode enum range
        {
            problems.Add("BackupMode must be a valid BackupMode value (0=Full, 1=Incremental).");
        }

        // Validate base backup reference if set
        if (result.BackupMode == (int)BackupMode.Incremental && !result.BaseBackupResultId.HasValue)
        {
            problems.Add("BaseBackupResultId must be set for incremental backups.");
        }

        if (result.BaseBackupResultId.HasValue && result.BaseBackupResultId.Value == Guid.Empty)
        {
            problems.Add("BaseBackupResultId cannot be an empty GUID.");
        }

        // Validate S3-specific fields
        if (result.IsStoredInS3)
        {
            if (string.IsNullOrWhiteSpace(result.S3ObjectKey))
            {
                problems.Add("S3ObjectKey must be set when IsStoredInS3 is true.");
            }
            else if (string.IsNullOrWhiteSpace(result.S3ObjectKey.Trim()))
            {
                problems.Add("S3ObjectKey cannot be whitespace.");
            }
        }

        // Validate error-related fields
        if (result.Status == (int)BackupStatus.Failed && string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            problems.Add("ErrorMessage must be set when Status is Failed.");
        }

        if (result.Status == (int)BackupStatus.Failed && string.IsNullOrWhiteSpace(result.StackTrace))
        {
            problems.Add("StackTrace should be set when Status is Failed for debugging purposes.");
        }

        // Validate verification fields
        if (result.IsVerified && !result.VerifiedAt.HasValue)
        {
            problems.Add("VerifiedAt must be set when IsVerified is true.");
        }

        if (result.VerifiedAt.HasValue)
        {
            if (result.VerifiedAt.Value == default)
            {
                problems.Add("VerifiedAt cannot be default DateTime.");
            }
            else if (result.VerifiedAt.Value.Kind != DateTimeKind.Utc)
            {
                problems.Add("VerifiedAt must be in UTC.");
            }
            else if (result.VerifiedAt.Value < result.StartedAt)
            {
                problems.Add("VerifiedAt cannot be before StartedAt.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BackupResult"/> instance is valid.
    /// </summary>
    /// <param name="result">The backup result to check.</param>
    /// <returns>True if the backup result is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static bool IsValid(this BackupResult? result)
    {
        return result is not null && !result.Validate().Any();
    }

    /// <summary>
    /// Ensures that the specified <see cref="BackupResult"/> instance is valid, throwing an exception
    /// with detailed validation messages if it is not.
    /// </summary>
    /// <param name="result">The backup result to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the result is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this BackupResult? result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var problems = result.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            "BackupResult is not valid. " +
            string.Join(" ", problems),
            nameof(result),
            new InvalidOperationException(string.Join("\n", problems)));
    }

    /// <summary>
    /// Validates a <see cref="BackupSchedule"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="schedule">The backup schedule to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedule"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this BackupSchedule? schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var problems = new List<string>();

        // Validate required string fields
        if (string.IsNullOrWhiteSpace(schedule.Name))
        {
            problems.Add("Name is required and cannot be null or whitespace.");
        }
        else if (schedule.Name.Length > 255)
        {
            problems.Add("Name cannot exceed 255 characters.");
        }

        if (string.IsNullOrWhiteSpace(schedule.DatabasePath))
        {
            problems.Add("DatabasePath is required and cannot be null or whitespace.");
        }
        else if (schedule.DatabasePath.Length > 1024)
        {
            problems.Add("DatabasePath cannot exceed 1024 characters.");
        }
        else if (schedule.DatabasePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            problems.Add("DatabasePath contains invalid path characters.");
        }

        if (string.IsNullOrWhiteSpace(schedule.CronExpression))
        {
            problems.Add("CronExpression is required and cannot be null or whitespace.");
        }
        else if (!IsValidCronExpression(schedule.CronExpression))
        {
            problems.Add("CronExpression must be a valid cron expression.");
        }

        // Validate numeric ranges
        if (schedule.RetentionDays < 1)
        {
            problems.Add("RetentionDays must be at least 1 day.");
        }
        else if (schedule.RetentionDays > 3650) // ~10 years max
        {
            problems.Add("RetentionDays cannot exceed 3650 days (~10 years).");
        }

        if (schedule.MaxBackupCount < 1)
        {
            problems.Add("MaxBackupCount must be at least 1.");
        }
        else if (schedule.MaxBackupCount > 1000)
        {
            problems.Add("MaxBackupCount cannot exceed 1000.");
        }

        // Validate backup mode
        if (schedule.BackupMode < 0 || schedule.BackupMode > 1) // BackupMode enum range
        {
            problems.Add("BackupMode must be a valid BackupMode value (0=Full, 1=Incremental).");
        }

        // Validate timestamps
        if (schedule.CreatedAt == default)
        {
            problems.Add("CreatedAt must be set to a valid DateTime.");
        }
        else if (schedule.CreatedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("CreatedAt must be in UTC.");
        }

        if (schedule.LastModifiedAt == default)
        {
            problems.Add("LastModifiedAt must be set to a valid DateTime.");
        }
        else if (schedule.LastModifiedAt.Kind != DateTimeKind.Utc)
        {
            problems.Add("LastModifiedAt must be in UTC.");
        }

        if (schedule.LastBackupAt.HasValue)
        {
            if (schedule.LastBackupAt.Value == default)
            {
                problems.Add("LastBackupAt cannot be default DateTime.");
            }
            else if (schedule.LastBackupAt.Value.Kind != DateTimeKind.Utc)
            {
                problems.Add("LastBackupAt must be in UTC.");
            }
            else if (schedule.LastBackupAt.Value > DateTime.UtcNow)
            {
                problems.Add("LastBackupAt cannot be in the future.");
            }
        }

        if (schedule.NextRunTime.HasValue)
        {
            if (schedule.NextRunTime.Value == default)
            {
                problems.Add("NextRunTime cannot be default DateTime.");
            }
            else if (schedule.NextRunTime.Value.Kind != DateTimeKind.Utc)
            {
                problems.Add("NextRunTime must be in UTC.");
            }
            else if (schedule.NextRunTime.Value < DateTime.UtcNow)
            {
                problems.Add("NextRunTime cannot be in the past.");
            }
        }

        // Validate notification emails if present
        if (!string.IsNullOrWhiteSpace(schedule.NotificationEmails))
        {
            var emails = schedule.NotificationEmails.Split(new[] { ',', ';', ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var email in emails)
            {
                if (!IsValidEmail(email.Trim()))
                {
                    problems.Add($"Notification email '{email.Trim()}' is not a valid email address.");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BackupSchedule"/> instance is valid.
    /// </summary>
    /// <param name="schedule">The backup schedule to check.</param>
    /// <returns>True if the backup schedule is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedule"/> is null.</exception>
    public static bool IsValid(this BackupSchedule? schedule)
    {
        return schedule is not null && !schedule.Validate().Any();
    }

    /// <summary>
    /// Ensures that the specified <see cref="BackupSchedule"/> instance is valid, throwing an exception
    /// with detailed validation messages if it is not.
    /// </summary>
    /// <param name="schedule">The backup schedule to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="schedule"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the schedule is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this BackupSchedule? schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var problems = schedule.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            "BackupSchedule is not valid. " +
            string.Join(" ", problems),
            nameof(schedule),
            new InvalidOperationException(string.Join("\n", problems)));
    }

    private static bool IsHexadecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var c in value)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidCronExpression(string cronExpression)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
        {
            return false;
        }

        try
        {
            // Try to parse the cron expression using Cronos library
            _ = Cronos.CronExpression.Parse(cronExpression, Cronos.CronFormat.Standard);
            return true;
        }
        catch (Cronos.CronFormatException)
        {
            return false;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254)
        {
            return false;
        }

        try
        {
            // Simple email validation
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex >= email.Length - 1)
            {
                return false;
            }

            var localPart = email[..atIndex];
            var domainPart = email[(atIndex + 1)..];

            if (localPart.Length > 64 || domainPart.Length > 255)
            {
                return false;
            }

            // Check for valid characters (simplified)
            foreach (var c in localPart + domainPart)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_' && c != '@')
                {
                    return false;
                }
            }

            return domainPart.Contains('.') && !domainPart.StartsWith('.') && !domainPart.EndsWith('.');
        }
        catch
        {
            return false;
        }
    }
}