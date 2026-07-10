using System;

namespace DockerSqliteBackup.Exceptions
{
    /// <summary>
    /// Extension methods for <see cref="ConfigurationException"/>.
    /// </summary>
    public static class ConfigurationExceptionExtensions
    {
        /// <summary>
        /// Returns the exception message, or a supplied default if the message is null or empty.
        /// </summary>
        public static string GetMessageOrDefault(this ConfigurationException ex, string defaultMessage = "Configuration error")
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            return string.IsNullOrWhiteSpace(ex.Message) ? defaultMessage : ex.Message;
        }

        /// <summary>
        /// Indicates whether the exception represents a missing configuration value.
        /// </summary>
        public static bool IsMissing(this ConfigurationException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            return ex is MissingConfigurationException;
        }

        /// <summary>
        /// Indicates whether the exception represents an invalid configuration value.
        /// </summary>
        public static bool IsInvalid(this ConfigurationException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            return ex is InvalidConfigurationException;
        }

        /// <summary>
        /// Formats the exception into a single log-friendly string containing the key, message and exception type.
        /// </summary>
        public static string ToLogString(this ConfigurationException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            var key = ex.ConfigurationKey ?? "N/A";
            var message = ex.GetMessageOrDefault();
            var typeName = ex.GetType().Name;
            return $"[ConfigurationException] Type: {typeName}, Key: {key}, Message: {message}";
        }
    }
}
