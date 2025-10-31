# StringUtilityTestsExtensions

`StringUtilityTestsExtensions` is a static utility class within the `docker-sqlite-backup` project that exposes public wrapper methods for internal string manipulation functions. It is designed exclusively for testing purposes, allowing unit tests to invoke otherwise inaccessible helper methods without modifying production access modifiers. Each method delegates directly to a corresponding internal implementation, preserving identical behaviour and parameter contracts.

## API

### TruncateForTest

```csharp
public static string TruncateForTest(string value, int maxLength)
```

Truncates a string to the specified maximum length. If `value` is shorter than or equal to `maxLength`, it is returned unchanged; otherwise, the result is a substring from the start of `value` with length `maxLength`.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null`.
  - `maxLength` (`int`): The maximum allowed length. Must be non-negative.
- **Returns**: The truncated string, or `null` if `value` is `null`.
- **Throws**: `ArgumentOutOfRangeException` when `maxLength` is less than zero.

### MaskForTest

```csharp
public static string MaskForTest(string value, char maskChar = '*', int visibleChars = 4)
```

Masks a string by replacing all but the last `visibleChars` characters with `maskChar`. If the string length is less than or equal to `visibleChars`, it is returned unchanged.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null`.
  - `maskChar` (`char`): The character used for masking. Defaults to `'*'`.
  - `visibleChars` (`int`): The number of trailing characters left unmasked. Must be non-negative.
- **Returns**: The masked string, or `null` if `value` is `null`.
- **Throws**: `ArgumentOutOfRangeException` when `visibleChars` is less than zero.

### ToKebabCaseForTest

```csharp
public static string ToKebabCaseForTest(string value)
```

Converts a string to kebab-case (lowercase words separated by hyphens). Whitespace, underscores, and camel/Pascal casing boundaries are treated as word separators. Consecutive non-alphanumeric characters collapse into a single hyphen.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null` or empty.
- **Returns**: The kebab-case representation, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### ToSnakeCaseForTest

```csharp
public static string ToSnakeCaseForTest(string value)
```

Converts a string to snake_case (lowercase words separated by underscores). Word boundary detection follows the same rules as `ToKebabCaseForTest`.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null` or empty.
- **Returns**: The snake_case representation, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### ToPascalCaseForTest

```csharp
public static string ToPascalCaseForTest(string value)
```

Converts a string to PascalCase (words capitalised and concatenated without separators). Whitespace, hyphens, and underscores are treated as word boundaries.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null` or empty.
- **Returns**: The PascalCase representation, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### ToCamelCaseForTest

```csharp
public static string ToCamelCaseForTest(string value)
```

Converts a string to camelCase (first word lowercased, subsequent words capitalised, no separators). Word boundary detection matches `ToPascalCaseForTest`.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null` or empty.
- **Returns**: The camelCase representation, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### RemoveWhitespaceForTest

```csharp
public static string RemoveWhitespaceForTest(string value)
```

