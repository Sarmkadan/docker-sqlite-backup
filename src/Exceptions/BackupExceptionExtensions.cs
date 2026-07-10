using System;

namespace DockerSqliteBackup.Exceptions
{
    public static class BackupExceptionExtensions
    {
        public static bool IsTimeoutException(this BackupException ex)
        {
            return ex is BackupTimeoutException;
        }

        public static bool IsCorruptedException(this BackupException ex)
        {
            return ex is BackupCorruptedException;
        }

        public static string GetDetails(this BackupException ex)
        {
            return $"BackupId: {ex.BackupId?.ToString() ?? "N/A"}, ScheduleId: {ex.ScheduleId?.ToString() ?? "N/A"}";
        }
    }
}
