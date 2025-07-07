// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Thrown when configuration is invalid or incomplete.
/// </summary>
public class ConfigurationException : Exception
{
    /// <summary>
    /// The configuration key that caused the error, if applicable.
    /// </summary>
    public string? ConfigurationKey { get; set; }

    /// <summary>
    /// The expected type or format of the configuration value.
    /// </summary>
    public string? ExpectedFormat { get; set; }

    /// <summary>
    /// Initializes a new instance of ConfigurationException with a message.
    /// </summary>
    public ConfigurationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of ConfigurationException with a message and inner exception.
    /// </summary>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with message and configuration context.
    /// </summary>
    public ConfigurationException(string message, string? configKey, string? expectedFormat)
        : base(message)
    {
        ConfigurationKey = configKey;
        ExpectedFormat = expectedFormat;
    }

    /// <summary>
    /// Initializes a new instance with message, inner exception, and configuration context.
    /// </summary>
    public ConfigurationException(string message, Exception innerException, string? configKey, string? expectedFormat)
        : base(message, innerException)
    {
        ConfigurationKey = configKey;
        ExpectedFormat = expectedFormat;
    }
}