Removes all whitespace characters from a string, including spaces, tabs, newlines, and other Unicode whitespace.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null`.
- **Returns**: The string with all whitespace removed, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### IsValidEmailForTest

```csharp
public static bool IsValidEmailForTest(string value)
```

Validates whether a string conforms to a basic email address format. The check typically verifies the presence of a single `@` symbol with non-empty local and domain parts, but does not guarantee full RFC 5322 compliance.

- **Parameters**:
  - `value` (`string`): The string to validate. Can be `null`.
- **Returns**: `true` if the string appears to be a valid email address; otherwise `false`. Returns `false` for `null`.
- **Throws**: No exceptions documented.

### IsValidGuidForTest

```csharp
public static bool IsValidGuidForTest(string value)
```

Determines whether a string represents a valid GUID. The check accepts common GUID formats (32 digits, hyphens, braces, parentheses) and is case-insensitive.

- **Parameters**:
  - `value` (`string`): The string to validate. Can be `null`.
- **Returns**: `true` if the string can be parsed as a `Guid`; otherwise `false`. Returns `false` for `null`.
- **Throws**: No exceptions documented.

### RepeatForTest

```csharp
public static string RepeatForTest(string value, int count)
```

Creates a new string by repeating the input string a specified number of times.

- **Parameters**:
  - `value` (`string`): The string to repeat. Can be `null`.
  - `count` (`int`): The number of repetitions. Must be non-negative.
- **Returns**: The concatenated result, or `null` if `value` is `null`. Returns `string.Empty` if `count` is zero.
- **Throws**: `ArgumentOutOfRangeException` when `count` is less than zero.

### SplitLinesForTest

```csharp
public static string[] SplitLinesForTest(string value)
```

Splits a multiline string into an array of lines. Line breaks are detected using `\r\n`, `\n`, or `\r`. Empty trailing lines may be preserved or omitted depending on the internal implementation.

- **Parameters**:
  - `value` (`string`): The input string. Can be `null`.
- **Returns**: An array of line strings, or `null` if `value` is `null`.
- **Throws**: No exceptions documented.

### JoinReadableForTest

```csharp
public static string JoinReadableForTest(IEnumerable<string> values, string separator = ", ", string lastSeparator = " and ")
```

Joins a sequence of strings into a human-readable list using a standard separator and a distinct final separator. For two elements, only the `lastSeparator` is used. For one element, the element is returned unchanged.

- **Parameters**:
  - `values` (`IEnumerable<string>`): The strings to join. Can be `null` or empty.
  - `separator` (`string`): The separator between all but the last two elements. Defaults to `", "`.
  - `lastSeparator` (`string`): The separator between the last two elements. Defaults to `" and "`.
- **Returns**: The joined string, or `string.Empty` if `values` is `null` or empty.
- **Throws**: No exceptions documented.

## Usage

### Example 1: Validating and formatting user input in a test

```csharp
[Fact]
public void ProcessUserInput_ShouldFormatCorrectly()
{
    // Arrange
    var rawEmail = "  User@Example.COM  ";
    var rawName  = "john_doe_42";

    // Act
    var cleanedEmail = StringUtilityTestsExtensions.RemoveWhitespaceForTest(rawEmail);
    var isValid      = StringUtilityTestsExtensions.IsValidEmailForTest(cleanedEmail);
    var displayName  = StringUtilityTestsExtensions.ToPascalCaseForTest(rawName);

    // Assert
    Assert.True(isValid);
    Assert.Equal("User@Example.COM", cleanedEmail);
    Assert.Equal("JohnDoe42", displayName);
}
```

### Example 2: Generating a readable summary from a list of items

```csharp
[Fact]
public void BuildSummary_ShouldJoinReadably()
{
    // Arrange
    var files = new[] { "backup.db", "archive.db", "snapshot.db" };

    // Act
    var summary = StringUtilityTestsExtensions.JoinReadableForTest(files, "; ", " and finally ");

    // Assert
    Assert.Equal("backup.db; archive.db and finally snapshot.db", summary);
}
```

## Notes

- **Null handling**: All methods that accept a `string` parameter return `null` when that parameter is `null`, except `IsValidEmailForTest` and `IsValidGuidForTest`, which return `false`. `JoinReadableForTest` returns `string.Empty` for a `null` collection.
- **Argument validation**: Only `TruncateForTest`, `MaskForTest`, and `RepeatForTest` throw `ArgumentOutOfRangeException` when their numeric arguments are negative. Other methods accept any input without throwing.
- **Case conversion edge cases**: Methods such as `ToKebabCaseForTest` and `ToSnakeCaseForTest` collapse consecutive non-alphanumeric characters into a single separator. Strings consisting entirely of separators or symbols may produce empty results or a single separator character.
- **Email validation scope**: `IsValidEmailForTest` performs a simplified check. It may accept addresses that are technically invalid per RFC 5322 (e.g., lacking a TLD) and reject exotic but valid forms (e.g., quoted local parts). It is suitable for basic input screening, not for production email verification.
- **GUID validation**: `IsValidGuidForTest` relies on `Guid.TryParse` and therefore accepts multiple formats. Strings with surrounding whitespace are typically rejected unless the internal implementation trims them first.
- **Thread safety**: All methods are static and operate on immutable string inputs without shared state. They are safe to call concurrently from multiple threads.
