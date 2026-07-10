// Author: Vladyslav Zaiets

using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Tests for the IntegrityCheckerService class.
/// </summary>
public class IntegrityCheckerServiceTests : IAsyncLifetime
{
    private string _tempDir = string.Empty;
    private IntegrityCheckerService _sut = null!;

    /// <summary>
    /// Initializes the test environment.
    /// </summary>
    /// <returns>A task representing the initialization operation.</returns>
    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"integrity-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _sut = new IntegrityCheckerService(new Mock<ILogger<IntegrityCheckerService>>().Object);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes of the test environment.
    /// </summary>
    /// <returns>A task representing the disposal operation.</returns>
    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a valid SQLite database with the specified table name.
    /// </summary>
    /// <param name="tableName">The name of the table to create.</param>
    /// <returns>The path to the created database file.</returns>
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

    /// <summary>
    /// Tests the CheckDatabaseAsync method with a valid database.
    /// </summary>
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

    /// <summary>
    /// Tests the CheckDatabaseAsync method with a valid database and metadata population.
    /// </summary>
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

    /// <summary>
    /// Tests the CheckDatabaseAsync method with a non-existent file.
    /// </summary>
    [Fact]
    public async Task CheckDatabaseAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "nonexistent.sqlite");

        var act = async () => await _sut.CheckDatabaseAsync(path);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    /// <summary>
    /// Tests the CheckDatabaseAsync method with quick check only.
    /// </summary>
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

    /// <summary>
    /// Tests the CheckDatabaseAsync method with multiple tables and data.
    /// </summary>
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

    /// <summary>
    /// Tests the QuickCheckAsync method with a valid database.
    /// </summary>
    [Fact]
    public async Task QuickCheckAsync_ValidDatabase_ReturnsTrue()
    {
        var dbPath = CreateValidSqliteDb();

        var result = await _sut.QuickCheckAsync(dbPath);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests the QuickCheckAsync method with a non-existent file.
    /// </summary>
    [Fact]
    public async Task QuickCheckAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var path = Path.Combine(_tempDir, "ghost.sqlite");

        var act = async () => await _sut.QuickCheckAsync(path);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── CheckBackupFileAsync ──────────────────────────────────────────────────

    /// <summary>
    /// Tests the CheckBackupFileAsync method with a valid database.
    /// </summary>
    [Fact]
    public async Task CheckBackupFileAsync_ValidDatabase_ReturnsHealthyReport()
    {
        var dbPath = CreateValidSqliteDb();

        var report = await _sut.CheckBackupFileAsync(dbPath);

        report.IsHealthy.Should().BeTrue();
        report.DatabasePath.Should().Be(dbPath);
    }

    // ── IntegrityReport ───────────────────────────────────────────────────────

    /// <summary>
    /// Tests the IntegrityReport class with a healthy report.
    /// </summary>
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

    /// <summary>
    /// Tests the IntegrityReport class with a summary containing a healthy message.
    /// </summary>
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
