using System;
using System.Text.Json;
using DockerSqliteBackup.Utilities;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class FileSystemUtilityJsonExtensionsTests
{
    private static readonly FileSystemUtilityJsonExtensions.FileSystemUtilityConfig ExpectedDefaultConfig = new()
    {
        MaxRetries = 3,
        RetryDelayMultiplier = 100,
        Recursive = true,
        DefaultSearchPattern = "*.*"
    };

    [Fact]
    public void ToJson_WithoutIndentation_ReturnsCompactJson()
    {
        // Act
        string json = FileSystemUtilityJsonExtensions.ToJson(indented: false);

        // Assert
        // The JSON should be a single line (no newline characters)
        Assert.DoesNotContain(Environment.NewLine, json);
        // It should deserialize back to the expected default config
        var deserialized = JsonSerializer.Deserialize<FileSystemUtilityJsonExtensions.FileSystemUtilityConfig>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(ExpectedDefaultConfig.MaxRetries, deserialized!.MaxRetries);
        Assert.Equal(ExpectedDefaultConfig.RetryDelayMultiplier, deserialized.RetryDelayMultiplier);
        Assert.Equal(ExpectedDefaultConfig.Recursive, deserialized.Recursive);
        Assert.Equal(ExpectedDefaultConfig.DefaultSearchPattern, deserialized.DefaultSearchPattern);
    }

    [Fact]
    public void ToJson_WithIndentation_ReturnsPrettyPrintedJson()
    {
        // Act
        string json = FileSystemUtilityJsonExtensions.ToJson(indented: true);

        // Assert
        // Indented JSON should contain at least one newline character
        Assert.Contains(Environment.NewLine, json);
        // Deserialization should still yield the default config
        var deserialized = JsonSerializer.Deserialize<FileSystemUtilityJsonExtensions.FileSystemUtilityConfig>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(ExpectedDefaultConfig.MaxRetries, deserialized!.MaxRetries);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsConfig()
    {
        // Arrange
        string json = FileSystemUtilityJsonExtensions.ToJson();

        // Act
        var config = FileSystemUtilityJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(ExpectedDefaultConfig.MaxRetries, config!.MaxRetries);
        Assert.Equal(ExpectedDefaultConfig.RetryDelayMultiplier, config.RetryDelayMultiplier);
        Assert.Equal(ExpectedDefaultConfig.Recursive, config.Recursive);
        Assert.Equal(ExpectedDefaultConfig.DefaultSearchPattern, config.DefaultSearchPattern);
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileSystemUtilityJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_EmptyOrWhiteSpace_ReturnsNull()
    {
        Assert.Null(FileSystemUtilityJsonExtensions.FromJson(string.Empty));
        Assert.Null(FileSystemUtilityJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndConfig()
    {
        // Arrange
        string json = FileSystemUtilityJsonExtensions.ToJson();

        // Act
        bool result = FileSystemUtilityJsonExtensions.TryFromJson(json, out var config);

        // Assert
        Assert.True(result);
        Assert.NotNull(config);
        Assert.Equal(ExpectedDefaultConfig.MaxRetries, config!.MaxRetries);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        string invalidJson = "{ this is not valid json }";

        // Act
        bool result = FileSystemUtilityJsonExtensions.TryFromJson(invalidJson, out var config);

        // Assert
        Assert.False(result);
        Assert.Null(config);
    }

    [Fact]
    public void TryFromJson_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => FileSystemUtilityJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void ConfigProperties_HaveExpectedDefaults()
    {
        var config = new FileSystemUtilityJsonExtensions.FileSystemUtilityConfig();

        Assert.Equal(3, config.MaxRetries);
        Assert.Equal(100, config.RetryDelayMultiplier);
        Assert.True(config.Recursive);
        Assert.Equal("*.*", config.DefaultSearchPattern);
    }
}
