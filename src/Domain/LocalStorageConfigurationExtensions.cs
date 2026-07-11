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
    public static string GetFullBackupPath(this LocalStorageConfiguration configuration, string scheduleName, DateTime backupDate)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(scheduleName);

        var basePath = configuration.GetBackupPath(scheduleName, backupDate);
        return basePath;
    }

    /// <summary>
    /// Checks if the local storage configuration has sufficient free space.
    /// </summary>
    /// <param name="configuration">The local storage configuration.</param>
    /// <param name="requiredFreeSpaceBytes">The required free space in bytes.</param>
    /// <returns>True if there is sufficient free space; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
    public static bool HasSufficientFreeSpace(this LocalStorageConfiguration configuration, long requiredFreeSpaceBytes)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var freeSpaceBytes = new DriveInfo(configuration.BaseDirectory).AvailableFreeSpace;
        return freeSpaceBytes >= requiredFreeSpaceBytes;
    }
}
