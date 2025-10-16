#nullable enable

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides validation helpers for <see cref="RotationPolicy"/> instances.
/// </summary>
public static class RotationPolicyValidation
{
    /// <summary>
    /// Validates the rotation policy and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The rotation policy to validate.</param>
    /// <returns>A read-only list of validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this RotationPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate required Guid fields
        if (value.Id == Guid.Empty)
        {
            problems.Add("Id must be a non-empty GUID.");
        }

        if (value.ScheduleId == Guid.Empty)
        {
            problems.Add("ScheduleId must be a non-empty GUID.");
        }

        // Validate strategy (should be a valid enum value)
        if (!Enum.IsDefined(typeof(Constants.RotationStrategy), value.Strategy))
        {
            problems.Add("Strategy must be a valid RotationStrategy enum value.");
        }

        // Validate MaxBackupCount
        if (value.MaxBackupCount < 0)
        {
            problems.Add("MaxBackupCount must be non-negative.");
        }

        // Validate MaxAgeDays
        if (value.MaxAgeDays < 1)
        {
            problems.Add("MaxAgeDays must be at least 1.");
        }

        // Validate MinimumBackupCount
        if (value.MinimumBackupCount < 0)
        {
            problems.Add("MinimumBackupCount must be non-negative.");
        }

        // Validate MinimumBackupCount against MaxBackupCount
        if (value.MaxBackupCount > 0 && value.MinimumBackupCount > value.MaxBackupCount)
        {
            problems.Add("MinimumBackupCount cannot exceed MaxBackupCount when MaxBackupCount is positive.");
        }

        // Validate CreatedAt (should not be default/MinValue)
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt must be set to a valid DateTime.");
        }

        // Validate LastModifiedAt (should not be default/MinValue)
        if (value.LastModifiedAt == default)
        {
            problems.Add("LastModifiedAt must be set to a valid DateTime.");
        }

        // Validate date relationships
        if (value.LastRotatedAt.HasValue)
        {
            if (value.LastRotatedAt.Value > value.LastModifiedAt)
            {
                problems.Add("LastRotatedAt cannot be in the future relative to LastModifiedAt.");
            }

            if (value.LastRotatedAt.Value < value.CreatedAt)
            {
                problems.Add("LastRotatedAt cannot be before CreatedAt.");
            }
        }

        // Validate boolean flags are reasonable (no specific validation needed beyond being boolean)
        // These are always valid as they're just flags

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the rotation policy is valid.
    /// </summary>
    /// <param name="value">The rotation policy to check.</param>
    /// <returns>True if the policy is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this RotationPolicy? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures the rotation policy is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The rotation policy to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the policy is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this RotationPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"RotationPolicy is invalid. Problems: {string.Join(" ", problems)}");
        }
    }
}