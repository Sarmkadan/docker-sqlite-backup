#nullable enable

using DockerSqliteBackup.Domain;
using DockerSqliteBackup.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DockerSqliteBackup.Services;

/// <summary>
/// Extension methods for <see cref="RotationService"/> providing additional functionality
/// for backup rotation operations.
/// </summary>
public static class RotationServiceExtensions
{
	/// <summary>
	/// Executes rotation policy for multiple schedules at once.
	/// </summary>
	/// <param name="rotationService">The rotation service instance.</param>
	/// <param name="scheduleIds">Collection of schedule IDs to rotate.</param>
	/// <returns>Total number of backups deleted across all schedules.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotationService"/> or <paramref name="scheduleIds"/> is <see langword="null"/>.</exception>
	public static async Task<int> ExecuteRotationAsync(this RotationService rotationService, IEnumerable<Guid> scheduleIds)
	{
		ArgumentNullException.ThrowIfNull(rotationService);
		ArgumentNullException.ThrowIfNull(scheduleIds);

		var totalDeleted = 0;
		foreach (var scheduleId in scheduleIds)
		{
			totalDeleted += await rotationService.ExecuteRotationAsync(scheduleId);
		}

		return totalDeleted;
	}

	/// <summary>
	/// Gets rotation policy with fallback to default policy if not configured.
	/// </summary>
	/// <param name="rotationService">The rotation service instance.</param>
	/// <param name="scheduleId">The schedule ID.</param>
	/// <param name="defaultPolicy">Default policy to use if none exists.</param>
	/// <returns>The rotation policy, or default policy if none exists.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotationService"/> is <see langword="null"/>.</exception>
	public static async Task<RotationPolicy> GetRotationPolicyAsync(this RotationService rotationService, Guid scheduleId, RotationPolicy defaultPolicy)
	{
		ArgumentNullException.ThrowIfNull(rotationService);

		var policy = await rotationService.GetRotationPolicyAsync(scheduleId);
		return policy ?? defaultPolicy;
	}

	/// <summary>
	/// Checks if rotation is needed based on policy configuration.
	/// </summary>
	/// <param name="rotationService">The rotation service instance.</param>
	/// <param name="scheduleId">The schedule ID to check.</param>
	/// <returns>True if rotation should be performed; otherwise false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotationService"/> is <see langword="null"/>.</exception>
	public static async Task<bool> ShouldRotateAsync(this RotationService rotationService, Guid scheduleId)
	{
		ArgumentNullException.ThrowIfNull(rotationService);

		var policy = await rotationService.GetRotationPolicyAsync(scheduleId);
		if (policy is null || policy.Strategy == (int)Constants.RotationStrategy.NoRotation)
		{
			return false;
		}

		var backups = await rotationService.GetBackupsForRotationAsync(scheduleId);
		var backupsList = backups.ToList();

		return policy.ShouldRotate(backupsList.Count, DateTime.UtcNow, false);
	}

	/// <summary>
	/// Gets the count of backups that would be deleted by rotation.
	/// </summary>
	/// <param name="rotationService">The rotation service instance.</param>
	/// <param name="scheduleId">The schedule ID.</param>
	/// <returns>Number of backups that would be deleted.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rotationService"/> is <see langword="null"/>.</exception>
	public static async Task<int> GetBackupsToDeleteCountAsync(this RotationService rotationService, Guid scheduleId)
	{
		ArgumentNullException.ThrowIfNull(rotationService);

		var backups = await rotationService.GetBackupsForRotationAsync(scheduleId);
		return backups.Count();
	}
}