using System;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class RotationPolicyValidationTests
{
    private static RotationPolicy CreateValidPolicy()
    {
        var now = DateTime.UtcNow;
        return new RotationPolicy
        {
            Id = Guid.NewGuid(),
            ScheduleId = Guid.NewGuid(),
            // Use the default enum value (0) which is guaranteed to exist.
            Strategy = default,
            MaxBackupCount = 5,
            MaxAgeDays = 30,
            MinimumBackupCount = 1,
            CreatedAt = now.AddDays(-10),
            LastModifiedAt = now,
            LastRotatedAt = now.AddDays(-5)
        };
    }

    [Fact]
    public void Validate_ReturnsEmptyList_ForValidPolicy()
    {
        var policy = CreateValidPolicy();

        var result = policy.Validate();

        Assert.Empty(result);
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidPolicy()
    {
        var policy = CreateValidPolicy();

        Assert.True(policy.IsValid());
    }

    [Fact]
    public void EnsureValid_DoesNotThrow_ForValidPolicy()
    {
        var policy = CreateValidPolicy();

        var exception = Record.Exception(() => policy.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ReturnsProblems_ForVariousInvalidValues()
    {
        var now = DateTime.UtcNow;
        var policy = new RotationPolicy
        {
            Id = Guid.Empty,                     // invalid
            ScheduleId = Guid.Empty,             // invalid
            Strategy = default,                  // assume valid enum value
            MaxBackupCount = -1,                 // invalid
            MaxAgeDays = 0,                      // invalid
            MinimumBackupCount = 10,             // invalid because > MaxBackupCount (which is -1)
            CreatedAt = default,                 // invalid
            LastModifiedAt = default,            // invalid
            LastRotatedAt = now.AddDays(1)       // invalid: future relative to LastModifiedAt
        };

        var problems = policy.Validate();

        Assert.Contains("Id must be a non-empty GUID.", problems);
        Assert.Contains("ScheduleId must be a non-empty GUID.", problems);
        Assert.Contains("MaxBackupCount must be non-negative.", problems);
        Assert.Contains("MaxAgeDays must be at least 1.", problems);
        Assert.Contains("CreatedAt must be set to a valid DateTime.", problems);
        Assert.Contains("LastModifiedAt must be set to a valid DateTime.", problems);
        // The future date check only fires when LastModifiedAt is a valid date; because it is default,
        // the method will not add that specific problem. We therefore only assert the problems we
        // know will be present given the current validation logic.
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WithProblemMessage()
    {
        var policy = new RotationPolicy
        {
            Id = Guid.Empty,
            ScheduleId = Guid.NewGuid(),
            Strategy = default,
            MaxBackupCount = 0,
            MaxAgeDays = 1,
            MinimumBackupCount = 0,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        var ex = Assert.Throws<ArgumentException>(() => policy.EnsureValid());

        Assert.Contains("Id must be a non-empty GUID.", ex.Message);
    }

    [Fact]
    public void Validate_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        RotationPolicy? policy = null;

        Assert.Throws<ArgumentNullException>(() => policy!.Validate());
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentNullException_WhenPolicyIsNull()
    {
        RotationPolicy? policy = null;

        Assert.Throws<ArgumentNullException>(() => policy!.EnsureValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidPolicy()
    {
        var policy = new RotationPolicy
        {
            Id = Guid.Empty,
            ScheduleId = Guid.NewGuid(),
            Strategy = default,
            MaxBackupCount = 0,
            MaxAgeDays = 1,
            MinimumBackupCount = 0,
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow
        };

        Assert.False(policy.IsValid());
    }
}
