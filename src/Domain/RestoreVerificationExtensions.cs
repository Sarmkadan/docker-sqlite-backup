using System;
using System.Globalization;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="RestoreVerification"/>.
/// </summary>
public static class RestoreVerificationExtensions
{
    /// <summary>
    /// Determines if the restore verification was successful and did not have any integrity check errors.
    /// </summary>
    /// <param name="restoreVerification">The restore verification to check.</param>
    /// <returns>True if the restore verification was successful and did not have any integrity check errors; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="restoreVerification"/> is null.</exception>
    public static bool IsValidRestore(this RestoreVerification restoreVerification)
    {
        ArgumentNullException.ThrowIfNull(restoreVerification);

        return restoreVerification.IsSuccessful && string.IsNullOrEmpty(restoreVerification.IntegrityCheckErrors);
    }

    /// <summary>
    /// Gets a human-readable status message for the restore verification.
    /// </summary>
    /// <param name="restoreVerification">The restore verification to get the status message for.</param>
    /// <returns>A human-readable status message for the restore verification.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="restoreVerification"/> is null.</exception>
    public static string GetStatusMessage(this RestoreVerification restoreVerification)
    {
        ArgumentNullException.ThrowIfNull(restoreVerification);

        return string.IsNullOrEmpty(restoreVerification.ErrorMessage)
            ? restoreVerification.StatusMessage
            : $"{restoreVerification.StatusMessage} - Error: {restoreVerification.ErrorMessage}";
    }

    /// <summary>
    /// Formats the database size in bytes to a human-readable string.
    /// </summary>
    /// <param name="restoreVerification">The restore verification to format the database size for.</param>
    /// <returns>A human-readable string representing the database size.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="restoreVerification"/> is null.</exception>
    public static string GetFormattedDatabaseSize(this RestoreVerification restoreVerification)
    {
        ArgumentNullException.ThrowIfNull(restoreVerification);

        return FormatBytes(restoreVerification.DatabaseSizeBytes);
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }

        if (bytes < 1024L * 1024L)
        {
            return $"{(bytes / 1024.0):F2} KB";
        }

        if (bytes < 1024L * 1024L * 1024L)
        {
            return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
        }

        return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
    }
}