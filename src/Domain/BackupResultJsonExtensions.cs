#nullable enable

namespace DockerSqliteBackup.Domain;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="BackupResult"/>.
/// </summary>
public static class BackupResultJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes the <see cref="BackupResult"/> to a JSON string.
    /// </summary>
    /// <param name="value">The backup result to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <param name="options">Optional serializer options to override defaults. If null, uses the configured defaults.</param>
    /// <returns>A JSON string representation of the backup result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this BackupResult value, bool indented = false, JsonSerializerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);

        var effectiveOptions = options ?? (indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options);

        return JsonSerializer.Serialize(value, effectiveOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="BackupResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized backup result, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static BackupResult? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<BackupResult>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="BackupResult"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized backup result if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out BackupResult? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<BackupResult>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}