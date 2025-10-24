# ConfigurationExceptionExtensions

Extension methods for working with configuration-related exceptions, providing utilities to inspect and format exception details.

## API

### `public static string GetMessageOrDefault(this Exception? exception)`

Extracts the exception message or returns a default string if the exception is null.

- **Parameters**
  - `exception` – The exception to inspect; may be null.
- **Return value**
  - The exception’s `Message` property if `exception` is not null; otherwise, the string `"No exception provided"`.
- **Exceptions**
  - Does not throw.

### `public static bool IsMissing(this Exception? exception)`

Determines whether the exception represents a missing configuration (e.g., `FileNotFoundException` or `DirectoryNotFoundException`).

- **Parameters**
  - `exception` – The exception to inspect; may be null.
- **Return value**
  - `true` if `exception` is an instance of `FileNotFoundException` or `DirectoryNotFoundException`; otherwise, `false`.
- **Exceptions**
  - Does not throw.

### `public static bool IsInvalid(this Exception? exception)`

Determines whether the exception represents an invalid configuration (e.g., `FormatException`, `ArgumentException`, or `ConfigurationErrorsException`).

- **Parameters**
  - `exception` – The exception to inspect; may be null.
- **Return value**
  - `true` if `exception` is an instance of `FormatException`, `ArgumentException`, or `ConfigurationErrorsException`; otherwise, `false`.
- **Exceptions**
  - Does not throw.

### `public static string ToLogString(this Exception? exception)`

Formats the exception and its inner exceptions into a single log-friendly string.

- **Parameters**
  - `exception` – The exception to format; may be null.
- **Return value**
  - A string containing the exception type, message, and all inner exception details, each on a new line; returns `"No exception provided"` if `exception` is null.
- **Exceptions**
  - Does not throw.

## Usage

```csharp
try
{
    var config = File.ReadAllText("missing.config");
}
catch (Exception ex)
{
    if (ex.IsMissing())
    {
        Console.WriteLine("Configuration file is missing.");
    }
    Console.WriteLine(ex.ToLogString());
}
```

```csharp
try
{
    var value = int.Parse("invalid");
}
catch (Exception ex)
{
    if (ex.IsInvalid())
    {
        Console.WriteLine("Configuration value is invalid.");
    }
    Console.WriteLine(ex.GetMessageOrDefault());
}
```

## Notes

- All methods are thread-safe as they perform only read operations on the input and do not mutate shared state.
- Passing `null` to any method will not throw; instead, a default or empty result is returned.
- The `ToLogString` method includes all inner exceptions, which may produce lengthy output for deeply nested exception chains; consider limiting depth in logging scenarios if needed.
