#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Provides extension methods for <see cref="EncryptionService"/> to enable string and stream encryption/decryption operations.
/// </summary>
public static class EncryptionServiceExtensions
{
    private const string TempFilePrefix = "enc-";

    /// <summary>
    /// Encrypts the contents of a string and returns the encrypted Base64-encoded result.
    /// </summary>
    /// <param name="service">The encryption service instance.</param>
    /// <param name="plainText">The plain text content to encrypt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Base64-encoded encrypted string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="plainText"/> is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when encryption is not properly configured.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<string> EncryptStringAsync(
        this EncryptionService service,
        string plainText,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        ArgumentNullException.ThrowIfNull(service);

        var key = service.GetActiveKey();
        if (key is null)
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");
        }

        ct.ThrowIfCancellationRequested();

        // Generate a temporary file path for the string encryption
        var tempFile = Path.GetTempFileName();
        var encryptedFile = Path.ChangeExtension(tempFile, ".enc");

        try
        {
            // Write plain text to temp file
            await File.WriteAllTextAsync(tempFile, plainText, ct);

            // Encrypt the file
            await service.EncryptFileAsync(tempFile, encryptedFile, ct);

            // Read the encrypted content as Base64
            var encryptedContent = await File.ReadAllTextAsync(encryptedFile, ct);
            return encryptedContent;
        }
        finally
        {
            // Clean up temporary files
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    if (File.Exists(encryptedFile))
                    {
                        File.Delete(encryptedFile);
                    }
                }
                catch (IOException)
                {
                    // Best effort cleanup - ignore failures during disposal
                }
            }, ct);
        }
    }

    /// <summary>
    /// Decrypts the contents of a Base64-encoded encrypted string and returns the plain text.
    /// </summary>
    /// <param name="service">The encryption service instance.</param>
    /// <param name="encryptedBase64">The Base64-encoded encrypted string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The decrypted plain text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="encryptedBase64"/> is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when decryption fails or key is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<string> DecryptStringAsync(
        this EncryptionService service,
        string encryptedBase64,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedBase64);
        ArgumentNullException.ThrowIfNull(service);

        var key = service.GetActiveKey();
        if (key is null)
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");
        }

        ct.ThrowIfCancellationRequested();

        // Generate temporary file paths
        var tempEncryptedFile = Path.GetTempFileName();
        var decryptedFile = Path.ChangeExtension(tempEncryptedFile, ".decrypted");

        try
        {
            // Write encrypted content to temp file
            await File.WriteAllTextAsync(tempEncryptedFile, encryptedBase64, ct);

            // Decrypt the file
            await service.DecryptFileAsync(tempEncryptedFile, decryptedFile, ct);

            // Read the decrypted content
            var decryptedContent = await File.ReadAllTextAsync(decryptedFile, ct);
            return decryptedContent;
        }
        finally
        {
            // Clean up temporary files
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(tempEncryptedFile))
                    {
                        File.Delete(tempEncryptedFile);
                    }
                    if (File.Exists(decryptedFile))
                    {
                        File.Delete(decryptedFile);
                    }
                }
                catch (IOException)
                {
                    // Best effort cleanup - ignore failures during disposal
                }
            }, ct);
        }
    }

    /// <summary>
    /// Encrypts a stream in memory and returns the encrypted Base64-encoded result.
    /// </summary>
    /// <param name="service">The encryption service instance.</param>
    /// <param name="inputStream">The input stream containing plain text to encrypt.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Base64-encoded encrypted string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="inputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when encryption is not properly configured.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<string> EncryptStreamAsync(
        this EncryptionService service,
        Stream inputStream,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(inputStream);

        var key = service.GetActiveKey();
        if (key is null)
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");
        }

        ct.ThrowIfCancellationRequested();

        // Generate temporary file paths
        var tempFile = Path.GetTempFileName();
        var encryptedFile = Path.ChangeExtension(tempFile, ".enc");

        try
        {
            // Copy stream to temp file
            await using (var fileStream = File.Create(tempFile))
            {
                await inputStream.CopyToAsync(fileStream, ct);
            }

            // Encrypt the file
            await service.EncryptFileAsync(tempFile, encryptedFile, ct);

            // Read the encrypted content as Base64
            var encryptedContent = await File.ReadAllTextAsync(encryptedFile, ct);
            return encryptedContent;
        }
        finally
        {
            // Clean up temporary files
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                    }
                    if (File.Exists(encryptedFile))
                    {
                        File.Delete(encryptedFile);
                    }
                }
                catch (IOException)
                {
                    // Best effort cleanup - ignore failures during disposal
                }
            }, ct);
        }
    }

    /// <summary>
    /// Decrypts a Base64-encoded encrypted string to a stream.
    /// </summary>
    /// <param name="service">The encryption service instance.</param>
    /// <param name="encryptedBase64">The Base64-encoded encrypted string.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A stream containing the decrypted content.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="encryptedBase64"/> is <see langword="null"/>, empty, or consists only of white-space characters.</exception>
    /// <exception cref="InvalidOperationException">Thrown when decryption fails or key is invalid.</exception>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    public static async Task<Stream> DecryptToStreamAsync(
        this EncryptionService service,
        string encryptedBase64,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(encryptedBase64);
        ArgumentNullException.ThrowIfNull(service);

        var key = service.GetActiveKey();
        if (key is null)
        {
            throw new InvalidOperationException(
                "Encryption key is not configured. Set BACKUP_ENCRYPTION_KEY or AppSettings__EncryptionKey.");
        }

        ct.ThrowIfCancellationRequested();

        // Generate temporary file paths
        var tempEncryptedFile = Path.GetTempFileName();
        var decryptedFile = Path.ChangeExtension(tempEncryptedFile, ".decrypted");

        try
        {
            // Write encrypted content to temp file
            await File.WriteAllTextAsync(tempEncryptedFile, encryptedBase64, ct);

            // Decrypt the file
            await service.DecryptFileAsync(tempEncryptedFile, decryptedFile, ct);

            // Open the decrypted file as a stream
            var stream = File.OpenRead(decryptedFile);
            return stream;
        }
        catch
        {
            // Ensure cleanup on failure
            await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(tempEncryptedFile))
                    {
                        File.Delete(tempEncryptedFile);
                    }
                    if (File.Exists(decryptedFile))
                    {
                        File.Delete(decryptedFile);
                    }
                }
                catch (IOException)
                {
                    // Best effort cleanup - ignore failures during disposal
                }
            }, ct);
            throw;
        }
    }
}