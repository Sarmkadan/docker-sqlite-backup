using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Constants;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

public class BackupJobExtensionsTests
{
    [Fact]
    public void IsSuccessful_ReturnsTrue_WhenStatusIsSuccess()
    {
        var job = new BackupJob { Status = (int)BackupStatus.Success };
        job.IsSuccessful().Should().BeTrue();
    }

    [Fact]
    public void IsFailed_ReturnsTrue_WhenStatusIsFailed()
    {
        var job = new BackupJob { Status = (int)BackupStatus.Failed };
        job.IsFailed().Should().BeTrue();
    }

    [Fact]
    public void IsPending_ReturnsTrue_WhenStatusIsPendingAndNotStarted()
    {
        var job = new BackupJob { Status = (int)BackupStatus.Pending, IsProcessing = false, StartedAt = null };
        job.IsPending().Should().BeTrue();
    }

    [Fact]
    public void IsInProgress_ReturnsTrue_WhenStatusIsInProgressAndProcessing()
    {
        var job = new BackupJob { Status = (int)BackupStatus.InProgress, IsProcessing = true };
        job.IsInProgress().Should().BeTrue();
    }

    [Fact]
    public void GetFormattedDuration_ReturnsExpectedFormat()
    {
        var startedAt = new DateTime(2025, 1, 1, 10, 0, 0);
        var completedAt = new DateTime(2025, 1, 1, 11, 29, 30);
        var job = new BackupJob { StartedAt = startedAt, CompletedAt = completedAt };
        
        // 1h 29m 30s
        job.GetFormattedDuration().Should().Be("1h 29m 30s");
    }

    [Fact]
    public void HasExceededRetries_ReturnsTrue_WhenRetryCountEqualsMaxRetries()
    {
        var job = new BackupJob { RetryCount = 3, MaxRetries = 3 };
        job.HasExceededRetries().Should().BeTrue();
    }

    [Fact]
    public void GetResult_ReturnsResult()
    {
        var result = new BackupResult();
        var job = new BackupJob { Result = result };
        job.GetResult().Should().Be(result);
    }
}
