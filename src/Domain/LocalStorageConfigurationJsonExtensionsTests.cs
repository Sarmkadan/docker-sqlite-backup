#nullable enable
using System;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class LocalStorageConfigurationJsonExtensionsTests
{
    private static LocalStorageConfiguration CreateSampleConfig()
    {
        var cfg = new LocalStorageConfiguration
        {
            // Base class properties
            Id = Guid.NewGuid(),
            Name = "SampleConfig",
            IsDefault = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            LastModifiedAt = DateTime.UtcNow
        };

        // If LocalStorageConfiguration defines additional properties, they can be set here.
        // The test only relies on the base properties which are guaranteed to exist.
        return cfg;
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        LocalStorageConfiguration? nullConfig = null;
        Assert.Throws<ArgumentNullException>(() => nullConfig!.ToJson());
    }

    [Fact]
    public void ToJson_DefaultOptions_ReturnsNonEmptyJson()
    {
        var config = CreateSampleConfig();

        string json = config.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should contain at least one known property name.
        Assert.Contains("\"name\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesIndentedJson()
    {
        var config = CreateSampleConfig();

        string json = config.ToJson(indented: true);

        // Indented JSON contains line breaks.
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsEquivalentObject()
    {
        var original = CreateSampleConfig();
        string json = original.ToJson();

        var deserialized = LocalStorageConfigurationJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.IsDefault, deserialized.IsDefault);
        // Allow a small tolerance for DateTime rounding during serialization.
        Assert.Equal(original.CreatedAt, deserialized.CreatedAt, precision: TimeSpan.FromSeconds(1));
        Assert.Equal(original.LastModifiedAt, deserialized.LastModifiedAt, precision: TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromJson_NullOrWhiteSpace_ReturnsNull(string? json)
    {
        var result = LocalStorageConfigurationJsonExtensions.FromJson(json ?? string.Empty);
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        string malformed = "{ this is not valid json }";
        var result = LocalStorageConfigurationJsonExtensions.FromJson(malformed);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndObject()
    {
        var original = CreateSampleConfig();
        string json = original.ToJson();

        bool success = LocalStorageConfigurationJsonExtensions.TryFromJson(json, out var deserialized);

        Assert.True(success);
        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized!.Id);
        Assert.Equal(original.Name, deserialized.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryFromJson_NullOrWhiteSpace_ReturnsFalse(string? json)
    {
        bool success = LocalStorageConfigurationJsonExtensions.TryFromJson(json ?? string.Empty, out var result);
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        string malformed = "[invalid json";
        bool success = LocalStorageConfigurationJsonExtensions.TryFromJson(malformed, out var result);
        Assert.False(success);
        Assert.Null(result);
    }
}
