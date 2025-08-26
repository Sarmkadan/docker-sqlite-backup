using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
public class ChecksumBenchmarks
{
    private string _tempFilePath = default!;
    private const int FileSize = 10 * 1024 * 1024; // 10 MB

    [GlobalSetup]
    public void Setup()
    {
        _tempFilePath = Path.GetTempFileName();
        var data = new byte[FileSize];
        Random.Shared.NextBytes(data);
        File.WriteAllBytes(_tempFilePath, data);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    [Benchmark]
    public async Task<string> CalculateSha256()
    {
        return await ChecksumUtility.CalculateFileSha256Async(_tempFilePath);
    }

    [Benchmark]
    public async Task<uint> CalculateCrc32()
    {
        return await ChecksumUtility.CalculateFileCrc32Async(_tempFilePath);
    }

    [Benchmark]
    public async Task<string> GenerateQuickChecksum()
    {
        return await ChecksumUtility.GenerateQuickChecksumAsync(_tempFilePath);
    }
}
