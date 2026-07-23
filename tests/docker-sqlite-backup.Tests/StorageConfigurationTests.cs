#nullable enable
using System;
using System.Threading.Tasks;
using DockerSqliteBackup.Domain;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class StorageConfigurationTests
{
    // A minimal concrete implementation used only for testing the base class behavior.
    private sealed class TestStorageConfiguration : StorageConfiguration
    {
        public override int StorageType => 0;

        public override bool IsValid() => _isValid;

        public override Task<bool> TestConnectionAsync() => Task.FromResult(_testConnectionResult);

        // Helpers to control the behavior of the abstract members in individual tests.
        private readonly bool _isValid;
        private readonly bool _testConnectionResult;

        public TestStorageConfiguration(bool isValid = true, bool testConnectionResult = true)
        {
            _isValid = isValid;
            _testConnectionResult = testConnectionResult;
        }
    }

    [Fact]
    public void DefaultValues_AreInitializedCorrectly()
    {
        var config = new TestStorageConfiguration();

        // Id should be a non‑empty GUID.
        Assert.NotEqual(Guid.Empty, config.Id);

        // Name defaults to empty string.
        Assert.Equal(string.Empty, config.Name);

        // IsDefault defaults to false.
        Assert.False(config.IsDefault);

        // CreatedAt and LastModifiedAt are set to a recent UTC time.
        var now = DateTime.UtcNow;
        Assert.InRange(config.CreatedAt, now.AddMinutes(-1), now);
        Assert.InRange(config.LastModifiedAt, now.AddMinutes(-1), now);
    }

    [Fact]
    public void PropertySetters_Getters_WorkAsExpected()
    {
        var config = new TestStorageConfiguration();

        var newId = Guid.NewGuid();
        var newName = "MyStorage";
        var newCreated = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var newModified = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        config.Id = newId;
        config.Name = newName;
        config.IsDefault = true;
        config.CreatedAt = newCreated;
        config.LastModifiedAt = newModified;

        Assert.Equal(newId, config.Id);
        Assert.Equal(newName, config.Name);
        Assert.True(config.IsDefault);
        Assert.Equal(newCreated, config.CreatedAt);
        Assert.Equal(newModified, config.LastModifiedAt);
    }

    [Fact]
    public void IsValid_ReturnsConfiguredValue()
    {
        var validConfig = new TestStorageConfiguration(isValid: true);
        var invalidConfig = new TestStorageConfiguration(isValid: false);

        Assert.True(validConfig.IsValid());
        Assert.False(invalidConfig.IsValid());
    }

    [Fact]
    public async Task TestConnectionAsync_ReturnsConfiguredResult()
    {
        var successConfig = new TestStorageConfiguration(testConnectionResult: true);
        var failureConfig = new TestStorageConfiguration(testConnectionResult: false);

        var successResult = await successConfig.TestConnectionAsync();
        var failureResult = await failureConfig.TestConnectionAsync();

        Assert.True(successResult);
        Assert.False(failureResult);
    }

    [Fact]
    public void SettingName_ToNull_ThrowsArgumentNullException()
    {
        var config = new TestStorageConfiguration();

        // The base class does not guard against null, but the project treats
        // non‑nullable reference types as errors. Enforcing the contract here
        // ensures future changes that add validation will still be covered.
        Assert.Throws<ArgumentNullException>(() => config.Name = null!);
    }
}
