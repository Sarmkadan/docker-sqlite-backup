// Author: Vladyslav Zaiets

using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

/// <summary>
/// Unit tests for the <see cref="BackupJob"/> domain model.
/// Tests various operations on backup job lifecycle including status transitions, retry logic, and timing calculations.
/// </summary>
public class BackupJobTests
{
	/// <summary>
	/// Tests that marking a pending job as started updates its status to InProgress, sets processing flag, and records start time.
	/// </summary>
	[Fact]
	public void MarkStarted_PendingJob_SetsStatusToInProgress()
	{
		var job = new BackupJob();

		job.MarkStarted();

		job.Status.Should().Be((int)BackupStatus.InProgress);
		job.IsProcessing.Should().BeTrue();
		job.StartedAt.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that marking an in-progress job as completed updates its status to Success and clears the processing flag.
	/// </summary>
	[Fact]
	public void MarkCompleted_InProgressJob_SetsStatusAndClearsProcessingFlag()
	{
		var job = new BackupJob();
		job.MarkStarted();

		job.MarkCompleted((int)BackupStatus.Success);

		job.Status.Should().Be((int)BackupStatus.Success);
		job.IsProcessing.Should().BeFalse();
		job.CompletedAt.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that a failed job with remaining retries can be retried.
	/// </summary>
	[Fact]
	public void CanRetry_FailedJobWithRemainingRetries_ReturnsTrue()
	{
		var job = new BackupJob { MaxRetries = 3 };
		job.MarkStarted();
		job.MarkCompleted((int)BackupStatus.Failed);
		job.IncrementRetry();

		job.CanRetry.Should().BeTrue();
	}

	/// <summary>
	/// Tests that a failed job with exhausted retries cannot be retried.
	/// </summary>
	[Fact]
	public void CanRetry_FailedJobWithExhaustedRetries_ReturnsFalse()
	{
		var job = new BackupJob { MaxRetries = 2 };
		job.MarkStarted();
		job.MarkCompleted((int)BackupStatus.Failed);
		job.IncrementRetry();
		job.IncrementRetry();

		job.CanRetry.Should().BeFalse();
	}

	/// <summary>
	/// Tests that a successful job cannot be retried.
	/// </summary>
	[Fact]
	public void CanRetry_SuccessfulJob_ReturnsFalse()
	{
		var job = new BackupJob();
		job.MarkStarted();
		job.MarkCompleted((int)BackupStatus.Success);

		job.CanRetry.Should().BeFalse();
	}

	/// <summary>
	/// Tests that a job that has not been started returns zero elapsed time.
	/// </summary>
	[Fact]
	public void GetElapsedTime_NotStarted_ReturnsZero()
	{
		var job = new BackupJob();

		job.GetElapsedTime().Should().Be(TimeSpan.Zero);
	}

	/// <summary>
	/// Tests that a completed job returns a positive elapsed time duration.
	/// </summary>
	[Fact]
	public void GetElapsedTime_CompletedJob_ReturnsPositiveDuration()
	{
		var job = new BackupJob();
		job.MarkStarted();
		Thread.Sleep(10);
		job.MarkCompleted((int)BackupStatus.Success);

		job.GetElapsedTime().Should().BePositive();
	}

	/// <summary>
	/// Tests that calling IncrementRetry multiple times correctly increments the retry counter.
	/// </summary>
	[Fact]
	public void IncrementRetry_CalledMultipleTimes_IncrementsCorrectly()
	{
		var job = new BackupJob();

		job.IncrementRetry();
		job.IncrementRetry();
		job.IncrementRetry();

		job.RetryCount.Should().Be(3);
	}
}

public class RotationPolicyTests
{
	[Fact]
	public void IsValid_DefaultPolicy_ReturnsTrue()
	{
		var policy = new RotationPolicy();

		policy.IsValid().Should().BeTrue();
	}

	[Fact]
	public void IsValid_ZeroMaxBackupCount_ReturnsTrue_UnlimitedBackupsAllowed()
	{
		// MaxBackupCount = 0 means unlimited retention — the policy is still valid.
		var policy = new RotationPolicy { MaxBackupCount = 0, MinimumBackupCount = 1 };

		policy.IsValid().Should().BeTrue();
	}

	[Fact]
	public void IsValid_NegativeMaxBackupCount_ReturnsFalse()
	{
		var policy = new RotationPolicy { MaxBackupCount = -1 };

		policy.IsValid().Should().BeFalse();
	}

	[Fact]
	public void IsValid_ZeroMaxAgeDays_ReturnsFalse()
	{
		var policy = new RotationPolicy { MaxAgeDays = 0 };

		policy.IsValid().Should().BeFalse();
	}

	[Fact]
	public void IsValid_MinimumBackupCountExceedsMax_ReturnsFalse()
	{
		var policy = new RotationPolicy
		{
			MaxBackupCount = 5,
			MinimumBackupCount = 10
		};

		policy.IsValid().Should().BeFalse();
	}

	[Fact]
	public void ShouldRotate_NoRotationStrategy_AlwaysReturnsFalse()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.NoRotation,
			MaxBackupCount = 1,
			MaxAgeDays = 1
		};

		policy.ShouldRotate(100, DateTime.UtcNow.AddYears(-10), isFailed: false).Should().BeFalse();
	}

