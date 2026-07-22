using System.IO.Compression;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ArgumentNullException = System.ArgumentNullException;
using ArgumentException = System.ArgumentException;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace DockerSqliteBackup.Tests.Services;

public class VerificationServiceTests : IDisposable
{
    private readonly Mock<IBackupRepository> _mockRepository;
    private readonly Mock<AppSettings> _mockAppSettings;
    private readonly Mock<ILogger<VerificationService>> _mockLogger;
    private readonly VerificationService _verificationService;
    private readonly string _tempDirectory;
    private readonly string _testDbPath;

    public VerificationServiceTests()
    {
        _mockRepository = new Mock<IBackupRepository>();
        _mockAppSettings = new Mock<AppSettings>();
        _mockLogger = new Mock<ILogger<VerificationService>>();

        _verificationService = new VerificationService(
            _mockRepository.Object,
            _mockAppSettings.Object,
            _mockLogger.Object
        );

        _tempDirectory = Path.Combine(Path.GetTempPath(), "sqlite-verify-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _testDbPath = Path.Combine(_tempDirectory, "test.db");

        // Create a valid SQLite database for testing
        CreateTestDatabase(_testDbPath);
    }

    private void CreateTestDatabase(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE TestTable (Id INTEGER PRIMARY KEY, Name TEXT);";
        command.ExecuteNonQuery();

        command.CommandText = "INSERT INTO TestTable (Name) VALUES ('Test1'), ('Test2');";
        command.ExecuteNonQuery();
    }

    private void CreateCorruptedDatabase(string path)
    {
        // Create a file with invalid SQLite header to simulate corruption
        using var stream = File.Create(path);
        using var writer = new StreamWriter(stream);
        writer.Write("This is not a valid SQLite database file");
    }

    private static string CalculateSha256(string filePath)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task VerifyBackupAsync_ValidBackup_PassesVerification()
    {
        // Arrange
        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = _testDbPath,
            Checksum = CalculateSha256(_testDbPath),
            Status = (int)Constants.BackupStatus.Success
        };

        // Act
        var result = await _verificationService.VerifyBackupAsync(backup);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("Backup verification successful", result.StatusMessage);
        Assert.True(result.IntegrityCheckPassed);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.RecordCount > 0);
        Assert.True(result.DatabaseSizeBytes > 0);
        Assert.True(result.DurationMilliseconds >= 0);

        _mockRepository.Verify(x => x.SaveRestoreVerificationAsync(It.IsAny<RestoreVerification>()), Times.Once);
    }

    [Fact]
    public async Task VerifyBackupAsync_CorruptedFile_FailsVerification()
    {
        // Arrange - create a corrupted database file
        var corruptedPath = Path.Combine(_tempDirectory, "corrupted.db");
        CreateCorruptedDatabase(corruptedPath);

        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = corruptedPath,
            Checksum = CalculateSha256(corruptedPath),
            Status = (int)Constants.BackupStatus.Success
        };

        // Act
        var result = await _verificationService.VerifyBackupAsync(backup);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.False(result.IntegrityCheckPassed);
    }

    [Fact]
    public async Task VerifyBackupAsync_MissingFile_FailsVerification()
    {
        // Arrange - create backup with non-existent file path
        var missingPath = Path.Combine(_tempDirectory, "missing.db");
        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = missingPath,
            Checksum = "test",
            Status = (int)Constants.BackupStatus.Success
        };

        // Act
        var result = await _verificationService.VerifyBackupAsync(backup);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Fact]
    public async Task VerifyChecksumAsync_MatchingChecksum_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "checksum-test.db");
        File.Copy(_testDbPath, filePath);
        var expectedChecksum = CalculateSha256(filePath);

        // Act
        var result = await _verificationService.VerifyChecksumAsync(filePath, expectedChecksum);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyChecksumAsync_NonMatchingChecksum_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "checksum-test2.db");
        File.Copy(_testDbPath, filePath);
        var wrongChecksum = "0000000000000000000000000000000000000000000000000000000000000000";

        // Act
        var result = await _verificationService.VerifyChecksumAsync(filePath, wrongChecksum);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task VerifyChecksumAsync_EmptyChecksum_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "checksum-test3.db");
        File.Copy(_testDbPath, filePath);

        // Act
        var result = await _verificationService.VerifyChecksumAsync(filePath, string.Empty);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_ValidDatabase_ReturnsTrue()
    {
        // Arrange
        var databasePath = Path.Combine(_tempDirectory, "integrity-test.db");
        File.Copy(_testDbPath, databasePath);

        // Act
        var (isValid, errors) = await _verificationService.PerformIntegrityCheckAsync(databasePath);

        // Assert
        Assert.True(isValid);
        Assert.Null(errors);
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_CorruptedDatabase_ReturnsFalseWithErrors()
    {
        // Arrange - create a corrupted database file
        var corruptedPath = Path.Combine(_tempDirectory, "corrupted-integrity.db");
        CreateCorruptedDatabase(corruptedPath);

        // Act & Assert - the method should throw SqliteException for corrupted files
        await Assert.ThrowsAsync<SqliteException>(
            () => _verificationService.PerformIntegrityCheckAsync(corruptedPath)
        );
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_MissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var missingPath = Path.Combine(_tempDirectory, "missing-integrity.db");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _verificationService.PerformIntegrityCheckAsync(missingPath)
        );
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DockerSqliteBackup.Exceptions.ArgumentNullException>(
            () => _verificationService.PerformIntegrityCheckAsync(null!)
        );
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_EmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DockerSqliteBackup.Exceptions.ArgumentException>(
            () => _verificationService.PerformIntegrityCheckAsync(string.Empty)
        );
    }

    [Fact]
    public async Task RestoreToTemporaryAsync_ValidBackup_ReturnsTempPath()
    {
        // Arrange
        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = _testDbPath,
            Status = (int)Constants.BackupStatus.Success
        };

        // Act
        var tempPath = await _verificationService.RestoreToTemporaryAsync(backup);

        // Assert
        Assert.NotNull(tempPath);
        Assert.True(File.Exists(tempPath));
        Assert.StartsWith(Path.GetTempPath(), tempPath);
        Assert.EndsWith("restore-check.sqlite", tempPath);

        // Cleanup
        try
        {
            var tempDir = Path.GetDirectoryName(tempPath);
            if (tempDir != null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
        catch { /* Best effort cleanup */ }
    }

    [Fact]
    public async Task CleanupTemporaryFilesAsync_RemovesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "cleanup-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test.txt");
        await File.WriteAllTextAsync(tempFile, "test");

        // Act
        await _verificationService.CleanupTemporaryFilesAsync(tempDir);

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }
}