#nullable enable
using System;
using System.Threading.Tasks;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class RestoreVerificationTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var before = DateTime.UtcNow;
        var verification = new RestoreVerification();
        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, verification.Id);
        Assert.Equal(string.Empty, verification.StatusMessage);
        Assert.InRange(verification.StartedAt, before, after);
        Assert.Null(verification.CompletedAt);
        Assert.Equal(0, verification.DurationMilliseconds);
        Assert.Equal(0, verification.RecordCount);
        Assert.Equal(0, verification.DatabaseSizeBytes);
        Assert.False(verification.IntegrityCheckPassed);
        Assert.Null(verification.IntegrityCheckErrors);
        Assert.Equal(string.Empty, verification.TemporaryDirectory);
        Assert.Null(verification.ErrorMessage);
    }

    [Fact]
    public void MarkCompleted_SetsPropertiesCorrectly()
    {
        var verification = new RestoreVerification();
        var before = DateTime.UtcNow;
        verification.MarkCompleted(true, "Success");
        var after = DateTime.UtcNow;

        Assert.True(verification.IsSuccessful);
        Assert.Equal("Success", verification.StatusMessage);
        Assert.NotNull(verification.CompletedAt);
        Assert.InRange(verification.CompletedAt!.Value, before, after);
    }

    [Fact]
    public void MarkCompleted_WithNullStatusMessage_ThrowsArgumentNullException()
    {
        var verification = new RestoreVerification();

        Assert.Throws<ArgumentNullException>(() => verification.MarkCompleted(true, null!));
    }

    [Fact]
    public void GetElapsedDuration_ReturnsCorrectDuration()
    {
        var verification = new RestoreVerification();
        var startedAt = verification.StartedAt;
        verification.CompletedAt = startedAt.AddMinutes(1);

        var duration = verification.GetElapsedDuration();

        Assert.Equal(TimeSpan.FromMinutes(1), duration);
    }

    [Fact]
    public void GetElapsedDuration_WithNullCompletedAt_ReturnsZeroDuration()
    {
        var verification = new RestoreVerification();

        var duration = verification.GetElapsedDuration();

        Assert.Equal(TimeSpan.Zero, duration);
    }
}
