# StringUtilityTests

This test class contains unit tests for the `StringUtility` helper methods in the `docker-sqlite-backup` project. Each test validates a specific formatting, conversion, or validation behavior of the utility functions.

## API

### `public void FormatBytes_ShouldReturnExpectedString`
Verifies that `StringUtility.FormatBytes` returns the correct human‑readable representation for various byte values.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception (typically an assertion failure) if the formatted string does not match the expected result.

### `public void ToKebabCase_ShouldConvertCorrectly`
Tests that `StringUtility.ToKebabCase` correctly converts input strings to kebab‑case.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the conversion does not produce the expected kebab‑case string.

### `public void ToSnakeCase_ShouldConvertCorrectly`
Tests that `StringUtility.ToSnakeCase` correctly converts input strings to snake_case.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the conversion does not produce the expected snake_case string.

### `public void ToPascalCase_ShouldConvertCorrectly`
Tests that `StringUtility.ToPascalCase` correctly converts input strings to PascalCase.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the conversion does not produce the expected PascalCase string.

### `public void ToCamelCase_ShouldConvertCorrectly`
Tests that `StringUtility.ToCamelCase` correctly converts input strings to camelCase.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the conversion does not produce the expected camelCase string.

### `public void Truncate_ShouldTruncateCorrectly`
Validates that `StringUtility.Truncate` shortens strings to the specified length and appends the ellipsis when needed.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the truncation result does not match the expected output.

### `public void MaskSensitive_ShouldMaskCorrectly`
Checks that `StringUtility.MaskSensitive` replaces sensitive portions of a string with a masking character while preserving the requested visible length.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the masked string does not conform to the expected pattern.

### `public void IsValidEmail_ShouldReturnExpectedResult`
Ensures that `StringUtility.IsValidEmail` correctly identifies valid and invalid e‑mail addresses.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the Boolean result differs from the expected outcome for any test case.

### `public void IsValidGuid_ShouldReturnExpectedResult`
Ensures that `StringUtility.IsValidGuid` correctly identifies valid and invalid GUID strings.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the Boolean result differs from the expected outcome for any test case.

### `public void SplitLines_ShouldHandleDifferentLineEndings`
Confirms that `StringUtility.SplitLines` correctly splits input strings into lines regardless of `\n`, `\r\n`, or `\r` line endings.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the resulting line array does not match the expected split.

### `public void JoinReadable_ShouldFormatCorrectly`
Tests that `StringUtility.JoinReadable` concatenates a collection of strings with appropriate separators and conjunctions (e.g., “a, b and c”).  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the joined string does not match the expected readable format.

### `public void RemoveWhitespace_ShouldRemoveAllWhitespace`
Verifies that `StringUtility.RemoveWhitespace` eliminates all Unicode whitespace characters from the input.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if any whitespace remains after processing.

### `public void QuoteIfNeeded_ShouldQuoteWhenNecessary`
Ensures that `StringUtility.QuoteIfNeeded` adds quotation marks around a string only when it contains delimiters, whitespace, or requires escaping.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if quoting behavior does not match the specification.

### `public void Repeat_ShouldRepeatCorrectly`
Checks that `StringUtility.Repeat` returns the input string repeated the requested number of times.  
- **Parameters:** None.  
- **Return value:** `void`.  
- **Throws:** Throws an exception if the repeated string length or content is incorrect.

## Usage

The test class is intended to be executed by a unit‑test runner (e.g., xUnit, NUnit, MSTest). Below are two typical ways to invoke its tests.

```csharp
// Example 1: Running a single test manually (useful for debugging)
var testInstance = new StringUtilityTests();
testInstance.FormatBytes_ShouldReturnExpectedString(); // passes if implementation is correct
```

```csharp
// Example 2: Executing all tests via the xUnit framework
// No explicit code is required; the test discovery mechanism picks up the public void methods.
// Ensure the project references xunit and xunit.runner.visualstudio, then run:
//   dotnet test
```

## Notes

- **Edge cases:** Each test method internally checks a variety of inputs, including empty strings, null‑equivalent values (where applicable), strings consisting solely of whitespace, and strings at the boundary of length limits. If the implementation does not handle these cases, the corresponding test will fail.
- **Thread‑safety:** The test class contains no static or instance state; each method operates only on its own local variables and the stateless `StringUtility` helpers. Consequently, the tests are safe to run in parallel without risk of shared‑state corruption. The underlying `StringUtility` methods are likewise expected to be pure functions and therefore thread‑safe.
