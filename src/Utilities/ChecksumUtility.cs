#nullable enable
// Author: Vladyslav Zaiets

using System.Security.Cryptography;
using System.Text;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Utility methods for calculating checksums and hashes.
/// Provides secure hash generation for data integrity verification.
/// </summary>
public static class ChecksumUtility
{
    /// <summary>
    /// Calculates the SHA256 hash of a file.
    /// </summary>
    public static async Task<string> CalculateFileSha256Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => sha256.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Calculates the SHA256 hash of a string.
    /// </summary>
    public static string CalculateStringSha256(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Calculates the MD5 hash of a file (for legacy compatibility).
    /// </summary>
    public static async Task<string> CalculateFileMd5Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => md5.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Verifies that a file's hash matches the expected value.
    /// </summary>
    public static async Task<bool> VerifyFileSha256Async(string filePath, string expectedHash)
    {
        var actualHash = await CalculateFileSha256Async(filePath);
        return actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates a CRC32 checksum for quick integrity checking.
    /// </summary>
    public static async Task<uint> CalculateFileCrc32Async(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        const uint polynomial = 0xedb88320;
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
                crc = (crc >> 1) ^ ((crc & 1) == 1 ? polynomial : 0);
            table[i] = crc;
        }

        uint crc32 = 0xffffffff;
        using var stream = File.OpenRead(filePath);
        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < bytesRead; i++)
                crc32 = (crc32 >> 8) ^ table[(crc32 ^ buffer[i]) & 0xff];
        }

        return crc32 ^ 0xffffffff;
    }

    /// <summary>
    /// Generates a simple checksum based on file size and first/last bytes.
    /// Fast but less reliable than cryptographic hashes.
    /// </summary>
    public static string GenerateQuickChecksum(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var size = fileInfo.Length;

        var firstBytes = new byte[16];
        var lastBytes = new byte[16];

        using (var stream = File.OpenRead(filePath))
        {
            _ = stream.Read(firstBytes, 0, Math.Min(16, (int)size));

            if (size > 16)
            {
                stream.Seek(-16, SeekOrigin.End);
                _ = stream.Read(lastBytes, 0, 16);
            }
        }

        var combined = Encoding.UTF8.GetBytes(size.ToString())
            .Concat(firstBytes)
            .Concat(lastBytes)
            .ToArray();

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(combined);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..16];
    }

    /// <summary>
    /// Async wrapper around <see cref="GenerateQuickChecksum"/>. Returns a 16-character
    /// hex string derived from the file's size and boundary bytes.
    /// </summary>
    public static Task<string> GenerateQuickChecksumAsync(string filePath) =>
        Task.FromResult(GenerateQuickChecksum(filePath));

    /// <summary>
    /// Generates a checksum for a collection of values.
    /// </summary>
    public static string CalculateCollectionChecksum(params object[] values)
    {
        var combined = string.Join("|", values);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()[..32];
    }
}
