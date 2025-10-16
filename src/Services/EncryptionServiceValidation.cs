#nullable enable

using System.Globalization;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Provides validation helpers for <see cref="EncryptionService"/> instances.
/// </summary>
public static class EncryptionServiceValidation
{
    /// <summary>
    /// Validates the provided <see cref="EncryptionService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Validate(this EncryptionService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate encryption key if encryption is enabled
        if (value.IsEncryptionEnabled())
        {
            var activeKey = value.GetActiveKey();
            if (string.IsNullOrWhiteSpace(activeKey))
            {
                problems.Add("Encryption is enabled but no valid encryption key is configured.");
            }
            else if (!value.ValidateKey(activeKey))
            {
                problems.Add("The configured encryption key is invalid — must be a Base64-encoded 32-byte AES-256 key.");
            }
        }

        // Validate status consistency
        var status = value.GetStatus();
        if (status.IsEnabled != value.IsEncryptionEnabled())
        {
            problems.Add("EncryptionStatus.IsEnabled does not match EncryptionService.IsEncryptionEnabled().");
        }

        if (status.HasValidKey && string.IsNullOrWhiteSpace(status.KeyFingerprint))
        {
            problems.Add("EncryptionStatus.HasValidKey is true but KeyFingerprint is null or empty.");
        }

        if (!status.HasValidKey && !string.IsNullOrWhiteSpace(status.KeyFingerprint))
        {
            problems.Add("EncryptionStatus.HasValidKey is false but KeyFingerprint is not null or empty.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the provided <see cref="EncryptionService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns><c>true</c> if the instance is valid; otherwise <c>false</c>.</returns>
    public static bool IsValid(this EncryptionService value)
    {
        try
        {
            return value.Validate().Count == 0;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the provided <see cref="EncryptionService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this EncryptionService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"The EncryptionService instance is not valid. Problems:\n{string.Join("\n", problems)}");
    }
}
