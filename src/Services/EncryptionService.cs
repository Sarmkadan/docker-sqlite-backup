#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using DockerSqliteBackup.Configuration;
using DockerSqliteBackup.Utilities;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// AES-256-CBC encryption service for backup files.
/// Key material is sourced first from the <c>BACKUP_ENCRYPTION_KEY</c> environment variable,
/// then from <see cref="AppSettings.EncryptionKey"/>. The encrypted file format produced by
/// <see cref="EncryptFileAsync"/> is <c>[16-byte IV][ciphertext]</c>.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly AppSettings _settings;
    private readonly ILogger<EncryptionService> _logger;

    private const string EnvKeyName = "BACKUP_ENCRYPTION_KEY";

    /// <summary>
    /// Initializes a new instance of <see cref="EncryptionService"/>.
    /// </summary>
    public EncryptionService(AppSettings settings, ILogger<EncryptionService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> EncryptFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);

        var key = ResolveKey() ?? throw new InvalidOperationException(
            "Encryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");

        ct.ThrowIfCancellationRequested();
        await EncryptionUtility.EncryptFileAsync(sourcePath, destinationPath, key);

        _logger.LogInformation(
            "File encrypted with AES-256-CBC. Source: {Source}, Destination: {Dest}",
            sourcePath, destinationPath);

        return destinationPath;
    }

    /// <inheritdoc />
    public async Task<string> DecryptFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Encrypted source file not found: {sourcePath}", sourcePath);

        var key = ResolveKey() ?? throw new InvalidOperationException(
            "Decryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");

        ct.ThrowIfCancellationRequested();
        await EncryptionUtility.DecryptFileAsync(sourcePath, destinationPath, key);

        _logger.LogInformation(
            "File decrypted. Source: {Source}, Destination: {Dest}",
            sourcePath, destinationPath);

        return destinationPath;
    }

    /// <inheritdoc />
    public string GenerateKey() => EncryptionUtility.GenerateBase64Key();

    /// <inheritdoc />
    public bool ValidateKey(string? base64Key) => EncryptionUtility.IsValidKey(base64Key);

    /// <inheritdoc />
    public bool IsEncryptionEnabled() => _settings.EnableEncryption;

    /// <inheritdoc />
    public string? GetActiveKey() => ResolveKey();

    /// <inheritdoc />
    public EncryptionStatus GetStatus()
    {
        var envKey = Environment.GetEnvironmentVariable(EnvKeyName);
        var configKey = _settings.EncryptionKey;

        string keySource;
        string? activeKey;

        if (!string.IsNullOrWhiteSpace(envKey))
        {
            keySource = "Environment";
            activeKey = envKey;
        }
        else if (!string.IsNullOrWhiteSpace(configKey))
        {
            keySource = "Configuration";
            activeKey = configKey;
        }
        else
        {
            keySource = "None";
            activeKey = null;
        }

        var hasValidKey = EncryptionUtility.IsValidKey(activeKey);
        var fingerprint = hasValidKey && activeKey is not null
            ? ComputeFingerprint(activeKey)
            : null;

        return new EncryptionStatus
        {
            IsEnabled = _settings.EnableEncryption,
            HasValidKey = hasValidKey,
            KeyFingerprint = fingerprint,
            KeySource = keySource
        };
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private string? ResolveKey()
    {
        if (!_settings.EnableEncryption)
            return null;

        var key = Environment.GetEnvironmentVariable(EnvKeyName) ?? _settings.EncryptionKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning(
                "Encryption is enabled but no key is configured. " +
                "Set {EnvVar} or AppSettings__EncryptionKey.", EnvKeyName);
            return null;
        }

        if (!EncryptionUtility.IsValidKey(key))
        {
            _logger.LogError(
                "Configured encryption key is invalid — must be a Base64-encoded 32-byte value.");
            return null;
        }

        return key;
    }

    private static string ComputeFingerprint(string base64Key)
    {
        var keyBytes = Convert.FromBase64String(base64Key);
        var hash = SHA256.HashData(keyBytes);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
