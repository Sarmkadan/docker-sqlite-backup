#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Services;

/// <summary>
/// Service for encrypting and decrypting backup files using AES-256-CBC.
/// Provides key management and encryption status operations.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a backup file and writes the encrypted result to the destination path.
    /// The IV is randomly generated per call and prepended to the output file.
    /// </summary>
    /// <param name="sourcePath">Path to the plaintext backup file.</param>
    /// <param name="destinationPath">Path where the encrypted file will be written.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The destination path of the encrypted file.</returns>
    Task<string> EncryptFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Decrypts an encrypted backup file and writes the plaintext to the destination path.
    /// </summary>
    /// <param name="sourcePath">Path to the encrypted backup file (must have IV prepended).</param>
    /// <param name="destinationPath">Path where the decrypted file will be written.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The destination path of the decrypted file.</returns>
    Task<string> DecryptFileAsync(string sourcePath, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Generates a new cryptographically random AES-256 key.
    /// </summary>
    /// <returns>Base64-encoded 32-byte key string.</returns>
    string GenerateKey();

    /// <summary>
    /// Validates whether the provided string is a correctly formatted AES-256 key.
    /// </summary>
    /// <param name="base64Key">The Base64-encoded key to validate.</param>
    /// <returns><c>true</c> if the key is valid; otherwise <c>false</c>.</returns>
    bool ValidateKey(string? base64Key);

    /// <summary>
    /// Returns whether encryption is currently enabled in the application configuration.
    /// </summary>
    bool IsEncryptionEnabled();

    /// <summary>
    /// Returns the currently configured encryption key, resolved from environment variables
    /// or application settings. Returns <c>null</c> when encryption is disabled or no key is set.
    /// </summary>
    string? GetActiveKey();

    /// <summary>
    /// Returns an <see cref="EncryptionStatus"/> snapshot describing the current configuration.
    /// </summary>
    EncryptionStatus GetStatus();
}

/// <summary>
/// Snapshot of the current encryption configuration state.
/// </summary>
public sealed class EncryptionStatus
{
    /// <summary>Whether AES-256 encryption is enabled.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Whether a valid key is currently configured.</summary>
    public bool HasValidKey { get; init; }

    /// <summary>
    /// A short opaque identifier derived from the first 8 characters of the key's SHA-256 hash.
    /// Useful for confirming which key is active without exposing the key material.
    /// <c>null</c> when no key is configured.
    /// </summary>
    public string? KeyFingerprint { get; init; }

    /// <summary>Source of the active key: "Environment", "Configuration", or "None".</summary>
    public string KeySource { get; init; } = "None";

    /// <summary>Human-readable summary of the encryption state.</summary>
    public string Summary =>
        IsEnabled && HasValidKey
            ? $"Encryption active — key fingerprint: {KeyFingerprint} (source: {KeySource})"
            : IsEnabled
                ? "Encryption enabled but no valid key is configured"
                : "Encryption disabled";
}
