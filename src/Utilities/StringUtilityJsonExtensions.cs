#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for primitive types and common .NET types.
/// </summary>
public static class StringUtilityJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the specified value to a JSON string using camelCase naming policy.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson<T>(T value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object of type T.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to type T.</exception>
    public static T? FromJson<T>(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized value if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson<T>(string json, out T? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = default;
            return false;
        }
    }
}