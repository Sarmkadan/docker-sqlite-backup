#nullable enable
// Author: Vladyslav Zaiets

using Microsoft.Extensions.Options;

namespace DockerSqliteBackup.Configuration;

/// <summary>
/// Validates the startup-critical <see cref="AppSettings"/> values (schedule check interval and
/// local backup directory). Runs when the host starts, so an invalid configuration fails fast with
/// an <see cref="OptionsValidationException"/> naming the offending property.
/// </summary>
public sealed class AppSettingsOptionsValidator : IValidateOptions<AppSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.ScheduleCheckIntervalSeconds <= 0)
        {
            failures.Add(
                $"{nameof(AppSettings.ScheduleCheckIntervalSeconds)} must be a positive number of seconds, but was {options.ScheduleCheckIntervalSeconds}.");
        }

        ValidateBackupDirectory(options.LocalStoragePath, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateBackupDirectory(string? directory, List<string> failures)
    {
        const string property = nameof(AppSettings.LocalStoragePath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            failures.Add($"{property} cannot be null or whitespace.");
            return;
        }

        if (!Directory.Exists(directory))
        {
            failures.Add($"{property} directory does not exist: '{directory}'.");
            return;
        }

        if (!IsDirectoryWritable(directory))
        {
            failures.Add($"{property} directory is not writable: '{directory}'.");
        }
    }

    private static bool IsDirectoryWritable(string directory)
    {
        // Probe by creating a temporary file that is deleted as soon as it is closed.
        var probePath = Path.Combine(directory, $".write-probe-{Guid.NewGuid():N}");

        try
        {
            File.Create(probePath, bufferSize: 1, FileOptions.DeleteOnClose).Dispose();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
