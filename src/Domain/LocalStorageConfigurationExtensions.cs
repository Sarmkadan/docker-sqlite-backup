namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides extension methods for <see cref="LocalStorageConfiguration"/>.
/// </summary>
public static class LocalStorageConfigurationExtensions
{
    /// <summary>
    /// Gets the full path to a backup file.
    /// </summary>
    /// <param name="configuration">The local storage configuration.</param>
    /// <param name="scheduleName">The name of the schedule.</param>
    /// <param name="backupDate">The date of the backup.</param>
    /// <returns>The full path to the backup file.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="scheduleName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="scheduleName"/> is empty or whitespace.</exception>
    public static string GetFullBackupPath(this LocalStorageConfiguration configuration, string scheduleName, DateTime backupDate)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(scheduleName);

        return configuration.GetBackupPath(scheduleName, backupDate);
    }

    /// <summary>
    /// Checks if the local storage configuration has sufficient free space.
    /// </summary>
    /// <param name="configuration">The local storage configuration.</param>
    /// <param name="requiredFreeSpaceBytes">The required free space in bytes. Must be greater than zero.</param>
    /// <returns>True if there is sufficient free space; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="requiredFreeSpaceBytes"/> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the base directory does not exist and cannot be accessed.</exception>
    public static bool HasSufficientFreeSpace(this LocalStorageConfiguration configuration, long requiredFreeSpaceBytes)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(requiredFreeSpaceBytes, 0L);

        if (string.IsNullOrWhiteSpace(configuration.BaseDirectory))
        {
            return false;
        }

        try
        {
            var driveInfo = new DriveInfo(configuration.BaseDirectory);
            return driveInfo.AvailableFreeSpace >= requiredFreeSpaceBytes;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
