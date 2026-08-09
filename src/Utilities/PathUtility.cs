#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for file system path operations. Handles path normalization,
/// validation, and manipulation in a platform-agnostic way.
/// </summary>
public static class PathUtility
{
    /// <summary>
    /// Sanitizes a file name by removing or replacing invalid characters.
    /// This ensures the file name can be safely used on all platforms.
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(fileName.Split(invalidChars));

        // Remove leading/trailing dots and spaces (invalid on Windows)
        sanitized = sanitized.Trim(new[] { '.', ' ' });

        return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
    }

    /// <summary>
    /// Normalizes a path to use forward slashes and removes redundant separators.
    /// </summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        return Path.GetFullPath(path).Replace('\\', '/');
    }

    /// <summary>
    /// Checks if a path is absolute.
    /// </summary>
    public static bool IsAbsolute(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return Path.IsPathRooted(path);
    }

    /// <summary>
    /// Gets the relative path from base to target.
    /// </summary>
    public static string GetRelativePath(string basePath, string targetPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(basePath);
        ArgumentException.ThrowIfNullOrEmpty(targetPath);

        try
        {
            return Path.GetRelativePath(basePath, targetPath);
        }
        catch
        {
            return targetPath;
        }
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(directoryPath);

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    /// <summary>
    /// Safely combines multiple path segments, handling edge cases.
    /// </summary>
    public static string CombinePath(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        if (segments.Length == 0)
            throw new ArgumentException("At least one segment is required", nameof(segments));

        var validSegments = segments.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        if (validSegments.Length == 0)
            throw new ArgumentException("All segments were empty", nameof(segments));

        return Path.Combine(validSegments);
    }

    /// <summary>
    /// Gets the size of a file in bytes. Returns -1 if file doesn't exist.
    /// </summary>
    public static long GetFileSize(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        try
        {
            var info = new FileInfo(filePath);
            return info.Exists ? info.Length : -1;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Validates that a path points to a valid file that exists.
    /// </summary>
    public static bool IsValidFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        try
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && fileInfo.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Generates a backup file name with timestamp.
    /// </summary>
    public static string GenerateBackupFileName(string baseName, DateTime? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(baseName);

        var time = timestamp ?? DateTime.UtcNow;
        var sanitized = SanitizeFileName(baseName);
        return $"{sanitized}_backup_{time:yyyy-MM-dd_HH-mm-ss}.sqlite";
    }
}