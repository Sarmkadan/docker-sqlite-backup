using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Domain;

/// <summary>
/// Contains unit tests for the <see cref="BackupResultExtensions"/> methods.
/// </summary>
public class BackupResultExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.GetStatusMessage"/> returns "Success"
    /// when the <c>Status</c> property of the <see cref="BackupResult"/> is zero.
    /// </summary>
    [Fact]
    public void GetStatusMessage_ReturnsSuccess_WhenStatusIsZero()
    {
        var result = new BackupResult { Status = 0 };
        result.GetStatusMessage().Should().Be("Success");
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.GetStatusMessage"/> returns "Failure"
    /// when the <c>Status</c> property of the <see cref="BackupResult"/> is non-zero.
    /// </summary>
    [Fact]
    public void GetStatusMessage_ReturnsFailure_WhenStatusIsNotZero()
    {
        var result = new BackupResult { Status = 1 };
        result.GetStatusMessage().Should().Be("Failure");
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.GetDuration"/> returns the time span
    /// calculated from <c>CompletedAt</c> minus <c>StartedAt</c> when both timestamps are set.
    /// </summary>
    [Fact]
    public void GetDuration_ReturnsCalculatedDuration_WhenCompletedAtIsSet()
    {
        var startedAt = DateTime.UtcNow;
        var completedAt = startedAt.AddMinutes(5);
        var result = new BackupResult { StartedAt = startedAt, CompletedAt = completedAt };
        
        result.GetDuration().Should().Be(TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.GetDuration"/> returns a <see cref="TimeSpan"/>
    /// derived from <c>DurationMilliseconds</c> when <c>CompletedAt</c> is not set.
    /// </summary>
    [Fact]
    public void GetDuration_ReturnsDurationFromMilliseconds_WhenCompletedAtIsNotSet()
    {
        var result = new BackupResult { DurationMilliseconds = 5000 };
        
        result.GetDuration().Should().Be(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.HasError"/> returns <c>true</c>
    /// when the <c>ErrorMessage</c> property of the <see cref="BackupResult"/> is populated.
    /// </summary>
    [Fact]
    public void HasError_ReturnsTrue_WhenErrorMessageIsSet()
    {
        var result = new BackupResult { ErrorMessage = "Error" };
        result.HasError().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.HasError"/> returns <c>true</c>
    /// when the <c>StackTrace</c> property of the <see cref="BackupResult"/> is populated.
    /// </summary>
    [Fact]
    public void HasError_ReturnsTrue_WhenStackTraceIsSet()
    {
        var result = new BackupResult { StackTrace = "Trace" };
        result.HasError().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="BackupResultExtensions.HasError"/> returns <c>false</c>
    /// when both <c>ErrorMessage</c> and <c>StackTrace</c> are null.
    /// </summary>
    [Fact]
    public void HasError_ReturnsFalse_WhenNoErrorMessageOrStackTrace()
    {
        var result = new BackupResult { ErrorMessage = null, StackTrace = null };
        result.HasError().Should().BeFalse();
    }
}
