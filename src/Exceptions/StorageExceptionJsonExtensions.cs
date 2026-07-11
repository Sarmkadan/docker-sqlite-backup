#nullable enable

using System.Text.Json;

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization extensions for StorageException and derived types.
/// </summary>
public static class StorageExceptionJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a StorageException to a JSON string.
    /// </summary>
    /// <param name="value">The StorageException to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the StorageException.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this StorageException value, bool indented = false)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a StorageException from a JSON string.
    /// Attempts to deserialize as specific derived exception types first, then falls back to the base StorageException type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized StorageException if successful; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static StorageException? FromJson(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<S3StorageException>(json, _jsonOptions)
                ?? JsonSerializer.Deserialize<LocalStorageException>(json, _jsonOptions)
                ?? JsonSerializer.Deserialize<AzureStorageException>(json, _jsonOptions)
                ?? JsonSerializer.Deserialize<InsufficientStorageException>(json, _jsonOptions)
                ?? JsonSerializer.Deserialize<StorageException>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a StorageException from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized StorageException if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out StorageException? value)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        try
        {
            value = FromJson(json);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}