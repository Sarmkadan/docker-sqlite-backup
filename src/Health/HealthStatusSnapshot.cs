#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Health;

/// <summary>
/// Serializable snapshot of the last known backup and verification state, persisted to
/// disk so that the <c>healthcheck</c> CLI subcommand can evaluate freshness from a
/// separate process invocation (as required by Docker's <c>HEALTHCHECK</c> mechanism).
/// </summary>
public sealed class HealthStatusSnapshot
{
    /// <summary>Gets or sets the timestamp of the most recent successful backup completion.</summary>
    public DateTime? LastBackupCompletedAt { get; set; }

    /// <summary>Gets or sets the cron expression of the schedule behind the most recent successful backup, when known.</summary>
    public string? LastBackupCompletedCronExpression { get; set; }

    /// <summary>Gets or sets the timestamp of the most recent backup failure.</summary>
    public DateTime? LastBackupFailedAt { get; set; }

    /// <summary>Gets or sets the error message of the most recent backup failure.</summary>
    public string? LastBackupFailedMessage { get; set; }

    /// <summary>Gets or sets the timestamp of the most recent restore verification result.</summary>
    public DateTime? LastRestoreVerificationAt { get; set; }

    /// <summary>Gets or sets whether the most recent restore verification passed.</summary>
    public bool? LastRestoreVerificationPassed { get; set; }

    /// <summary>Gets or sets the message accompanying the most recent restore verification result.</summary>
    public string? LastRestoreVerificationMessage { get; set; }
}
