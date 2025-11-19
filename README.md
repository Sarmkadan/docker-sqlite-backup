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
