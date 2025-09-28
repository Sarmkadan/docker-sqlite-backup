// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Caching;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Extensions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

public class ScheduleServiceTests
{
    private readonly Mock<IBackupRepository> _repositoryMock;
    private readonly Mock<ILogger<ScheduleService>> _loggerMock;
    private readonly ScheduleService _sut;

    public ScheduleServiceTests()
    {
        _repositoryMock = new Mock<IBackupRepository>();
        _loggerMock = new Mock<ILogger<ScheduleService>>();
        _sut = new ScheduleService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ValidateCronExpression_ValidExpression_ReturnsTrue()
    {
        _sut.ValidateCronExpression("0 2 * * *").Should().BeTrue();
    }

    [Fact]
    public void ValidateCronExpression_InvalidExpression_ReturnsFalse()
    {
        _sut.ValidateCronExpression("not-a-cron").Should().BeFalse();
    }

    [Fact]
    public void GetNextExecutionTime_ValidSchedule_ReturnsDateInFuture()
    {
        var schedule = new BackupSchedule { CronExpression = "0 2 * * *" };

        var next = _sut.GetNextExecutionTime(schedule);

        next.Should().NotBeNull();
        next!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GetNextExecutionTime_InvalidCronExpression_ReturnsNull()
    {
        var schedule = new BackupSchedule { CronExpression = "invalid-cron" };

        var next = _sut.GetNextExecutionTime(schedule);

        next.Should().BeNull();
    }

    [Fact]
    public async Task CreateScheduleAsync_ValidSchedule_DelegatesToRepository()
    {
        var schedule = new BackupSchedule
        {
            Name = "Nightly Backup",
            DatabasePath = "/data/app.db",
            CronExpression = "0 2 * * *"
        };
        _repositoryMock
            .Setup(r => r.CreateScheduleAsync(It.IsAny<BackupSchedule>()))
            .ReturnsAsync(schedule);

        var result = await _sut.CreateScheduleAsync(schedule);

        _repositoryMock.Verify(r => r.CreateScheduleAsync(It.IsAny<BackupSchedule>()), Times.Once);
        result.Should().NotBeNull();
        result.Name.Should().Be("Nightly Backup");
    }

    [Fact]
    public async Task CreateScheduleAsync_InvalidSchedule_ThrowsInvalidScheduleException()
    {
        var schedule = new BackupSchedule
        {
            Name = "",
            DatabasePath = "/data/app.db",
            CronExpression = "0 2 * * *"
        };

        await _sut.Invoking(s => s.CreateScheduleAsync(schedule))
            .Should().ThrowAsync<InvalidScheduleException>();
    }

    [Fact]
    public async Task CreateScheduleAsync_InvalidCronExpression_ThrowsInvalidCronExpressionException()
    {
        var schedule = new BackupSchedule
        {
            Name = "Nightly Backup",
            DatabasePath = "/data/app.db",
            CronExpression = "bad-cron"
        };

        await _sut.Invoking(s => s.CreateScheduleAsync(schedule))
            .Should().ThrowAsync<InvalidCronExpressionException>();
    }

    [Fact]
    public async Task GetScheduleAsync_ExistingId_ReturnsScheduleFromRepository()
    {
        var scheduleId = Guid.NewGuid();
        var schedule = new BackupSchedule { Id = scheduleId, Name = "Test" };
        _repositoryMock
            .Setup(r => r.GetScheduleAsync(scheduleId))
            .ReturnsAsync(schedule);

        var result = await _sut.GetScheduleAsync(scheduleId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(scheduleId);
    }

    [Fact]
    public async Task GetScheduleAsync_NonExistingId_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetScheduleAsync(It.IsAny<Guid>()))
            .ReturnsAsync((BackupSchedule?)null);

        var result = await _sut.GetScheduleAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateScheduleAsync_ExistingSchedule_UpdatesIsActiveFalse()
    {
        var scheduleId = Guid.NewGuid();
        var schedule = new BackupSchedule
        {
            Id = scheduleId,
            Name = "Active Backup",
            DatabasePath = "/data/app.db",
            CronExpression = "0 2 * * *",
            IsActive = true
        };
        _repositoryMock
            .Setup(r => r.GetScheduleAsync(scheduleId))
            .ReturnsAsync(schedule);
        _repositoryMock
            .Setup(r => r.UpdateScheduleAsync(It.IsAny<BackupSchedule>()))
            .ReturnsAsync(schedule);

        await _sut.DeactivateScheduleAsync(scheduleId);

        _repositoryMock.Verify(
            r => r.UpdateScheduleAsync(It.Is<BackupSchedule>(s => !s.IsActive)),
            Times.Once);
    }
}

public class CacheKeyBuilderTests
{
    [Fact]
    public void Build_WithStringParts_AlwaysIncludesBackupPrefix()
    {
        var key = new CacheKeyBuilder()
            .Add("schedules")
            .Add("all")
            .Build();

        key.Should().StartWith("backup:");
    }

    [Fact]
    public void Build_WithGuidPart_IncludesGuidInKey()
    {
        var id = Guid.NewGuid();

        var key = new CacheKeyBuilder()
            .Add("schedule")
            .Add(id)
            .Build();

        key.Should().Contain(id.ToString());
    }

    [Fact]
    public void Build_WithIntPart_IncludesIntValueInKey()
    {
        var key = new CacheKeyBuilder()
            .Add("history")
            .Add(25)
            .Build();

        key.Should().Contain("25");
    }

    [Fact]
    public void Add_WhitespaceOnlyString_IsSkipped()
    {
        var keyWithBlank = new CacheKeyBuilder().Add("   ").Build();
        var keyWithoutPart = new CacheKeyBuilder().Build();

        keyWithBlank.Should().Be(keyWithoutPart);
    }

    [Fact]
    public void Keys_Schedule_ReturnsExpectedFormat()
    {
        var id = Guid.NewGuid();

        var key = CacheKeyBuilder.Keys.Schedule(id);

        key.Should().Be($"backup:schedule:{id}");
    }

    [Fact]
    public void Keys_AllSchedules_ReturnsExpectedKey()
    {
        var key = CacheKeyBuilder.Keys.AllSchedules();

        key.Should().Be("backup:schedules:all");
    }

    [Fact]
    public void Keys_HealthStatus_ReturnsExpectedKey()
    {
        var key = CacheKeyBuilder.Keys.HealthStatus();

        key.Should().Be("backup:health:status");
    }

    [Fact]
    public void Keys_BackupHistory_IncludesLimitInKey()
    {
        var id = Guid.NewGuid();

        var key = CacheKeyBuilder.Keys.BackupHistory(id, limit: 50);

        key.Should().Contain("50");
    }
}

public class StringExtensionsTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("hello", false)]
    public void IsEmpty_VariousInputs_ReturnsExpected(string? input, bool expected)
    {
        input.IsEmpty().Should().Be(expected);
    }

