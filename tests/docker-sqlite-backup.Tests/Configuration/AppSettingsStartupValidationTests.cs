// Author: Vladyslav Zaiets

using DockerSqliteBackup.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace DockerSqliteBackup.Tests.Configuration
{
    /// <summary>
    /// Verifies that invalid <c>AppSettings</c> values abort host startup with an
    /// <see cref="OptionsValidationException"/> naming the offending property.
    /// </summary>
    public class AppSettingsStartupValidationTests : IDisposable
    {
        private const string IntervalKey = "AppSettings:ScheduleCheckIntervalSeconds";
        private const string BackupDirectoryKey = "AppSettings:LocalStoragePath";

        private readonly string _backupDirectory;

        public AppSettingsStartupValidationTests()
        {
            _backupDirectory = Path.Combine(Path.GetTempPath(), $"startup-validation-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_backupDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_backupDirectory))
            {
                Directory.Delete(_backupDirectory, recursive: true);
            }
        }

        /// <summary>
        /// Tests that a valid configuration starts the host without raising a validation error.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithValidSettings_Succeeds()
        {
            // Arrange
            using var host = BuildHost(ValidSettings());

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            await act.Should().NotThrowAsync();
            await host.StopAsync();
        }

        /// <summary>
        /// Tests that a non-positive schedule check interval aborts startup and names the property.
        /// </summary>
        [Theory]
        [InlineData("0")]
        [InlineData("-5")]
        public async Task StartAsync_WithNonPositiveInterval_ThrowsOptionsValidationExceptionNamingProperty(string interval)
        {
            // Arrange
            var settings = ValidSettings();
            settings[IntervalKey] = interval;
            using var host = BuildHost(settings);

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            var error = await act.Should().ThrowAsync<OptionsValidationException>();
            error.Which.Failures.Should().ContainSingle(f => f.Contains("ScheduleCheckIntervalSeconds"));
            error.WithMessage("*ScheduleCheckIntervalSeconds*");
        }

        /// <summary>
        /// Tests that a backup directory that does not exist aborts startup and names the property.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithMissingBackupDirectory_ThrowsOptionsValidationExceptionNamingProperty()
        {
            // Arrange
            var missingDirectory = Path.Combine(_backupDirectory, "does-not-exist");
            var settings = ValidSettings();
            settings[BackupDirectoryKey] = missingDirectory;
            using var host = BuildHost(settings);

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            var error = await act.Should().ThrowAsync<OptionsValidationException>();
            error.Which.Failures.Should().ContainSingle(f => f.Contains("LocalStoragePath") && f.Contains("does not exist"));
            error.WithMessage("*LocalStoragePath*");
        }

        /// <summary>
        /// Tests that a blank backup directory aborts startup and names the property.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithBlankBackupDirectory_ThrowsOptionsValidationExceptionNamingProperty()
        {
            // Arrange
            var settings = ValidSettings();
            settings[BackupDirectoryKey] = "   ";
            using var host = BuildHost(settings);

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            var error = await act.Should().ThrowAsync<OptionsValidationException>();
            error.Which.Failures.Should().ContainSingle(f => f.Contains("LocalStoragePath"));
        }

        /// <summary>
        /// Tests that a backup directory that exists but cannot be written to aborts startup and names the property.
        /// Root bypasses permission bits, so this scenario is only exercised on Linux when not running privileged.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithReadOnlyBackupDirectory_ThrowsOptionsValidationExceptionNamingProperty()
        {
            // Arrange
            if (!OperatingSystem.IsLinux() || Environment.IsPrivilegedProcess)
            {
                return;
            }

            var settings = ValidSettings();
            settings[BackupDirectoryKey] = _backupDirectory;
            File.SetUnixFileMode(_backupDirectory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            using var host = BuildHost(settings);

            try
            {
                // Act
                Func<Task> act = () => host.StartAsync();

                // Assert
                var error = await act.Should().ThrowAsync<OptionsValidationException>();
                error.Which.Failures.Should().ContainSingle(f => f.Contains("LocalStoragePath") && f.Contains("not writable"));
            }
            finally
            {
                File.SetUnixFileMode(_backupDirectory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }

        private Dictionary<string, string?> ValidSettings() => new()
        {
            [IntervalKey] = "60",
            [BackupDirectoryKey] = _backupDirectory,
        };

        private static IHost BuildHost(Dictionary<string, string?> settings) =>
            new HostBuilder()
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings))
                .ConfigureServices((context, services) => services.AddBackupOptions(context.Configuration))
                .Build();
    }
}
