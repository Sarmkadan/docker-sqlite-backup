# FileSystemUtilityJsonExtensions

`FileSystemUtilityJsonExtensions` provides JSON‑based serialization and deserialization helpers for configuring the file‑system utilities used throughout the **docker‑sqlite‑backup** project. The type centralises the conversion logic so that callers can persist and restore `FileSystemUtilityConfig` objects in a consistent, version‑tolerant format.

## API

### `public static string ToJson(FileSystemUtilityConfig config)`

Serialises a `FileSystemUtilityConfig` instance to a JSON string.

- **Parameters**
  - `config`: The configuration object to serialise. Must not be `null`.
- **Returns**  
  A JSON representation of the supplied configuration.
- **Exceptions**
  - `ArgumentNullException` – if `config` is `null`.
  - `JsonException` – if the object cannot be serialised (e.g., circular references).

### `public static FileSystemUtilityConfig? FromJson(string json)`

Deserialises a JSON string into a `FileSystemUtilityConfig` instance.

- **Parameters**
  - `json`: A JSON string that was produced by `ToJson`. Must not be `null` or empty.
- **Returns**  
  A new `FileSystemUtilityConfig` populated from the JSON, or `null` if the JSON represents a `null` value.
- **Exceptions**
  - `ArgumentException` – if `json` is `null`, empty, or whitespace.
  - `JsonException` – if the JSON is malformed or does not match the expected schema.

### `public static bool TryFromJson(string json, out FileSystemUtilityConfig? config)`

Attempts to deserialise a JSON string without throwing exceptions.

- **Parameters**
  - `json`: The JSON string to parse. May be `null` or empty.
  - `config`: When the method returns `true`, contains the deserialised configuration; otherwise `null`.
- **Returns**  
  `true` if deserialization succeeded; `false` otherwise.
- **Exceptions**  
  None. All error conditions are reported via the return value.

### `public int MaxRetries`

Gets or sets the maximum number of retry attempts that the file‑system utility will perform when an operation fails.

- **Value semantics**  
  Must be a non‑negative integer. A value of `0` disables retry logic.

### `public int RetryDelayMultiplier`

Gets or sets the multiplier (in milliseconds) applied to the base delay between retry attempts.

- **Value semantics**  
  Must be a positive integer. The actual delay for attempt *n* is `RetryDelayMultiplier * n`.

### `public bool Recursive`

Indicates whether directory searches performed by the utility should be recursive.

- **Value semantics**  
  `true` enables recursive traversal; `false` limits the search to the top‑level directory.

### `public string DefaultSearchPattern`

Gets or sets the default file‑search pattern (e.g., `"*.bak"`). This pattern is used when callers do not supply an explicit pattern.

- **Value semantics**  
  Must be a valid search pattern; an empty string defaults to `"*"`.

## Usage

### Example 1 – Persisting a configuration

