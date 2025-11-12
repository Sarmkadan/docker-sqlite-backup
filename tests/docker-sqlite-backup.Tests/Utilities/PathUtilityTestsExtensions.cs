// Author: Vladyslav Zaiets

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using System;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

/// <summary>
    /// Extension methods for asserting path utility operations in tests.
    /// </summary>
    public static class PathUtilityTestsExtensions
{
    /// <summary>
    /// Asserts that a file path is valid, exists, and contains expected content.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="expectedContent">The expected content of the file.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedContent"/> is null.</exception>
    public static void ShouldBeValidFilePathWithContent(this PathUtilityTests _, string filePath, string expectedContent)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(expectedContent);

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
    /// <param name="filePath">The file path to validate.</param>
    /// <param name="expectedSize">The expected file size in bytes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    public static void ShouldHaveFileSize(this PathUtilityTests _, string filePath, long expectedSize)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        PathUtility.IsValidFilePath(filePath).Should().BeTrue("File path should be valid");

        var actualSize = PathUtility.GetFileSize(filePath);
        actualSize.Should().Be(expectedSize, $"File size should be {expectedSize} bytes");
    }

    /// <summary>
    /// Asserts that a path is normalized correctly.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <param name="expectedNormalizedPath">The expected normalized path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedNormalizedPath"/> is null.</exception>
    public static void ShouldBeNormalized(this PathUtilityTests _, string path, string expectedNormalizedPath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedNormalizedPath);

        var normalized = PathUtility.Normalize(path);
        normalized.Should().Be(expectedNormalizedPath, "Path should be normalized correctly");
    }

    /// <summary>
    /// Asserts that a relative path calculation is correct.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="targetPath">The target path.</param>
    /// <param name="expectedRelativePath">The expected relative path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="basePath"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="targetPath"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expectedRelativePath"/> is null.</exception>
    public static void ShouldHaveRelativePath(this PathUtilityTests _, string basePath, string targetPath, string expectedRelativePath)
    {
        ArgumentNullException.ThrowIfNull(basePath);
        ArgumentNullException.ThrowIfNull(targetPath);
        ArgumentNullException.ThrowIfNull(expectedRelativePath);

        var relative = PathUtility.GetRelativePath(basePath, targetPath);
        relative.Should().Be(expectedRelativePath, "Relative path should match expected");
    }
}