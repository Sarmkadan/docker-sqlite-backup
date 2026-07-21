// Author: Vladyslav Zaiets

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Exceptions;
using DockerSqliteBackup.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DockerSqliteBackup.Tests.Services;

/// <summary>
/// Provides unit tests for the <see cref="StorageService"/> class to verify storage adapter functionality
/// for both local filesystem and Azure Blob Storage configurations.
/// </summary>
/// <remarks>
/// This test class implements <see cref="IAsyncLifetime"/> to manage temporary test directories
/// that are created and cleaned up for each test run.
/// </remarks>
public class StorageAdapterTests : IAsyncLifetime
{
	/// <summary>
	/// Gets or sets the temporary directory path used for test file operations.
	/// </summary>
	private string _tempDir = string.Empty;

	/// <summary>
	/// Gets the storage service instance under test.
	/// </summary>
	private StorageService _sut = null!;

	/// <summary>
	/// Initializes the test by creating a temporary directory for test file operations.
	/// </summary>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public Task InitializeAsync()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), $"storage-adapter-tests-{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<LocalStorageBackend>();
		services.AddSingleton<S3StorageBackend>();
		services.AddSingleton<AzureStorageBackend>();
		var serviceProvider = services.BuildServiceProvider();
		_sut = new StorageService(new Mock<ILogger<StorageService>>().Object, serviceProvider);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Cleans up the test by deleting the temporary directory and all its contents.
	/// </summary>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	public Task DisposeAsync()
	{
		if (Directory.Exists(_tempDir))
			Directory.Delete(_tempDir, recursive: true);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Creates a temporary SQLite backup file in the test directory with the specified content.
	/// </summary>
	/// <param name="content">The content to write to the temporary file. Defaults to "backup-data".</param>
	/// <returns>The full path to the created temporary file.</returns>
	private string CreateTempFile(string content = "backup-data")
	{
		var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.sqlite");
		File.WriteAllText(path, content);
		return path;
	}

	// ── Local storage ─────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the storage service correctly copies a backup file to the destination directory
	/// when using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task UploadBackupAsync_LocalStorage_CopiesFileToDestination()
	{
		var sourceFile = CreateTempFile("local-backup-content");
		var destDir = Path.Combine(_tempDir, "dest");
		Directory.CreateDirectory(destDir);

		var config = new LocalStorageConfiguration
		{
			Name = "local-test",
			BaseDirectory = destDir
		};

		var resultPath = await _sut.UploadBackupAsync(sourceFile, config);

		resultPath.Should().NotBeNullOrEmpty();
		File.Exists(resultPath).Should().BeTrue();
		var content = await File.ReadAllTextAsync(resultPath);
		content.Should().Be("local-backup-content");
	}

	/// <summary>
	/// Tests that the storage service automatically creates the destination directory if it doesn't exist
	/// when uploading a backup file using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task UploadBackupAsync_LocalStorage_CreatesDirectoryIfMissing()
	{
		var sourceFile = CreateTempFile();
		var destDir = Path.Combine(_tempDir, "auto-created-dir");

		var config = new LocalStorageConfiguration
		{
			Name = "auto-dir",
			BaseDirectory = destDir
		};

		// destDir does not exist yet
		Directory.Exists(destDir).Should().BeFalse();

		await _sut.UploadBackupAsync(sourceFile, config);

		Directory.Exists(destDir).Should().BeTrue();
	}

	/// <summary>
	/// Tests that the storage service throws a <see cref="LocalStorageException"/> when attempting to upload
	/// a file that does not exist using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task UploadBackupAsync_FileNotFound_ThrowsLocalStorageException()
	{
		var missingFile = Path.Combine(_tempDir, "no-such-file.sqlite");
		var config = new LocalStorageConfiguration
		{
			Name = "test",
			BaseDirectory = _tempDir
		};

		var act = async () => await _sut.UploadBackupAsync(missingFile, config);

		await act.Should().ThrowAsync<LocalStorageException>();
	}

	/// <summary>
	/// Tests that the storage service correctly lists all uploaded backup files in the destination directory
	/// when using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task ListBackupsAsync_LocalStorage_ReturnsUploadedFiles()
	{
		var destDir = Path.Combine(_tempDir, "list-test");
		Directory.CreateDirectory(destDir);
		File.WriteAllText(Path.Combine(destDir, "backup_2025.sqlite"), "data");
		File.WriteAllText(Path.Combine(destDir, "backup_2026.sqlite"), "data");

		var config = new LocalStorageConfiguration
		{
			Name = "list-config",
			BaseDirectory = destDir
		};

		var results = (await _sut.ListBackupsAsync(config)).ToList();

		results.Should().HaveCount(2);
		results.Should().OnlyContain(r => r.Size > 0);
	}

	/// <summary>
	/// Tests that the storage service returns an empty collection when attempting to list backups
	/// from a non-existent directory using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task ListBackupsAsync_LocalStorage_MissingDirectory_ReturnsEmpty()
	{
		var config = new LocalStorageConfiguration
		{
			Name = "empty-config",
			BaseDirectory = Path.Combine(_tempDir, "nonexistent-dir")
		};

		var results = await _sut.ListBackupsAsync(config);

		results.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that the storage service correctly deletes a backup file from the storage location
	/// when using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task DeleteBackupAsync_LocalStorage_RemovesFile()
	{
		var filePath = CreateTempFile();
		File.Exists(filePath).Should().BeTrue();

		var config = new LocalStorageConfiguration { Name = "delete-test", BaseDirectory = _tempDir };
		await _sut.DeleteBackupAsync(filePath, config);

		File.Exists(filePath).Should().BeFalse();
	}

	/// <summary>
	/// Tests that the storage service correctly downloads a backup file to a temporary location
	/// when using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task DownloadBackupAsync_LocalStorage_CopiesFileToTempLocation()
	{
		var sourceFile = CreateTempFile("download-content");
		var config = new LocalStorageConfiguration { Name = "dl-test", BaseDirectory = _tempDir };

		var result = await _sut.DownloadBackupAsync(sourceFile, config);

		File.Exists(result).Should().BeTrue();
		var content = await File.ReadAllTextAsync(result);
		content.Should().Be("download-content");

		// Clean up temp download
		if (File.Exists(result)) File.Delete(result);
	}

	// ── AzureConfiguration validation ─────────────────────────────────────────

	/// <summary>
	/// Tests that an Azure storage configuration with both connection string and container name
	/// is considered valid.
	/// </summary>
	[Fact]
	public void AzureConfiguration_WithConnectionStringAndContainer_IsValid()
	{
		var config = new AzureConfiguration
		{
			Name = "azure-test",
			ConnectionString = "DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=https://devstoreaccount1.blob.core.windows.net",
			ContainerName = "backups"
		};

		config.IsValid().Should().BeTrue();
	}

	/// <summary>
	/// Tests that an Azure storage configuration is considered invalid when the container name is missing.
	/// </summary>
	[Fact]
	public void AzureConfiguration_MissingContainerName_IsInvalid()
	{
		var config = new AzureConfiguration
		{
			Name = "azure-no-container",
			ConnectionString = "UseDevelopmentStorage=true",
			ContainerName = ""
		};

		config.IsValid().Should().BeFalse();
	}

	/// <summary>
	/// Tests that an Azure storage configuration is considered invalid when credentials are missing.
	/// </summary>
	[Fact]
	public void AzureConfiguration_MissingCredentials_IsInvalid()
	{
		var config = new AzureConfiguration
		{
			Name = "azure-no-creds",
			ContainerName = "backups"
		};

		config.IsValid().Should().BeFalse();
	}

	/// <summary>
	/// Tests that an Azure storage configuration with a SAS URI and container name
	/// is considered valid.
	/// </summary>
	[Fact]
	public void AzureConfiguration_WithSasUri_IsValid()
	{
		var config = new AzureConfiguration
		{
			Name = "sas-config",
			SasUri = "https://myaccount.blob.core.windows.net/backups?sv=2020-08-04&ss=b&srt=co&sp=rwdlacuptfx&se=2030-01-01T00:00:00Z&st=2025-01-01T00:00:00Z&spr=https&sig=fakesig",
			ContainerName = "backups"
		};

		config.IsValid().Should().BeTrue();
	}

	// ── GetAvailableSpaceAsync ────────────────────────────────────────────────

	/// <summary>
	/// Tests that the storage service returns <see cref="long.MaxValue"/> for available space
	/// when using Azure Blob Storage configuration.
	/// </summary>
	[Fact]
	public async Task GetAvailableSpaceAsync_AzureConfig_ReturnsLongMaxValue()
	{
		var config = new AzureConfiguration
		{
			Name = "azure-space",
			ConnectionString = "UseDevelopmentStorage=true",
			ContainerName = "backups"
		};

		var space = await _sut.GetAvailableSpaceAsync(config);

		space.Should().Be(long.MaxValue);
	}

	/// <summary>
	/// Tests that the storage service returns a positive value for available space
	/// when using local filesystem storage configuration.
	/// </summary>
	[Fact]
	public async Task GetAvailableSpaceAsync_LocalConfig_ReturnsPositiveValue()
	{
		var config = new LocalStorageConfiguration
		{
			Name = "local-space",
			BaseDirectory = _tempDir
		};

		var space = await _sut.GetAvailableSpaceAsync(config);

		space.Should().BeGreaterThan(0);
	}
}