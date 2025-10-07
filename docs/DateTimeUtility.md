# DateTimeUtility

A static utility class providing consistent, timezone-aware date and time operations for the backup system. It centralizes ISO 8601 formatting, parsing, human-readable display, relative time calculation, duration formatting, and boundary rounding to eliminate duplicated date logic across the codebase.

## API

### ToIso8601

```csharp
public static string ToIso8601(DateTime dateTime)
```

Converts a `DateTime` value to its ISO 8601 string representation in UTC (the `O` format specifier, e.g. `2025-03-15T14:30:00.0000000Z`).

**Parameters:**
- `dateTime` — The `DateTime` to format. If `DateTimeKind.Local` or `DateTimeKind.Unspecified`, it is first converted to UTC.

**Returns:** A culture-invariant ISO 8601 string.

**Throws:** Does not throw under normal input; standard `DateTime` overflow behavior applies for extreme values.

---

### TryParseIso8601

```csharp
public static bool TryParseIso8601(string value, out DateTime result)
```

Attempts to parse a string as an ISO 8601 UTC timestamp using the `O` format and the `RoundtripKind` style.

**Parameters:**
- `value` — The string to parse.
- `result` — On success, receives the parsed `DateTime` with `DateTimeKind.Utc`; on failure, `DateTime.MinValue`.

**Returns:** `true` if parsing succeeded; otherwise `false`.

**Throws:** Never throws — all parsing failures are captured in the return value.

---

### FormatForDisplay

```csharp
public static string FormatForDisplay(DateTime dateTime)
```

Formats a `DateTime` for user-facing display using the local time zone and the current culture's long date and short time pattern (e.g. "Saturday, March 15, 2025 2:30 PM").

**Parameters:**
- `dateTime` — The `DateTime` to display. Converted to local time if not already.

**Returns:** A localized, human-readable string.

**Throws:** Does not throw under normal input.

---

### GetRelativeTime

```csharp
public static string GetRelativeTime(DateTime dateTime)
```

Returns a human-readable relative time string comparing the given UTC `DateTime` to the current UTC time (e.g. "2 hours ago", "in 3 days").

**Parameters:**
- `dateTime` — The reference `DateTime`, treated as UTC.

**Returns:** A string such as "just now", "5 minutes ago", "yesterday", "3 days ago", or "in 1 hour".

**Throws:** Does not throw.

---

### FormatDuration

```csharp
public static string FormatDuration(TimeSpan duration)
```

Formats a `TimeSpan` into a compact human-readable duration string (e.g. "1h 23m 45s"). Zero components are omitted unless the entire duration is zero, in which case "0s" is returned.

**Parameters:**
- `duration` — The `TimeSpan` to format. Negative durations are supported and produce a leading minus sign.

**Returns:** A string like `"-2h 15m 5s"` or `"45s"`.

**Throws:** Does not throw.

---

### GetDayStart

```csharp
public static DateTime GetDayStart(DateTime dateTime)
```

Returns the start of the calendar day (midnight, 00:00:00.0000000) for the given `DateTime`, preserving its `DateTimeKind`.

**Parameters:**
- `dateTime` — The reference `DateTime`.

**Returns:** A `DateTime` at the start of the same day.

**Throws:** Does not throw.

---

### GetDayEnd

```csharp
public static DateTime GetDayEnd(DateTime dateTime)
```

Returns the end of the calendar day (23:59:59.9999999) for the given `DateTime`, preserving its `DateTimeKind`.

**Parameters:**
- `dateTime` — The reference `DateTime`.

**Returns:** A `DateTime` at the last representable tick of the same day.

**Throws:** Does not throw.

---

### GetMonthStart

```csharp
public static DateTime GetMonthStart(DateTime dateTime)
```

Returns the start of the calendar month (first day at midnight) for the given `DateTime`, preserving its `DateTimeKind`.

**Parameters:**
- `dateTime` — The reference `DateTime`.

**Returns:** A `DateTime` at midnight on the first day of the month.

**Throws:** Does not throw.

---

### GetMonthEnd

```csharp
public static DateTime GetMonthEnd(DateTime dateTime)
```

Returns the end of the calendar month (last day at 23:59:59.9999999) for the given `DateTime`, preserving its `DateTimeKind`.

**Parameters:**
- `dateTime` — The reference `DateTime`.

**Returns:** A `DateTime` at the last tick of the final day of the month.

**Throws:** Does not throw.

---

### GetTimeUntil

```csharp
public static TimeSpan GetTimeUntil(DateTime target)
```

