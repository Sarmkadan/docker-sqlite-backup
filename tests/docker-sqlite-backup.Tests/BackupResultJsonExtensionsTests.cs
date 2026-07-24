#nullable enable
using System;
using System.Text.Json;
using DockerSqliteBackup.Domain;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class BackupResultJsonExtensionsTests
{
    private readonly BackupResult _sampleBackupResult;

    public BackupResultJsonExtensionsTests()
    {
        _sampleBackupResult = new BackupResult
        {
            Id = Guid.Parse("12345678-1234-1234-1234-123456789012"),
            ScheduleId = Guid.Parse("22345678-1234-1234-1234-123456789012"),
            BackupJobId = Guid.Parse("32345678-1234-1234-1234-123456789012"),
            Status = (int)Constants.BackupStatus.Success,
            BackupFilePath = "/backups/db_20240724_123456.sqlite",
            BackupFileSizeBytes = 10485760,
            OriginalFileSizeBytes = 20971520,
            CompressionRatio = 2.0,
            Checksum = "a1b2c3d4e5f67890abcdef1234567890abcdef12",
            StartedAt = new DateTime(2024, 7, 24, 10, 30, 0, DateTimeKind.Utc),
            CompletedAt = new DateTime(2024, 7, 24, 10, 35, 30, DateTimeKind.Utc),
            DurationMilliseconds = 330000,
            ErrorMessage = null,
            StackTrace = null,
            IsVerified = true,
            VerifiedAt = new DateTime(2024, 7, 24, 10, 40, 0, DateTimeKind.Utc),
            Notes = "Daily backup completed successfully",
            IsStoredInS3 = true,
            IsStoredLocally = false,
            S3ObjectKey = "backups/2024/07/db_20240724_123456.sqlite",
            BackupMode = (int)Constants.BackupMode.Full,
            BaseBackupResultId = null
        };
    }

    [Fact]
    public void ToJson_WithValidBackupResult_ReturnsNonEmptyJsonString()
    {
        var json = _sampleBackupResult.ToJson();
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("id");
        json.Should().Contain("backupFilePath");
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        var json = _sampleBackupResult.ToJson(indented: true);
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\n");
        json.Should().Contain("  ");
    }

    [Fact]
    public void ToJson_WithCustomOptions_UsesCustomOptions()
    {
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true
        };
        var json = _sampleBackupResult.ToJson(options: customOptions);
        json.Should().Contain("backup_file_path");
        json.Should().Contain("\n");
    }

    [Fact]
    public void ToJson_WithNullValue_ThrowsArgumentNullException()
    {
        BackupResult? nullBackup = null;
        Assert.Throws<ArgumentNullException>(() => nullBackup!.ToJson());
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsBackupResult()
    {
        var json = _sampleBackupResult.ToJson();
        var result = BackupResultJsonExtensions.FromJson(json);
        result.Should().NotBeNull();
        result!.Id.Should().Be(_sampleBackupResult.Id);
        result.ScheduleId.Should().Be(_sampleBackupResult.ScheduleId);
        result.BackupJobId.Should().Be(_sampleBackupResult.BackupJobId);
        result.Status.Should().Be(_sampleBackupResult.Status);
        result.BackupFilePath.Should().Be(_sampleBackupResult.BackupFilePath);
        result.BackupFileSizeBytes.Should().Be(_sampleBackupResult.BackupFileSizeBytes);
        result.OriginalFileSizeBytes.Should().Be(_sampleBackupResult.OriginalFileSizeBytes);
        result.CompressionRatio.Should().Be(_sampleBackupResult.CompressionRatio);
        result.Checksum.Should().Be(_sampleBackupResult.Checksum);
        result.StartedAt.Should().Be(_sampleBackupResult.StartedAt);
        result.CompletedAt.Should().Be(_sampleBackupResult.CompletedAt);
        result.DurationMilliseconds.Should().Be(_sampleBackupResult.DurationMilliseconds);
        result.IsVerified.Should().Be(_sampleBackupResult.IsVerified);
        result.VerifiedAt.Should().Be(_sampleBackupResult.VerifiedAt);
        result.Notes.Should().Be(_sampleBackupResult.Notes);
        result.IsStoredInS3.Should().Be(_sampleBackupResult.IsStoredInS3);
        result.IsStoredLocally.Should().Be(_sampleBackupResult.IsStoredLocally);
        result.S3ObjectKey.Should().Be(_sampleBackupResult.S3ObjectKey);
        result.BackupMode.Should().Be(_sampleBackupResult.BackupMode);
        result.BaseBackupResultId.Should().Be(_sampleBackupResult.BaseBackupResultId);
    }

    [Fact]
    public void FromJson_WithNullJson_ReturnsNull()
    {
        var result = BackupResultJsonExtensions.FromJson(null);
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithEmptyString_ReturnsNull()
    {
        var result = BackupResultJsonExtensions.FromJson(string.Empty);
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithWhitespaceOnly_ReturnsNull()
    {
        var result = BackupResultJsonExtensions.FromJson("   \n\t  ");
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{ invalid json";
        Assert.Throws<JsonException>(() => BackupResultJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializes()
    {
        var json = _sampleBackupResult.ToJson();
        var success = BackupResultJsonExtensions.TryFromJson(json, out var result);
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.Id.Should().Be(_sampleBackupResult.Id);
    }

    [Fact]
    public void TryFromJson_WithNullJson_ReturnsFalseAndNullValue()
    {
        string? nullJson = null;
        var success = BackupResultJsonExtensions.TryFromJson(nullJson, out var result);
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithEmptyString_ReturnsFalseAndNullValue()
    {
        var emptyJson = string.Empty;
        var success = BackupResultJsonExtensions.TryFromJson(emptyJson, out var result);
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithWhitespaceOnly_ReturnsFalseAndNullValue()
    {
        var whitespaceJson = "   \n\t  ";
        var success = BackupResultJsonExtensions.TryFromJson(whitespaceJson, out var result);
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNullValue()
    {
        var invalidJson = "{ invalid json";
        var success = BackupResultJsonExtensions.TryFromJson(invalidJson, out var result);
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void Roundtrip_ToJsonThenFromJson_PreservesAllData()
    {
        var original = _sampleBackupResult;
        var json = original.ToJson();
        var deserialized = BackupResultJsonExtensions.FromJson(json);
        deserialized.Should().NotBeNull();
        deserialized.Should().BeEquivalentTo(original, options => options
            .Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation, TimeSpan.FromSeconds(1)))
            .WhenTypeIs<DateTime>()
        );
    }

    [Fact]
    public void Roundtrip_WithIndentedJson_DeserializesCorrectly()
    {
        var original = _sampleBackupResult;
        var json = original.ToJson(indented: true);
        var deserialized = BackupResultJsonExtensions.FromJson(json);
        deserialized.Should().NotBeNull();
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void ToJson_WithMinimalBackupResult_ReturnsValidJson()
    {
        var minimalBackup = new BackupResult
        {
            Id = Guid.NewGuid(),
            ScheduleId = Guid.NewGuid(),
            BackupJobId = Guid.NewGuid(),
            Status = (int)Constants.BackupStatus.Pending
        };
        var json = minimalBackup.ToJson();
        json.Should().NotBeNullOrEmpty();
        var deserialized = BackupResultJsonExtensions.FromJson(json);
        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(minimalBackup.Id);
    }

    [Fact]
    public void FromJson_WithCamelCaseProperties_DeserializesCorrectly()
    {
        var json = _sampleBackupResult.ToJson();
        json.Should().Contain("backupFilePath");
        json.Should().Contain("backupFileSizeBytes");
        json.Should().NotContain("BackupFilePath");
        var result = BackupResultJsonExtensions.FromJson(json);
        result.Should().NotBeNull();
        result!.BackupFilePath.Should().Be(_sampleBackupResult.BackupFilePath);
    }

    [Fact]
    public void TryFromJson_WithValidJson_SetsOutParameter()
    {
        var json = _sampleBackupResult.ToJson();
        BackupResult? result = null;
        var success = BackupResultJsonExtensions.TryFromJson(json, out result);
        success.Should().BeTrue();
        result.Should().NotBeNull();
    }
}
