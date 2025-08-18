// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

public class FileSystemUtilityTests : IAsyncLifetime
{
    private string _tempDir = string.Empty;

    public Task InitializeAsync()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"fs-util-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_tempDir))
            await FileSystemUtility.DeleteDirectoryAsync(_tempDir, recursive: true);
    }

    private string CreateFile(string name, string content = "test content")
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task SafeCopyFileAsync_ExistingFile_CopiesSuccessfully()
    {
        var source = CreateFile("source.txt", "copy me");
        var dest = Path.Combine(_tempDir, "dest.txt");

        await FileSystemUtility.SafeCopyFileAsync(source, dest);

        File.Exists(dest).Should().BeTrue();
        File.ReadAllText(dest).Should().Be("copy me");
    }

    [Fact]
    public async Task SafeCopyFileAsync_NonExistentSource_ThrowsFileNotFoundException()
    {
        var source = Path.Combine(_tempDir, "missing.txt");
        var dest = Path.Combine(_tempDir, "dest.txt");

        var act = async () => await FileSystemUtility.SafeCopyFileAsync(source, dest);

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task SafeCopyFileAsync_CreatesDestinationDirectory()
    {
        var source = CreateFile("source.txt", "content");
        var dest = Path.Combine(_tempDir, "sub", "nested", "dest.txt");

        await FileSystemUtility.SafeCopyFileAsync(source, dest);

        File.Exists(dest).Should().BeTrue();
    }

    [Fact]
    public void SafeDeleteFile_ExistingFile_DeletesSuccessfully()
    {
        var path = CreateFile("to-delete.txt");

        FileSystemUtility.SafeDeleteFile(path);

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void SafeDeleteFile_NonExistentFile_DoesNotThrow()
    {
        var path = Path.Combine(_tempDir, "missing.txt");

        var act = () => FileSystemUtility.SafeDeleteFile(path);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetFilesWithPattern_DirectoryWithMatchingFiles_ReturnsMatches()
    {
        CreateFile("a.txt");
        CreateFile("b.txt");
        CreateFile("c.csv");

        var result = FileSystemUtility.GetFilesWithPattern(_tempDir, "*.txt").ToList();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(f => f.Should().EndWith(".txt"));
    }

    [Fact]
    public void GetFilesWithPattern_NoMatchingFiles_ReturnsEmpty()
    {
        var result = FileSystemUtility.GetFilesWithPattern(_tempDir, "*.sqlite").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetFilesWithPattern_NonExistentDirectory_ReturnsEmpty()
    {
        var result = FileSystemUtility.GetFilesWithPattern(
            Path.Combine(_tempDir, "nonexistent"), "*.txt").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void CalculateDirectorySize_WithFiles_ReturnsSumOfFileSizes()
    {
        CreateFile("file1.txt", new string('A', 100));
        CreateFile("file2.txt", new string('B', 200));

        var size = FileSystemUtility.CalculateDirectorySize(_tempDir);

        size.Should().BeGreaterThanOrEqualTo(300);
    }

    [Fact]
    public void CalculateDirectorySize_NonExistentDirectory_ReturnsZero()
    {
        var size = FileSystemUtility.CalculateDirectorySize(Path.Combine(_tempDir, "ghost-dir"));

        size.Should().Be(0);
    }

    [Fact]
    public void CalculateDirectorySize_EmptyDirectory_ReturnsZero()
    {
        var emptyDir = Path.Combine(_tempDir, "empty-sub");
        Directory.CreateDirectory(emptyDir);

        var size = FileSystemUtility.CalculateDirectorySize(emptyDir);

        size.Should().Be(0);
    }

    [Fact]
    public void CopyDirectory_ExistingDirectory_CopiesAllFiles()
    {
        var srcDir = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(srcDir);
        File.WriteAllText(Path.Combine(srcDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(srcDir, "file2.txt"), "content2");

        var destDir = Path.Combine(_tempDir, "destination");

        FileSystemUtility.CopyDirectory(srcDir, destDir);

        File.Exists(Path.Combine(destDir, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(destDir, "file2.txt")).Should().BeTrue();
    }

    [Fact]
    public void CopyDirectory_RecursiveCopy_CopiesSubdirectories()
    {
        var srcDir = Path.Combine(_tempDir, "src-recursive");
        var subDir = Path.Combine(srcDir, "sub");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(srcDir, "root.txt"), "root");
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        var destDir = Path.Combine(_tempDir, "dest-recursive");

        FileSystemUtility.CopyDirectory(srcDir, destDir, recursive: true);

        File.Exists(Path.Combine(destDir, "root.txt")).Should().BeTrue();
        File.Exists(Path.Combine(destDir, "sub", "nested.txt")).Should().BeTrue();
    }

    [Fact]
    public void CopyDirectory_NonExistentSource_ThrowsDirectoryNotFoundException()
    {
        var act = () => FileSystemUtility.CopyDirectory(
            Path.Combine(_tempDir, "missing-source"),
            Path.Combine(_tempDir, "dest"));

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_ExistingDirectory_DeletesItAndContents()
    {
        var dir = Path.Combine(_tempDir, "to-delete");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "file.txt"), "data");

        await FileSystemUtility.DeleteDirectoryAsync(dir);

        Directory.Exists(dir).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonExistentDirectory_DoesNotThrow()
    {
        var act = async () => await FileSystemUtility.DeleteDirectoryAsync(
            Path.Combine(_tempDir, "nonexistent"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void IsFileInUse_ExistingUnlockedFile_ReturnsFalse()
    {
        var path = CreateFile("unlocked.txt");

        FileSystemUtility.IsFileInUse(path).Should().BeFalse();
    }

    [Fact]
    public void GetAvailableDiskSpace_RootPath_ReturnsPositiveValue()
    {
        var space = FileSystemUtility.GetAvailableDiskSpace("/");

        space.Should().BeGreaterThan(0);
    }
}
