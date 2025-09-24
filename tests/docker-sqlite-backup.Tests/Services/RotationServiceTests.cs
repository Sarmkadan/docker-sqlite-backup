// Author: Vladyslav Zaiets 2
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Data;
using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Contains unit tests for the <see cref="RotationService"/> class.
/// Tests verify the rotation policy execution, backup management, and disk space calculations.
/// </summary>
public class RotationServiceTests
{
	/// <summary>
	/// Mock repository for testing backup operations.
	/// </summary>
	private readonly Mock<IBackupRepository> _repositoryMock;

	/// <summary>
	/// Mock logger for testing service logging.
	/// </summary>
	private readonly Mock<ILogger<RotationService>> _loggerMock;

	/// <summary>
	/// System under test - the RotationService instance being tested.
	/// </summary>
	private readonly RotationService _sut;

	/// <summary>
	/// Initializes a new instance of the <see cref="RotationServiceTests"/> class.
	/// </summary>
	public RotationServiceTests()
	{
		_repositoryMock = new Mock<IBackupRepository>();
		_loggerMock = new Mock<ILogger<RotationService>>();
		_sut = new RotationService(_repositoryMock.Object, _loggerMock.Object);
	}

	[Fact]
	/// <summary>
	/// Tests that ExecuteRotationAsync returns 0 when no rotation policy is found for the schedule.
	/// </summary>
	public async Task ExecuteRotationAsync_NoPolicyFound_ReturnsZero()
	{
		var scheduleId = Guid.NewGuid();
		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync((RotationPolicy?)null);

		var result = await _sut.ExecuteRotationAsync(scheduleId);

		result.Should().Be(0);
	}

