// Author: Vladyslav Zaiets

using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public class IntegrityCheckerServiceTests : IAsyncLifetime
{
    private string _tempDir = string.Empty;
    private IntegrityCheckerService _sut = null!;

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"integrity-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sut = new IntegrityCheckerService(new Mock<ILogger<IntegrityCheckerService>>().Object);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    private string CreateValidSqliteDb(string? tableName = null)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.sqlite");
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE {tableName ?? "items"} (id INTEGER PRIMARY KEY, name TEXT NOT NULL)";
        cmd.ExecuteNonQuery();

        using var insert = connection.CreateCommand();
        insert.CommandText = $"INSERT INTO {tableName ?? "items"} (name) VALUES ('test-row')";
        insert.ExecuteNonQuery();

        return path;
    }

    // ── CheckDatabaseAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task CheckDatabaseAsync_ValidDatabase_ReturnsHealthyReport()
    {
        var dbPath = CreateValidSqliteDb();

        var report = await _sut.CheckDatabaseAsync(dbPath, fullCheck: true);

        report.Should().NotBeNull();
        report.IsHealthy.Should().BeTrue();
        report.PassedQuickCheck.Should().BeTrue();
        report.PassedFullCheck.Should().BeTrue();
        report.PassedForeignKeyCheck.Should().BeTrue();
        report.DatabasePath.Should().Be(dbPath);
        report.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckDatabaseAsync_ValidDatabase_PopulatesMetadata()
    {
        var dbPath = CreateValidSqliteDb("products");

        var report = await _sut.CheckDatabaseAsync(dbPath, fullCheck: false);

        report.PageSize.Should().BeGreaterThan(0);
        report.PageCount.Should().BeGreaterThan(0);
        report.TableCount.Should().BeGreaterThan(0);
        report.JournalMode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CheckDatabaseAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "nonexistent.sqlite");

        var act = async () => await _sut.CheckDatabaseAsync(path);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task CheckDatabaseAsync_QuickCheckOnly_SkipsFullAndFkChecks()
    {
        var dbPath = CreateValidSqliteDb();

        var report = await _sut.CheckDatabaseAsync(dbPath, fullCheck: false);

        report.PassedQuickCheck.Should().BeTrue();
        // In quick-only mode full and FK checks are marked as passed without running.
        report.PassedFullCheck.Should().BeTrue();
        report.PassedForeignKeyCheck.Should().BeTrue();
    }

    [Fact]
    public async Task CheckDatabaseAsync_MultipleTablesWithData_CountsTablesCorrectly()
    {
        var dbPath = Path.Combine(_tempDir, $"{Guid.NewGuid()}.sqlite");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE orders (id INTEGER PRIMARY KEY, total REAL);
            CREATE TABLE customers (id INTEGER PRIMARY KEY, name TEXT);
            CREATE TABLE products (id INTEGER PRIMARY KEY, sku TEXT);";
        cmd.ExecuteNonQuery();

        var report = await _sut.CheckDatabaseAsync(dbPath, fullCheck: true);

        report.TableCount.Should().Be(3);
        report.IsHealthy.Should().BeTrue();
    }

    // ── QuickCheckAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task QuickCheckAsync_ValidDatabase_ReturnsTrue()
    {
        var dbPath = CreateValidSqliteDb();

        var result = await _sut.QuickCheckAsync(dbPath);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task QuickCheckAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "ghost.sqlite");

        var act = async () => await _sut.QuickCheckAsync(path);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── CheckBackupFileAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CheckBackupFileAsync_ValidDatabase_ReturnsHealthyReport()
    {
        var dbPath = CreateValidSqliteDb();

        var report = await _sut.CheckBackupFileAsync(dbPath);

        report.IsHealthy.Should().BeTrue();
        report.DatabasePath.Should().Be(dbPath);
    }

    // ── IntegrityReport ───────────────────────────────────────────────────────

    [Fact]
    public void IntegrityReport_IsHealthy_RequiresAllThreeChecksPassed()
    {
        var report = new DockerSqliteBackup.Domain.IntegrityReport
        {
            PassedQuickCheck = true,
            PassedFullCheck = true,
            PassedForeignKeyCheck = false
        };

        report.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public void IntegrityReport_Summary_ContainsHealthyMessageWhenAllPassed()
    {
        var report = new DockerSqliteBackup.Domain.IntegrityReport
        {
            PassedQuickCheck = true,
            PassedFullCheck = true,
            PassedForeignKeyCheck = true,
            PageCount = 10,
            TableCount = 2,
            JournalMode = "delete"
        };

        report.Summary.Should().Contain("healthy");
    }
}
