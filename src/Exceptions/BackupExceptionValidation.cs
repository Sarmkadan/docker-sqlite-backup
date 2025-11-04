using System;
using System.Collections.Generic;

namespace DockerSqliteBackup.Exceptions
{
    /// <summary>
    /// Provides validation extension methods for <see cref="BackupException"/> instances.
    /// </summary>
    public static class BackupExceptionValidation
    {
        /// <summary>
        /// Validates the specified <see cref="BackupException"/> instance.
        /// </summary>
        /// <param name="value">The exception to validate. Cannot be null.</param>
        /// <returns>A read-only list of validation error messages. Empty if validation succeeds.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this BackupException value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(value.Message))
            {
                problems.Add("Message cannot be null or empty.");
            }

            if (value.BackupId.HasValue && value.BackupId.Value == Guid.Empty)
            {
                problems.Add("BackupId cannot be an empty GUID.");
            }

            if (value.ScheduleId.HasValue && value.ScheduleId.Value == Guid.Empty)
            {
                problems.Add("ScheduleId cannot be an empty GUID.");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="BackupException"/> is valid.
        /// </summary>
        /// <param name="value">The exception to check. Cannot be null.</param>
        /// <returns><see langword="true"/> if the exception is valid; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        public static bool IsValid(this BackupException value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Validate().Count == 0;
        }

        /// <summary>
        /// Validates and throws an <see cref="ArgumentException"/> if the specified <see cref="BackupException"/> is invalid.
        /// </summary>
        /// <param name="value">The exception to validate. Cannot be null.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">The exception is invalid.</exception>
        public static void EnsureValid(this BackupException value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                string message = "BackupException validation failed: " + string.Join("; ", problems);
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}