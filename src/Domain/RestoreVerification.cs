#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Represents the verification result of a backup by attempting to restore it.
/// </summary>
public class RestoreVerification
{
    /// <summary>Gets or sets the unique identifier for this verification.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Gets or sets the backup result ID being verified.</summary>
    public Guid BackupResultId { get; set; }

    /// <summary>Gets or sets whether the verification was successful.</summary>
    public bool IsSuccessful { get; set; }

    /// <summary>Gets or sets the verification status message.</summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets when the verification started.</summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets when the verification completed.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Gets or sets the duration of the verification in milliseconds.</summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>Gets or sets the number of records in the restored database.</summary>
    public long RecordCount { get; set; }

    /// <summary>Gets or sets the size of the restored database in bytes.</summary>
    public long DatabaseSizeBytes { get; set; }

    /// <summary>Gets or sets whether the database integrity check passed.</summary>
    public bool IntegrityCheckPassed { get; set; }

    /// <summary>Gets or sets any integrity check errors.</summary>
    public string? IntegrityCheckErrors { get; set; }

    /// <summary>Gets or sets the temporary directory used for verification.</summary>
    public string TemporaryDirectory { get; set; } = string.Empty;

    /// <summary>Gets or sets the error message if verification failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Marks the verification as completed.
    /// </summary>
    public void MarkCompleted(bool successful, string statusMessage)
    {
        IsSuccessful = successful;
        StatusMessage = statusMessage;
        CompletedAt = DateTime.UtcNow;
        
        if (StartedAt != DateTime.MinValue)
        {
            DurationMilliseconds = (long)(CompletedAt.Value - StartedAt).TotalMilliseconds;
        }
    }

    /// <summary>
    /// Gets the elapsed duration of the verification.
    /// </summary>
    public TimeSpan GetElapsedDuration()
    {
        var endTime = CompletedAt ?? DateTime.UtcNow;
        return endTime - StartedAt;
    }
}
