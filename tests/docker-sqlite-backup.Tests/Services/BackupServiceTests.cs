#nullable enable
// Author: Vladyslav Zaiets

using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class BackupServiceTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly Mock<ILogger<BackupService>> _loggerMock;
        private readonly Mock<IBackupRepository> _repositoryMock;
        private readonly Mock<IStorageService> _storageServiceMock;
        private readonly AppSettings _appSettings;
        private readonly BackupService _backupService;

        public BackupServiceTests()
        {
            // Create a unique test directory
            _testDirectory = Path.Combine(Path.GetTempPath(), $"docker-sqlite-backup-test-{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDirectory);

            // Create a temporary database path
            _dbPath = Path.Combine(_testDirectory, "test.db");
            _connectionString = $"Data Source={_dbPath}";

            // Setup mocks
            _loggerMock = new Mock<ILogger<BackupService>>();
            _repositoryMock = new Mock<IBackupRepository>();
            _storageServiceMock = new Mock<IStorageService>();

            // Configure AppSettings for testing (no compression, no encryption, local backups)
            _appSettings = new AppSettings
            {
                CompressBackups = false,
                EnableEncryption = false,
                LocalStoragePath = _testDirectory // Set backup location to our test directory
            };

            // Create the backup service
            _backupService = new BackupService(
                _repositoryMock.Object,
                _storageServiceMock.Object,
                _appSettings,
                _loggerMock.Object);
        }

        public void Dispose()
        {
            // Clean up test directory
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }

        private void InitializeDatabaseWithWalMode()
        {
            // Ensure the database file does not exist
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            // Create database and enable WAL mode
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            // Create a test table
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TestTable (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Value TEXT NOT NULL,
                    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );
                CREATE INDEX IF NOT EXISTS IX_TestTable_Value ON TestTable(Value);
            ";
            command.ExecuteNonQuery();

            // Enable WAL mode
            command.CommandText = "PRAGMA journal_mode=WAL;";
            command.ExecuteNonQuery();

            // Insert some initial rows
            command.CommandText = "INSERT INTO TestTable (Value) VALUES ('Initial1'), ('Initial2'), ('Initial3');";
            command.ExecuteNonQuery();
        }

        [Fact]
        public async Task ExecuteBackupAsync_WithConcurrentWrites_ProducesConsistentBackup()
        {
            // Arrange
            InitializeDatabaseWithWalMode();

            // Create a backup schedule pointing to our test database
            var scheduleId = Guid.NewGuid();
            var schedule = new BackupSchedule
            {
                Id = scheduleId,
                Name = "TestSchedule",
                DatabasePath = _dbPath,
                CronExpression = "* * * * *", // Every minute (not used in this test)
                CreatedAt = DateTime.UtcNow,
                LastModifiedAt = DateTime.UtcNow,
                BackupMode = (int)DockerSqliteBackup.Constants.BackupMode.Full,
                StorageConfiguration = new LocalStorageConfiguration() // Use local storage
            };

            // Setup repository mock to return the schedule and accept backup results
            _repositoryMock.Setup(r => r.GetScheduleAsync(scheduleId))
                .ReturnsAsync(schedule);
            _repositoryMock.Setup(r => r.CreateBackupResultAsync(It.IsAny<BackupResult>()))
                .ReturnsAsync((BackupResult result) => result);

            // Setup storage service mock to do nothing (local storage)
            _storageServiceMock.Setup(s => s.UploadBackupAsync(It.IsAny<string>(), It.IsAny<StorageConfiguration>()))
                .ReturnsAsync(string.Empty);

            // Start background task to simulate concurrent writes
            using var cts = new CancellationTokenSource();
            var writeTask = Task.Run(async () =>
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                var insertCommand = connection.CreateCommand();
                insertCommand.CommandText = "INSERT INTO TestTable (Value) VALUES ($value);";
                var param = insertCommand.CreateParameter();
                param.ParameterName = "$value";
                insertCommand.Parameters.Add(param);

                int i = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    param.Value = $"ConcurrentWrite_{i++}";
                    await insertCommand.ExecuteNonQueryAsync();
                    // Small delay to allow for interleaving with backup operation
                    await Task.Delay(10);
                }
            }, cts.Token);

            try
            {
                // Act: Execute the backup
                var result = await _backupService.ExecuteBackupAsync(schedule, CancellationToken.None);

                // Assert: Backup should succeed
                result.Should().NotBeNull();
                result.Status.Should().Be((int)DockerSqliteBackup.Constants.BackupStatus.Success);
                result.BackupFilePath.Should().NotBeNullOrEmpty();
                File.Exists(result.BackupFilePath).Should().BeTrue();

                // Stop the background write task
                cts.Cancel();
                await writeTask; // Wait for the write task to finish

                // Now verify the backup file is consistent
                using var backupConnection = new SqliteConnection($"Data Source={result.BackupFilePath};Mode=ReadOnly");
                await backupConnection.OpenAsync();

                // Check integrity
                using var integrityCommand = backupConnection.CreateCommand();
                integrityCommand.CommandText = "PRAGMA integrity_check;";
                var integrityResult = await integrityCommand.ExecuteScalarAsync();
                integrityResult.Should().Be("ok");

                // Check row count - should be at least the initial rows we inserted
                using var countCommand = backupConnection.CreateCommand();
                countCommand.CommandText = "SELECT COUNT(*) FROM TestTable;";
                var rowCount = await countCommand.ExecuteScalarAsync();
                ((long)rowCount).Should().BeGreaterOrEqualTo(3L); // We inserted 3 initial rows

                // Additionally, we can verify that there are no partial transactions by checking
                // that the database is in a consistent state (integrity check already does this)
                // We could also check that the WAL file is empty (since we used Online Backup API)
                // but that's implementation-specific. The integrity check is sufficient.
            }
            finally
            {
                // Ensure we clean up the background task even if the test fails
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }

                if (!writeTask.IsCompleted)
                {
                    await writeTask;
                }
            }
        }
    }
}