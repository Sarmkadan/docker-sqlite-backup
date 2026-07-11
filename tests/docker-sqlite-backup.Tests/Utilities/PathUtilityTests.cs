// Author: Vladyslav Zaiets

using DockerSqliteBackup.Utilities;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Utilities;

/// <summary>
/// Provides unit tests for the <see cref="DockerSqliteBackup.Utilities.PathUtility"/> class.
/// Tests various path manipulation and file system utility functions.
/// </summary>
public class PathUtilityTests : IDisposable
{
	/// <summary>
	/// Temporary directory used for test file operations.
	/// </summary>
	private readonly string _tempDir;

	/// <summary>
	/// Initializes a new instance of the <see cref="PathUtilityTests"/> class.
	/// Creates a temporary directory for test file operations.
	/// </summary>
	public PathUtilityTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), $"path-util-tests-{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);
	}

	/// <summary>
	/// Cleans up the temporary directory after test execution.
	/// </summary>
	public void Dispose()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.SanitizeFileName"/> returns the input unchanged when it contains only valid filename characters.
	/// </summary>
	public void SanitizeFileName_NormalName_ReturnsUnchanged()
	{
		var result = PathUtility.SanitizeFileName("backup-2024.sqlite");

		result.Should().Be("backup-2024.sqlite");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.SanitizeFileName"/> throws <see cref="ArgumentException"/> when given a string containing only whitespace.
	/// </summary>
	public void SanitizeFileName_EmptyInput_ThrowsArgumentException()
	{
		var act = () => PathUtility.SanitizeFileName(" ");

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.SanitizeFileName"/> throws <see cref="ArgumentException"/> when given an empty string.
	/// </summary>
	public void SanitizeFileName_NullOrEmptyString_ThrowsArgumentException()
	{
		var act = () => PathUtility.SanitizeFileName(string.Empty);

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.SanitizeFileName"/> returns "file" as fallback when given a string containing all invalid filename characters.
	/// </summary>
	public void SanitizeFileName_AllInvalidChars_ReturnsFallback()
	{
		var allInvalid = new string(Path.GetInvalidFileNameChars());

		var result = PathUtility.SanitizeFileName(allInvalid);

		result.Should().Be("file");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.IsAbsolute"/> returns true for an absolute Unix-style path.
	/// </summary>
	public void IsAbsolute_AbsolutePath_ReturnsTrue()
	{
		PathUtility.IsAbsolute("/usr/local/data").Should().BeTrue();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.IsAbsolute"/> returns false for a relative path.
	/// </summary>
	public void IsAbsolute_RelativePath_ReturnsFalse()
	{
		PathUtility.IsAbsolute("relative/path").Should().BeFalse();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.GetFileSize"/> returns the correct size for an existing file.
	/// </summary>
	public void GetFileSize_ExistingFile_ReturnsCorrectSize()
	{
		var path = Path.Combine(_tempDir, "size-test.txt");
		File.WriteAllText(path, "hello");

		var size = PathUtility.GetFileSize(path);

		size.Should().Be(5);
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.GetFileSize"/> returns -1 for a non-existent file.
	/// </summary>
	public void GetFileSize_NonExistentFile_ReturnsNegativeOne()
	{
		var size = PathUtility.GetFileSize(Path.Combine(_tempDir, "missing.txt"));

		size.Should().Be(-1);
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.IsValidFilePath"/> returns true for an existing non-empty file.
	/// </summary>
	public void IsValidFilePath_ExistingNonEmptyFile_ReturnsTrue()
	{
		var path = Path.Combine(_tempDir, "valid-file.txt");
		File.WriteAllText(path, "content");

		PathUtility.IsValidFilePath(path).Should().BeTrue();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.IsValidFilePath"/> returns false for a non-existent file.
	/// </summary>
	public void IsValidFilePath_NonExistentFile_ReturnsFalse()
	{
		PathUtility.IsValidFilePath(Path.Combine(_tempDir, "nope.txt")).Should().BeFalse();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.IsValidFilePath"/> returns false for an empty file.
	/// </summary>
	public void IsValidFilePath_EmptyFile_ReturnsFalse()
	{
		var path = Path.Combine(_tempDir, "empty.txt");
		File.WriteAllText(path, string.Empty);

		PathUtility.IsValidFilePath(path).Should().BeFalse();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.GenerateBackupFileName"/> generates a filename with the expected format when given a timestamp.
	/// </summary>
	public void GenerateBackupFileName_WithKnownTimestamp_ContainsExpectedParts()
	{
		var timestamp = new DateTime(2024, 6, 15, 10, 30, 0);

		var result = PathUtility.GenerateBackupFileName("myapp", timestamp);

		result.Should().StartWith("myapp_backup_2024-06-15");
		result.Should().EndWith(".sqlite");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.GenerateBackupFileName"/> generates a filename with the expected format using the current date when no timestamp is provided.
	/// </summary>
	public void GenerateBackupFileName_WithoutTimestamp_ContainsCurrentDate()
	{
		var result = PathUtility.GenerateBackupFileName("myapp");

		result.Should().StartWith("myapp_backup_");
		result.Should().EndWith(".sqlite");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.CombinePath"/> correctly combines two path segments.
	/// </summary>
	public void CombinePath_TwoSegments_CombinesCorrectly()
	{
		var result = PathUtility.CombinePath("/base", "sub");

		result.Should().Be(Path.Combine("/base", "sub"));
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.CombinePath"/> skips empty segments and produces a valid path.
	/// </summary>
	public void CombinePath_SkipsEmptySegments_ProducesValidPath()
	{
		var result = PathUtility.CombinePath("/base", " ", "file.txt");

		result.Should().Be(Path.Combine("/base", "file.txt"));
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.CombinePath"/> throws <see cref="ArgumentException"/> when called with no segments.
	/// </summary>
	public void CombinePath_NoSegments_ThrowsArgumentException()
	{
		var act = () => PathUtility.CombinePath();

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.CombinePath"/> throws <see cref="ArgumentException"/> when all segments are empty or whitespace.
	/// </summary>
	public void CombinePath_AllSegmentsEmpty_ThrowsArgumentException()
	{
		var act = () => PathUtility.CombinePath("", " ", "\t");

		act.Should().Throw<ArgumentException>();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.EnsureDirectoryExists"/> creates a directory that does not exist.
	/// </summary>
	public void EnsureDirectoryExists_DirectoryDoesNotExist_CreatesIt()
	{
		var newDir = Path.Combine(_tempDir, "new-sub-dir");
		Directory.Exists(newDir).Should().BeFalse();

		PathUtility.EnsureDirectoryExists(newDir);

		Directory.Exists(newDir).Should().BeTrue();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.EnsureDirectoryExists"/> does not throw when the directory already exists.
	/// </summary>
	public void EnsureDirectoryExists_DirectoryAlreadyExists_DoesNotThrow()
	{
		var act = () => PathUtility.EnsureDirectoryExists(_tempDir);

		act.Should().NotThrow();
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="PathUtility.GetRelativePath"/> returns a path relative to the base directory.
	/// </summary>
	public void GetRelativePath_RelativeToBase_ReturnsRelativePath()
	{
		var result = PathUtility.GetRelativePath("/base/dir", "/base/dir/sub/file.txt");

		result.Should().Contain("sub");
		result.Should().Contain("file.txt");
	}
}
