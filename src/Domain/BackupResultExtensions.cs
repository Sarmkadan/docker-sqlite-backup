namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="BackupResult"/>.
/// </summary>
public static class BackupResultExtensions
{
    /// <summary>
    /// Gets a human-readable status message for the backup result.
    /// </summary>
    /// <param name="backupResult">The backup result.</param>
    /// <returns>A human-readable status message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="backupResult"/> is <see langword="null"/>.</exception>
    public static string GetStatusMessage(this BackupResult backupResult)
    {
        ArgumentNullException.ThrowIfNull(backupResult);

        return backupResult.Status switch
        {
            0 => "Success",
            _ => "Failure"
        };
    }

    /// <summary>
    /// Gets the duration of the backup operation.
    /// </summary>
    /// <param name="backupResult">The backup result.</param>
    /// <returns>The duration of the backup operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="backupResult"/> is <see langword="null"/>.</exception>
    public static TimeSpan GetDuration(this BackupResult backupResult)
    {
        ArgumentNullException.ThrowIfNull(backupResult);

        return backupResult.CompletedAt.HasValue
            ? backupResult.CompletedAt.Value - backupResult.StartedAt
            : TimeSpan.FromMilliseconds(backupResult.DurationMilliseconds);
    }

    /// <summary>
    /// Checks if the backup result has an error.
    /// </summary>
    /// <param name="backupResult">The backup result.</param>
    /// <returns><c>true</c> if the backup result has an error; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="backupResult"/> is <see langword="null"/>.</exception>
    public static bool HasError(this BackupResult backupResult)
    {
        ArgumentNullException.ThrowIfNull(backupResult);

        return !string.IsNullOrEmpty(backupResult.ErrorMessage) || !string.IsNullOrEmpty(backupResult.StackTrace);
    }

    /// <summary>
    /// Gets the number of bytes saved by compression (original size minus stored size).
    /// </summary>
    /// <param name="backupResult">The backup result.</param>
    /// <returns>
    /// The number of bytes saved, or <see langword="null"/> when the backup was not compressed
    /// (<see cref="BackupResult.OriginalFileSizeBytes"/> is not set). The value is negative if the compressed file is larger than the original.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="backupResult"/> is <see langword="null"/>.</exception>
    public static long? GetSpaceSavedBytes(this BackupResult backupResult)
    {
        ArgumentNullException.ThrowIfNull(backupResult);

        return backupResult.OriginalFileSizeBytes.HasValue
            ? backupResult.OriginalFileSizeBytes.Value - backupResult.BackupFileSizeBytes
            : null;
    }
}
