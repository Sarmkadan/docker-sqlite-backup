#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Configuration for local file system storage backend.
/// </summary>
public class LocalStorageConfiguration : StorageConfiguration
{
    /// <summary>Gets the storage type for local storage.</summary>
    public override int StorageType => (int)Constants.StorageType.Local;

    /// <summary>Gets or sets the base directory where backups will be stored.</summary>
    public string BaseDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to create subdirectories by schedule name.</summary>
    public bool CreateSubdirectoriesBySchedule { get; set; } = true;

    /// <summary>Gets or sets the file permissions (Unix-style) for backup files.</summary>
    public string FilePermissions { get; set; } = "0640";

    /// <summary>Gets or sets whether to compress backup files using GZIP.</summary>
    public bool CompressBackups { get; set; } = false;

    /// <summary>Gets or sets the minimum free space required (in bytes) before storing backups.</summary>
    public long MinimumFreeSpaceBytes { get; set; } = 1073741824; // 1 GB

    /// <summary>Gets or sets whether to preserve the original file modification time.</summary>
    public bool PreserveFileTimestamp { get; set; } = true;

    /// <summary>
    /// Validates the local storage configuration.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the name and base directory are set, the file permissions are valid Unix octal
    /// and the minimum free space is non-negative; otherwise <c>false</c>.
    /// </returns>
    public override bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (string.IsNullOrWhiteSpace(BaseDirectory))
            return false;

        if (!IsValidUnixPermissions(FilePermissions))
            return false;

        if (MinimumFreeSpaceBytes < 0)
            return false;

        return true;
    }

    /// <summary>
    /// Tests the local storage by checking directory access and free space.
    /// </summary>
    /// <remarks>
    /// Creates <see cref="BaseDirectory"/> if it does not exist, then writes and deletes a temporary file.
    /// Any exception is caught and reported as a failure.
    /// </remarks>
    /// <returns>
    /// <c>true</c> if the directory is writable and has at least <see cref="MinimumFreeSpaceBytes"/>
    /// available; otherwise <c>false</c>.
    /// </returns>
    public override async Task<bool> TestConnectionAsync()
    {
        try
        {
            // Check if directory exists or can be created
            var dir = new DirectoryInfo(BaseDirectory);
            if (!dir.Exists)
            {
                Directory.CreateDirectory(BaseDirectory);
            }

            // Check write access by creating a temporary file
            var testFile = Path.Combine(BaseDirectory, $".test_{Guid.NewGuid()}");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);

            // Check free space
            var driveInfo = new DriveInfo(Path.GetPathRoot(BaseDirectory) ?? "/");
            return driveInfo.AvailableFreeSpace >= MinimumFreeSpaceBytes;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the full path for a backup file based on the schedule and timestamp.
    /// </summary>
    /// <param name="scheduleName">The backup schedule name, used as the subdirectory and in the file name.</param>
    /// <param name="timestamp">The backup timestamp, formatted as <c>yyyy-MM-dd_HH-mm-ss</c> in the file name.</param>
    /// <returns>
    /// The full path under <see cref="BaseDirectory"/>. Appends <c>.gz</c> when <see cref="CompressBackups"/> is set.
    /// </returns>
    public string GetBackupPath(string scheduleName, DateTime timestamp)
    {
        var path = BaseDirectory;

        if (CreateSubdirectoriesBySchedule)
        {
            path = Path.Combine(path, SanitizeFileName(scheduleName));
        }

        var fileName = $"backup_{scheduleName}_{timestamp:yyyy-MM-dd_HH-mm-ss}.sqlite";
        if (CompressBackups)
        {
            fileName += ".gz";
        }

        return Path.Combine(path, fileName);
    }

    /// <summary>
    /// Validates Unix-style file permissions.
    /// </summary>
    /// <param name="permissions">The permission string, e.g. <c>0640</c> or <c>640</c>.</param>
    /// <returns>
    /// <c>true</c> if every character is an octal digit (0-7) and the value has at least four digits
    /// after a leading zero is added when missing; otherwise <c>false</c>.
    /// </returns>
    private static bool IsValidUnixPermissions(string permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
            return false;

        var normalized = permissions.StartsWith("0") ? permissions : "0" + permissions;
        return normalized.Length >= 4 && normalized.All(c => c >= '0' && c <= '7');
    }

    /// <summary>
    /// Sanitizes a string to be used as a file/folder name.
    /// </summary>
    /// <param name="fileName">The raw name to sanitize.</param>
    /// <returns>The name with all characters invalid for file names removed.</returns>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Split(invalidChars));
    }

    /// <summary>
    /// Returns a concise string representation of the configuration.
    /// </summary>
    /// <returns>A string listing the key configuration values.</returns>
    public override string ToString()
    {
        return $"LocalStorageConfiguration {{ BaseDirectory = {BaseDirectory}, CreateSubdirectoriesBySchedule = {CreateSubdirectoriesBySchedule}, FilePermissions = {FilePermissions}, CompressBackups = {CompressBackups}, MinimumFreeSpaceBytes = {MinimumFreeSpaceBytes}, PreserveFileTimestamp = {PreserveFileTimestamp} }}";
    }
}
