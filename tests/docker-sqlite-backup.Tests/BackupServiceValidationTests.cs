#nullable enable

// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class BackupServiceValidationTests
{
    private static BackupService CreateService() => new(
        new Mock<IBackupRepository>().Object,
        new Mock<IStorageService>().Object,
        new AppSettings(),
        new Mock<ILogger<BackupService>>().Object);

    private static BackupResult CreateValidResult() => new()
    {
        ScheduleId = Guid.NewGuid(),
        Status = (int)BackupStatus.Success,
        BackupFilePath = "/backups/app.db.gz",
        BackupFileSizeBytes = 1024,
        Checksum = new string('a', 64),
        StartedAt = new DateTime(2026, 1, 1, 2, 0, 0, DateTimeKind.Utc),
        CompletedAt = new DateTime(2026, 1, 1, 2, 1, 0, DateTimeKind.Utc),
        DurationMilliseconds = 60_000,
        BackupMode = (int)BackupMode.Full
    };

    private static BackupSchedule CreateValidSchedule() => new()
    {
        Name = "nightly",
        DatabasePath = "/data/app.db",
        CronExpression = "0 2 * * *",
        RetentionDays = 30,
        MaxBackupCount = 10,
        BackupMode = (int)BackupMode.Full
    };

    [Fact]
    public void BackupService_ValidInstance_IsValidAndEnsureValidDoesNotThrow()
    {
        BackupService? service = CreateService();

        service.Validate().Should().BeEmpty();
        service.IsValid().Should().BeTrue();
        service.Invoking(s => s.EnsureValid()).Should().NotThrow();
    }

    [Fact]
    public void BackupService_Null_ValidateThrowsButIsValidReturnsFalse()
    {
        BackupService? service = null;
        Action validate = () => service.Validate();
        Action ensureValid = () => service.EnsureValid();

        validate.Should().Throw<ArgumentNullException>().WithParameterName("value");
        service.IsValid().Should().BeFalse();
        ensureValid.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void BackupResult_ValidResult_HasNoProblems()
    {
        BackupResult? result = CreateValidResult();

        result.Validate().Should().BeEmpty();
        result.IsValid().Should().BeTrue();
        result.Invoking(r => r.EnsureValid()).Should().NotThrow();
    }

    [Fact]
    public void BackupResult_MultipleViolations_ReportsEachProblem()
    {
        var result = CreateValidResult();
        result.ScheduleId = Guid.Empty;
        result.Status = 7;
        result.BackupFileSizeBytes = -1;
        result.Checksum = "xyz";

        var problems = result.Validate();

        problems.Should().BeEquivalentTo(new[]
        {
            "ScheduleId must be a non-empty GUID.",
            "Status must be a valid BackupStatus value (0-6).",
            "BackupFileSizeBytes cannot be negative.",
            "Checksum must be a 64-character hexadecimal string (SHA256)."
        });
        result.IsValid().Should().BeFalse();
    }

    [Fact]
    public void BackupResult_FailedWithoutDiagnostics_EnsureValidThrowsArgumentException()
    {
        var result = CreateValidResult();
        result.Status = (int)BackupStatus.Failed;

        var act = () => result.EnsureValid();

        act.Should().Throw<ArgumentException>().WithParameterName("result")
            .Which.InnerException!.Message
            .Should().Contain("ErrorMessage must be set when Status is Failed.");
    }

    [Fact]
    public void BackupSchedule_ValidSchedule_HasNoProblemsAndNullIsInvalid()
    {
        BackupSchedule? schedule = CreateValidSchedule();
        BackupSchedule? nullSchedule = null;

        // BackupSchedule declares its own instance IsValid(), which shadows the extension,
        // so the extension is called explicitly here.
        schedule.Validate().Should().BeEmpty();
        BackupServiceValidation.IsValid(schedule).Should().BeTrue();
        schedule.Invoking(s => s.EnsureValid()).Should().NotThrow();
        BackupServiceValidation.IsValid(nullSchedule).Should().BeFalse();
        Action validateNull = () => nullSchedule.Validate();
        validateNull.Should().Throw<ArgumentNullException>().WithParameterName("schedule");
    }

    [Fact]
    public void BackupSchedule_InvalidCronAndEmail_ReportsBothProblems()
    {
        var schedule = CreateValidSchedule();
        schedule.CronExpression = "not a cron";
        schedule.NotificationEmails = "ops@example.com; broken-address";

        schedule.Validate().Should().BeEquivalentTo(new[]
        {
            "CronExpression must be a valid cron expression.",
            "Notification email 'broken-address' is not a valid email address."
        });
    }

    [Theory]
    [InlineData(0, 10, false)]
    [InlineData(1, 1, true)]
    [InlineData(3650, 1000, true)]
    [InlineData(3651, 1001, false)]
    public void BackupSchedule_RetentionAndCountBoundaries_AreEnforced(int retentionDays, int maxBackupCount, bool expectedValid)
    {
        var schedule = CreateValidSchedule();
        schedule.RetentionDays = retentionDays;
        schedule.MaxBackupCount = maxBackupCount;

        BackupServiceValidation.IsValid(schedule).Should().Be(expectedValid);
    }
}
