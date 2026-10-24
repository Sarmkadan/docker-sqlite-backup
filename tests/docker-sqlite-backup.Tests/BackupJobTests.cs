using System;
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests
{
    public class BackupJobTests
    {
        [Fact]
        public void Defaults_ArePendingWithNoTimestampsOrResult()
        {
            var before = DateTime.UtcNow;
            var job = new BackupJob();

            job.Id.Should().NotBeEmpty();
            job.ScheduleId.Should().BeEmpty();
            job.Status.Should().Be((int)BackupStatus.Pending);
            job.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
            job.StartedAt.Should().BeNull();
            job.CompletedAt.Should().BeNull();
            job.RetryCount.Should().Be(0);
            job.MaxRetries.Should().Be(3);
            job.IsProcessing.Should().BeFalse();
            job.Result.Should().BeNull();
        }

        [Fact]
        public void MarkStarted_SetsStartedAtInProgressAndProcessingFlag()
        {
            var job = new BackupJob();
            var before = DateTime.UtcNow;

            job.MarkStarted();

            job.StartedAt.Should().NotBeNull();
            job.StartedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
            job.Status.Should().Be((int)BackupStatus.InProgress);
            job.IsProcessing.Should().BeTrue();
            job.CompletedAt.Should().BeNull();
        }

        [Fact]
        public void MarkCompleted_SetsStatusCompletedAtAndClearsProcessingFlag()
        {
            var job = new BackupJob();
            job.MarkStarted();
            var before = DateTime.UtcNow;

            job.MarkCompleted((int)BackupStatus.Failed);

            job.Status.Should().Be((int)BackupStatus.Failed);
            job.CompletedAt.Should().NotBeNull();
            job.CompletedAt!.Value.Should().BeOnOrAfter(before);
            job.IsProcessing.Should().BeFalse();
            job.StartedAt.Should().NotBeNull();
        }

        [Fact]
        public void GetElapsedTime_ReturnsZero_WhenJobNeverStarted()
        {
            new BackupJob().GetElapsedTime().Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void GetElapsedTime_UsesCompletedAt_WhenJobHasCompleted()
        {
            var start = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            var job = new BackupJob { StartedAt = start, CompletedAt = start.AddMinutes(2).AddSeconds(30) };

            job.GetElapsedTime().Should().Be(TimeSpan.FromSeconds(150));
        }

        [Theory]
        [InlineData(0, 3, (int)BackupStatus.Failed, true)]
        [InlineData(2, 3, (int)BackupStatus.Failed, true)]
        [InlineData(3, 3, (int)BackupStatus.Failed, false)]
        [InlineData(0, 3, (int)BackupStatus.Success, false)]
        [InlineData(0, 0, (int)BackupStatus.Failed, false)]
        public void CanRetry_RequiresFailedStatusAndRetriesLeft(int retryCount, int maxRetries, int status, bool expected)
        {
            var job = new BackupJob { RetryCount = retryCount, MaxRetries = maxRetries, Status = status };

            job.CanRetry.Should().Be(expected);
        }

        [Fact]
        public void IncrementRetry_RaisesRetryCountUntilCanRetryTurnsFalse()
        {
            var job = new BackupJob { Status = (int)BackupStatus.Failed };

            job.IncrementRetry();
            job.RetryCount.Should().Be(1);
            job.CanRetry.Should().BeTrue();

            job.IncrementRetry();
            job.IncrementRetry();
            job.RetryCount.Should().Be(3);
            job.MaxRetries.Should().Be(3);
            job.CanRetry.Should().BeFalse();
        }

        [Fact]
        public void ToString_IncludesIdsAndStatus()
        {
            var job = new BackupJob { ScheduleId = Guid.NewGuid(), Status = (int)BackupStatus.Success };

            var text = job.ToString();

            text.Should().Contain(job.Id.ToString());
            text.Should().Contain(job.ScheduleId.ToString());
            text.Should().Contain("Status = 2");
        }
    }
}
