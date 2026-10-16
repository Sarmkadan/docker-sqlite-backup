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
    /// Verifies that invalid <c>RetentionPolicy</c> configuration aborts host startup with an
    /// <see cref="OptionsValidationException"/> naming the offending property.
    /// </summary>
    public class RetentionPolicyStartupValidationTests : IDisposable
    {
        private const string DailyKey = "RetentionPolicy:DailyRetentionDays";
        private const string WeeklyKey = "RetentionPolicy:WeeklyRetentionWeeks";
        private const string MonthlyKey = "RetentionPolicy:MonthlyRetentionMonths";
        private const string StorageKey = "RetentionPolicy:MaxTotalStorageBytes";
        private const string BackupDirectoryKey = "AppSettings:LocalStoragePath";

        private readonly string _backupDirectory;

        public RetentionPolicyStartupValidationTests()
        {
            _backupDirectory = Path.Combine(Path.GetTempPath(), $"retention-validation-{Guid.NewGuid():N}");
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
        /// Tests that the default retention tiers (7 daily, 4 weekly, 12 monthly) start the host without error.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithValidRetentionPolicy_Succeeds()
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
        /// Tests that a negative keep-count or storage cap aborts startup and names the property.
        /// </summary>
        [Theory]
        [InlineData(DailyKey, "-1", "DailyRetentionDays")]
        [InlineData(WeeklyKey, "-1", "WeeklyRetentionWeeks")]
        [InlineData(MonthlyKey, "-1", "MonthlyRetentionMonths")]
        [InlineData(StorageKey, "-1", "MaxTotalStorageBytes")]
        public async Task StartAsync_WithNegativeRetentionValue_ThrowsOptionsValidationExceptionNamingProperty(
            string key, string value, string property)
        {
            // Arrange
            var settings = ValidSettings();
            settings[key] = value;
            using var host = BuildHost(settings);

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            var error = await act.Should().ThrowAsync<OptionsValidationException>();
            error.Which.Failures.Should().ContainSingle(f => f.Contains(property) && f.Contains("must not be negative"));
            error.WithMessage($"*{property}*");
        }

        /// <summary>
        /// Tests that a policy with every tier set to zero aborts startup, since it would delete every backup immediately.
        /// </summary>
        [Fact]
        public async Task StartAsync_WithAllTiersZero_ThrowsOptionsValidationExceptionNamingTiers()
        {
            // Arrange
            var settings = ValidSettings();
            settings[DailyKey] = "0";
            settings[WeeklyKey] = "0";
            settings[MonthlyKey] = "0";
            using var host = BuildHost(settings);

            // Act
            Func<Task> act = () => host.StartAsync();

            // Assert
            var error = await act.Should().ThrowAsync<OptionsValidationException>();
            error.Which.Failures.Should().ContainSingle(f =>
                f.Contains("DailyRetentionDays") && f.Contains("WeeklyRetentionWeeks") && f.Contains("MonthlyRetentionMonths"));
            error.WithMessage("*deleted immediately*");
        }

        private Dictionary<string, string?> ValidSettings() => new()
        {
            [BackupDirectoryKey] = _backupDirectory,
            [DailyKey] = "7",
            [WeeklyKey] = "4",
            [MonthlyKey] = "12",
            [StorageKey] = "0",
        };

        private static IHost BuildHost(Dictionary<string, string?> settings) =>
            new HostBuilder()
                .ConfigureAppConfiguration(config => config.AddInMemoryCollection(settings))
                .ConfigureServices((context, services) => services.AddBackupOptions(context.Configuration))
                .Build();
    }
}