    [Fact]
    public void OrDefault_NullInput_ReturnsDefaultValue()
    {
        string? value = null;

        value.OrDefault("fallback").Should().Be("fallback");
    }

    [Fact]
    public void OrDefault_NonNullInput_ReturnsOriginalValue()
    {
        "actual".OrDefault("fallback").Should().Be("actual");
    }

    [Fact]
    public void TruncateAt_ExceedsLimit_AppendsSuffix()
    {
        var result = "HelloWorld".TruncateAt(5);

        result.Should().Be("Hello...");
    }

    [Fact]
    public void TruncateAt_WithinLimit_ReturnsOriginal()
    {
        var result = "Hi".TruncateAt(10);

        result.Should().Be("Hi");
    }

    [Fact]
    public void ToGuid_ValidGuidString_ReturnsParsedGuid()
    {
        var guid = Guid.NewGuid();

        guid.ToString().ToGuid().Should().Be(guid);
    }

    [Fact]
    public void ToGuid_InvalidString_ReturnsNull()
    {
        "not-a-guid".ToGuid().Should().BeNull();
    }

    [Fact]
    public void QuoteIfNeeded_StringWithSpaces_WrapsInQuotes()
    {
        "hello world".QuoteIfNeeded().Should().Be("\"hello world\"");
    }

    [Fact]
    public void QuoteIfNeeded_StringWithoutSpaces_ReturnsOriginal()
    {
        "helloworld".QuoteIfNeeded().Should().Be("helloworld");
    }

    [Fact]
    public void First_RequestedCountLessThanLength_ReturnsFirstNChars()
    {
        "abcdef".First(3).Should().Be("abc");
    }

    [Fact]
    public void Last_RequestedCountLessThanLength_ReturnsLastNChars()
    {
        "abcdef".Last(3).Should().Be("def");
    }
}
