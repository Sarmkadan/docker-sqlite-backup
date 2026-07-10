# ConfigurationException

`ConfigurationException` serves as the base exception type within the `docker-sqlite-backup` project for errors encountered during the loading, parsing, or validation of application configurations. By providing a structured way to report configuration-related failures, this exception class allows developers to associate specific configuration keys with error messages, significantly streamlining the identification and resolution of misconfigured settings.

## API

- `public string? ConfigurationKey`
  Gets the configuration key associated with the exception, if applicable.
- `public ConfigurationException`
  Initializes a new instance of the `ConfigurationException` class.
- `public ConfigurationException(string message) : base`
  Initializes a new instance of the `ConfigurationException` class with a specified error message.
- `public ConfigurationException(string message, Exception innerException) : base`
  Initializes a new instance of the `ConfigurationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.
- `public ConfigurationException(string configurationKey, string message) : base`
  Initializes a new instance of the `ConfigurationException` class with a specified configuration key and error message.
- `public ConfigurationException(string configurationKey, string message, Exception innerException) : base`
  Initializes a new instance of the `ConfigurationException` class with a specified configuration key, error message, and a reference to the inner exception.
- `public MissingConfigurationException`
  Initializes a new instance of the `MissingConfigurationException` class.
- `public MissingConfigurationException(string configurationKey, string message) : base`
  Initializes a new instance of the `MissingConfigurationException` class with the specified configuration key and error message.
- `public InvalidConfigurationException`
  Initializes a new instance of the `InvalidConfigurationException` class.
- `public InvalidConfigurationException(string message) : base`
  Initializes a new instance of the `InvalidConfigurationException` class with a specified error message.

## Usage

### Throwing a Missing Configuration Exception
```csharp
if (string.IsNullOrEmpty(environmentVariable))
{
    throw new MissingConfigurationException("DB_PATH", "The required database path environment variable is missing.");
}
```

### Catching and Handling Configuration Exceptions
```csharp
try
{
    LoadConfiguration();
}
catch (ConfigurationException ex)
{
    if (ex.ConfigurationKey != null)
    {
        Console.WriteLine($"Configuration error for key '{ex.ConfigurationKey}': {ex.Message}");
    }
    else
    {
        Console.WriteLine($"General configuration error: {ex.Message}");
    }
}
```

## Notes

- **Inheritance:** `ConfigurationException` and its derived types (`MissingConfigurationException`, `InvalidConfigurationException`) inherit from the standard `System.Exception` class.
- **Thread Safety:** These exception types are immutable after they are thrown and are inherently thread-safe.
- **Usage:** Use `ConfigurationKey` to pinpoint the specific configuration setting that caused the failure, which is particularly useful in logs or user-facing error reports when multiple configuration sources are involved.
