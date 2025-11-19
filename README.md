## IntegrityCheckerServiceTestsExtensions

The `IntegrityCheckerServiceTestsExtensions` class provides a set of extension methods for testing the `IntegrityCheckerService`. It offers methods for creating test databases with various characteristics and for verifying the service's behavior.

### Usage Example

```csharp
using docker_sqlite_backup.Services;

// Create a test database with complex data
var complexTestDatabase = IntegrityCheckerServiceTestsExtensions.CreateComplexTestDatabase();

// Check that the service indicates the database is healthy
var mockService = IntegrityCheckerServiceTestsExtensions.CreateMockService();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateHealthy(mockService, complexTestDatabase);

// Check that the service indicates corruption in a corrupted database
var corruptedDatabase = IntegrityCheckerServiceTestsExtensions.CreateCorruptedDatabase();
IntegrityCheckerServiceTestsExtensions.ShouldIndicateCorruption(mockService, corruptedDatabase);
```

## StringUtilityTestsExtensions

The `StringUtilityTestsExtensions` class provides a set of utility methods for testing string manipulation operations. It includes methods for formatting, transforming, and validating strings in test scenarios.

### Usage Example

```csharp
using docker_sqlite_backup.Tests.Utilities;

// Test string truncation and case conversion
var longText = "ThisIsAVeryLongStringThatNeedsTruncating";
var truncated = StringUtilityTestsExtensions.TruncateForTest(longText, 10);
var camelCase = StringUtilityTestsExtensions.ToCamelCaseForTest("example_string");

// Validate and format strings
var validEmail = "test@example.com";
var invalidEmail = "bad-email";
var isValid = StringUtilityTestsExtensions.IsValidEmailForTest(validEmail);

// Join strings with readable formatting
var parts = new[] { "apple", "banana", "cherry" };
var joined = StringUtilityTestsExtensions.JoinReadableForTest(parts);
```

## ValidationException

The `ValidationException` class represents a custom exception for validation errors. It provides information about the parameter that caused the validation failure and a collection of error messages.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to validate a value
    var validator = new MyValidator();
    validator.Validate("invalid-value");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed for parameter '{ex.ParameterName}': {string.Join(", ", ex.Errors?.Select(e => e.Key + ": " + e.Value))}");
}
```

## ConfigurationException

The `ConfigurationException` class represents a custom exception for configuration errors. It provides information about the configuration key that caused the error and a detailed error message.

### Usage Example

```csharp
using docker_sqlite_backup.Exceptions;

try
{
    // Attempt to load a configuration value
    var config = new MyConfig();
    var value = config.Load("invalid-key");
}
catch (ConfigurationException ex)
{
    Console.WriteLine($"Configuration error for key '{ex.ConfigurationKey}': {ex.Message}");
}
```
```