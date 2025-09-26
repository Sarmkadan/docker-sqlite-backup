// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public static class ScheduleServiceTestsExtensions
{
    public static BackupSchedule CreateValidSchedule(this ScheduleServiceTests tests)
    {
        return new BackupSchedule
        {
            Name = "Test Backup",
            DatabasePath = "/data/test.db",
            CronExpression = "0 2 * * *",
            IsActive = true
        };
    }

    public static BackupSchedule CreateInvalidSchedule(this ScheduleServiceTests tests)
    {
        return new BackupSchedule
        {
            Name = "",
            DatabasePath = "/data/test.db",
            CronExpression = "0 2 * * *",
            IsActive = true
        };
    }

    public static BackupSchedule CreateScheduleWithInvalidCron(this ScheduleServiceTests tests)
    {
        return new BackupSchedule
        {
            Name = "Test Backup",
            DatabasePath = "/data/test.db",
            CronExpression = "invalid-cron-expression",
            IsActive = true
        };
    }

    public static void AssertScheduleIsActive(this ScheduleServiceTests tests, BackupSchedule schedule, bool expectedIsActive)
    {
        schedule.IsActive.Should().Be(expectedIsActive);
    }

    public static void AssertScheduleHasId(this ScheduleServiceTests tests, BackupSchedule schedule, Guid expectedId)
    {
        schedule.Id.Should().Be(expectedId);
    }

    public static void AssertScheduleHasName(this ScheduleServiceTests tests, BackupSchedule schedule, string expectedName)
    {
        schedule.Name.Should().Be(expectedName);
    }

    public static void AssertScheduleHasDatabasePath(this ScheduleServiceTests tests, BackupSchedule schedule, string expectedPath)
    {
        schedule.DatabasePath.Should().Be(expectedPath);
    }
}