Computes the remaining time from the current UTC moment to the given `target` UTC `DateTime`.

**Parameters:**
- `target` — The future or past `DateTime`, treated as UTC.

**Returns:** A positive `TimeSpan` if `target` is in the future, negative if in the past, zero if exactly now.

**Throws:** Does not throw.

---

### RoundDown

```csharp
public static DateTime RoundDown(DateTime dateTime, TimeSpan interval)
```

Rounds a `DateTime` down to the nearest preceding interval boundary. The rounding is performed against the Unix epoch origin (1970-01-01T00:00:00Z) to ensure consistent alignment regardless of time zone.

**Parameters:**
- `dateTime` — The `DateTime` to round down. Treated as UTC for the calculation; the result preserves the original `DateTimeKind`.
- `interval` — The rounding granularity (e.g., 15 minutes, 1 hour). Must be positive.

**Returns:** A `DateTime` at or before the input value, aligned to the interval.

**Throws:** `ArgumentOutOfRangeException` if `interval` is zero or negative.

---

### RoundUp

```csharp
public static DateTime RoundUp(DateTime dateTime, TimeSpan interval)
```

Rounds a `DateTime` up to the nearest following interval boundary. Uses the same epoch-based alignment as `RoundDown`. If the input is already exactly on a boundary, it is returned unchanged.

**Parameters:**
- `dateTime` — The `DateTime` to round up. Treated as UTC for the calculation; the result preserves the original `DateTimeKind`.
- `interval` — The rounding granularity. Must be positive.

**Returns:** A `DateTime` at or after the input value, aligned to the interval.

**Throws:** `ArgumentOutOfRangeException` if `interval` is zero or negative.

---

## Usage

### Example 1: Formatting a backup timestamp for a report

```csharp
DateTime backupTime = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(45));
string iso = DateTimeUtility.ToIso8601(backupTime);
string display = DateTimeUtility.FormatForDisplay(backupTime);
string relative = DateTimeUtility.GetRelativeTime(backupTime);

Console.WriteLine($"Backup occurred at {iso}");
Console.WriteLine($"Local time: {display}");
Console.WriteLine($"Relative: {relative}");
// Output:
// Backup occurred at 2025-03-15T13:45:00.0000000Z
// Local time: Saturday, March 15, 2025 9:45 AM
// Relative: 45 minutes ago
```

### Example 2: Scheduling retention cleanup with rounded boundaries

```csharp
DateTime now = DateTime.UtcNow;
DateTime monthStart = DateTimeUtility.GetMonthStart(now);
DateTime monthEnd = DateTimeUtility.GetMonthEnd(now);
DateTime roundedStart = DateTimeUtility.RoundDown(monthStart, TimeSpan.FromHours(6));
DateTime roundedEnd = DateTimeUtility.RoundUp(monthEnd, TimeSpan.FromHours(6));

TimeSpan untilCleanup = DateTimeUtility.GetTimeUntil(roundedEnd);
string duration = DateTimeUtility.FormatDuration(untilCleanup);

Console.WriteLine($"Retention window: {DateTimeUtility.ToIso8601(roundedStart)} to {DateTimeUtility.ToIso8601(roundedEnd)}");
Console.WriteLine($"Next cleanup in: {duration}");
```

---

## Notes

- **Time zone handling:** Methods that produce strings (`ToIso8601`, `FormatForDisplay`, `GetRelativeTime`) internally convert to UTC or local time as appropriate. Boundary methods (`GetDayStart`, `GetDayEnd`, `GetMonthStart`, `GetMonthEnd`) preserve the input `DateTimeKind` without conversion, so callers should ensure the kind is set correctly before calling.
- **Rounding alignment:** `RoundDown` and `RoundUp` use the Unix epoch as the alignment origin. This guarantees consistent boundaries across all time zones and avoids drift when intervals do not evenly divide local offsets. The original `DateTimeKind` is preserved in the returned value.
- **Edge cases in `FormatDuration`:** A `TimeSpan` of zero yields `"0s"`. Negative durations produce a leading minus sign with absolute component values (e.g., `"-1h 5m"`). Components with zero magnitude are omitted except when all are zero.
- **Edge cases in `GetRelativeTime`:** Future times return "in ..." strings; times within a few seconds of now return "just now". The method uses UTC to avoid ambiguous comparisons around DST transitions.
- **Thread safety:** All members are static and operate on immutable input types (`DateTime`, `TimeSpan`, `string`). No shared mutable state is used. The class is safe to call concurrently from multiple threads without external synchronization.
