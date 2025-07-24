// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Integration;

/// <summary>
/// Integration tests for VerificationService using real SQLite databases.
/// Tests the full verification workflow without mocking the storage layer.
/// </summary>
public class VerificationServiceIntegrationTests : IAsyncLifetime
{
    private readonly VerificationService _sut;
    private readonly Mock<IBackupRepository> _repositoryMock;
    private string _tempDir = string.Empty;

    public VerificationServiceIntegrationTests()
    {
        _repositoryMock = new Mock<IBackupRepository>();
        _repositoryMock
            .Setup(r => r.SaveRestoreVerificationAsync(It.IsAny<RestoreVerification>()))
            .ReturnsAsync((RestoreVerification v) => v);

        var appSettings = new AppSettings();
        var logger = new Mock<ILogger<VerificationService>>().Object;
        _sut = new VerificationService(_repositoryMock.Object, appSettings, logger);
    }

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"verify-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    private string CreateValidSqliteDatabase(string? name = null)
    {
        var path = Path.Combine(_tempDir, name ?? $"db-{Guid.NewGuid()}.sqlite");
        using var conn = new SqliteConnection($"Data Source={path}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE backups (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                created_at TEXT
            );
            INSERT INTO backups (name, created_at) VALUES ('test1', '2024-01-01');
            INSERT INTO backups (name, created_at) VALUES ('test2', '2024-01-02');
        ";
        cmd.ExecuteNonQuery();
        return path;
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_ValidDatabase_ReturnsValidWithNoErrors()
    {
        var dbPath = CreateValidSqliteDatabase();

        var (isValid, errors) = await _sut.PerformIntegrityCheckAsync(dbPath);

        isValid.Should().BeTrue();
        errors.Should().BeNull();
    }

    [Fact]
    public async Task PerformIntegrityCheckAsync_NonExistentFile_ReturnsInvalidWithError()
    {
        var nonExistent = Path.Combine(_tempDir, "no-such-db.sqlite");

        var (isValid, errors) = await _sut.PerformIntegrityCheckAsync(nonExistent);

        isValid.Should().BeFalse();
        errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task VerifyChecksumAsync_CorrectChecksum_ReturnsTrue()
    {
        var dbPath = CreateValidSqliteDatabase();
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(dbPath);
        var hashBytes = sha256.ComputeHash(stream);
        var expectedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        var result = await _sut.VerifyChecksumAsync(dbPath, expectedHash);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyChecksumAsync_WrongChecksum_ReturnsFalse()
    {
        var dbPath = CreateValidSqliteDatabase();

        var result = await _sut.VerifyChecksumAsync(dbPath, "0000000000000000000000000000000000000000000000000000000000000000");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyChecksumAsync_EmptyExpectedChecksum_ReturnsTrue()
    {
        var dbPath = CreateValidSqliteDatabase();

        var result = await _sut.VerifyChecksumAsync(dbPath, string.Empty);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreToTemporaryAsync_UnencryptedBackup_CopiesFileToTempDir()
    {
        var dbPath = CreateValidSqliteDatabase();
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(dbPath);
        var hashBytes = sha256.ComputeHash(stream);

        var backup = new BackupResult
        {
            BackupFilePath = dbPath,
            Checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
        };

        var tempPath = await _sut.RestoreToTemporaryAsync(backup);

        try
        {
            tempPath.Should().NotBeNullOrEmpty();
            File.Exists(tempPath).Should().BeTrue();
            new FileInfo(tempPath).Length.Should().BeGreaterThan(0);
        }
        finally
        {
            var tempDir = Path.GetDirectoryName(tempPath);
            if (tempDir != null && Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task CleanupTemporaryFilesAsync_ExistingDirectory_DeletesIt()
    {
        var tempSubDir = Path.Combine(_tempDir, $"temp-verify-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempSubDir);
        File.WriteAllText(Path.Combine(tempSubDir, "file.txt"), "data");

        await _sut.CleanupTemporaryFilesAsync(tempSubDir);

        Directory.Exists(tempSubDir).Should().BeFalse();
    }

    [Fact]
    public async Task VerifyBackupAsync_ValidSqliteFile_ReturnsSuccessfulVerification()
    {
        var dbPath = CreateValidSqliteDatabase();
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(dbPath);
        var hashBytes = sha256.ComputeHash(stream);

        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = dbPath,
            Checksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant()
        };

        var result = await _sut.VerifyBackupAsync(backup);

        result.Should().NotBeNull();
        result.IsSuccessful.Should().BeTrue();
        result.IntegrityCheckPassed.Should().BeTrue();
        result.RecordCount.Should().Be(2);
        result.DatabaseSizeBytes.Should().BeGreaterThan(0);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyBackupAsync_WrongChecksum_ReturnsFailedVerification()
    {
        var dbPath = CreateValidSqliteDatabase();
        var backup = new BackupResult
        {
            Id = Guid.NewGuid(),
            BackupFilePath = dbPath,
            Checksum = "badhash00badhash00badhash00badhash00badhash00badhash00badhash00ba"
        };

        var result = await _sut.VerifyBackupAsync(backup);

        result.IsSuccessful.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetVerificationHistoryAsync_DelegatesToRepository()
    {
        var backupId = Guid.NewGuid();
        var history = new List<RestoreVerification>
        {
            new() { BackupResultId = backupId, IsSuccessful = true }
        };

        _repositoryMock
            .Setup(r => r.GetVerificationHistoryAsync(backupId))
            .ReturnsAsync(history);

        var result = await _sut.GetVerificationHistoryAsync(backupId);

        result.Should().HaveCount(1);
        result.First().BackupResultId.Should().Be(backupId);
    }
}
