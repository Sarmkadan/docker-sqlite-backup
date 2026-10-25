#nullable enable
// Author: Vladyslav Zaiets

namespace DockerSqliteBackup.Domain;

/// <summary>
/// Extension methods for <see cref="RotationPolicy"/>.
/// </summary>
public static class RotationPolicyExtensions
{
    /// <summary>
    /// Selects the backups that the policy considers expired and eligible for deletion.
    /// Walks from the oldest backup towards the newest, so count-based rules drop the oldest backups first.
    /// </summary>
    /// <param name="policy">The rotation policy.</param>
    /// <param name="backups">The backups to evaluate, in any order.</param>
    /// <returns>
    /// The expired backups, ordered oldest first. The newest <see cref="RotationPolicy.MinimumBackupCount"/>
    /// backups are never selected.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> or <paramref name="backups"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<BackupResult> SelectExpired(this RotationPolicy policy, IEnumerable<BackupResult> backups)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(backups);

        var newestFirst = backups.OrderByDescending(b => b.StartedAt).ToList();
        var keepCount = Math.Max(0, policy.MinimumBackupCount);
        var expired = new List<BackupResult>();
        var remainingCount = newestFirst.Count;

        for (var i = newestFirst.Count - 1; i >= keepCount; i--)
        {
            var backup = newestFirst[i];

            if (policy.DeleteFailedBackups && !backup.IsSuccess)
            {
                expired.Add(backup);
                remainingCount--;
                continue;
            }

            if (!policy.ShouldRotate(remainingCount, backup.StartedAt, !backup.IsSuccess))
            {
                continue;
            }

            if (policy.VerifyBeforeDeletion && backup.Status != (int)Constants.BackupStatus.VerifiedSuccess)
            {
                continue;
            }

            expired.Add(backup);
            remainingCount--;
        }

        return expired;
    }

    /// <summary>
    /// Builds a human-readable summary of the rules the policy applies.
    /// Only the rules relevant to the active strategy are described.
    /// </summary>
    /// <param name="policy">The rotation policy.</param>
    /// <returns>A one-line description of the policy, without timestamps.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policy"/> is <see langword="null"/>.</exception>
    public static string Describe(this RotationPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!Enum.IsDefined(typeof(Constants.RotationStrategy), policy.Strategy))
        {
            return $"Unknown rotation strategy {policy.Strategy}.";
        }

        var strategy = (Constants.RotationStrategy)policy.Strategy;

        if (strategy == Constants.RotationStrategy.NoRotation)
        {
            return "NoRotation: all backups are kept.";
        }

        var rules = new List<string>();

        if (strategy is Constants.RotationStrategy.MaxFileCount or Constants.RotationStrategy.Combined)
        {
            rules.Add(policy.MaxBackupCount > 0
                ? $"keep at most {policy.MaxBackupCount} backups"
                : "no count limit");
        }

        if (strategy is Constants.RotationStrategy.MaxAge or Constants.RotationStrategy.Combined)
        {
            rules.Add($"delete backups older than {policy.MaxAgeDays} day(s)");
        }

        rules.Add($"always keep the newest {policy.MinimumBackupCount}");
        rules.Add(policy.VerifyBeforeDeletion
            ? "delete only verified backups"
            : "verification not required before deletion");
        rules.Add(policy.DeleteFailedBackups
            ? "failed backups are deleted"
            : "failed backups are kept");

        return $"{strategy}: {string.Join("; ", rules)}.";
    }
}
