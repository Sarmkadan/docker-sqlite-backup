using System;

namespace DockerSqliteBackup.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="ConfigurationException"/>.
    /// </summary>
    public static class ConfigurationExceptionExtensions
    {
        /// <summary>
        /// Returns the exception message, or a supplied default if the message is null or empty.
        /// </summary>
        /// <param name="ex">The configuration exception.</param>
        /// <param name="defaultMessage">The default message to return if the exception message is null or whitespace. Defaults to "Configuration error".</param>
        /// <returns>The exception message if not null or whitespace; otherwise, the default message.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ex"/> is null.</exception>
        public static string GetMessageOrDefault(this ConfigurationException ex, string defaultMessage = "Configuration error")
            => string.IsNullOrWhiteSpace(ex?.Message) == true
                ? defaultMessage
                : ex.Message;

        /// <summary>
        /// Indicates whether the exception represents a missing configuration value.
        /// </summary>
        /// <param name="ex">The configuration exception to check.</param>
        /// <returns>True if the exception is a <see cref="MissingConfigurationException"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ex"/> is null.</exception>
        public static bool IsMissing(this ConfigurationException ex)
            => ex is MissingConfigurationException;

        /// <summary>
        /// Indicates whether the exception represents an invalid configuration value.
        /// </summary>
        /// <param name="ex">The configuration exception to check.</param>
        /// <returns>True if the exception is an <see cref="InvalidConfigurationException"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ex"/> is null.</exception>
        public static bool IsInvalid(this ConfigurationException ex)
            => ex is InvalidConfigurationException;

        /// <summary>
        /// Formats the exception into a single log-friendly string containing the key, message and exception type.
        /// </summary>
        /// <param name="ex">The configuration exception to format.</param>
        /// <returns>A log-friendly string representation of the exception.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ex"/> is null.</exception>
        public static string ToLogString(this ConfigurationException ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            var key = ex.ConfigurationKey ?? "N/A";
            var message = ex.GetMessageOrDefault();
            var typeName = ex.GetType().Name;
            return $"[ConfigurationException] Type: {typeName}, Key: {key}, Message: {message}";
        }
    }
}
