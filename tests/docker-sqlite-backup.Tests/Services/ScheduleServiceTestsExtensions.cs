// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// =============================================================================

using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Extension methods for creating test schedules and asserting schedule properties in ScheduleService tests.
/// </summary>
public static class ScheduleServiceTestsExtensions
{
    /// <summary>
    /// Creates a valid backup schedule with default test values.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <returns>A valid <see cref="BackupSchedule"/> instance with name "Test Backup", database path "/data/test.db", cron expression "0 2 * * *", and active status.</returns>
    public static BackupSchedule CreateValidSchedule(this ScheduleServiceTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return new BackupSchedule
        {
            Name = "Test Backup",
            DatabasePath = "/data/test.db",
            CronExpression = "0 2 * * *",
            IsActive = true
        };
    }

    /// <summary>
    /// Creates an invalid backup schedule with an empty name.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <returns>An invalid <see cref="BackupSchedule"/> instance with empty name, valid database path, cron expression, and active status.</returns>
    public static BackupSchedule CreateInvalidSchedule(this ScheduleServiceTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return new BackupSchedule
        {
            Name = string.Empty,
            DatabasePath = "/data/test.db",
            CronExpression = "0 2 * * *",
            IsActive = true
        };
    }

    /// <summary>
    /// Creates a backup schedule with an invalid cron expression.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <returns>A <see cref="BackupSchedule"/> instance with valid name and database path, but invalid cron expression "invalid-cron-expression", and active status.</returns>
    public static BackupSchedule CreateScheduleWithInvalidCron(this ScheduleServiceTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        return new BackupSchedule
        {
            Name = "Test Backup",
            DatabasePath = "/data/test.db",
            CronExpression = "invalid-cron-expression",
            IsActive = true
        };
    }

    /// <summary>
    /// Asserts that a schedule has the expected active status.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <param name="schedule">The schedule to assert.</param>
    /// <param name="expectedIsActive">The expected active status to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedule"/> is null.</exception>
    public static void AssertScheduleIsActive(this ScheduleServiceTests tests, BackupSchedule schedule, bool expectedIsActive)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        schedule.IsActive.Should().Be(expectedIsActive);
    }

    /// <summary>
    /// Asserts that a schedule has the expected identifier.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <param name="schedule">The schedule to assert.</param>
    /// <param name="expectedId">The expected identifier to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedule"/> is null.</exception>
    public static void AssertScheduleHasId(this ScheduleServiceTests tests, BackupSchedule schedule, Guid expectedId)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        schedule.Id.Should().Be(expectedId);
    }

    /// <summary>
    /// Asserts that a schedule has the expected name.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <param name="schedule">The schedule to assert.</param>
    /// <param name="expectedName">The expected name to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedule"/> or <paramref name="expectedName"/> is null.</exception>
    public static void AssertScheduleHasName(this ScheduleServiceTests tests, BackupSchedule schedule, string expectedName)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(expectedName);

        schedule.Name.Should().Be(expectedName);
    }

    /// <summary>
    /// Asserts that a schedule has the expected database path.
    /// </summary>
    /// <param name="tests">The test instance used for validation.</param>
    /// <param name="schedule">The schedule to assert.</param>
    /// <param name="expectedPath">The expected database path to verify.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="schedule"/> or <paramref name="expectedPath"/> is null.</exception>
    public static void AssertScheduleHasDatabasePath(this ScheduleServiceTests tests, BackupSchedule schedule, string expectedPath)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentNullException.ThrowIfNull(expectedPath);

        schedule.DatabasePath.Should().Be(expectedPath);
    }
}
