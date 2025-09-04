using BenchmarkDotNet.Attributes;
using DockerSqliteBackup.Utilities;

namespace DockerSqliteBackup.Benchmarks;

/// <summary>
/// Benchmark tests for encryption and decryption utilities.
/// </summary>
[MemoryDiagnoser]
public class EncryptionBenchmarks
{
    private string _sourceFilePath = default!;
    private string _encryptedFilePath = default!;
    private string _decryptedFilePath = default!;
    private string _key = default!;
    private const int FileSize = 1 * 1024 * 1024; // 1 MB

    /// <summary>
    /// Initializes test data and temporary file paths.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _key = EncryptionUtility.GenerateBase64Key();
        _sourceFilePath = Path.GetTempFileName();
        _encryptedFilePath = Path.GetTempFileName();
        _decryptedFilePath = Path.GetTempFileName();

        var data = new byte[FileSize];
        Random.Shared.NextBytes(data);
        File.WriteAllBytes(_sourceFilePath, data);
    }

    /// <summary>
    /// Deletes temporary files created during the benchmark.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_sourceFilePath)) File.Delete(_sourceFilePath);
        if (File.Exists(_encryptedFilePath)) File.Delete(_encryptedFilePath);
        if (File.Exists(_decryptedFilePath)) File.Delete(_decryptedFilePath);
    }

    /// <summary>
    /// Encrypts the source file using the provided key.
    /// </summary>
    [Benchmark]
    public async Task Encrypt()
    {
        await EncryptionUtility.EncryptFileAsync(_sourceFilePath, _encryptedFilePath, _key);
    }

    /// <summary>
    /// Encrypts the source file then decrypts the encrypted file.
    /// </summary>
    [Benchmark]
    public async Task Decrypt()
    {
        // Need a file that's already encrypted
        await EncryptionUtility.EncryptFileAsync(_sourceFilePath, _encryptedFilePath, _key);
        await EncryptionUtility.DecryptFileAsync(_encryptedFilePath, _decryptedFilePath, _key);
    }
}
