using System;
using System.IO;
using System.Threading.Tasks;
using DockerSqliteBackup.Utilities;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class ChecksumUtilityTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _emptyFilePath;
        private readonly string _smallFilePath;
        private readonly string _largeFilePath;
        private readonly string _differentFilePath;

        public ChecksumUtilityTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);

            _emptyFilePath = Path.Combine(_testDirectory, "empty.txt");
            File.WriteAllText(_emptyFilePath, string.Empty);

            _smallFilePath = Path.Combine(_testDirectory, "small.txt");
            File.WriteAllText(_smallFilePath, "Hello, World!");

            _largeFilePath = Path.Combine(_testDirectory, "large.txt");
            File.WriteAllText(_largeFilePath, new string('A', 1024 * 1024)); // 1MB file

            _differentFilePath = Path.Combine(_testDirectory, "different.txt");
            File.WriteAllText(_differentFilePath, "Different content");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        [Fact]
        public async Task CalculateFileSha256Async_KnownInput_KnownHash()
        {
            // Arrange
            var testContent = "Test content for SHA256";
            var testFilePath = Path.Combine(_testDirectory, "test_sha256.txt");
            File.WriteAllText(testFilePath, testContent);

            // Expected SHA256 hash of "Test content for SHA256"
            var expectedHash = "a5f39977e7c9d35156a1fcfa4858cd80cfc8ff2c982a26b8f87c92e22ea78c4b";

            // Act
            var actualHash = await ChecksumUtility.CalculateFileSha256Async(testFilePath);

            // Assert
            Assert.Equal(expectedHash, actualHash, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CalculateFileSha256Async_SameContent_SameChecksum()
        {
            // Arrange
            var filePath1 = Path.Combine(_testDirectory, "file1.txt");
            var filePath2 = Path.Combine(_testDirectory, "file2.txt");
            var content = "Same content";
            File.WriteAllText(filePath1, content);
            File.WriteAllText(filePath2, content);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileSha256Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileSha256Async(filePath2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileSha256Async_DifferentContent_DifferentChecksum()
        {
            // Arrange
            var content1 = "Content one";
            var content2 = "Content two";
            var filePath1 = Path.Combine(_testDirectory, "content1.txt");
            var filePath2 = Path.Combine(_testDirectory, "content2.txt");
            File.WriteAllText(filePath1, content1);
            File.WriteAllText(filePath2, content2);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileSha256Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileSha256Async(filePath2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileSha256Async_EmptyFile_ValidHash()
        {
            // Arrange & Act
            var hash = await ChecksumUtility.CalculateFileSha256Async(_emptyFilePath);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.Equal(64, hash.Length); // SHA256 is 32 bytes = 64 hex chars
        }

        [Fact]
        public async Task CalculateFileSha256Async_MissingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => ChecksumUtility.CalculateFileSha256Async(nonExistentPath));
        }

        [Fact]
        public void CalculateStringSha256_KnownInput_KnownHash()
        {
            // Arrange
            var input = "Test string for SHA256";
            var expectedHash = "fe857e15959fce6652084e1a74c99ec416299e7b6eefed6e415f05adc044b821";

            // Act
            var actualHash = ChecksumUtility.CalculateStringSha256(input);

            // Assert
            Assert.Equal(expectedHash, actualHash, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void CalculateStringSha256_SameInput_SameHash()
        {
            // Arrange
            var input = "Consistent input";

            // Act
            var hash1 = ChecksumUtility.CalculateStringSha256(input);
            var hash2 = ChecksumUtility.CalculateStringSha256(input);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalculateStringSha256_DifferentInput_DifferentHash()
        {
            // Arrange
            var input1 = "First input";
            var input2 = "Second input";

            // Act
            var hash1 = ChecksumUtility.CalculateStringSha256(input1);
            var hash2 = ChecksumUtility.CalculateStringSha256(input2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void CalculateStringSha256_EmptyString_ValidHash()
        {
            // Arrange & Act
            var hash = ChecksumUtility.CalculateStringSha256(string.Empty);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.Equal(64, hash.Length);
        }

        [Fact]
        public async Task CalculateFileMd5Async_KnownInput_KnownHash()
        {
            // Arrange
            var testContent = "Test content for MD5";
            var testFilePath = Path.Combine(_testDirectory, "test_md5.txt");
            File.WriteAllText(testFilePath, testContent);

            // Expected MD5 hash of "Test content for MD5"
            var expectedHash = "d43a44ccc736b56f4e657651332a70a2";

            // Act
            var actualHash = await ChecksumUtility.CalculateFileMd5Async(testFilePath);

            // Assert
            Assert.Equal(expectedHash, actualHash, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CalculateFileMd5Async_SameContent_SameChecksum()
        {
            // Arrange
            var filePath1 = Path.Combine(_testDirectory, "md5_file1.txt");
            var filePath2 = Path.Combine(_testDirectory, "md5_file2.txt");
            var content = "MD5 test content";
            File.WriteAllText(filePath1, content);
            File.WriteAllText(filePath2, content);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileMd5Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileMd5Async(filePath2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileMd5Async_DifferentContent_DifferentChecksum()
        {
            // Arrange
            var content1 = "First content";
            var content2 = "Second content";
            var filePath1 = Path.Combine(_testDirectory, "md5_content1.txt");
            var filePath2 = Path.Combine(_testDirectory, "md5_content2.txt");
            File.WriteAllText(filePath1, content1);
            File.WriteAllText(filePath2, content2);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileMd5Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileMd5Async(filePath2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileMd5Async_EmptyFile_ValidHash()
        {
            // Arrange & Act
            var hash = await ChecksumUtility.CalculateFileMd5Async(_emptyFilePath);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.Equal(32, hash.Length); // MD5 is 16 bytes = 32 hex chars
        }

        [Fact]
        public async Task CalculateFileMd5Async_MissingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent_md5.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => ChecksumUtility.CalculateFileMd5Async(nonExistentPath));
        }

        [Fact]
        public async Task VerifyFileSha256Async_MatchingHash_ReturnsTrue()
        {
            // Arrange
            var content = "Test content for verification";
            var testFilePath = Path.Combine(_testDirectory, "verify_test.txt");
            File.WriteAllText(testFilePath, content);

            var expectedHash = await ChecksumUtility.CalculateFileSha256Async(testFilePath);

            // Act
            var result = await ChecksumUtility.VerifyFileSha256Async(testFilePath, expectedHash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task VerifyFileSha256Async_NonMatchingHash_ReturnsFalse()
        {
            // Arrange
            var content = "Test content";
            var testFilePath = Path.Combine(_testDirectory, "verify_false_test.txt");
            File.WriteAllText(testFilePath, content);

            var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";

            // Act
            var result = await ChecksumUtility.VerifyFileSha256Async(testFilePath, wrongHash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task VerifyFileSha256Async_CaseInsensitiveHashComparison_ReturnsTrue()
        {
            // Arrange
            var content = "Case insensitive test";
            var testFilePath = Path.Combine(_testDirectory, "case_test.txt");
            File.WriteAllText(testFilePath, content);

            var expectedHash = await ChecksumUtility.CalculateFileSha256Async(testFilePath);
            var upperCaseHash = expectedHash.ToUpperInvariant();

            // Act
            var result = await ChecksumUtility.VerifyFileSha256Async(testFilePath, upperCaseHash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CalculateFileCrc32Async_KnownInput_KnownHash()
        {
            // Arrange
            var testContent = "Test content for CRC32";
            var testFilePath = Path.Combine(_testDirectory, "test_crc32.txt");
            File.WriteAllText(testFilePath, testContent);

            // Expected CRC32 hash of "Test content for CRC32"
            // This is a known value we can calculate
            var expectedHash = 2804791789u; // Pre-calculated expected value

            // Act
            var actualHash = await ChecksumUtility.CalculateFileCrc32Async(testFilePath);

            // Assert
            Assert.Equal(expectedHash, actualHash);
        }

        [Fact]
        public async Task CalculateFileCrc32Async_SameContent_SameChecksum()
        {
            // Arrange
            var filePath1 = Path.Combine(_testDirectory, "crc32_file1.txt");
            var filePath2 = Path.Combine(_testDirectory, "crc32_file2.txt");
            var content = "CRC32 consistent content";
            File.WriteAllText(filePath1, content);
            File.WriteAllText(filePath2, content);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileCrc32Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileCrc32Async(filePath2);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileCrc32Async_DifferentContent_DifferentChecksum()
        {
            // Arrange
            var content1 = "First content";
            var content2 = "Second content";
            var filePath1 = Path.Combine(_testDirectory, "crc32_content1.txt");
            var filePath2 = Path.Combine(_testDirectory, "crc32_content2.txt");
            File.WriteAllText(filePath1, content1);
            File.WriteAllText(filePath2, content2);

            // Act
            var hash1 = await ChecksumUtility.CalculateFileCrc32Async(filePath1);
            var hash2 = await ChecksumUtility.CalculateFileCrc32Async(filePath2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public async Task CalculateFileCrc32Async_EmptyFile_ValidHash()
        {
            // Arrange & Act
            var hash = await ChecksumUtility.CalculateFileCrc32Async(_emptyFilePath);

            // Assert
            Assert.NotEqual(0xFFFFFFFFu, hash); // Empty file should have specific CRC32 value
        }

        [Fact]
        public async Task CalculateFileCrc32Async_MissingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent_crc32.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => ChecksumUtility.CalculateFileCrc32Async(nonExistentPath));
        }

        [Fact]
        public void GenerateQuickChecksum_SameContent_SameChecksum()
        {
            // Arrange
            var content = "Quick checksum test content";
            var filePath1 = Path.Combine(_testDirectory, "quick1.txt");
            var filePath2 = Path.Combine(_testDirectory, "quick2.txt");
            File.WriteAllText(filePath1, content);
            File.WriteAllText(filePath2, content);

            // Act
            var checksum1 = ChecksumUtility.GenerateQuickChecksum(filePath1);
            var checksum2 = ChecksumUtility.GenerateQuickChecksum(filePath2);

            // Assert
            Assert.Equal(checksum1, checksum2);
        }

        [Fact]
        public void GenerateQuickChecksum_DifferentContent_DifferentChecksum()
        {
            // Arrange
            var content1 = "First content for quick checksum";
            var content2 = "Second content for quick checksum";
            var filePath1 = Path.Combine(_testDirectory, "quick_diff1.txt");
            var filePath2 = Path.Combine(_testDirectory, "quick_diff2.txt");
            File.WriteAllText(filePath1, content1);
            File.WriteAllText(filePath2, content2);

            // Act
            var checksum1 = ChecksumUtility.GenerateQuickChecksum(filePath1);
            var checksum2 = ChecksumUtility.GenerateQuickChecksum(filePath2);

            // Assert
            Assert.NotEqual(checksum1, checksum2);
        }

        [Fact]
        public void GenerateQuickChecksum_EmptyFile_ValidChecksum()
        {
            // Arrange & Act
            var checksum = ChecksumUtility.GenerateQuickChecksum(_emptyFilePath);

            // Assert
            Assert.NotNull(checksum);
            Assert.NotEmpty(checksum);
            Assert.Equal(16, checksum.Length); // Quick checksum is 16 hex chars
        }

        [Fact]
        public void GenerateQuickChecksum_MissingFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var nonExistentPath = Path.Combine(_testDirectory, "nonexistent_quick.txt");

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => ChecksumUtility.GenerateQuickChecksum(nonExistentPath));
        }

        [Fact]
        public async Task GenerateQuickChecksumAsync_SameAsSyncMethod()
        {
            // Arrange
            var content = "Async vs sync test";
            var filePath = Path.Combine(_testDirectory, "async_sync.txt");
            File.WriteAllText(filePath, content);

            // Act
            var syncChecksum = ChecksumUtility.GenerateQuickChecksum(filePath);
            var asyncChecksum = await ChecksumUtility.GenerateQuickChecksumAsync(filePath);

            // Assert
            Assert.Equal(syncChecksum, asyncChecksum);
        }

        [Fact]
        public void CalculateCollectionChecksum_KnownValues_KnownHash()
        {
            // Arrange
            var value1 = "test";
            var value2 = 42;
            var value3 = Guid.Parse("12345678-1234-1234-1234-1234567890ab");

            // Expected hash of "test|42|12345678-1234-1234-1234-1234567890ab"
            var expectedHash = "37dd4ea9f464a45cef9883b9a3001724";

            // Act
            var actualHash = ChecksumUtility.CalculateCollectionChecksum(value1, value2, value3);

            // Assert
            Assert.Equal(expectedHash, actualHash, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void CalculateCollectionChecksum_SameValues_SameHash()
        {
            // Arrange
            var values = new object[] { "same", "values", 123 };

            // Act
            var hash1 = ChecksumUtility.CalculateCollectionChecksum(values);
            var hash2 = ChecksumUtility.CalculateCollectionChecksum(values);

            // Assert
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void CalculateCollectionChecksum_DifferentValues_DifferentHash()
        {
            // Arrange
            var values1 = new object[] { "first", "set" };
            var values2 = new object[] { "second", "set" };

            // Act
            var hash1 = ChecksumUtility.CalculateCollectionChecksum(values1);
            var hash2 = ChecksumUtility.CalculateCollectionChecksum(values2);

            // Assert
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void CalculateCollectionChecksum_EmptyArray_ValidHash()
        {
            // Arrange & Act
            var hash = ChecksumUtility.CalculateCollectionChecksum(Array.Empty<object>());

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.Equal(32, hash.Length); // Collection checksum is 32 hex chars
        }

        [Fact]
        public void CalculateCollectionChecksum_WithMixedTypes_GeneratesConsistentHash()
        {
            // Arrange & Act
            var hash1 = ChecksumUtility.CalculateCollectionChecksum("hello", 42, true);
            var hash2 = ChecksumUtility.CalculateCollectionChecksum("hello", 42, true);

            // Assert - same values should produce same hash
            Assert.Equal(hash1, hash2);
            Assert.NotNull(hash1);
            Assert.NotEmpty(hash1);
            Assert.Equal(32, hash1.Length);
        }
    }
}