// Author: Vladyslav Zaiets
using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Contains unit tests for the <see cref="RotationPolicy"/> class.
/// Tests verify the rotation policy logic for different strategies: keep-last-N, age-based deletion,
/// empty directories, and exactly-at-limit scenarios.
/// </summary>
public class RotationPolicyTests
{
    [Fact]
    /// <summary>
    /// Tests keep-last-N strategy: should rotate backups when count exceeds MaxBackupCount.
    /// </summary>
    public void ShouldRotate_MaxFileCountStrategy_ExceedsLimit_ReturnsTrue()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 3,
            MinimumBackupCount = 1
        };

        var oldBackup = new DateTime(2024, 1, 1);
        var recentBackup = new DateTime(2024, 6, 1);

        // Act & Assert
        // With 4 backups (exceeds MaxBackupCount of 3), oldest should be rotated
        policy.ShouldRotate(4, oldBackup, false).Should().BeTrue();

        // With exactly 3 backups (at limit), nothing should be rotated
        policy.ShouldRotate(3, recentBackup, false).Should().BeFalse();

        // With 2 backups (below limit), nothing should be rotated
        policy.ShouldRotate(2, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests keep-last-N strategy: should not rotate when MinimumBackupCount is not satisfied.
    /// </summary>
    public void ShouldRotate_MaxFileCountStrategy_BelowMinimumCount_ReturnsFalse()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 5,
            MinimumBackupCount = 3
        };

        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // With 4 backups but MinimumBackupCount is 3, we need at least 3 backups
        // So with 4 backups, oldest (index 3) should NOT be rotated because total (4) > Minimum (3) is true
        // but we need total > Max to rotate by count
        policy.ShouldRotate(4, recentBackup, false).Should().BeFalse();

        // With 5 backups (at MaxBackupCount), nothing should be rotated
        policy.ShouldRotate(5, recentBackup, false).Should().BeFalse();

        // With 6 backups (exceeds MaxBackupCount), oldest should be rotated
        policy.ShouldRotate(6, recentBackup, false).Should().BeTrue();
    }

    [Fact]
    /// <summary>
    /// Tests age-based deletion strategy: should rotate backups older than MaxAgeDays.
    /// </summary>
    public void ShouldRotate_MaxAgeStrategy_OlderThanMaxAge_ReturnsTrue()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };

        var oldBackup = DateTime.UtcNow.AddDays(-31); // 31 days old (older than 30)
        var recentBackup = DateTime.UtcNow.AddDays(-29); // 29 days old (within 30)

        // Act & Assert
        // Old backup should be rotated
        policy.ShouldRotate(5, oldBackup, false).Should().BeTrue();

        // Recent backup should not be rotated
        policy.ShouldRotate(5, recentBackup, false).Should().BeFalse();

        // Even with many backups, recent ones should not be rotated by age
        policy.ShouldRotate(100, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests age-based deletion strategy: should not rotate when below MinimumBackupCount.
    /// </summary>
    public void ShouldRotate_MaxAgeStrategy_BelowMinimumCount_ReturnsFalse()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxAgeDays = 30,
            MinimumBackupCount = 5
        };

        var oldBackup = DateTime.UtcNow.AddDays(-45);

        // Act & Assert
        // Even though backup is old, with only 4 backups (below MinimumBackupCount of 5),
        // nothing should be rotated because totalBackupCount must be > MinimumBackupCount
        policy.ShouldRotate(4, oldBackup, false).Should().BeFalse();

        // With exactly 5 backups, totalBackupCount is not > MinimumBackupCount, so nothing should be rotated
        policy.ShouldRotate(5, oldBackup, false).Should().BeFalse();

        // With 6 backups (exceeds MinimumBackupCount) and old backup, should rotate
        policy.ShouldRotate(6, oldBackup, false).Should().BeTrue();
    }

    [Fact]
    /// <summary>
    /// Tests combined strategy: should rotate when either count OR age criteria are met.
    /// </summary>
    public void ShouldRotate_CombinedStrategy_OrLogic_ReturnsTrue()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.Combined,
            MaxBackupCount = 3,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };

        var oldBackup = DateTime.UtcNow.AddDays(-31); // Old backup
        var recentBackup = DateTime.UtcNow.AddDays(-29); // Recent backup

        // Act & Assert
        // Old backup should be rotated (age criteria)
        policy.ShouldRotate(5, oldBackup, false).Should().BeTrue();

        // Too many backups should be rotated (count criteria)
        policy.ShouldRotate(5, recentBackup, false).Should().BeTrue();

        // At limit with recent backup should not be rotated
        policy.ShouldRotate(3, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests empty directory scenario: should not rotate when there are no backups.
    /// </summary>
    public void ShouldRotate_EmptyDirectory_ReturnsFalse()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 5,
            MinimumBackupCount = 1
        };

        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // With 0 backups, nothing to rotate
        policy.ShouldRotate(0, recentBackup, false).Should().BeFalse();

        // With 1 backup, nothing to rotate
        policy.ShouldRotate(1, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests exactly-at-limit scenario: should not rotate when total backups equals MaxBackupCount.
    /// </summary>
    public void ShouldRotate_ExactlyAtLimit_ReturnsFalse()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 10,
            MinimumBackupCount = 2
        };

        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // With exactly MaxBackupCount backups, nothing should be rotated
        policy.ShouldRotate(10, recentBackup, false).Should().BeFalse();

        // With MaxBackupCount + 1 backups, oldest should be rotated
        policy.ShouldRotate(11, recentBackup, false).Should().BeTrue();
    }

    [Fact]
    /// <summary>
    /// Tests NoRotation strategy: should never rotate regardless of backup count or age.
    /// </summary>
    public void ShouldRotate_NoRotationStrategy_NeverReturnsTrue()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.NoRotation,
            MaxBackupCount = 5,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };

        var oldBackup = DateTime.UtcNow.AddDays(-100);
        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // NoRotation strategy should never rotate
        policy.ShouldRotate(100, oldBackup, false).Should().BeFalse();
        policy.ShouldRotate(100, recentBackup, false).Should();
        policy.ShouldRotate(1, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests MinimumBackupCount enforcement: should always keep at least MinimumBackupCount backups.
    /// </summary>
    public void ShouldRotate_MinimumBackupCount_AlwaysKeepsMinimum()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 100, // High limit
            MinimumBackupCount = 5
        };

        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // With 6 backups but totalBackupCount (6) is not > MaxBackupCount (100), nothing rotates
        policy.ShouldRotate(6, recentBackup, false).Should().BeFalse();

        // With 101 backups (exceeds MaxBackupCount of 100), oldest should be rotated
        policy.ShouldRotate(101, recentBackup, false).Should().BeTrue();

        // With exactly MinimumBackupCount backups, nothing should be rotated
        policy.ShouldRotate(5, recentBackup, false).Should().BeFalse();
    }

    [Fact]
    /// <summary>
    /// Tests that isFailed parameter doesn't affect ShouldRotate logic directly.
    /// The DeleteFailedBackups flag is handled at a higher level in GetBackupsForRotationAsync.
    /// </summary>
    public void ShouldRotate_IsFailedParameter_NotUsedInRotationLogic()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 3,
            MinimumBackupCount = 1
        };

        var recentBackup = DateTime.UtcNow;

        // Act & Assert
        // Whether backup is failed or not, ShouldRotate logic is the same
        // Failed backup at position where rotation applies
        policy.ShouldRotate(3, recentBackup, true).Should().BeFalse();

        // Successful backup at same position
        policy.ShouldRotate(3, recentBackup, false).Should().BeFalse();

        // With 4 backups, oldest should be rotated regardless of failure status
        policy.ShouldRotate(4, recentBackup, true).Should().BeTrue();
        policy.ShouldRotate(4, recentBackup, false).Should().BeTrue();
    }

    [Fact]
    /// <summary>
    /// Tests that MaxBackupCount of 0 means unlimited (no count-based rotation).
    /// </summary>
    public void ShouldRotate_MaxBackupCountZero_UnlimitedNoRotation()
    {
        // Arrange
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 0, // Unlimited
            MinimumBackupCount = 1
        };

        var oldBackup = DateTime.UtcNow.AddDays(-100);

        // Act & Assert
        // With unlimited backups, count-based rotation should never happen
        policy.ShouldRotate(1000, oldBackup, false).Should().BeFalse();
        policy.ShouldRotate(1, oldBackup, false).Should().BeFalse();
    }
}
