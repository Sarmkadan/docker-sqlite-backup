using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
/// <summary>
/// Provides benchmark tests for various checksum calculation utilities.
/// </summary>
public class ChecksumBenchmarks
{
    private string _tempFilePath = default!;
    private const int FileSize = 10 * 1024 * 1024; // 10 MB

    [GlobalSetup]
    /// <summary>
    /// Creates a temporary file with random data of size <see cref="FileSize"/> bytes and stores its path in <see cref="_tempFilePath"/>.
    /// </summary>
    public void Setup()
    {
        _tempFilePath = Path.GetTempFileName();
        var data = new byte[FileSize];
        Random.Shared.NextBytes(data);
        File.WriteAllBytes(_tempFilePath, data);
    }

    [GlobalCleanup]
    /// <summary>
    /// Deletes the temporary file created during setup if it exists.
    /// </summary>
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark]
    /// <summary>
    /// Calculates the SHA-256 hash of the temporary file asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the SHA-256 hash as a hexadecimal string.</returns>
    public async Task<string> CalculateSha256()
    {
        return await ChecksumUtility.CalculateFileSha256Async(_tempFilePath);
    }

    [Benchmark]
    /// <summary>
    /// Calculates the CRC32 checksum of the temporary file asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the CRC32 checksum as an unsigned integer.</returns>
    public async Task<uint> CalculateCrc32()
    {
        return await ChecksumUtility.CalculateFileCrc32Async(_tempFilePath);
    }

    [Benchmark]
    /// <summary>
    /// Generates a quick checksum string for the temporary file asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the quick checksum as a string.</returns>
    public async Task<string> GenerateQuickChecksum()
    {
        return await ChecksumUtility.GenerateQuickChecksumAsync(_tempFilePath);
    }
}
