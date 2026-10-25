using DockerSqliteBackup.Constants;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

/// <summary>
/// Contains unit tests for the <see cref="RotationPolicyExtensions"/> methods.
/// </summary>
public class RotationPolicyExtensionsTests
{
    private static BackupResult CreateBackup(int daysAgo, BackupStatus status = BackupStatus.Success)
    {
        return new BackupResult
        {
            Status = (int)status,
            StartedAt = DateTime.UtcNow.AddDays(-daysAgo)
        };
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> selects nothing
    /// when the number of backups is within <c>MaxBackupCount</c> and none are too old.
    /// </summary>
    [Fact]
    public void SelectExpired_ReturnsEmpty_WhenWithinLimits()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.Combined,
            MaxBackupCount = 5,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };
        var backups = new[] { CreateBackup(1), CreateBackup(2), CreateBackup(3) };

        policy.SelectExpired(backups).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> removes the oldest
    /// backups beyond <c>MaxBackupCount</c> under the <c>MaxFileCount</c> strategy, keeping the newest.
    /// </summary>
    [Fact]
    public void SelectExpired_MaxFileCount_SelectsOldestBeyondLimit()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 2,
            MinimumBackupCount = 1
        };
        var newest = CreateBackup(1);
        var middle = CreateBackup(2);
        var oldest = CreateBackup(3);
        var oldest2 = CreateBackup(4);

        var expired = policy.SelectExpired(new[] { oldest, newest, oldest2, middle });

        expired.Should().Equal(oldest2, oldest);
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> selects backups older
    /// than <c>MaxAgeDays</c> under the <c>MaxAge</c> strategy.
    /// </summary>
    [Fact]
    public void SelectExpired_MaxAge_SelectsBackupsOlderThanMaxAgeDays()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxAgeDays = 7,
            MinimumBackupCount = 1
        };
        var recent = CreateBackup(1);
        var stale = CreateBackup(10);

        policy.SelectExpired(new[] { stale, recent }).Should().Equal(stale);
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> never selects the newest
    /// <c>MinimumBackupCount</c> backups, even when they are older than <c>MaxAgeDays</c>.
    /// </summary>
    [Fact]
    public void SelectExpired_NeverSelectsMinimumBackups()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxAgeDays = 1,
            MinimumBackupCount = 2
        };
        var newest = CreateBackup(50);
        var second = CreateBackup(60);
        var third = CreateBackup(70);

        policy.SelectExpired(new[] { third, second, newest }).Should().Equal(third);
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> returns nothing
    /// for the <c>NoRotation</c> strategy.
    /// </summary>
    [Fact]
    public void SelectExpired_NoRotation_ReturnsEmpty()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.NoRotation,
            MinimumBackupCount = 1
        };

        policy.SelectExpired(new[] { CreateBackup(100), CreateBackup(200) }).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> selects failed backups
    /// when <c>DeleteFailedBackups</c> is enabled, regardless of age.
    /// </summary>
    [Fact]
    public void SelectExpired_DeleteFailedBackups_SelectsFailedBackups()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.NoRotation,
            MinimumBackupCount = 1,
            DeleteFailedBackups = true
        };
        var newest = CreateBackup(1);
        var failed = CreateBackup(2, BackupStatus.Failed);

        policy.SelectExpired(new[] { newest, failed }).Should().Equal(failed);
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> keeps failed backups
    /// when <c>DeleteFailedBackups</c> is disabled.
    /// </summary>
    [Fact]
    public void SelectExpired_DeleteFailedBackupsDisabled_KeepsFailedBackups()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.NoRotation,
            MinimumBackupCount = 1,
            DeleteFailedBackups = false
        };
        var failed = CreateBackup(2, BackupStatus.Failed);

        policy.SelectExpired(new[] { CreateBackup(1), failed }).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> skips unverified backups
    /// when <c>VerifyBeforeDeletion</c> is enabled.
    /// </summary>
    [Fact]
    public void SelectExpired_VerifyBeforeDeletion_SkipsUnverifiedBackups()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxAgeDays = 1,
            MinimumBackupCount = 1,
            VerifyBeforeDeletion = true
        };
        var unverified = CreateBackup(10, BackupStatus.Success);
        var verified = CreateBackup(11, BackupStatus.VerifiedSuccess);

        policy.SelectExpired(new[] { CreateBackup(0), unverified, verified }).Should().Equal(verified);
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> throws
    /// <see cref="ArgumentNullException"/> when the backups collection is null.
    /// </summary>
    [Fact]
    public void SelectExpired_ThrowsArgumentNullException_WhenBackupsIsNull()
    {
        var policy = new RotationPolicy();

        var act = () => policy.SelectExpired(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("backups");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.SelectExpired"/> throws
    /// <see cref="ArgumentNullException"/> when the policy is null.
    /// </summary>
    [Fact]
    public void SelectExpired_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        RotationPolicy? policy = null;

        var act = () => policy!.SelectExpired(Array.Empty<BackupResult>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("policy");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> mentions the count limit,
    /// the age limit and the minimum for the <c>Combined</c> strategy.
    /// </summary>
    [Fact]
    public void Describe_Combined_IncludesCountAndAgeRules()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.Combined,
            MaxBackupCount = 10,
            MaxAgeDays = 30,
            MinimumBackupCount = 3
        };

        var description = policy.Describe();

        description.Should().StartWith("Combined: ")
            .And.Contain("keep at most 10 backups")
            .And.Contain("delete backups older than 30 day(s)")
            .And.Contain("always keep the newest 3");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> omits the age rule
    /// for the <c>MaxFileCount</c> strategy.
    /// </summary>
    [Fact]
    public void Describe_MaxFileCount_OmitsAgeRule()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxFileCount,
            MaxBackupCount = 5,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };

        policy.Describe().Should().Contain("keep at most 5 backups").And.NotContain("older than");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> reports "no count limit"
    /// when <c>MaxBackupCount</c> is zero.
    /// </summary>
    [Fact]
    public void Describe_ZeroMaxBackupCount_ReportsNoCountLimit()
    {
        var policy = new RotationPolicy
        {
            Strategy = (int)RotationStrategy.MaxAge,
            MaxBackupCount = 0,
            MaxAgeDays = 30,
            MinimumBackupCount = 1
        };

        policy.Describe().Should().NotContain("keep at most");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> returns a fixed message
    /// for the <c>NoRotation</c> strategy.
    /// </summary>
    [Fact]
    public void Describe_NoRotation_ReturnsKeepAllMessage()
    {
        var policy = new RotationPolicy { Strategy = (int)RotationStrategy.NoRotation };

        policy.Describe().Should().Be("NoRotation: all backups are kept.");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> reports an undefined strategy value.
    /// </summary>
    [Fact]
    public void Describe_UnknownStrategy_ReportsUnknown()
    {
        var policy = new RotationPolicy { Strategy = 999 };

        policy.Describe().Should().Be("Unknown rotation strategy 999.");
    }

    /// <summary>
    /// Verifies that <see cref="RotationPolicyExtensions.Describe"/> throws
    /// <see cref="ArgumentNullException"/> when the policy is null.
    /// </summary>
    [Fact]
    public void Describe_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        RotationPolicy? policy = null;

        var act = () => policy!.Describe();

        act.Should().Throw<ArgumentNullException>().WithParameterName("policy");
    }
}
