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
    /// <param name="settings">The application settings. Cannot be <see langword="null"/>.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if verification should be enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool IsVerificationEnabled(this AppSettings settings, bool? overrideValue = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return overrideValue ?? settings.EnableVerificationByDefault;
    }

    /// <summary>
    /// Determines whether S3 storage is enabled based on the setting and provided override.
    /// </summary>
    /// <param name="settings">The application settings. Cannot be <see langword="null"/>.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if S3 storage should be enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool IsS3StorageEnabled(this AppSettings settings, bool? overrideValue = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return overrideValue ?? settings.EnableS3StorageByDefault;
    }

    /// <summary>
    /// Gets the notification emails as a comma-separated string.
    /// </summary>
    /// <param name="settings">The application settings. Cannot be <see langword="null"/>.</param>
    /// <returns>A comma-separated string of notification emails, or <see langword="null"/> if no emails are configured.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static string? GetNotificationEmailsAsString(this AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.NotificationEmails?.Length > 0
            ? string.Join(", ", settings.NotificationEmails)
            : null;
    }

    /// <summary>
    /// Determines whether encryption is properly configured.
    /// </summary>
    /// <param name="settings">The application settings. Cannot be <see langword="null"/>.</param>
    /// <returns>True if encryption is enabled and a key is provided; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool IsEncryptionConfigured(this AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.EnableEncryption && !string.IsNullOrWhiteSpace(settings.EncryptionKey);
    }

    /// <summary>
    /// Gets the effective compression setting, considering the default and any override.
    /// </summary>
    /// <param name="settings">The application settings. Cannot be <see langword="null"/>.</param>
    /// <param name="overrideValue">Optional override value. If provided, this value takes precedence.</param>
    /// <returns>True if compression should be enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    public static bool ShouldCompressBackups(this AppSettings settings, bool? overrideValue = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return overrideValue ?? settings.CompressBackups;
    }

    /// <summary>
    /// Validates that the AppSettings object has all required values properly configured.
    /// </summary>
    /// <param name="settings">The application settings to validate. Cannot be <see langword="null"/>.</param>
    /// <param name="throwOnError">Whether to throw an exception if validation fails.</param>
    /// <returns>True if all validations pass; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if validation fails and <paramref name="throwOnError"/> is true.</exception>
    public static bool Validate(this AppSettings settings, bool throwOnError = false)
    {
        ArgumentNullException.ThrowIfNull(settings);

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