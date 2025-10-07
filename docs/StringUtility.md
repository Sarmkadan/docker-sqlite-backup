# StringUtility

`StringUtility` is a static helper class that provides common string manipulation and validation routines used throughout the `docker-sqlite-backup` project. The methods are pure functions; they do not retain state and are safe to call from multiple threads concurrently.

## API

### FormatBytes
**Purpose:** Converts a numeric byte count into a human‑readable representation using appropriate size suffixes (B, KB, MB, GB, TB, PB).  
**Parameters:**  
- `long bytes` – The number of bytes to format. Must be non‑negative.  
**Return value:** A string such as `"1.23 KB"` or `"0 B"`.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `bytes` is less than zero.

### ToKebabCase
**Purpose:** Transforms the input string into kebab‑case (lowercase words separated by hyphens).  
**Parameters:**  
- `string input` – The string to convert. May be null or empty.  
**Return value:** The kebab‑cased string, or an empty string if `input` is null or empty.  
**Exceptions:** None.

### ToSnakeCase
**Purpose:** Transforms the input string into snake_case (lowercase words separated by underscores).  
**Parameters:**  
- `string input` – The string to convert. May be null or empty.  
**Return value:** The snake_case string, or an empty string if `input` is null or empty.  
**Exceptions:** None.

### ToPascalCase
**Purpose:** Transforms the input string into PascalCase (first letter of each word capitalized, no separators).  
**Parameters:**  
- `string input` – The string to convert. May be null or empty.  
**Return value:** The PascalCased string, or an empty string if `input` is null or empty.  
**Exceptions:** None.

### ToCamelCase
**Purpose:** Transforms the input string into camelCase (first word lowercase, subsequent words capitalized, no separators).  
**Parameters:**  
- `string input` – The string to convert. May be null or empty.  
**Return value:** The camelCased string, or an empty string if `input` is null or empty.  
**Exceptions:** None.

### Truncate
**Purpose:** Shortens a string to a specified maximum length, appending an ellipsis (`…`) when truncation occurs.  
**Parameters:**  
- `string input` – The string to truncate. May be null.  
- `int maxLength` – The maximum length of the returned string, including the ellipsis. Must be greater than zero.  
**Return value:** The original string if its length ≤ `maxLength`; otherwise, the first `maxLength - 1` characters followed by `…`. If `input` is null, returns null.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `maxLength` is less than or equal to zero.

### MaskSensitive
**Purpose:** Returns a copy of the string where all characters are replaced with a masking character (default `*`), optionally preserving a configurable number of leading and/or trailing characters.  
**Parameters:**  
- `string input` – The string to mask. May be null or empty.  
- `int? showFirst` (optional) – Number of leading characters to leave unmasked; null means none.  
- `int? showLast` (optional) – Number of trailing characters to leave unmasked; null means none.  
- `char mask` (optional) – The masking character; default is `*`.  
**Return value:** The masked string, or an empty string if `input` is empty, or null if `input` is null.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `showFirst` or `showLast` is negative or exceeds the length of `input`.

### IsValidEmail
**Purpose:** Checks whether the supplied string conforms to a basic email address pattern.  
**Parameters:**  
- `string email` – The email address to validate. May be null or empty.  
**Return value:** `true` if `email` matches the pattern; otherwise `false`.  
**Exceptions:** None.

### IsValidGuid
**Purpose:** Determines whether the supplied string is a valid GUID representation.  
**Parameters:**  
- `string guid` – The GUID string to validate. May be null or empty.  
**Return value:** `true` if `guid` can be parsed by `Guid.TryParse`; otherwise `false`.  
**Exceptions:** None.

### SplitLines
**Purpose:** Splits the input string into an array of substrings separated by line breaks (`\r\n`, `\n`, or `\r`).  
**Parameters:**  
- `string input` – The string to split. May be null or empty.  
**Return value:** An array of lines; empty array if `input` is null or empty.  
**Exceptions:** None.

### JoinReadable
**Purpose:** Concatenates an enumeration of strings into a single human‑readable list, using a comma separator and “and” before the final item (Oxford comma style).  
**Parameters:**  
- `IEnumerable<string> items` – The strings to join. May be null or empty.  
- `string separator` (optional) – The separator between items; default is `", "`.  
- `string finalSeparator` (optional) – The separator before the last item; default is `" and "`.  
**Return value:** A single string representing the list; returns an empty string if `items` is null or contains no elements.  
**Exceptions:**  
- `ArgumentNullException` if `items` is null.

### RemoveWhitespace
**Purpose:** Returns a copy of the string with all Unicode whitespace characters removed.  
**Parameters:**  
- `string input` – The string to process. May be null or empty.  
**Return value:** The string without any whitespace, or an empty string if `input` is empty, or null if `input` is null.  
**Exceptions:** None.

### QuoteIfNeeded
**Purpose:** Wraps the string in double quotes if it contains whitespace, a double quote, or a delimiter character; otherwise returns the string unchanged.  
**Parameters:**  
- `string input` – The string to evaluate. May be null or empty.  
- `char[] delimiters` (optional) – Characters that also trigger quoting; default includes comma and semicolon.  
**Return value:** The quoted or unquoted string, or an empty string if `input` is empty, or null if `input` is null.  
**Exceptions:** None.

### Repeat
**Purpose:** Produces a new string consisting of the input string repeated a specified number of times.  
**Parameters:**  
- `string input` – The string to repeat. May be null or empty.  
- `int count` – Number of repetitions; must be zero or greater.  
**Return value:** The repeated string, or an empty string if `input` is empty or `count` is zero, or null if `input` is null.  
**Exceptions:**  
- `ArgumentOutOfRangeException` if `count` is negative.

## Usage

```csharp
using static docker_sqlite_backup.StringUtility;

// Format a file size for logging
long size = 3_456_789;
string readable = FormatBytes(size);
// readable == "3.30 MB"

// Build a safe CLI argument from user input
string user = GetUserName(); // could contain spaces
string arg = QuoteIfNeeded(user);
// arg == "\"John Doe\"" if user contained a space
```

```csharp
using static docker_sqlite_backup.StringUtility;

// Validate and mask an email address before logging
string email = userInput.Email;
if (IsValidEmail(email))
{
    string masked = MaskSensitive(email, showFirst: 2, showLast: 2);
    // masked == "ja*****e@example.com"
}
else
{
    Log.Warning("Invalid email supplied: {Email}", email);
}
```

## Notes

- All members are **static** and **pure**; they depend solely on their input parameters and have no side effects. Consequently, they are **thread‑safe** and can be invoked concurrently without external synchronization.
- Methods that accept a string argument treat a `null` input as a valid value and typically return `null` (or an empty string where documented) rather than throwing, unless otherwise noted.
- Length‑based methods (`Truncate`, `MaskSensitive`, `Repeat`) validate non‑negative parameters and will throw `ArgumentOutOfRangeException` for invalid values.
- The validation helpers (`IsValidEmail`, `IsValidGuid`) use relatively permissive patterns; they are intended for quick sanity checks, not rigorous RFC‑compliant validation.
- `JoinReadable` expects a non‑null enumerable; passing `null` results in an `ArgumentNullException`. An empty enumeration yields an empty string.
- Culture‑specific behavior is not applied; operations such as case conversion rely on the invariant culture semantics of the underlying .NET string methods. If culture‑sensitive results are required, callers should preprocess the input accordingly.
