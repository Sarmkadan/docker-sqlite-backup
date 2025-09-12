#nullable enable

using System;
using System.Linq;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Extension methods for <see cref="AppSettings"/> that provide convenient functionality
/// for common operations like validation, configuration checks, and default value handling.
/// </summary>
public static class AppSettingsExtensions
{
    /// <summary>
    /// Determines whether backup verification is enabled based on the setting and provided override.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if verification should be enabled; otherwise, false.</returns>
    public static bool IsVerificationEnabled(this AppSettings settings, bool? overrideValue = null)
    {
        return overrideValue ?? settings.EnableVerificationByDefault;
    }

    /// <summary>
    /// Determines whether S3 storage is enabled based on the setting and provided override.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if S3 storage should be enabled; otherwise, false.</returns>
    public static bool IsS3StorageEnabled(this AppSettings settings, bool? overrideValue = null)
    {
        return overrideValue ?? settings.EnableS3StorageByDefault;
    }

    /// <summary>
    /// Gets the notification emails as a comma-separated string.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <returns>A comma-separated string of notification emails, or null if no emails are configured.</returns>
    public static string? GetNotificationEmailsAsString(this AppSettings settings)
    {
        return settings.NotificationEmails?.Length > 0
            ? string.Join(", ", settings.NotificationEmails)
            : null;
    }

    /// <summary>
    /// Determines whether encryption is properly configured.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <returns>True if encryption is enabled and a key is provided; otherwise, false.</returns>
    public static bool IsEncryptionConfigured(this AppSettings settings)
    {
        return settings.EnableEncryption && !string.IsNullOrWhiteSpace(settings.EncryptionKey);
    }

    /// <summary>
    /// Gets the effective compression setting, considering the default and any override.
    /// </summary>
    /// <param name="settings">The application settings.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if compression should be enabled; otherwise, false.</returns>
    public static bool ShouldCompressBackups(this AppSettings settings, bool? overrideValue = null)
    {
        return overrideValue ?? settings.CompressBackups;
    }

    /// <summary>
    /// Validates that the AppSettings object has all required values properly configured.
    /// </summary>
    /// <param name="settings">The application settings to validate.</param>
    /// <param name="throwOnError">Whether to throw an exception if validation fails.</param>
    /// <returns>True if all validations pass; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if settings is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation fails and throwOnError is true.</exception>
    public static bool Validate(this AppSettings settings, bool throwOnError = false)
    {
        if (settings == null)
        {
            if (throwOnError)
            {
                throw new ArgumentNullException(nameof(settings), "AppSettings cannot be null.");
            }
            return false;
        }

        bool isValid = true;

        // Validate encryption configuration if enabled
        if (settings.EnableEncryption && string.IsNullOrWhiteSpace(settings.EncryptionKey))
        {
            isValid = false;
            if (throwOnError)
            {
                throw new InvalidOperationException(
                    "Encryption is enabled but no EncryptionKey is provided. Either set EnableEncryption to false or provide a valid EncryptionKey.");
            }
        }

        // Validate notification emails array
        if (settings.NotificationEmails == null)
        {
            isValid = false;
            if (throwOnError)
            {
                throw new InvalidOperationException("NotificationEmails cannot be null.");
            }
        }

        return isValid;
    }
}