// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Models;
using DockerSqliteBackup.Exceptions;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Validates all configuration for backup jobs and service settings.
/// </summary>
public class ConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;

    public ConfigurationValidationService(ILogger<ConfigurationValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates the entire backup service configuration.
    /// </summary>
    public ValidationResult ValidateServiceConfiguration(BackupServiceConfiguration config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var result = new ValidationResult();
        var errors = config.Validate();

        if (errors.Count > 0)
        {
            result.IsValid = false;
            result.Errors.AddRange(errors);
            return result;
        }

        // Validate metadata database path
        var metadataDir = Path.GetDirectoryName(config.MetadataDatabasePath);
        if (!string.IsNullOrEmpty(metadataDir) && !Directory.Exists(metadataDir))
        {
            try
            {
                Directory.CreateDirectory(metadataDir);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Cannot create metadata database directory: {ex.Message}");
            }
        }

        // Validate local backup path
        if (!Directory.Exists(config.LocalBackupBasePath))
        {
            try
            {
                Directory.CreateDirectory(config.LocalBackupBasePath);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Cannot create local backup directory: {ex.Message}");
            }
        }

        // Validate S3 configuration if present
        if (config.S3Config != null && !ValidateS3Config(config.S3Config, result))
        {
            result.IsValid = false;
        }

        // Validate notification configuration if present
        if (config.Notifications != null && !ValidateNotificationConfig(config.Notifications, result))
        {
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// Validates a backup job configuration.
    /// </summary>
    public ValidationResult ValidateBackupJob(BackupJob job)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        var result = new ValidationResult();
        var errors = job.Validate();

        if (errors.Count > 0)
        {
            result.IsValid = false;
            result.Errors.AddRange(errors);
            return result;
        }

        _logger.LogInformation("Backup job {JobId} configuration is valid", job.Id);
        return result;
    }

    /// <summary>
    /// Validates a storage provider configuration.
    /// </summary>
    public ValidationResult ValidateStorageProvider(StorageProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));

        var result = new ValidationResult();
        var errors = provider.Validate();

        if (errors.Count > 0)
        {
            result.IsValid = false;
            result.Errors.AddRange(errors);
            return result;
        }

        return result;
    }

    /// <summary>
    /// Validates a backup schedule configuration.
    /// </summary>
    public ValidationResult ValidateSchedule(BackupSchedule schedule)
    {
        if (schedule == null)
            throw new ArgumentNullException(nameof(schedule));

        var result = new ValidationResult();
        var errors = schedule.Validate();

        if (errors.Count > 0)
        {
            result.IsValid = false;
            result.Errors.AddRange(errors);
            return result;
        }

        return result;
    }

    /// <summary>
    /// Validates S3 configuration.
    /// </summary>
    private bool ValidateS3Config(S3Configuration config, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(config.BucketName))
        {
            result.Errors.Add("S3 bucket name is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.Region))
        {
            result.Errors.Add("S3 region is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.AccessKeyId))
        {
            result.Errors.Add("S3 access key ID is required");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.SecretAccessKey))
        {
            result.Errors.Add("S3 secret access key is required");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates notification configuration.
    /// </summary>
    private bool ValidateNotificationConfig(NotificationConfiguration config, ValidationResult result)
    {
        if (!config.EnableEmailNotifications)
            return true; // Notifications are disabled, no need to validate

        if (string.IsNullOrWhiteSpace(config.SmtpHost))
        {
            result.Errors.Add("SMTP host is required for email notifications");
            return false;
        }

        if (config.SmtpPort < 1 || config.SmtpPort > 65535)
        {
            result.Errors.Add("SMTP port must be between 1 and 65535");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.FromAddress))
        {
            result.Errors.Add("From email address is required for email notifications");
            return false;
        }

        if (!config.ToAddresses.Any())
        {
            result.Errors.Add("At least one recipient email address is required");
            return false;
        }

        // Validate email addresses
        foreach (var email in config.ToAddresses)
        {
            if (!IsValidEmail(email))
            {
                result.Errors.Add($"Invalid email address: {email}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Simple email validation.
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets a formatted error summary.
        /// </summary>
        public string GetErrorSummary()
        {
            if (Errors.Count == 0)
                return "No errors";

            return string.Join("\n", Errors.Select(e => $"  - {e}"));
        }

        /// <summary>
        /// Gets a formatted warning summary.
        /// </summary>
        public string GetWarningSummary()
        {
            if (Warnings.Count == 0)
                return "No warnings";

            return string.Join("\n", Warnings.Select(w => $"  - {w}"));
        }
    }
}
