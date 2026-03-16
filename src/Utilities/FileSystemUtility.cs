#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for file system operations. Handles safe file operations,
/// directory management, and file I/O with error handling.
/// </summary>
public static class FileSystemUtility
{
    /// <summary>
    /// Safely copies a file with retry logic for locked files.
    /// </summary>
    public static async Task SafeCopyFileAsync(string sourceFile, string destinationFile, int maxRetries = 3)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException($"Source file not found: {sourceFile}");

        var directory = Path.GetDirectoryName(destinationFile);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory!);

        var lastException = default(Exception);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                File.Copy(sourceFile, destinationFile, overwrite: true);
                return;
            }
            catch (IOException ex)
            {
                lastException = ex;
                if (attempt < maxRetries - 1)
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)));
            }
        }

        throw new IOException($"Failed to copy file after {maxRetries} attempts: {sourceFile}", lastException);
    }

    /// <summary>
    /// Safely deletes a file, ignoring if it doesn't exist.
    /// </summary>
    public static void SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            // Log but don't throw - file may be in use or already deleted
            System.Diagnostics.Debug.WriteLine($"Failed to delete file {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all files in a directory matching a pattern, with exception handling.
    /// </summary>
    public static IEnumerable<string> GetFilesWithPattern(string directory, string searchPattern)
    {
        try
        {
            if (!Directory.Exists(directory))
                return [];

            return Directory.GetFiles(directory, searchPattern);
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// Calculates the total size of a directory.
    /// </summary>
    public static long CalculateDirectorySize(string directory)
    {
        try
        {
            var dirInfo = new DirectoryInfo(directory);
            if (!dirInfo.Exists)
                return 0;

            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Deletes a directory and all its contents.
    /// </summary>
    public static async Task DeleteDirectoryAsync(string directory, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(directory))
                return;

            if (recursive)
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    File.Delete(file);
                    await Task.Delay(10); // Small delay to avoid file lock issues
                }

                foreach (var subdir in Directory.GetDirectories(directory))
                {
                    await DeleteDirectoryAsync(subdir, true);
                }
            }

            Directory.Delete(directory);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to delete directory: {directory}", ex);
        }
    }

    /// <summary>
    /// Gets the free disk space for a given path.
    /// </summary>
    public static long GetAvailableDiskSpace(string path = "/")
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => path.StartsWith(d.RootDirectory.FullName));
            return drive?.AvailableFreeSpace ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if a file is in use by another process.
    /// </summary>
    public static bool IsFileInUse(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    /// <summary>
    /// Recursively copies a directory to a destination.
    /// </summary>
    public static void CopyDirectory(string sourceDir, string destDir, bool recursive = true)
    {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");

        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        foreach (var file in dir.GetFiles())
        {
            file.CopyTo(Path.Combine(destDir, file.Name), overwrite: true);
        }

        if (recursive)
        {
            foreach (var subdir in dir.GetDirectories())
            {
                CopyDirectory(subdir.FullName, Path.Combine(destDir, subdir.Name), true);
            }
        }
    }
}
