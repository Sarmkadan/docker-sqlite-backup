#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Utilities;

/// <summary>
/// Provides System.Text.Json serialization utilities for file system operations.
/// </summary>
public static class FileSystemUtilityJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes file system utility configuration to a JSON string.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of file system utility configuration.</returns>
    public static string ToJson(bool indented = false)
    {
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        var config = new FileSystemUtilityConfig
        {
            MaxRetries = 3,
            RetryDelayMultiplier = 100,
            Recursive = true,
            DefaultSearchPattern = "*.*"
        };

        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes a JSON string containing file system utility configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A configuration object deserialized from the JSON string, or null if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static FileSystemUtilityConfig? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<FileSystemUtilityConfig>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a file system utility configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized configuration if successful; otherwise, null.</param>
    /// <returns>True if the deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out FileSystemUtilityConfig? value)
    {
        value = default;

        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<FileSystemUtilityConfig>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Configuration class for file system utility operations.
    /// </summary>
    [JsonSerializable(typeof(FileSystemUtilityConfig))]
    public sealed class FileSystemUtilityConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of retry attempts for file operations.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay multiplier for retry operations.
        /// </summary>
        public int RetryDelayMultiplier { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to use recursive directory operations.
        /// </summary>
        public bool Recursive { get; set; } = true;

        /// <summary>
        /// Gets or sets the default search pattern for file operations.
        /// </summary>
        public string DefaultSearchPattern { get; set; } = "*.*";
    }
}
