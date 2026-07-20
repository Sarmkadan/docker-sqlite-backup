#nullable enable
// Author: Vladyslav Zaiets

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="BackupManifest"/> objects.
/// </summary>
public static class BackupManifestExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes the <see cref="BackupManifest"/> to a JSON string.
	/// </summary>
	/// <param name="manifest">The manifest to serialize.</param>
	/// <returns>A JSON string representation of the manifest.</returns>
	public static string ToJson(this BackupManifest manifest)
	{
		if (manifest is null)
		{
			throw new ArgumentNullException(nameof(manifest));
		}

		return JsonSerializer.Serialize(manifest, _jsonSerializerOptions);
	}

	/// <summary>
	/// Deserializes a <see cref="BackupManifest"/> from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>A deserialized <see cref="BackupManifest"/> object.</returns>
	public static BackupManifest? FromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			throw new ArgumentException("JSON string cannot be null or whitespace.", nameof(json));
		}

		return JsonSerializer.Deserialize<BackupManifest>(json, _jsonSerializerOptions);
	}

	/// <summary>
	/// Writes the manifest to a file.
	/// </summary>
	/// <param name="manifest">The manifest to write.</param>
	/// <param name="filePath">The path to the file where the manifest will be written.</param>
	public static void WriteToFile(this BackupManifest manifest, string filePath)
	{
		if (manifest is null)
		{
			throw new ArgumentNullException(nameof(manifest));
		}

		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
		}

		var directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		File.WriteAllText(filePath, manifest.ToJson());
	}

	/// <summary>
	/// Reads a manifest from a file.
	/// </summary>
	/// <param name="filePath">The path to the manifest file.</param>
	/// <returns>A deserialized <see cref="BackupManifest"/> object, or null if the file doesn't exist.</returns>
	public static BackupManifest? ReadFromFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
		}

		if (!File.Exists(filePath))
		{
			return null;
		}

		var json = File.ReadAllText(filePath);
		return FromJson(json);
	}
}
