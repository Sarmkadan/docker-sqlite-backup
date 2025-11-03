#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace DockerSqliteBackup.Health;

/// <summary>
/// Provides validation helpers for <see cref="HealthCheckService"/> instances.
/// </summary>
public static class HealthCheckServiceValidation
{
    /// <summary>
    /// Validates a <see cref="HealthCheckService"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The health check service to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // HealthCheckService has no public properties to validate
        // It only has the PerformHealthCheckAsync method and internal state
        // No validation needed for the service itself

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="HealthCheckService"/> instance is valid.
    /// </summary>
    /// <param name="value">The health check service to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this HealthCheckService? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="HealthCheckService"/> instance is valid, throwing an <see cref="ArgumentException"/> if it is not.
    /// </summary>
    /// <param name="value">The health check service to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid.</exception>
    public static void EnsureValid(this HealthCheckService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"HealthCheckService is not valid. Problems: {string.Join("; ", problems)}");
    }
}
