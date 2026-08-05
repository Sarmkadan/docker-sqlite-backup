using System;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Services;
using DockerSqliteBackup.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DockerSqliteBackup.Tests;

public class EncryptionServiceValidationTests : IDisposable
{
    private const string EnvKeyName = "BACKUP_ENCRYPTION_KEY";
    private readonly string? _savedEnvKey;

    public EncryptionServiceValidationTests()
    {
        // Ensure the environment variable never leaks into these tests, since
        // EncryptionService.ResolveKey/GetStatus prefer it over AppSettings.
        _savedEnvKey = Environment.GetEnvironmentVariable(EnvKeyName);
        Environment.SetEnvironmentVariable(EnvKeyName, null);
    }

    public void Dispose() => Environment.SetEnvironmentVariable(EnvKeyName, _savedEnvKey);

    private static EncryptionService CreateService(bool enableEncryption, string? encryptionKey)
    {
        var settings = new AppSettings { EnableEncryption = enableEncryption, EncryptionKey = encryptionKey };
        return new EncryptionService(settings, NullLogger<EncryptionService>.Instance);
    }

    [Fact]
    public void Validate_EncryptionDisabled_ReturnsEmptyList()
    {
        var service = CreateService(enableEncryption: false, encryptionKey: null);

        var problems = service.Validate();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_EncryptionEnabledWithValidKey_ReturnsEmptyList()
    {
        var service = CreateService(enableEncryption: true, encryptionKey: EncryptionUtility.GenerateBase64Key());

        var problems = service.Validate();

        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_EncryptionEnabledWithNoKey_ReportsMissingKey()
    {
        var service = CreateService(enableEncryption: true, encryptionKey: null);

        var problems = service.Validate();

        Assert.Contains(problems, p => p.Contains("no valid encryption key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EncryptionEnabledWithMalformedKey_ReportsMissingKey()
    {
        // EncryptionService.ResolveKey rejects malformed keys before exposing them via
        // GetActiveKey(), so a malformed key surfaces the same "no key" problem as a
        // missing one rather than the (currently unreachable) "invalid key" branch.
        var service = CreateService(enableEncryption: true, encryptionKey: "not-a-valid-base64-key");

        var problems = service.Validate();

        Assert.Contains(problems, p => p.Contains("no valid encryption key", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_NullInstance_ThrowsArgumentNullException()
    {
        EncryptionService? service = null;

        Assert.Throws<ArgumentNullException>(() => service!.Validate());
    }

    [Fact]
    public void IsValid_WithValidInstance_ReturnsTrueAndMatchesValidateResult()
    {
        var service = CreateService(enableEncryption: false, encryptionKey: null);

        Assert.True(service.IsValid());
        Assert.Empty(service.Validate());
    }

    [Fact]
    public void IsValid_WithInvalidInstance_ReturnsFalse()
    {
        var service = CreateService(enableEncryption: true, encryptionKey: null);

        Assert.False(service.IsValid());
    }

    [Fact]
    public void EnsureValid_WithInvalidInstance_ThrowsArgumentExceptionListingProblems()
    {
        var service = CreateService(enableEncryption: true, encryptionKey: "still-not-valid");

        var ex = Assert.Throws<ArgumentException>(() => service.EnsureValid());
        Assert.Contains("not valid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
