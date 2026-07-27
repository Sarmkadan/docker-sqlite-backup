using System;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class RestoreVerificationExtensionsTests
{
    [Fact]
    public void IsValidRestore_ReturnsTrue_WhenSuccessfulAndNoErrors()
    {
        var rv = new RestoreVerification
        {
            IsSuccessful = true,
            IntegrityCheckErrors = string.Empty
        };

        Assert.True(rv.IsValidRestore());
    }

    [Fact]
    public void IsValidRestore_ReturnsFalse_WhenNotSuccessful()
    {
        var rv = new RestoreVerification
        {
            IsSuccessful = false,
            IntegrityCheckErrors = string.Empty
        };

        Assert.False(rv.IsValidRestore());
    }

    [Fact]
    public void IsValidRestore_ReturnsFalse_WhenIntegrityErrorsPresent()
    {
        var rv = new RestoreVerification
        {
            IsSuccessful = true,
            IntegrityCheckErrors = "Some error"
        };

        Assert.False(rv.IsValidRestore());
    }

    [Fact]
    public void GetStatusMessage_ReturnsStatus_WhenNoError()
    {
        var rv = new RestoreVerification
        {
            StatusMessage = "All good",
            ErrorMessage = null
        };

        Assert.Equal("All good", rv.GetStatusMessage());
    }

    [Fact]
    public void GetStatusMessage_AppendsError_WhenErrorPresent()
    {
        var rv = new RestoreVerification
        {
            StatusMessage = "All good",
            ErrorMessage = "Disk full"
        };

        Assert.Equal("All good - Error: Disk full", rv.GetStatusMessage());
    }

    [Fact]
    public void GetFormattedDatabaseSize_ReturnsBytes_WhenLessThan1KB()
    {
        var rv = new RestoreVerification { DatabaseSizeBytes = 500 };
        Assert.Equal("500 B", rv.GetFormattedDatabaseSize());
    }

    [Fact]
    public void GetFormattedDatabaseSize_ReturnsKB_WhenInKBRange()
    {
        var rv = new RestoreVerification { DatabaseSizeBytes = 2_048 }; // 2 KB
        Assert.Equal("2.00 KB", rv.GetFormattedDatabaseSize());
    }

    [Fact]
    public void GetFormattedDatabaseSize_ReturnsMB_WhenInMBRange()
    {
        var rv = new RestoreVerification { DatabaseSizeBytes = 5_000_000 }; // ~4.77 MB
        Assert.Equal("4.77 MB", rv.GetFormattedDatabaseSize());
    }

    [Fact]
    public void GetFormattedDatabaseSize_ReturnsGB_WhenInGBRange()
    {
        var rv = new RestoreVerification { DatabaseSizeBytes = 10_000_000_000 }; // ~9.31 GB
        Assert.Equal("9.31 GB", rv.GetFormattedDatabaseSize());
    }

    [Fact]
    public void IsValidRestore_ThrowsArgumentNull_WhenNull()
    {
        RestoreVerification? rv = null;
        Assert.Throws<ArgumentNullException>(() => rv!.IsValidRestore());
    }

    [Fact]
    public void GetStatusMessage_ThrowsArgumentNull_WhenNull()
    {
        RestoreVerification? rv = null;
        Assert.Throws<ArgumentNullException>(() => rv!.GetStatusMessage());
    }

    [Fact]
    public void GetFormattedDatabaseSize_ThrowsArgumentNull_WhenNull()
    {
        RestoreVerification? rv = null;
        Assert.Throws<ArgumentNullException>(() => rv!.GetFormattedDatabaseSize());
    }
}
