#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="StringUtility"/>.
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
    /// Serializes the <see cref="StringUtility"/> type to a JSON string.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the <see cref="StringUtility"/> type metadata.</returns>
    public static string ToJson(bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions)
            {
                WriteIndented = true
            }
            : _jsonOptions;

        return JsonSerializer.Serialize(typeof(StringUtility), options);
    }

    /// <summary>
    /// Deserializes a JSON string representing a type to <see cref="StringUtility"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>Always returns typeof(StringUtility) since this is a static type reference.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static Type? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return typeof(StringUtility);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string representing a type to <see cref="StringUtility"/>.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives typeof(StringUtility) if successful; otherwise, null.</param>
    /// <returns>Always returns true since this always succeeds for StringUtility type reference.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out Type? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        value = typeof(StringUtility);
        return true;
    }
}