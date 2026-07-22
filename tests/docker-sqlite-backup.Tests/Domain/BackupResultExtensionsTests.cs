using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

public class BackupResultExtensionsTests
{
    [Fact]
    public void GetStatusMessage_ReturnsSuccess_WhenStatusIsZero()
    {
        var result = new BackupResult { Status = 0 };
        result.GetStatusMessage().Should().Be("Success");
    }

    [Fact]
    public void GetStatusMessage_ReturnsFailure_WhenStatusIsNotZero()
    {
        var result = new BackupResult { Status = 1 };
        result.GetStatusMessage().Should().Be("Failure");
    }

    [Fact]
    public void GetDuration_ReturnsCalculatedDuration_WhenCompletedAtIsSet()
    {
        var startedAt = DateTime.UtcNow;
        var completedAt = startedAt.AddMinutes(5);
        var result = new BackupResult { StartedAt = startedAt, CompletedAt = completedAt };
        
        result.GetDuration().Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GetDuration_ReturnsDurationFromMilliseconds_WhenCompletedAtIsNotSet()
    {
        var result = new BackupResult { DurationMilliseconds = 5000 };
        
        result.GetDuration().Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void HasError_ReturnsTrue_WhenErrorMessageIsSet()
    {
        var result = new BackupResult { ErrorMessage = "Error" };
        result.HasError().Should().BeTrue();
    }

    [Fact]
    public void HasError_ReturnsTrue_WhenStackTraceIsSet()
    {
        var result = new BackupResult { StackTrace = "Trace" };
        result.HasError().Should().BeTrue();
    }

    [Fact]
    public void HasError_ReturnsFalse_WhenNoErrorMessageOrStackTrace()
    {
        var result = new BackupResult { ErrorMessage = null, StackTrace = null };
        result.HasError().Should().BeFalse();
    }
}
