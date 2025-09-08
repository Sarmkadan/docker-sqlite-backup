# ValidationException
The `ValidationException` class is designed to handle validation errors that occur during the execution of the `docker-sqlite-backup` project. It provides a way to encapsulate and propagate validation errors, allowing for more robust and informative error handling. This exception type is intended to be thrown when validation checks fail, providing detailed information about the errors that occurred.

## API
* `public string? ParameterName`: Gets the name of the parameter that caused the validation error.
* `public IReadOnlyDictionary<string, string>? Errors`: Gets a dictionary of error messages, where each key is an error code and each value is a corresponding error message.
* `public ValidationException()`: Initializes a new instance of the `ValidationException` class with a default error message.
* `public ValidationException(string message)`: Initializes a new instance of the `ValidationException` class with a specified error message.
* `public ValidationException(string message, Exception innerException)`: Initializes a new instance of the `ValidationException` class with a specified error message and a reference to the inner exception that caused this exception.

## Usage
The following examples demonstrate how to use the `ValidationException` class:
```csharp
// Example 1: Throwing a ValidationException with a custom error message
if (string.IsNullOrEmpty(input))
{
    throw new ValidationException("Input cannot be empty");
}

// Example 2: Catching and handling a ValidationException
try
{
    // Code that may throw a ValidationException
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
    if (ex.Errors != null)
    {
        foreach (var error in ex.Errors)
        {
            Console.WriteLine($"Error code: {error.Key}, Error message: {error.Value}");
        }
    }
}
```

## Notes
When using the `ValidationException` class, consider the following edge cases and thread-safety remarks:
* The `ParameterName` property may be null if the validation error is not related to a specific parameter.
* The `Errors` dictionary may be null if no error messages are available.
* The `ValidationException` class is not thread-safe, as it relies on the `Exception` class, which is not designed to be thread-safe. Therefore, it is recommended to create a new instance of the `ValidationException` class for each validation error, rather than reusing an existing instance.
* When throwing a `ValidationException`, it is recommended to provide a descriptive error message and, if possible, a reference to the inner exception that caused the validation error. This allows for more informative error handling and debugging.
