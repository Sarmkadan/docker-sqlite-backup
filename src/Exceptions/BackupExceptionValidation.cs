using System;
using System.Collections.Generic;

namespace DockerSqliteBackup.Exceptions
{
    public static class BackupExceptionValidation
    {
        public static IReadOnlyList<string> Validate(this BackupException value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("BackupException instance is null.");
                return problems.AsReadOnly();
            }

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

        public static bool IsValid(this BackupException value)
        {
            return value.Validate().Count == 0;
        }

        public static void EnsureValid(this BackupException value)
        {
            var problems = value.Validate();
            if (problems.Count > 0)
            {
                string message = "BackupException validation failed: " + string.Join("; ", problems);
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}
