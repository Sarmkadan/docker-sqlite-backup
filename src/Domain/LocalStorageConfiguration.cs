// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Split(invalidChars));
    }
}
