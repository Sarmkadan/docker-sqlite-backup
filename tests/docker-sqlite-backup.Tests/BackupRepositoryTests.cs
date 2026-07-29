using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using FluentAssertions;

namespace DockerSqliteBackup.Tests
{
    public class BackupRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly string _connectionString;
        private readonly Mock<ILogger<BackupRepository>> _loggerMock;
        private readonly BackupRepository _repository;

        public BackupRepositoryTests()
        {
            _dbPath = $"test_{Guid.NewGuid()}.db";
            _connectionString = $"Data Source={_dbPath}";
            _loggerMock = new Mock<ILogger<BackupRepository>>();
            _repository = new BackupRepository(_connectionString, _loggerMock.Object);
        }

        public void Dispose()
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }

        [Fact]
        public async Task InitializeAsync_CreatesTables()
        {
            await _repository.InitializeAsync();
            
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='BackupSchedules'";
            var result = await command.ExecuteScalarAsync();
            
            result.Should().Be("BackupSchedules");
        }

        [Fact]
        public async Task HealthCheckAsync_ReturnsTrue_WhenConnectionIsSuccessful()
        {
            await _repository.InitializeAsync();
            var isHealthy = await _repository.HealthCheckAsync();
            
            isHealthy.Should().BeTrue();
        }

        [Fact]
        public async Task CreateScheduleAsync_SavesScheduleSuccessfully()
        {
            await _repository.InitializeAsync();
            var schedule = new BackupSchedule 
            { 
                Id = Guid.NewGuid(), 
                Name = "TestSchedule", 
                DatabasePath = "/path/to/db", 
                CronExpression = "* * * * *", 
                CreatedAt = DateTime.UtcNow, 
                LastModifiedAt = DateTime.UtcNow 
            };

            await _repository.CreateScheduleAsync(schedule);
            var savedSchedule = await _repository.GetScheduleAsync(schedule.Id);
            
            savedSchedule.Should().NotBeNull();
            savedSchedule!.Name.Should().Be(schedule.Name);
        }

        [Fact]
        public async Task GetScheduleAsync_ReturnsNull_WhenScheduleNotFound()
        {
            await _repository.InitializeAsync();
            var result = await _repository.GetScheduleAsync(Guid.NewGuid());
            
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateBackupResultAsync_SavesResultSuccessfully()
        {
            await _repository.InitializeAsync();
            var scheduleId = Guid.NewGuid();
            var jobId = Guid.NewGuid();
            
            // Create and save schedule first due to foreign key constraint
            var schedule = new BackupSchedule 
            { 
                Id = scheduleId, 
                Name = "ResultTestSchedule", 
                DatabasePath = "/path/to/db", 
                CronExpression = "* * * * *", 
                CreatedAt = DateTime.UtcNow, 
                LastModifiedAt = DateTime.UtcNow 
            };
            await _repository.CreateScheduleAsync(schedule);

            var result = new BackupResult 
            { 
                Id = Guid.NewGuid(), 
                ScheduleId = scheduleId, 
                BackupJobId = jobId, 
                StartedAt = DateTime.UtcNow 
            };

            await _repository.CreateBackupResultAsync(result);
            
            result.Should().NotBeNull();
        }
    }
}
