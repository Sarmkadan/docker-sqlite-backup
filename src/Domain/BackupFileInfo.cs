#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Summary of a backup file that already exists in a storage backend.
/// </summary>
/// <param name="Name">The file name (last path segment) of the backup.</param>
/// <param name="Size">The size of the backup in bytes.</param>
/// <param name="Created">
/// The backup timestamp in UTC. Backends do not expose a portable creation time, so this is the
/// last-write (or last-modified) time reported by the backend.
/// </param>
public sealed record BackupFileInfo(string Name, long Size, DateTime Created);
