#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Provides validation helpers for <see cref="AppSettings"/> configuration.
/// </summary>
public static class AppSettingsValidation
{
    /// <summary>
    /// Validates the provided <see cref="AppSettings"/> instance.
    /// </summary>
    /// <param name="value">The settings to validate.</param>
    /// <returns>A list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AppSettings value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate NotificationEmails array
        ArgumentNullException.ThrowIfNull(value.NotificationEmails);

        foreach (var email in value.NotificationEmails)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                problems.Add("NotificationEmails contains null or whitespace entry.");
                break;
            }

            // Validate email format using DataAnnotations.EmailAddressAttribute for proper validation
            if (!new EmailAddressAttribute().IsValid(email))
            {
                problems.Add($"NotificationEmails contains invalid email format: '{email}'.");
            }
        }

        // Validate EncryptionKey when encryption is enabled
        if (value.EnableEncryption && string.IsNullOrWhiteSpace(value.EncryptionKey))
        {
            problems.Add("EncryptionKey must be provided when EnableEncryption is true.");
        }

        // Validate EncryptionKey format when provided
        if (!string.IsNullOrWhiteSpace(value.EncryptionKey))
        {
            try
            {
                // Base64 decode to validate it's a valid Base64 string
                // AES-256 key should be 32 bytes = 44 Base64 chars (with padding)
                var decoded = Convert.FromBase64String(value.EncryptionKey);
                if (decoded.Length != 32)
                {
                    problems.Add("EncryptionKey must be a Base64-encoded 32-byte (256-bit) AES key.");
                }
            }
            catch (FormatException)
            {
                problems.Add("EncryptionKey must be a valid Base64-encoded string.");
            }
            catch (OverflowException)
            {
                problems.Add("EncryptionKey is too large to be a valid 32-byte AES key.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the provided <see cref="AppSettings"/> instance is valid.
    /// </summary>
    /// <param name="value">The settings to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this AppSettings value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the provided <see cref="AppSettings"/> instance is valid.
    /// </summary>
    /// <param name="value">The settings to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this AppSettings value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"AppSettings validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", problems)
                }");
        }
    }
}
