#nullable enable
using System.Text.Json;
using DockerSqliteBackup.Services;

namespace DockerSqliteBackup.Services;

/// <summary>
/// JSON serialization extensions for <see cref="VerificationService"/>.
/// </summary>
public static class VerificationServiceJsonExtensions
{
    // Cached serializer options with camelCase naming.
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes the <see cref="VerificationService"/> instance to JSON.
    /// </summary>
    /// <param name="value">The service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representing the instance.</returns>
    public static string ToJson(this VerificationService value, bool indented = false)
    {
        if (value is null) throw new System.ArgumentNullException(nameof(value));

        // Use a copy of the cached options if indentation is requested.
        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="VerificationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON representation of a <see cref="VerificationService"/>.</param>
    /// <returns>The deserialized instance, or <c>null</c> if deserialization fails.</returns>
    public static VerificationService? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        return JsonSerializer.Deserialize<VerificationService>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="VerificationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized instance if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    public static bool TryFromJson(string json, out VerificationService? value)
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