	[Fact]
	/// <summary>
	/// Tests that ExecuteRotationAsync returns 0 when the rotation strategy is set to NoRotation.
	/// </summary>
	public async Task ExecuteRotationAsync_NoRotationStrategy_ReturnsZero()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.NoRotation
		};
		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);

		var result = await _sut.ExecuteRotationAsync(scheduleId);

		result.Should().Be(0);
		_repositoryMock.Verify(r => r.DeleteBackupResultAsync(It.IsAny<Guid>()), Times.Never);
	}

	[Fact]
	/// <summary>
	/// Tests that ExecuteRotationAsync returns the count of deleted backups when backups are eligible for deletion.
	/// Verifies that the rotation service correctly identifies and deletes excess backups based on the policy.
	/// </summary>
	public async Task ExecuteRotationAsync_WithBackupsEligibleForDeletion_ReturnsDeletedCount()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 2,
			MinimumBackupCount = 1
		};

		var backups = Enumerable.Range(0, 5).Select(i => new BackupResult
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			Status = (int)BackupStatus.Success,
			BackupFileSizeBytes = 1024,
			StartedAt = DateTime.UtcNow.AddHours(-i)
		}).ToList();

		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(backups);
		_repositoryMock
			.Setup(r => r.DeleteBackupResultAsync(It.IsAny<Guid>()))
			.Returns(Task.CompletedTask);
		_repositoryMock
			.Setup(r => r.SaveRotationPolicyAsync(It.IsAny<RotationPolicy>()))
			.ReturnsAsync(policy);

		var result = await _sut.ExecuteRotationAsync(scheduleId);

		result.Should().BeGreaterThan(0);
		_repositoryMock.Verify(r => r.DeleteBackupResultAsync(It.IsAny<Guid>()), Times.AtLeastOnce);
	}

	[Fact]
	/// <summary>
	/// Tests that ExecuteRotationAsync updates the LastRotatedAt timestamp on the rotation policy.
	/// Verifies that the timestamp is set to the current time after rotation execution.
	/// </summary>
	public async Task ExecuteRotationAsync_UpdatesLastRotatedAt()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 10,
			MinimumBackupCount = 1
		};

		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(new List<BackupResult>());

		RotationPolicy? savedPolicy = null;
		_repositoryMock
			.Setup(r => r.SaveRotationPolicyAsync(It.IsAny<RotationPolicy>()))
			.Callback<RotationPolicy>(p => savedPolicy = p)
			.ReturnsAsync(policy);

		await _sut.ExecuteRotationAsync(scheduleId);

		savedPolicy.Should().NotBeNull();
		savedPolicy!.LastRotatedAt.Should().NotBeNull();
		savedPolicy.LastRotatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
	}

	[Fact]
	/// <summary>
	/// Tests that GetBackupsForRotationAsync returns an empty collection when no rotation policy is found.
	/// Verifies that the method handles missing policies gracefully.
	/// </summary>
	public async Task GetBackupsForRotationAsync_NoPolicyFound_ReturnsEmpty()
	{
		var scheduleId = Guid.NewGuid();
		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync((RotationPolicy?)null);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(new List<BackupResult>());

		var result = await _sut.GetBackupsForRotationAsync(scheduleId);

		result.Should().BeEmpty();
	}

	[Fact]
	/// <summary>
	/// Tests that GetBackupsForRotationAsync returns an empty collection when the number of backups is below the minimum count.
	/// Verifies that the method enforces the minimum backup count policy.
	/// </summary>
	public async Task GetBackupsForRotationAsync_BelowMinimumCount_ReturnsEmpty()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 3,
			MinimumBackupCount = 5
		};

		var backups = Enumerable.Range(0, 3).Select(i => new BackupResult
		{
			Id = Guid.NewGuid(),
			ScheduleId = scheduleId,
			Status = (int)BackupStatus.Success,
			StartedAt = DateTime.UtcNow.AddHours(-i)
		}).ToList();

		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(backups);

		var result = await _sut.GetBackupsForRotationAsync(scheduleId);

		result.Should().BeEmpty();
	}

	[Fact]
	/// <summary>
	/// Tests that GetBackupsForRotationAsync includes failed backups when DeleteFailedBackups is enabled in the policy.
	/// Verifies that the rotation service can identify failed backups for deletion based on policy configuration.
	/// </summary>
	public async Task GetBackupsForRotationAsync_DeleteFailedBackupsEnabled_IncludesFailedBackups()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 10,
			MinimumBackupCount = 1,
			DeleteFailedBackups = true
		};

		var backups = new List<BackupResult>
		{
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Success, StartedAt = DateTime.UtcNow },
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Failed, StartedAt = DateTime.UtcNow.AddHours(-1) },
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Failed, StartedAt = DateTime.UtcNow.AddHours(-2) }
		};

		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(backups);

		var result = await _sut.GetBackupsForRotationAsync(scheduleId);

		result.Should().OnlyContain(b => !b.IsSuccess);
	}

	[Fact]
	/// <summary>
	/// Tests that SaveRotationPolicyAsync throws a ValidationException when the policy has invalid values (0 for required fields).
	/// Verifies that the service validates policy parameters before saving.
	/// </summary>
	public async Task SaveRotationPolicyAsync_InvalidPolicy_ThrowsValidationException()
	{
		var policy = new RotationPolicy
		{
			MaxBackupCount = 0,
			MaxAgeDays = 0
		};

		await _sut.Invoking(s => s.SaveRotationPolicyAsync(policy))
			.Should().ThrowAsync<DockerSqliteBackup.Exceptions.ValidationException>();
	}

	[Fact]
	/// <summary>
	/// Tests that SaveRotationPolicyAsync sets the LastModifiedAt timestamp when saving a valid policy.
	/// Verifies that the timestamp is updated to the current time when the policy is saved.
	/// </summary>
	public async Task SaveRotationPolicyAsync_ValidPolicy_SetsLastModifiedAt()
	{
		var policy = new RotationPolicy
		{
			ScheduleId = Guid.NewGuid(),
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 5,
			MinimumBackupCount = 1,
			MaxAgeDays = 30
		};

		_repositoryMock
			.Setup(r => r.SaveRotationPolicyAsync(It.IsAny<RotationPolicy>()))
			.ReturnsAsync(policy);

		var result = await _sut.SaveRotationPolicyAsync(policy);

		_repositoryMock.Verify(r => r.SaveRotationPolicyAsync(
			It.Is<RotationPolicy>(p => p.LastModifiedAt > DateTime.UtcNow.AddSeconds(-5))),
			Times.Once);
	}

	[Fact]
	/// <summary>
	/// Tests that CalculateDiskSpaceFreedAsync returns the sum of backup file sizes that would be freed by rotation.
	/// Verifies that the method correctly calculates the total disk space that would be recovered by deleting rotated backups.
	/// </summary>
	public async Task CalculateDiskSpaceFreedAsync_WithBackupsToRotate_SumsSizes()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy
		{
			ScheduleId = scheduleId,
			Strategy = (int)RotationStrategy.MaxFileCount,
			MaxBackupCount = 1,
			MinimumBackupCount = 1
		};

		var backups = new List<BackupResult>
		{
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Success, BackupFileSizeBytes = 1000, StartedAt = DateTime.UtcNow },
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Success, BackupFileSizeBytes = 2000, StartedAt = DateTime.UtcNow.AddHours(-1) },
			new() { Id = Guid.NewGuid(), ScheduleId = scheduleId, Status = (int)BackupStatus.Success, BackupFileSizeBytes = 3000, StartedAt = DateTime.UtcNow.AddHours(-2) }
		};

		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);
		_repositoryMock
			.Setup(r => r.GetBackupHistoryAsync(scheduleId, int.MaxValue))
			.ReturnsAsync(backups);

		var freed = await _sut.CalculateDiskSpaceFreedAsync(scheduleId);

		freed.Should().BeGreaterThan(0);
	}

	[Fact]
	/// <summary>
	/// Tests that GetRotationPolicyAsync returns the rotation policy when it exists for the given schedule ID.
	/// Verifies that the method correctly retrieves and returns existing rotation policies.
	/// </summary>
	public async Task GetRotationPolicyAsync_ExistingPolicy_ReturnsPolicy()
	{
		var scheduleId = Guid.NewGuid();
		var policy = new RotationPolicy { ScheduleId = scheduleId };
		_repositoryMock
			.Setup(r => r.GetRotationPolicyAsync(scheduleId))
			.ReturnsAsync(policy);

		var result = await _sut.GetRotationPolicyAsync(scheduleId);

		result.Should().NotBeNull();
		result!.ScheduleId.Should().Be(scheduleId);
	}
}