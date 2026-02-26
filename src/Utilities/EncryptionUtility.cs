#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Provides AES-256-CBC encryption and decryption for backup archive files.
/// The encrypted file format is: [16-byte IV][encrypted data].
/// </summary>
public static class EncryptionUtility
{
    private const int KeySizeBytes = 32;  // AES-256
    private const int IvSizeBytes = 16;   // AES block size

    /// <summary>
    /// Encrypts a file using AES-256-CBC and writes the result to <paramref name="destinationPath"/>.
    /// The IV is randomly generated per operation and prepended to the output.
    /// </summary>
    /// <param name="sourcePath">Path of the plaintext file to encrypt.</param>
    /// <param name="destinationPath">Path where the encrypted file will be written.</param>
    /// <param name="base64Key">Base64-encoded 32-byte AES key.</param>
    public static async Task EncryptFileAsync(string sourcePath, string destinationPath, string base64Key)
    {
        var key = DecodeKey(base64Key);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.GenerateIV();

        await using var destStream = File.OpenWrite(destinationPath);
        // Prepend the IV so decryption can recover it without an out-of-band channel.
        await destStream.WriteAsync(aes.IV);

        await using var cryptoStream = new CryptoStream(destStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await using var sourceStream = File.OpenRead(sourcePath);
        await sourceStream.CopyToAsync(cryptoStream);
    }

    /// <summary>
    /// Decrypts a file that was encrypted with <see cref="EncryptFileAsync"/> and writes
    /// the plaintext to <paramref name="destinationPath"/>.
    /// </summary>
    /// <param name="sourcePath">Path of the encrypted file (IV prepended).</param>
    /// <param name="destinationPath">Path where the decrypted file will be written.</param>
    /// <param name="base64Key">Base64-encoded 32-byte AES key.</param>
    public static async Task DecryptFileAsync(string sourcePath, string destinationPath, string base64Key)
    {
        var key = DecodeKey(base64Key);

        await using var sourceStream = File.OpenRead(sourcePath);

        var iv = new byte[IvSizeBytes];
        var bytesRead = await sourceStream.ReadAsync(iv);
        if (bytesRead != IvSizeBytes)
            throw new InvalidDataException($"Encrypted file is too short to contain a valid IV: {sourcePath}");

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.IV = iv;

        await using var cryptoStream = new CryptoStream(sourceStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        await using var destStream = File.OpenWrite(destinationPath);
        await cryptoStream.CopyToAsync(destStream);
    }

    /// <summary>
    /// Generates a new cryptographically random 256-bit key and returns it as a Base64 string.
    /// </summary>
    public static string GenerateBase64Key()
    {
        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        return Convert.ToBase64String(key);
    }

    /// <summary>
    /// Returns whether the provided string is a valid Base64-encoded 32-byte key.
    /// </summary>
    public static bool IsValidKey(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(base64Key);
            return bytes.Length == KeySizeBytes;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] DecodeKey(string base64Key)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Encryption key is not valid Base64.", nameof(base64Key), ex);
        }

        if (key.Length != KeySizeBytes)
            throw new ArgumentException(
                $"Encryption key must be {KeySizeBytes} bytes (256 bits). Provided key is {key.Length} bytes.",
                nameof(base64Key));

        return key;
    }
}
