// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class PathUtilityTests : IDisposable
{
    private readonly string _tempDir;

    public PathUtilityTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"path-util-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void SanitizeFileName_NormalName_ReturnsUnchanged()
    {
        var result = PathUtility.SanitizeFileName("backup-2024.sqlite");

        result.Should().Be("backup-2024.sqlite");
    }

    [Fact]
    public void SanitizeFileName_EmptyInput_ThrowsArgumentException()
    {
        var act = () => PathUtility.SanitizeFileName("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SanitizeFileName_NullOrEmptyString_ThrowsArgumentException()
    {
        var act = () => PathUtility.SanitizeFileName(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SanitizeFileName_AllInvalidChars_ReturnsFallback()
    {
        var allInvalid = new string(Path.GetInvalidFileNameChars());

        var result = PathUtility.SanitizeFileName(allInvalid);

        result.Should().Be("file");
    }

    [Fact]
    public void IsAbsolute_AbsolutePath_ReturnsTrue()
    {
        PathUtility.IsAbsolute("/usr/local/data").Should().BeTrue();
    }

    [Fact]
    public void IsAbsolute_RelativePath_ReturnsFalse()
    {
        PathUtility.IsAbsolute("relative/path").Should().BeFalse();
    }

    [Fact]
    public void GetFileSize_ExistingFile_ReturnsCorrectSize()
    {
        var path = Path.Combine(_tempDir, "size-test.txt");
        File.WriteAllText(path, "hello");

        var size = PathUtility.GetFileSize(path);

        size.Should().Be(5);
    }

    [Fact]
    public void GetFileSize_NonExistentFile_ReturnsNegativeOne()
    {
        var size = PathUtility.GetFileSize(Path.Combine(_tempDir, "missing.txt"));

        size.Should().Be(-1);
    }

    [Fact]
    public void IsValidFilePath_ExistingNonEmptyFile_ReturnsTrue()
    {
        var path = Path.Combine(_tempDir, "valid-file.txt");
        File.WriteAllText(path, "content");

        PathUtility.IsValidFilePath(path).Should().BeTrue();
    }

    [Fact]
    public void IsValidFilePath_NonExistentFile_ReturnsFalse()
    {
        PathUtility.IsValidFilePath(Path.Combine(_tempDir, "nope.txt")).Should().BeFalse();
    }

    [Fact]
    public void IsValidFilePath_EmptyFile_ReturnsFalse()
    {
        var path = Path.Combine(_tempDir, "empty.txt");
        File.WriteAllText(path, string.Empty);

        PathUtility.IsValidFilePath(path).Should().BeFalse();
    }

    [Fact]
    public void GenerateBackupFileName_WithKnownTimestamp_ContainsExpectedParts()
    {
        var timestamp = new DateTime(2024, 6, 15, 10, 30, 0);

        var result = PathUtility.GenerateBackupFileName("myapp", timestamp);

        result.Should().StartWith("myapp_backup_2024-06-15");
        result.Should().EndWith(".sqlite");
    }

    [Fact]
    public void GenerateBackupFileName_WithoutTimestamp_ContainsCurrentDate()
    {
        var result = PathUtility.GenerateBackupFileName("myapp");

        result.Should().StartWith("myapp_backup_");
        result.Should().EndWith(".sqlite");
    }

    [Fact]
    public void CombinePath_TwoSegments_CombinesCorrectly()
    {
        var result = PathUtility.CombinePath("/base", "sub");

        result.Should().Be(Path.Combine("/base", "sub"));
    }

    [Fact]
    public void CombinePath_SkipsEmptySegments_ProducesValidPath()
    {
        var result = PathUtility.CombinePath("/base", "   ", "file.txt");

        result.Should().Be(Path.Combine("/base", "file.txt"));
    }

    [Fact]
    public void CombinePath_NoSegments_ThrowsArgumentException()
    {
        var act = () => PathUtility.CombinePath();

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CombinePath_AllSegmentsEmpty_ThrowsArgumentException()
    {
        var act = () => PathUtility.CombinePath("", "   ", "\t");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureDirectoryExists_DirectoryDoesNotExist_CreatesIt()
    {
        var newDir = Path.Combine(_tempDir, "new-sub-dir");
        Directory.Exists(newDir).Should().BeFalse();

        PathUtility.EnsureDirectoryExists(newDir);

        Directory.Exists(newDir).Should().BeTrue();
    }

    [Fact]
    public void EnsureDirectoryExists_DirectoryAlreadyExists_DoesNotThrow()
    {
        var act = () => PathUtility.EnsureDirectoryExists(_tempDir);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetRelativePath_RelativeToBase_ReturnsRelativePath()
    {
        var result = PathUtility.GetRelativePath("/base/dir", "/base/dir/sub/file.txt");

        result.Should().Contain("sub");
        result.Should().Contain("file.txt");
    }
}
