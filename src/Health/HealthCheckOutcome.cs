#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Health;

/// <summary>
/// Result of evaluating a <see cref="HealthStatusSnapshot"/> for the Docker
/// <c>HEALTHCHECK</c> subcommand.
/// </summary>
/// <param name="IsHealthy">Whether the container should be reported as healthy.</param>
/// <param name="Reason">Human-readable explanation of the outcome, printed to stdout by the CLI.</param>
public sealed record HealthCheckOutcome(bool IsHealthy, string Reason);
