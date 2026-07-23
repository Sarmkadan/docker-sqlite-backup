#nullable enable
// Author: Vladyslav Zaiets

using System.Text.Json;

namespace DockerSqliteBackup.Health;

/// <summary>
/// Reads and writes the <see cref="HealthStatusSnapshot"/> status file used to drive the
/// Docker <c>HEALTHCHECK</c> subcommand. Writes are atomic (write-to-temp-then-move) so a
/// concurrent read never observes a partially written file.
/// </summary>
public class HealthStatusStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _statusFilePath;
    private readonly object _writeLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthStatusStore"/> class.
    /// </summary>
    /// <param name="statusFilePath">The path of the status file. Relative paths are resolved against the application base directory.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="statusFilePath"/> is null or whitespace.</exception>
    public HealthStatusStore(string statusFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statusFilePath);

        _statusFilePath = Path.IsPathRooted(statusFilePath)
            ? statusFilePath
            : Path.Combine(AppContext.BaseDirectory, statusFilePath);
    }

    /// <summary>
    /// Gets the resolved absolute path of the status file.
    /// </summary>
    public string StatusFilePath => _statusFilePath;

    /// <summary>
    /// Loads the persisted snapshot from disk, or <see langword="null"/> when the status
    /// file does not exist yet or cannot be parsed.
    /// </summary>
    public HealthStatusSnapshot? Load()
    {
        if (!File.Exists(_statusFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_statusFilePath);
            return JsonSerializer.Deserialize<HealthStatusSnapshot>(json, SerializerOptions);
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Persists the snapshot to disk atomically, creating the parent directory if needed.
    /// </summary>
    /// <param name="snapshot">The snapshot to persist.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public void Save(HealthStatusSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var json = JsonSerializer.Serialize(snapshot, SerializerOptions);

        lock (_writeLock)
        {
            var directory = Path.GetDirectoryName(_statusFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempFilePath = $"{_statusFilePath}.tmp-{Guid.NewGuid():N}";
            File.WriteAllText(tempFilePath, json);
            File.Move(tempFilePath, _statusFilePath, overwrite: true);
        }
    }

    /// <summary>
    /// Loads the current snapshot (or a fresh one if none exists), applies <paramref name="mutate"/>,
    /// and persists the result.
    /// </summary>
    /// <param name="mutate">Callback that updates the snapshot in place.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mutate"/> is <see langword="null"/>.</exception>
    public void Update(Action<HealthStatusSnapshot> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        lock (_writeLock)
        {
            var snapshot = Load() ?? new HealthStatusSnapshot();
            mutate(snapshot);
            Save(snapshot);
        }
    }
}
