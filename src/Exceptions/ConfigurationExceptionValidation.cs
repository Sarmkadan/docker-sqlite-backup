#nullable enable

using System;

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="ConfigurationException"/> instances.
/// </summary>
public static class ConfigurationExceptionValidation
{
    /// <summary>
    /// Validates a <see cref="ConfigurationException"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The configuration exception to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the exception is valid.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this ConfigurationException? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var problems = new List<string>();

        if (string.IsNullOrEmpty(value.ConfigurationKey))
        {
            problems.Add("ConfigurationKey must not be null or empty.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="ConfigurationException"/> instance is valid.
    /// </summary>
    /// <param name="value">The configuration exception to check.</param>
    /// <returns>True if the exception is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this ConfigurationException? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return !string.IsNullOrEmpty(value.ConfigurationKey);
    }

    /// <summary>
    /// Ensures that a <see cref="ConfigurationException"/> instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The configuration exception to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The exception is invalid.</exception>
    public static void EnsureValid(this ConfigurationException? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (string.IsNullOrEmpty(value.ConfigurationKey))
        {
            throw new ArgumentException(
                "ConfigurationException is invalid: ConfigurationKey must not be null or empty.",
                nameof(value));
        }
    }
}