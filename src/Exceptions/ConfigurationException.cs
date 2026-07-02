#nullable enable

namespace DockerSqliteBackup.Exceptions;

/// <summary>
/// Exception thrown when configuration-related errors occur.
/// </summary>
public class ConfigurationException : DockerSqliteBackupException
{
    /// <summary>
    /// Gets the configuration key that caused the error.
    /// </summary>
    public string? ConfigurationKey { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a configuration key.
    /// </summary>
    /// <param name="configurationKey">The configuration key that caused the error.</param>
    /// <param name="message">The error message.</param>
    public ConfigurationException(string configurationKey, string message) : base(message)
    {
        ConfigurationKey = configurationKey;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a configuration key, message, and inner exception.
    /// </summary>
    /// <param name="configurationKey">The configuration key that caused the error.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConfigurationException(string configurationKey, string message, Exception innerException) : base(message, innerException)
    {
        ConfigurationKey = configurationKey;
    }
}

/// <summary>
/// Exception thrown when required configuration is missing.
/// </summary>
public class MissingConfigurationException : ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingConfigurationException"/> class.
    /// </summary>
    /// <param name="configurationKey">The missing configuration key.</param>
    public MissingConfigurationException(string configurationKey)
        : base(configurationKey, $"Required configuration is missing: {configurationKey}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingConfigurationException"/> class with a custom message.
    /// </summary>
    /// <param name="configurationKey">The missing configuration key.</param>
    /// <param name="message">Custom error message.</param>
    public MissingConfigurationException(string configurationKey, string message)
        : base(configurationKey, message)
    {
    }
}

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class InvalidConfigurationException : ConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class.
    /// </summary>
    /// <param name="configurationKey">The configuration key that is invalid.</param>
    /// <param name="message">The validation error message.</param>
    public InvalidConfigurationException(string configurationKey, string message)
        : base(configurationKey, $"Invalid configuration for '{configurationKey}': {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationException"/> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public InvalidConfigurationException(string message) : base(message)
    {
    }
}
