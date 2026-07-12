using System;

namespace DockerSqliteBackup.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="BackupException"/> to simplify exception type checking and extraction of backup-related details.
    /// </summary>
    public static class BackupExceptionExtensions
    {
        /// <summary>
        /// Determines whether the specified exception is a timeout exception.
        /// </summary>
        /// <param name="ex">The exception to check. Must not be null.</param>
        /// <returns>True if the exception is a <see cref="BackupTimeoutException"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
        public static bool IsTimeoutException(this BackupException ex) => ex is BackupTimeoutException;

        /// <summary>
        /// Determines whether the specified exception is a corrupted backup exception.
        /// </summary>
        /// <param name="ex">The exception to check. Must not be null.</param>
        /// <returns>True if the exception is a <see cref="BackupCorruptedException"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
        public static bool IsCorruptedException(this BackupException ex) => ex is BackupCorruptedException;

        /// <summary>
        /// Extracts backup-related details from the exception as a formatted string.
        /// </summary>
        /// <param name="ex">The exception from which to extract details. Must not be null.</param>
        /// <returns>A string containing the backup ID and schedule ID, or "N/A" if they are null.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
        public static string GetDetails(this BackupException ex)
        {
            if (ex is null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            return $"BackupId: {ex.BackupId?.ToString() ?? "N/A"}, ScheduleId: {ex.ScheduleId?.ToString() ?? "N/A"}";
        }
    }
}
