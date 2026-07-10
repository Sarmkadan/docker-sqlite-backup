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
    public static string ToJson(this StorageException value, bool indented = false)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a StorageException from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized StorageException, or null if the JSON is null or empty.</returns>
    public static StorageException? FromJson(string json)
    {
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
        catch (JsonException)
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
    public static bool TryFromJson(string json, out StorageException? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}