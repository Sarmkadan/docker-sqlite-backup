// Author: Vladyslav Zaiets

using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public static class IntegrityCheckerServiceTestsExtensions
{
    /// <summary>
    /// Creates a test database with the specified table structure and data.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <param name="tableName">Optional table name (defaults to "items")</param>
    /// <param name="rowCount">Number of rows to insert (default: 1)</param>
    /// <returns>Full path to the created SQLite database file</returns>
    public static string CreateTestDatabase(this IntegrityCheckerServiceTests test, string? tableName = null, int rowCount = 1)
    {
        var path = Path.Combine(test.GetTempDir(), $"test-{Guid.NewGuid()}.sqlite");

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"CREATE TABLE {tableName ?? "items"} (id INTEGER PRIMARY KEY, name TEXT NOT NULL, value INTEGER)";
        cmd.ExecuteNonQuery();

        if (rowCount > 0)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = $"INSERT INTO {tableName ?? "items"} (name, value) VALUES (@name, @value)";

            var nameParam = insert.CreateParameter();
            nameParam.ParameterName = "@name";
            nameParam.Value = "test-item";
            insert.Parameters.Add(nameParam);

            var valueParam = insert.CreateParameter();
            valueParam.ParameterName = "@value";
            valueParam.Value = 42;
            insert.Parameters.Add(valueParam);

            for (int i = 0; i < rowCount; i++)
            {
                insert.ExecuteNonQuery();
            }
        }

        return path;
    }

    /// <summary>
    /// Creates a test database with multiple related tables (orders, customers, products)
    /// to test foreign key relationships and complex database structures.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>Full path to the created SQLite database file</returns>
    public static string CreateComplexTestDatabase(this IntegrityCheckerServiceTests test)
    {
        var path = Path.Combine(test.GetTempDir(), $"complex-{Guid.NewGuid()}.sqlite");

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                email TEXT UNIQUE
            );

            CREATE TABLE products (
                id INTEGER PRIMARY KEY,
                sku TEXT UNIQUE NOT NULL,
                price REAL NOT NULL
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                customer_id INTEGER NOT NULL,
                product_id INTEGER NOT NULL,
                quantity INTEGER NOT NULL,
                order_date TEXT NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id),
                FOREIGN KEY (product_id) REFERENCES products(id)
            );
        ";
        cmd.ExecuteNonQuery();

        // Insert sample data
        using var insertCustomers = connection.CreateCommand();
        insertCustomers.CommandText = "INSERT INTO customers (name, email) VALUES (@name, @email)";
        insertCustomers.Parameters.AddWithValue("@name", "John Doe");
        insertCustomers.Parameters.AddWithValue("@email", "john@example.com");
        insertCustomers.ExecuteNonQuery();

        using var insertProducts = connection.CreateCommand();
        insertProducts.CommandText = "INSERT INTO products (sku, price) VALUES (@sku, @price)";
        insertProducts.Parameters.AddWithValue("@sku", "PROD-001");
        insertProducts.Parameters.AddWithValue("@price", 99.99);
        insertProducts.ExecuteNonQuery();

        return path;
    }

    /// <summary>
    /// Creates a corrupted database file (missing header, invalid SQLite format)
    /// to test error handling and validation scenarios.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>Full path to the corrupted database file</returns>
    public static string CreateCorruptedDatabase(this IntegrityCheckerServiceTests test)
    {
        var path = Path.Combine(test.GetTempDir(), $"corrupted-{Guid.NewGuid()}.sqlite");

        // Write invalid SQLite header (SQLite databases start with "SQLite format 3\0")
        File.WriteAllText(path, "This is not a valid SQLite database file");

        return path;
    }

    /// <summary>
    /// Creates an empty database file (valid SQLite format but no tables)
    /// to test edge cases with minimal database structures.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>Full path to the empty database file</returns>
    public static string CreateEmptyDatabase(this IntegrityCheckerServiceTests test)
    {
        var path = Path.Combine(test.GetTempDir(), $"empty-{Guid.NewGuid()}.sqlite");

        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        // Create database with no tables - just an empty database
        return path;
    }

    /// <summary>
    /// Verifies that a report indicates corruption by checking specific properties.
    /// </summary>
    /// <param name="report">The integrity report to verify</param>
    public static void ShouldIndicateCorruption(this DockerSqliteBackup.Domain.IntegrityReport report)
    {
        report.IsHealthy.Should().BeFalse("Database should be marked as unhealthy");
        report.PassedQuickCheck.Should().BeFalse("Quick check should have failed");
        report.PassedFullCheck.Should().BeFalse("Full check should have failed");
        report.PassedForeignKeyCheck.Should().BeFalse("Foreign key check should have failed");
    }

    /// <summary>
    /// Verifies that a report indicates a healthy database with detailed assertions.
    /// </summary>
    /// <param name="report">The integrity report to verify</param>
    /// <param name="expectedTableCount">Expected number of tables in the database</param>
    public static void ShouldIndicateHealthy(this DockerSqliteBackup.Domain.IntegrityReport report, int expectedTableCount)
    {
        report.IsHealthy.Should().BeTrue("Database should be marked as healthy");
        report.PassedQuickCheck.Should().BeTrue("Quick check should have passed");
        report.PassedFullCheck.Should().BeTrue("Full check should have passed");
        report.PassedForeignKeyCheck.Should().BeTrue("Foreign key check should have passed");
        report.TableCount.Should().Be(expectedTableCount, "Should have exactly {0} table(s)");
        report.PageCount.Should().BeGreaterThan(0, "Should have positive page count");
        report.PageSize.Should().BeGreaterThan(0, "Should have positive page size");
        report.JournalMode.Should().NotBeNullOrEmpty("Journal mode should be set");
    }

    /// <summary>
    /// Creates a mock IntegrityCheckerService for unit testing without file system dependencies.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>Mock IntegrityCheckerService instance</returns>
    public static IntegrityCheckerService CreateMockService(this IntegrityCheckerServiceTests test)
    {
        return new IntegrityCheckerService(new Mock<ILogger<IntegrityCheckerService>>().Object);
    }

    /// <summary>
    /// Gets the temporary directory path from the test instance.
    /// </summary>
    /// <param name="test">The test instance</param>
    /// <returns>Temporary directory path</returns>
    private static string GetTempDir(this IntegrityCheckerServiceTests test)
    {
        // Use reflection to access the private _tempDir field
        var field = typeof(IntegrityCheckerServiceTests).GetField("_tempDir", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var value = field?.GetValue(test);
        return value as string ?? string.Empty;
    }
}