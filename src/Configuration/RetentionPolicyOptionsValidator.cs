#nullable enable
// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using Microsoft.Extensions.Options;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Validates the <see cref="RetentionPolicy"/> bound from the <c>RetentionPolicy</c> configuration section.
/// Every keep-count must be non-negative, and at least one tier must be enabled: a policy where all tiers
/// are zero would classify every backup as expired. Runs at host startup, so an invalid policy fails fast
/// with an <see cref="OptionsValidationException"/> naming the offending property.
/// </summary>
public sealed class RetentionPolicyOptionsValidator : IValidateOptions<RetentionPolicy>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RetentionPolicy options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        RequireNonNegative(nameof(RetentionPolicy.DailyRetentionDays), options.DailyRetentionDays, failures);
        RequireNonNegative(nameof(RetentionPolicy.WeeklyRetentionWeeks), options.WeeklyRetentionWeeks, failures);
        RequireNonNegative(nameof(RetentionPolicy.MonthlyRetentionMonths), options.MonthlyRetentionMonths, failures);
        RequireNonNegative(nameof(RetentionPolicy.MaxTotalStorageBytes), options.MaxTotalStorageBytes, failures);

        if (options.DailyRetentionDays <= 0 && options.WeeklyRetentionWeeks <= 0 && options.MonthlyRetentionMonths <= 0)
        {
            failures.Add(
                $"At least one of {nameof(RetentionPolicy.DailyRetentionDays)}, {nameof(RetentionPolicy.WeeklyRetentionWeeks)} " +
                $"or {nameof(RetentionPolicy.MonthlyRetentionMonths)} must be greater than zero; " +
                "otherwise every backup would be expired and deleted immediately.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireNonNegative(string property, long value, List<string> failures)
    {
        if (value < 0)
        {
            failures.Add($"{property} must not be negative, but was {value}.");
        }
    }
}
