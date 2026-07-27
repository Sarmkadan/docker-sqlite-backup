#nullable enable

using System.Text.Json.Serialization;

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Source-generated <see cref="JsonSerializerContext"/> for the <see cref="DockerSqliteBackupException"/>
/// hierarchy. Generating metadata at compile time avoids reflection-based type discovery for every
/// exception logged at runtime, while still honoring the polymorphic
/// <see cref="JsonPolymorphicAttribute"/>/<see cref="JsonDerivedTypeAttribute"/> declarations on
/// <see cref="DockerSqliteBackupException"/> for round-tripping derived exception types.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(DockerSqliteBackupException))]
public partial class DockerSqliteBackupExceptionJsonContext : JsonSerializerContext
{
}