	[Fact]
	public void ShouldRotate_MaxFileCountExceeded_ReturnsTrue()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 5,
			MinimumBackupCount = 3
		};

		policy.ShouldRotate(totalBackupCount: 10, DateTime.UtcNow, isFailed: false).Should().BeTrue();
	}

	[Fact]
	public void ShouldRotate_MaxFileCountWithinLimit_ReturnsFalse()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 10,
			MinimumBackupCount = 3
		};

		policy.ShouldRotate(totalBackupCount: 5, DateTime.UtcNow, isFailed: false).Should().BeFalse();
	}

	[Fact]
	public void ShouldRotate_MaxAgeExceeded_ReturnsTrue()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.MaxAge,
			MaxAgeDays = 7,
			MinimumBackupCount = 1
		};

		var oldBackupDate = DateTime.UtcNow.AddDays(-30);
		policy.ShouldRotate(totalBackupCount: 5, oldBackupDate, isFailed: false).Should().BeTrue();
	}

	[Fact]
	public void ShouldRotate_MaxAgeNotExceeded_ReturnsFalse()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.MaxAge,
			MaxAgeDays = 30,
			MinimumBackupCount = 1
		};

		var recentBackupDate = DateTime.UtcNow.AddDays(-5);
		policy.ShouldRotate(totalBackupCount: 5, recentBackupDate, isFailed: false).Should().BeFalse();
	}

	[Fact]
	public void ShouldRotate_CombinedStrategyWithOldFile_ReturnsTrue()
	{
		var policy = new RotationPolicy
		{
			Strategy = (int)RotationStrategy.Combined,
			MaxBackupCount = 10,
			MaxAgeDays = 7,
			MinimumBackupCount = 1
		};

		var oldBackupDate = DateTime.UtcNow.AddDays(-60);
		policy.ShouldRotate(totalBackupCount: 3, oldBackupDate, isFailed: false).Should().BeTrue();
	}
}

public class BackupScheduleValidationTests
{
	[Fact]
	public void IsValid_ValidSchedule_ReturnsTrue()
	{
		var schedule = new BackupSchedule
		{
			Name = "Daily Backup",
			DatabasePath = "/data/app.db",
			CronExpression = "0 2 * * *",
			RetentionDays = 30,
			MaxBackupCount = 10
		};

		schedule.IsValid().Should().BeTrue();
	}

	[Fact]
	public void IsValid_EmptyName_ReturnsFalse()
	{
		var schedule = new BackupSchedule
		{
			Name = "",
			DatabasePath = "/data/app.db",
			CronExpression = "0 2 * * *"
		};

		schedule.IsValid().Should().BeFalse();
	}

	[Fact]
	public void IsValid_EmptyDatabasePath_ReturnsFalse()
	{
		var schedule = new BackupSchedule
		{
			Name = "Daily Backup",
			DatabasePath = "",
			CronExpression = "0 2 * * *"
		};

		schedule.IsValid().Should().BeFalse();
	}

	[Fact]
	public void IsValid_ZeroRetentionDays_ReturnsFalse()
	{
		var schedule = new BackupSchedule
		{
			Name = "Daily Backup",
			DatabasePath = "/data/app.db",
			CronExpression = "0 2 * * *",
			RetentionDays = 0
		};

		schedule.IsValid().Should().BeFalse();
	}
}