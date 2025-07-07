// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for file operations.
/// </summary>
public static class FileUtilities
{
    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    /// <param name="bytes">Size in bytes</param>
    /// <returns>Formatted size string (e.g., "1.5 MB")</returns>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Formats a time duration to a human-readable string.
    /// </summary>
    /// <param name="duration">TimeSpan duration</param>
    /// <returns>Formatted duration string (e.g., "1h 30m 45s")</returns>
    public static string FormatDuration(TimeSpan duration)
    {
        var parts = new List<string>();

        if (duration.Days > 0)
            parts.Add($"{duration.Days}d");

        if (duration.Hours > 0)
            parts.Add($"{duration.Hours}h");

        if (duration.Minutes > 0)
            parts.Add($"{duration.Minutes}m");

        if (duration.Seconds > 0 || parts.Count == 0)
            parts.Add($"{duration.Seconds}s");

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Generates a unique temporary file path.
    /// </summary>
    /// <param name="prefix">Prefix for the temporary file</param>
    /// <param name="extension">File extension</param>
    /// <returns>Full path to temporary file</returns>
    public static string GetTemporaryFilePath(string prefix = "tmp", string extension = "")
    {
        return Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}{extension}");
    }

    /// <summary>
    /// Safely deletes a file, ignoring errors if it doesn't exist.
    /// </summary>
    /// <param name="filePath">Path to the file to delete</param>
    /// <returns>True if file was deleted, false otherwise</returns>
    public static bool SafeDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <returns>True if directory exists or was created, false otherwise</returns>
    public static bool EnsureDirectoryExists(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Calculates the total size of a directory and its contents.
    /// </summary>
    /// <param name="directoryPath">Path to the directory</param>
    /// <returns>Total size in bytes</returns>
    public static long GetDirectorySize(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var directoryInfo = new DirectoryInfo(directoryPath);
        return directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(f => f.Length);
    }

    /// <summary>
    /// Validates that a path is a valid database file path.
    /// </summary>
    /// <param name="filePath">Path to validate</param>
    /// <returns>True if path is valid for a database file</returns>
    public static bool IsValidDatabasePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            var fileName = Path.GetFileName(filePath);
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            // Check for invalid characters
            var invalidChars = Path.GetInvalidPathChars();
            return !fileName.Any(c => invalidChars.Contains(c));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the relative path from a base directory to a target file.
    /// </summary>
    /// <param name="fromPath">Base directory path</param>
    /// <param name="toPath">Target file path</param>
    /// <returns>Relative path or full path if relative path cannot be computed</returns>
    public static string GetRelativePath(string fromPath, string toPath)
    {
        try
        {
            var fromUri = new Uri(Path.GetFullPath(fromPath) + Path.DirectorySeparatorChar);
            var toUri = new Uri(Path.GetFullPath(toPath));

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
        catch
        {
            return toPath;
        }
    }

    /// <summary>
    /// Converts a file size string to bytes.
    /// </summary>
    /// <param name="sizeString">Size string (e.g., "1.5 GB", "512 MB")</param>
    /// <returns>Size in bytes, or 0 if parsing fails</returns>
    public static long ParseFileSize(string sizeString)
    {
        if (string.IsNullOrWhiteSpace(sizeString))
            return 0;

        sizeString = sizeString.Trim().ToUpperInvariant();
        var multipliers = new Dictionary<string, long>
        {
            { "B", 1 },
            { "KB", 1024 },
            { "MB", 1024 * 1024 },
            { "GB", 1024 * 1024 * 1024 },
            { "TB", 1024L * 1024 * 1024 * 1024 }
        };

        foreach (var mult in multipliers)
        {
            if (sizeString.EndsWith(mult.Key))
            {
                var numberPart = sizeString[..^mult.Key.Length].Trim();
                if (double.TryParse(numberPart, out var number))
                {
                    return (long)(number * mult.Value);
                }
            }
        }

        return 0;
    }
}
