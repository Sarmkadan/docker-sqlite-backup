using System;
using System.Collections.Generic;
using System.Text;

namespace DockerSqliteBackup.Exceptions
{
    /// <summary>
    /// Extension methods for <see cref="ValidationException"/>.
    /// </summary>
    public static class ValidationExceptionExtensions
    {
        /// <summary>
        /// Determines whether the exception contains an error entry for the specified key.
        /// </summary>
        /// <param name="ex">The validation exception.</param>
        /// <param name="key">The error key to look for.</param>
        /// <returns><c>true</c> if an error with the given key exists; otherwise, <c>false</c>.</returns>
        public static bool HasError(this ValidationException ex, string key)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            return ex.Errors?.ContainsKey(key) ?? false;
        }

        /// <summary>
        /// Retrieves the error message associated with the specified key, if any.
        /// </summary>
        /// <param name="ex">The validation exception.</param>
        /// <param name="key">The error key.</param>
        /// <returns>The error message, or <c>null</c> if the key is not present.</returns>
        public static string? GetError(this ValidationException ex, string key)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            if (ex.Errors != null && ex.Errors.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Returns a detailed, multi‑line string representation of the exception,
        /// including the message, parameter name and all error entries.
        /// </summary>
        /// <param name="ex">The validation exception.</param>
        /// <returns>A formatted string with all available information.</returns>
        public static string ToDetailedString(this ValidationException ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            var sb = new StringBuilder();
            sb.AppendLine($"Validation failed: {ex.Message}");

            if (!string.IsNullOrEmpty(ex.ParameterName))
            {
                sb.AppendLine($"Parameter: {ex.ParameterName}");
            }

            if (ex.Errors != null && ex.Errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                foreach (KeyValuePair<string, string> kvp in ex.Errors)
                {
                    sb.AppendLine($" - {kvp.Key}: {kvp.Value}");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
