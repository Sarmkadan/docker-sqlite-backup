// tests/docker-sqlite-backup.Tests/BackupScheduleExtensionsTests.cs
using System;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class BackupScheduleExtensionsTests
{
    private static BackupSchedule CreateSchedule(
        bool isActive = true,
        string? cronExpression = "* * * * *",
        string? name = "TestBackup",
        int backupMode = (int)Constants.BackupMode.Full,
        DateTime? lastBackupAt = null)
    {
        var schedule = new BackupSchedule
        {
            IsActive = isActive,
            CronExpression = cronExpression,
            Name = name,
            BackupMode = backupMode,
            LastBackupAt = lastBackupAt
        };

        return schedule;
    }

    [Fact]
    public void GetNextRunTime_ValidCron_ReturnsFutureDate()
    {
        // Arrange
        var schedule = CreateSchedule(cronExpression: "* * * * *"); // every minute

        // Act
        var next = schedule.GetNextRunTime();

        // Assert
        Assert.NotNull(next);
        Assert.True(next > DateTime.UtcNow);
    }

    [Fact]
    public void GetNextRunTime_InvalidCron_ReturnsNull()
    {
        // Arrange
        var schedule = CreateSchedule(cronExpression: "invalid cron");

        // Act
        var next = schedule.GetNextRunTime();

        // Assert
        Assert.Null(next);
    }

    [Fact]
    public void GetNextRunTime_InactiveOrEmptyCron_ReturnsNull()
    {
        // Inactive schedule
        var inactive = CreateSchedule(isActive: false, cronExpression: "* * * * *");
        Assert.Null(inactive.GetNextRunTime());

        // Empty cron expression
        var emptyCron = CreateSchedule(isActive: true, cronExpression: string.Empty);
        Assert.Null(emptyCron.GetNextRunTime());
    }

    [Fact]
    public void ShouldPerformBackup_InactiveSchedule_ReturnsFalse()
    {
        // Arrange
        var schedule = CreateSchedule(isActive: false);

        // Act
        var result = schedule.ShouldPerformBackup();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldPerformBackup_InvalidCron_ReturnsFalse()
    {
        // Arrange
        var schedule = CreateSchedule(cronExpression: "not a cron");

        // Act
        var result = schedule.ShouldPerformBackup();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldPerformBackup_NoLastBackup_ReturnsTrue()
    {
        // Arrange
        var schedule = CreateSchedule(lastBackupAt: null);

        // Act
        var result = schedule.ShouldPerformBackup();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetBackupName_WithName_FormatsCorrectly()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 01, 02, 15, 04, 05, DateTimeKind.Utc);
        var schedule = CreateSchedule(name: "My Backup");

        // Act
        var backupName = schedule.GetBackupName(fixedTime);

        // Assert
        Assert.Equal("my_backup_20230102_150405", backupName);
    }

    [Fact]
    public void GetBackupName_NullOrWhiteSpaceName_UsesDefault()
    {
        // Arrange
        var fixedTime = new DateTime(2023, 01, 02, 15, 04, 05, DateTimeKind.Utc);
        var schedule = CreateSchedule(name: "   ");

        // Act
        var backupName = schedule.GetBackupName(fixedTime);

        // Assert
        Assert.Equal("backup_20230102_150405", backupName);
    }

    [Fact]
    public void GetBackupFileExtension_FullMode_ReturnsFullExtension()
    {
        // Arrange
        var schedule = CreateSchedule(backupMode: (int)Constants.BackupMode.Full);

        // Act
        var ext = schedule.GetBackupFileExtension();

        // Assert
        Assert.Equal(".full.db", ext);
    }

    [Fact]
    public void GetBackupFileExtension_IncrementalMode_ReturnsIncExtension()
    {
        // Arrange
        var schedule = CreateSchedule(backupMode: (int)Constants.BackupMode.Incremental);

        // Act
        var ext = schedule.GetBackupFileExtension();

        // Assert
        Assert.Equal(".inc.db", ext);
    }

    [Fact]
    public void GetBackupFileExtension_UnknownMode_ReturnsDefaultExtension()
    {
        // Arrange
        var schedule = CreateSchedule(backupMode: 9999);

        // Act
        var ext = schedule.GetBackupFileExtension();

        // Assert
        Assert.Equal(".db", ext);
    }
}
