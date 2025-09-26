// Author: Vladyslav Zaiets

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public static class PathUtilityTestsExtensions
{
    /// <summary>
    /// Asserts that a file path is valid and contains expected content.
    /// </summary>
    public static void ShouldBeValidFilePathWithContent(this PathUtilityTests _, string filePath, string expectedContent)
    {
        PathUtility.IsValidFilePath(filePath).Should().BeTrue("File path should be valid");

        var fileInfo = new FileInfo(filePath);
        fileInfo.Exists.Should().BeTrue("File should exist");
        fileInfo.Length.Should().BeGreaterThan(0, "File should have content");

        var actualContent = File.ReadAllText(filePath);
        actualContent.Should().Be(expectedContent, "File content should match expected");
    }

    /// <summary>
    /// Asserts that a file path is valid and has the expected size.
    /// </summary>
    public static void ShouldHaveFileSize(this PathUtilityTests _, string filePath, long expectedSize)
    {
        PathUtility.IsValidFilePath(filePath).Should().BeTrue("File path should be valid");

        var actualSize = PathUtility.GetFileSize(filePath);
        actualSize.Should().Be(expectedSize, $"File size should be {expectedSize} bytes");
    }

    /// <summary>
    /// Asserts that a path is normalized correctly.
    /// </summary>
    public static void ShouldBeNormalized(this PathUtilityTests _, string path, string expectedNormalizedPath)
    {
        var normalized = PathUtility.Normalize(path);
        normalized.Should().Be(expectedNormalizedPath, "Path should be normalized correctly");
    }

    /// <summary>
    /// Asserts that a relative path calculation is correct.
    /// </summary>
    public static void ShouldHaveRelativePath(this PathUtilityTests _, string basePath, string targetPath, string expectedRelativePath)
    {
        var relative = PathUtility.GetRelativePath(basePath, targetPath);
        relative.Should().Be(expectedRelativePath, "Relative path should match expected");
    }
}