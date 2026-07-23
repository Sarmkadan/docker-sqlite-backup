using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace DockerSqliteBackup.Tests.Integration;

/// <summary>
/// Integration tests for backup integrity verification before rotation.
/// Ensures that old backups are preserved when new backup verification fails.
/// </summary>
public class BackupIntegrityVerificationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private string _testDataDirectory = string.Empty;
    private ServiceProvider? _serviceProvider;

    public BackupIntegrityVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _testDataDirectory = Path.Combine(Path.GetTempPath(), $"backup-integrity-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDataDirectory);

        // Setup service provider
        var services = new ServiceCollection();

        // Create in-memory database for tests
        var dbPath = Path.Combine(_testDataDirectory, "test.db");
        var connectionString = $"Data Source={dbPath}";

        // Setup repository
        var repository = new BackupRepository(connectionString,
            new Logger<BackupRepository>(new LoggerFactory()));
        await repository.InitializeAsync();

        // Setup services
        var appSettings = new AppSettings();
        var loggerFactory = new LoggerFactory();

        services.AddSingleton<IBackupRepository>(repository);
        services.AddSingleton(appSettings);
        services.AddSingleton<IVerificationService, VerificationService>();
        services.AddSingleton<IRotationService, RotationService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IScheduleService>(new Mock<IScheduleService>().Object);

        _serviceProvider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        if (Directory.Exists(_testDataDirectory))
        {
            try
            {
                Directory.Delete(_testDataDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    private ServiceProvider CreateServiceProvider()
    {
        if (_serviceProvider == null)
        {
            throw new InvalidOperationException("InitializeAsync must be called before tests run");
        }
        return _serviceProvider;
    }

    private async Task CreateTestDatabaseAsync(string dbPath, string testData)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS TestTable (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER
            );
            DELETE FROM TestTable;
            INSERT INTO TestTable (Name, Value) VALUES ('TestData', 123);
        ";
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Tests that when backup verification fails, old backups are NOT deleted during rotation.
    /// This ensures data safety - we never delete the last good backup until the new one is verified.
    /// </summary>
    [Fact]
    public async Task Rotation_ShouldNotDeleteOldBackups_WhenNewBackupVerificationFails()
    {
        // Arrange
        var services = CreateServiceProvider();
        var scheduleService = services.GetRequiredService<IScheduleService>();
        var backupService = services.GetRequiredService<IBackupService>();
        var verificationService = services.GetRequiredService<IVerificationService>();
        var rotationService = services.GetRequiredService<IRotationService>();
        var repository = services.GetRequiredService<IBackupRepository>();

        // Create a test schedule
        var schedule = new BackupSchedule
        {
            Name = "test-corrupt-backup-schedule",
            Description = "Test schedule for corrupt backup scenario",
            DatabasePath = Path.Combine(_testDataDirectory, "test-corrupt.db"),
            CronExpression = BackupConstants.CronDailyAtMidnight,
            IsActive = true,
            RetentionDays = 7,
            MaxBackupCount = 5,
            VerifyAfterBackup = true
        };

        // Create a test database
        await CreateTestDatabaseAsync(schedule.DatabasePath, "TestData");

        // Create rotation policy with VerifyBeforeDeletion enabled
        var rotationPolicy = new RotationPolicy
        {
            ScheduleId = schedule.Id,
            Strategy = (int)RotationStrategy.Combined,
            MaxBackupCount = 3,
            MaxAgeDays = 30,
            VerifyBeforeDeletion = true, // This is the key setting
            MinimumBackupCount = 2,
            DeleteFailedBackups = true
        };

        await repository.CreateScheduleAsync(schedule);
        await repository.SaveRotationPolicyAsync(rotationPolicy);

        // Create initial backup (this will be our "old good backup")
        var oldBackup = await backupService.ExecuteBackupAsync(schedule, default);
        Assert.True(oldBackup.IsSuccess);
        Assert.Equal((int)BackupStatus.Success, oldBackup.Status);

        // Verify the old backup
        var oldVerification = await verificationService.VerifyBackupAsync(oldBackup, default);
        Assert.True(oldVerification.IsSuccessful);
        oldBackup.Status = (int)BackupStatus.VerifiedSuccess;
        oldBackup.IsVerified = true;
        oldBackup.VerifiedAt = DateTime.UtcNow;
        await repository.UpdateBackupResultAsync(oldBackup);

        _output.WriteLine("Created and verified initial backup: {0}", oldBackup.Id);

        // Create second backup (another good one)
        var secondBackup = await backupService.ExecuteBackupAsync(schedule, default);
        Assert.True(secondBackup.IsSuccess);
        var secondVerification = await verificationService.VerifyBackupAsync(secondBackup, default);
        Assert.True(secondVerification.IsSuccessful);
        secondBackup.Status = (int)BackupStatus.VerifiedSuccess;
        secondBackup.IsVerified = true;
        secondBackup.VerifiedAt = DateTime.UtcNow;
        await repository.UpdateBackupResultAsync(secondBackup);

        _output.WriteLine("Created and verified second backup: {0}", secondBackup.Id);

        // Get count before creating corrupt backup
        var backupsBefore = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        _output.WriteLine("Backups before corrupt backup: {0}", backupsBefore);

        // Create a corrupt backup (simulate by creating a file with invalid SQLite header)
        var corruptBackupPath = Path.Combine(_testDataDirectory, "corrupt-backup.sqlite");
        await File.WriteAllTextAsync(corruptBackupPath, "This is not a valid SQLite database file");
        var corruptBackup = new BackupResult
        {
            ScheduleId = schedule.Id,
            BackupFilePath = corruptBackupPath,
            BackupFileSizeBytes = new FileInfo(corruptBackupPath).Length,
            Checksum = "corrupt",
            Status = (int)BackupStatus.Success, // Backup creation succeeded but file is corrupt
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            DurationMilliseconds = 100
        };
        await repository.CreateBackupResultAsync(corruptBackup);

        _output.WriteLine("Created corrupt backup: {0}", corruptBackup.Id);

        // Get count after creating corrupt backup
        var backupsAfterCorrupt = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        _output.WriteLine("Backups after corrupt backup: {0}", backupsAfterCorrupt);

        // Attempt to verify the corrupt backup - this should fail
        var corruptVerification = await verificationService.VerifyBackupAsync(corruptBackup, default);
        Assert.False(corruptVerification.IsSuccessful);
        _output.WriteLine("Corrupt backup verification failed as expected: {0}", corruptVerification.StatusMessage);

        // Now attempt rotation - it should NOT delete any backups because:
        // 1. The corrupt backup failed verification
        // 2. VerifyBeforeDeletion is enabled
        // 3. Old backups should be preserved
        var deletedCount = await rotationService.ExecuteRotationAsync(schedule.Id);
        _output.WriteLine("Rotation attempted to delete {0} backups", deletedCount);

        // Get count after rotation
        var backupsAfterRotation = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        _output.WriteLine("Backups after rotation: {0}", backupsAfterRotation);

        // Assert that no backups were deleted (all 3 should still exist)
        Assert.Equal(backupsAfterCorrupt, backupsAfterRotation);
        Assert.Equal(3, backupsAfterRotation);

        // Verify that the old verified backups still exist
        var remainingBackups = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue))
            .OrderByDescending(b => b.StartedAt)
            .ToList();

        Assert.Equal(3, remainingBackups.Count);
        Assert.Contains(oldBackup.Id, remainingBackups.Select(b => b.Id));
        Assert.Contains(secondBackup.Id, remainingBackups.Select(b => b.Id));
        Assert.Contains(corruptBackup.Id, remainingBackups.Select(b => b.Id));

        _output.WriteLine("✓ All backups preserved after corrupt backup verification failure");

        // Cleanup
        foreach (var backup in remainingBackups)
        {
            if (File.Exists(backup.BackupFilePath))
            {
                File.Delete(backup.BackupFilePath);
            }
        }
        File.Delete(corruptBackupPath);
    }

    /// <summary>
    /// Tests that when backup verification succeeds, old backups CAN be deleted during rotation.
    /// This ensures rotation works correctly when new backups are verified.
    /// </summary>
    [Fact]
    public async Task Rotation_ShouldDeleteOldBackups_WhenNewBackupVerificationSucceeds()
    {
        // Arrange
        var services = CreateServiceProvider();
        var scheduleService = services.GetRequiredService<IScheduleService>();
        var backupService = services.GetRequiredService<IBackupService>();
        var verificationService = services.GetRequiredService<IVerificationService>();
        var rotationService = services.GetRequiredService<IRotationService>();
        var repository = services.GetRequiredService<IBackupRepository>();

        // Create a test schedule
        var schedule = new BackupSchedule
        {
            Name = "test-good-backup-schedule",
            Description = "Test schedule for successful backup scenario",
            DatabasePath = Path.Combine(_testDataDirectory, "test-good.db"),
            CronExpression = BackupConstants.CronDailyAtMidnight,
            IsActive = true,
            RetentionDays = 7,
            MaxBackupCount = 3,
            VerifyAfterBackup = true
        };

        // Create a test database
        await CreateTestDatabaseAsync(schedule.DatabasePath, "TestData");

        // Create rotation policy with VerifyBeforeDeletion enabled
        var rotationPolicy = new RotationPolicy
        {
            ScheduleId = schedule.Id,
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 3,
            MaxAgeDays = 30,
            VerifyBeforeDeletion = true, // This is the key setting
            MinimumBackupCount = 1,
            DeleteFailedBackups = true
        };

        await repository.CreateScheduleAsync(schedule);
        await repository.SaveRotationPolicyAsync(rotationPolicy);

        // Create 5 backups with verification
        for (int i = 0; i < 5; i++)
        {
            var backup = await backupService.ExecuteBackupAsync(schedule, default);
            Assert.True(backup.IsSuccess);

            var verification = await verificationService.VerifyBackupAsync(backup, default);
            Assert.True(verification.IsSuccessful);

            backup.Status = (int)BackupStatus.VerifiedSuccess;
            backup.IsVerified = true;
            backup.VerifiedAt = DateTime.UtcNow;
            await repository.UpdateBackupResultAsync(backup);
        }

        _output.WriteLine("Created and verified 5 backups");

        // Get count before rotation
        var backupsBefore = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        Assert.Equal(5, backupsBefore);
        _output.WriteLine("Backups before rotation: {0}", backupsBefore);

        // Execute rotation - should delete 2 oldest backups (keeping 3 newest verified ones)
        var deletedCount = await rotationService.ExecuteRotationAsync(schedule.Id);
        _output.WriteLine("Rotation deleted {0} backups", deletedCount);

        // Get count after rotation
        var backupsAfter = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        _output.WriteLine("Backups after rotation: {0}", backupsAfter);

        // Assert that 2 backups were deleted (5 - 3 = 2)
        Assert.Equal(3, backupsAfter);
        Assert.Equal(2, deletedCount);

        // Verify that only the 3 newest verified backups remain
        var remainingBackups = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue))
            .OrderByDescending(b => b.StartedAt)
            .ToList();

        Assert.All(remainingBackups, b => Assert.Equal((int)BackupStatus.VerifiedSuccess, b.Status));
        Assert.Equal(3, remainingBackups.Count);

        _output.WriteLine("✓ Old backups correctly deleted after successful verification");

        // Cleanup
        foreach (var backup in remainingBackups)
        {
            if (File.Exists(backup.BackupFilePath))
            {
                File.Delete(backup.BackupFilePath);
            }
        }
        File.Delete(schedule.DatabasePath);
    }

    /// <summary>
    /// Tests that when VerifyBeforeDeletion is disabled, rotation works regardless of verification status.
    /// </summary>
    [Fact]
    public async Task Rotation_ShouldDeleteOldBackups_WhenVerifyBeforeDeletionDisabled()
    {
        // Arrange
        var services = CreateServiceProvider();
        var scheduleService = services.GetRequiredService<IScheduleService>();
        var backupService = services.GetRequiredService<IBackupService>();
        var verificationService = services.GetRequiredService<IVerificationService>();
        var rotationService = services.GetRequiredService<IRotationService>();
        var repository = services.GetRequiredService<IBackupRepository>();

        // Create a test schedule
        var schedule = new BackupSchedule
        {
            Name = "test-no-verify-schedule",
            Description = "Test schedule with verification disabled",
            DatabasePath = Path.Combine(_testDataDirectory, "test-noverify.db"),
            CronExpression = BackupConstants.CronDailyAtMidnight,
            IsActive = true,
            RetentionDays = 7,
            MaxBackupCount = 3,
            VerifyAfterBackup = false // Verification disabled
        };

        // Create a test database
        await CreateTestDatabaseAsync(schedule.DatabasePath, "TestData");

        // Create rotation policy WITHOUT VerifyBeforeDeletion
        var rotationPolicy = new RotationPolicy
        {
            ScheduleId = schedule.Id,
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 3,
            MaxAgeDays = 30,
            VerifyBeforeDeletion = false, // Verification NOT required before deletion
            MinimumBackupCount = 1,
            DeleteFailedBackups = true
        };

        await repository.CreateScheduleAsync(schedule);
        await repository.SaveRotationPolicyAsync(rotationPolicy);

        // Create 5 backups WITHOUT verification
        for (int i = 0; i < 5; i++)
        {
            var backup = await backupService.ExecuteBackupAsync(schedule, default);
            Assert.True(backup.IsSuccess);
            await repository.CreateBackupResultAsync(backup);
        }

        _output.WriteLine("Created 5 backups without verification");

        // Get count before rotation
        var backupsBefore = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        Assert.Equal(5, backupsBefore);

        // Execute rotation - should delete 2 oldest backups regardless of verification status
        var deletedCount = await rotationService.ExecuteRotationAsync(schedule.Id);
        _output.WriteLine("Rotation deleted {0} backups", deletedCount);

        // Get count after rotation
        var backupsAfter = (await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue)).Count();
        _output.WriteLine("Backups after rotation: {0}", backupsAfter);

        // Assert that 2 backups were deleted
        Assert.Equal(3, backupsAfter);
        Assert.Equal(2, deletedCount);

        _output.WriteLine("✓ Rotation works correctly when VerifyBeforeDeletion is disabled");

        // Cleanup
        var remainingBackups = await repository.GetBackupHistoryAsync(schedule.Id, int.MaxValue);
        foreach (var backup in remainingBackups)
        {
            if (File.Exists(backup.BackupFilePath))
            {
                File.Delete(backup.BackupFilePath);
            }
        }
        File.Delete(schedule.DatabasePath);
    }
}
