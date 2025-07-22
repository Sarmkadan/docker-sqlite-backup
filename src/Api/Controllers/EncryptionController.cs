#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Api;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Api.Controllers;

/// <summary>
/// API controller for managing backup encryption.
/// Exposes operations for querying encryption status, generating keys, and
/// performing on-demand encrypt/decrypt of backup artifacts.
/// </summary>
public class EncryptionController
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EncryptionController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EncryptionController"/>.
    /// </summary>
    public EncryptionController(
        IEncryptionService encryptionService,
        ILogger<EncryptionController> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current encryption configuration status, including whether encryption
    /// is enabled, whether a valid key is present, and the key source.
    /// </summary>
    public ApiResponse<object> GetStatus()
    {
        try
        {
            var status = _encryptionService.GetStatus();
            return ApiResponse<object>.SuccessResponse(new
            {
                status.IsEnabled,
                status.HasValidKey,
                status.KeyFingerprint,
                status.KeySource,
                status.Summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve encryption status");
            return ApiResponse<object>.ErrorResponse("ENCRYPTION_STATUS_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Generates a new cryptographically random AES-256 key.
    /// The returned key should be stored securely and supplied via the
    /// <c>BACKUP_ENCRYPTION_KEY</c> environment variable or application settings.
    /// </summary>
    public ApiResponse<object> GenerateKey()
    {
        try
        {
            var key = _encryptionService.GenerateKey();
            return ApiResponse<object>.SuccessResponse(new
            {
                Key = key,
                Note = "Store this key securely. It cannot be recovered if lost."
            }, "New AES-256 key generated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate encryption key");
            return ApiResponse<object>.ErrorResponse("KEY_GENERATION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Validates whether the provided Base64-encoded key is a valid AES-256 key.
    /// </summary>
    public ApiResponse<object> ValidateKey(ValidateKeyRequest request)
    {
        try
        {
            var isValid = _encryptionService.ValidateKey(request.Key);
            return ApiResponse<object>.SuccessResponse(new
            {
                IsValid = isValid,
                Message = isValid ? "Key is valid (32-byte AES-256)" : "Key is invalid (must be Base64-encoded 32 bytes)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate encryption key");
            return ApiResponse<object>.ErrorResponse("KEY_VALIDATION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Encrypts the file at <paramref name="request"/>.<see cref="EncryptFileRequest.SourcePath"/>
    /// and writes the result to <see cref="EncryptFileRequest.DestinationPath"/>.
    /// Uses the currently configured AES-256 key.
    /// </summary>
    public async Task<ApiResponse<object>> EncryptFile(EncryptFileRequest request, CancellationToken ct = default)
    {
        try
        {
            var destination = await _encryptionService.EncryptFileAsync(
                request.SourcePath, request.DestinationPath, ct);

            var info = new FileInfo(destination);
            return ApiResponse<object>.SuccessResponse(new
            {
                DestinationPath = destination,
                FileSizeBytes = info.Length
            }, "File encrypted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encryption failed for {Path}", request.SourcePath);
            return ApiResponse<object>.ErrorResponse("ENCRYPTION_FAILED", ex.Message);
        }
    }

    /// <summary>
    /// Decrypts the file at <paramref name="request"/>.<see cref="DecryptFileRequest.SourcePath"/>
    /// and writes the plaintext to <see cref="DecryptFileRequest.DestinationPath"/>.
    /// Uses the currently configured AES-256 key.
    /// </summary>
    public async Task<ApiResponse<object>> DecryptFile(DecryptFileRequest request, CancellationToken ct = default)
    {
        try
        {
            var destination = await _encryptionService.DecryptFileAsync(
                request.SourcePath, request.DestinationPath, ct);

            var info = new FileInfo(destination);
            return ApiResponse<object>.SuccessResponse(new
            {
                DestinationPath = destination,
                FileSizeBytes = info.Length
            }, "File decrypted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Decryption failed for {Path}", request.SourcePath);
            return ApiResponse<object>.ErrorResponse("DECRYPTION_FAILED", ex.Message);
        }
    }
}

/// <summary>Request model for key validation.</summary>
public class ValidateKeyRequest
{
    /// <summary>Base64-encoded key to validate.</summary>
    public string Key { get; set; } = string.Empty;
}

/// <summary>Request model for file encryption.</summary>
public class EncryptFileRequest
{
    /// <summary>Path of the plaintext file to encrypt.</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Path where the encrypted file will be written.</summary>
    public string DestinationPath { get; set; } = string.Empty;
}

/// <summary>Request model for file decryption.</summary>
public class DecryptFileRequest
{
    /// <summary>Path of the encrypted file to decrypt.</summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>Path where the decrypted file will be written.</summary>
    public string DestinationPath { get; set; } = string.Empty;
}
