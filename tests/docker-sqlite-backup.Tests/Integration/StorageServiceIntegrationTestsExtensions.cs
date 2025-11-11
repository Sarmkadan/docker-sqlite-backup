// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace DockerSqliteBackup.Tests.Integration;

/// <summary>
/// Extension methods for <see cref="StorageServiceIntegrationTests"/> to provide additional test utilities
/// and helper methods for common test scenarios.
/// </summary>
public static class StorageServiceIntegrationTestsExtensions
{
	/// <summary>
	/// Creates a temporary file in the test's temp directory with the specified content.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="name">The filename.</param>
	/// <param name="content">The file content.</param>
	/// <returns>The full path to the created file.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
	public static string CreateTempFile(this StorageServiceIntegrationTests test, string name, string content = "sqlite backup data")
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(name);

		var path = Path.Combine(test.GetTempDir(), "source", name);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, content);
		return path;
	}

	/// <summary>
	/// Creates a <see cref="LocalStorageConfiguration"/> pointing to a subdirectory within the test's temp directory.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="subDir">Optional subdirectory name.</param>
	/// <returns>A configured <see cref="LocalStorageConfiguration"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
	public static LocalStorageConfiguration MakeLocalConfig(this StorageServiceIntegrationTests test, string? subDir = null)
	{
		ArgumentNullException.ThrowIfNull(test);

		return new LocalStorageConfiguration
		{
			BaseDirectory = Path.Combine(test.GetTempDir(), subDir ?? "storage")
		};
	}

	/// <summary>
	/// Gets the temporary directory path used by the test.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <returns>The temp directory path.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
	public static string GetTempDir(this StorageServiceIntegrationTests test)
	{
		ArgumentNullException.ThrowIfNull(test);

		return test.GetFieldValue<string>("_tempDir");
	}

	/// <summary>
	/// Gets the <see cref="StorageService"/> instance under test.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <returns>The <see cref="StorageService"/> instance.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
	public static StorageService GetStorageService(this StorageServiceIntegrationTests test)
	{
		ArgumentNullException.ThrowIfNull(test);

		return test.GetFieldValue<StorageService>("_sut");
	}

	/// <summary>
	/// Verifies that a backup file exists at the specified path and contains expected content.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="filePath">The path to verify.</param>
	/// <param name="expectedContent">The expected content.</param>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="filePath"/> is <see langword="null"/>.</exception>
	public static void VerifyBackupFile(this StorageServiceIntegrationTests test, string filePath, string expectedContent = "sqlite backup data")
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(filePath);

		File.Exists(filePath).Should().BeTrue($"Backup file should exist at {filePath}");
		File.ReadAllText(filePath).Should().Be(expectedContent, "Backup file should contain expected content");
	}

	/// <summary>
	/// Creates a backup file with random content of specified size.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="name">The filename.</param>
	/// <param name="size">The size in bytes.</param>
	/// <returns>The full path to the created file.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than or equal to 0.</exception>
	public static string CreateRandomBackupFile(this StorageServiceIntegrationTests test, string name, int size = 1024)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(name);
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(size, 0);

		var path = Path.Combine(test.GetTempDir(), "random", name);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var randomContent = new byte[size];
		Random.Shared.NextBytes(randomContent);
		File.WriteAllBytes(path, randomContent);

		return path;
	}

	/// <summary>
	/// Asserts that a backup tuple represents the expected file.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="actual">The actual backup tuple.</param>
	/// <param name="expectedPath">The expected file path.</param>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="expectedPath"/> is <see langword="null"/>.</exception>
	public static void ShouldMatchBackupPath(this StorageServiceIntegrationTests test, (string Path, long Size, DateTime Modified) actual, string expectedPath)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(expectedPath);

		actual.Path.Should().Be(expectedPath, "Backup tuple path should match");
		Path.GetFileName(actual.Path).Should().Be(Path.GetFileName(expectedPath), "Backup tuple filename should match");
	}

	/// <summary>
	/// Creates multiple backup files in a directory and returns their paths.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="count">Number of files to create.</param>
	/// <param name="prefix">Filename prefix.</param>
	/// <returns>List of created file paths.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
	public static List<string> CreateMultipleBackupFiles(this StorageServiceIntegrationTests test, int count, string prefix = "backup")
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(prefix);
		ArgumentOutOfRangeException.ThrowIfNegative(count);

		var paths = new List<string>();
		var storageDir = Path.Combine(test.GetTempDir(), "multi");
		Directory.CreateDirectory(storageDir);

		for (int i = 0; i < count; i++)
		{
			var path = Path.Combine(storageDir, $"{prefix}{i}.sqlite");
			File.WriteAllText(path, $"backup content {i}");
			paths.Add(path);
		}

		return paths;
	}

	/// <summary>
	/// Gets the size of a file in bytes.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="filePath">The file path.</param>
	/// <returns>The file size in bytes.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="filePath"/> is <see langword="null"/>.</exception>
	public static long GetFileSize(this StorageServiceIntegrationTests test, string filePath)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(filePath);

		return new FileInfo(filePath).Length;
	}

	/// <summary>
	/// Creates a backup tuple with the specified properties.
	/// </summary>
	/// <param name="test">The test instance.</param>
	/// <param name="name">The backup name.</param>
	/// <param name="size">The backup size in bytes.</param>
	/// <param name="timestamp">Optional timestamp.</param>
	/// <returns>A backup tuple (Path, Size, Modified).</returns>
	/// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
	public static (string Path, long Size, DateTime Modified) CreateBackupTuple(this StorageServiceIntegrationTests test, string name, long size, DateTime? timestamp = null)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(name);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);

		return (Path.Combine(test.GetTempDir(), name), size, timestamp ?? DateTime.UtcNow);
	}

	/// <summary>
	/// Sealed class to prevent inheritance.
	/// </summary>
	private sealed class ReflectionHelper
	{
		private readonly StorageServiceIntegrationTests _test;

		public ReflectionHelper(StorageServiceIntegrationTests test) => _test = test ?? throw new ArgumentNullException(nameof(test));

		public T GetFieldValue<T>(string fieldName)
		{
			ArgumentNullException.ThrowIfNull(fieldName);

			var field = typeof(StorageServiceIntegrationTests).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			return (T)field?.GetValue(_test)!;
		}
	}

	private static T GetFieldValue<T>(this StorageServiceIntegrationTests test, string fieldName)
	{
		ArgumentNullException.ThrowIfNull(test);
		ArgumentNullException.ThrowIfNull(fieldName);

		var helper = new ReflectionHelper(test);
		return helper.GetFieldValue<T>(fieldName);
	}
}