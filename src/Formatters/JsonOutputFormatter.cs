// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Formatters;

/// <summary>
/// Formats output as JSON. Provides pretty-printing and compact options.
/// Uses System.Text.Json for high-performance serialization.
/// </summary>
public class JsonOutputFormatter : IOutputFormatter
{
    private readonly JsonSerializerOptions _options;
    private readonly JsonSerializerOptions _compactOptions;

    public string Name => "JSON";
    public string FileExtension => ".json";

    public JsonOutputFormatter(bool prettyPrint = true)
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new DateTimeConverter()
            }
        };

        _compactOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new DateTimeConverter()
            }
        };
    }

    /// <summary>
    /// Formats a single object as JSON.
    /// </summary>
    public string Format(object? value)
    {
        if (value == null)
            return "null";

        return JsonSerializer.Serialize(value, _options);
    }

    /// <summary>
    /// Formats a collection as a JSON array.
    /// </summary>
    public string FormatCollection(IEnumerable<object?> values)
    {
        var list = values.ToList();
        return JsonSerializer.Serialize(list, _options);
    }

    /// <summary>
    /// Formats a dictionary as a JSON object.
    /// </summary>
    public string FormatDictionary(Dictionary<string, object?> data)
    {
        return JsonSerializer.Serialize(data, _options);
    }

    /// <summary>
    /// Custom JSON converter for DateTime to ISO 8601 format.
    /// </summary>
    private class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString() ?? DateTime.UtcNow.ToString("O"));
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
        }
    }
}
