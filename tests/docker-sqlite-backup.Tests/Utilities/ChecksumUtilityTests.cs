// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class ChecksumUtilityTests : IAsyncLifetime
{
    private string _tempDir = string.Empty;

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"checksum-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    private string CreateTempFile(string content)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.txt");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task CalculateFileSha256Async_KnownContent_ReturnsDeterministicHash()
    {
        var path1 = CreateTempFile("hello world");
        var path2 = CreateTempFile("hello world");

        var hash1 = await ChecksumUtility.CalculateFileSha256Async(path1);
        var hash2 = await ChecksumUtility.CalculateFileSha256Async(path2);

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
        hash1.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public async Task CalculateFileSha256Async_DifferentContent_ReturnsDifferentHash()
    {
        var path1 = CreateTempFile("content-one");
        var path2 = CreateTempFile("content-two");

        var hash1 = await ChecksumUtility.CalculateFileSha256Async(path1);
        var hash2 = await ChecksumUtility.CalculateFileSha256Async(path2);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task CalculateFileSha256Async_NonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist.txt");

        var act = async () => await ChecksumUtility.CalculateFileSha256Async(nonExistentPath);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void CalculateStringSha256_KnownInput_ReturnsDeterministicHash()
    {
        var hash1 = ChecksumUtility.CalculateStringSha256("test-input");
        var hash2 = ChecksumUtility.CalculateStringSha256("test-input");

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
    }

    [Fact]
    public void CalculateStringSha256_DifferentInputs_ReturnsDifferentHashes()
    {
        var hash1 = ChecksumUtility.CalculateStringSha256("input-a");
        var hash2 = ChecksumUtility.CalculateStringSha256("input-b");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task VerifyFileSha256Async_CorrectHash_ReturnsTrue()
    {
        var path = CreateTempFile("verify-me");
        var hash = await ChecksumUtility.CalculateFileSha256Async(path);

        var isValid = await ChecksumUtility.VerifyFileSha256Async(path, hash);

        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyFileSha256Async_WrongHash_ReturnsFalse()
    {
        var path = CreateTempFile("verify-me");

        var isValid = await ChecksumUtility.VerifyFileSha256Async(path, "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef");

        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyFileSha256Async_HashIsCaseInsensitive_ReturnsTrue()
    {
        var path = CreateTempFile("case-test");
        var hash = await ChecksumUtility.CalculateFileSha256Async(path);

        var isValidUpper = await ChecksumUtility.VerifyFileSha256Async(path, hash.ToUpper());

        isValidUpper.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateFileMd5Async_KnownContent_ReturnsDeterministicHash()
    {
        var path = CreateTempFile("md5-test-content");

        var hash1 = await ChecksumUtility.CalculateFileMd5Async(path);
        var hash2 = await ChecksumUtility.CalculateFileMd5Async(path);

        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(32);
        hash1.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public async Task CalculateFileMd5Async_NonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "missing.txt");

        var act = async () => await ChecksumUtility.CalculateFileMd5Async(nonExistentPath);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void CalculateCollectionChecksum_SameValues_ReturnsSameHash()
    {
        var hash1 = ChecksumUtility.CalculateCollectionChecksum("a", 1, true);
        var hash2 = ChecksumUtility.CalculateCollectionChecksum("a", 1, true);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CalculateCollectionChecksum_DifferentValues_ReturnsDifferentHash()
    {
        var hash1 = ChecksumUtility.CalculateCollectionChecksum("value-one");
        var hash2 = ChecksumUtility.CalculateCollectionChecksum("value-two");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task GenerateQuickChecksumAsync_SameFile_ReturnsSameChecksum()
    {
        var path = CreateTempFile("quick-checksum-test-content");

        var cs1 = await ChecksumUtility.GenerateQuickChecksumAsync(path);
        var cs2 = await ChecksumUtility.GenerateQuickChecksumAsync(path);

        cs1.Should().Be(cs2);
        cs1.Should().HaveLength(16);
    }

    [Fact]
    public async Task GenerateQuickChecksumAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var act = async () => await ChecksumUtility.GenerateQuickChecksumAsync(Path.Combine(_tempDir, "nope.db"));

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task CalculateFileCrc32Async_KnownContent_ReturnsDeterministicResult()
    {
        var path = CreateTempFile("crc32-test");

        var crc1 = await ChecksumUtility.CalculateFileCrc32Async(path);
        var crc2 = await ChecksumUtility.CalculateFileCrc32Async(path);

        crc1.Should().Be(crc2);
    }

    [Fact]
    public async Task CalculateFileCrc32Async_NonExistentFile_ThrowsFileNotFoundException()
    {
        var act = async () => await ChecksumUtility.CalculateFileCrc32Async(Path.Combine(_tempDir, "nope.bin"));

        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}
