# DateTimeUtilityTests

The `DateTimeUtilityTests` class contains unit tests for validating the functionality of date and time utility methods within the `docker-sqlite-backup` project. These tests ensure correct parsing, formatting, relative time calculation, and boundary value handling for various date and time operations, including ISO 8601 string conversion, display formatting, duration representation, and temporal rounding.

## API

### `ToIso8601_UtcDateTime_ReturnsRoundTripFormat`
**Purpose**: Verifies that a UTC `DateTime` object is correctly serialized to an ISO 8601 string format that can be parsed back without data loss.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the round-trip conversion fails.

### `TryParseIso8601_ValidIso8601String_ReturnsTrueAndParsedDate`
**Purpose**: Tests the parsing of a valid ISO 8601 formatted string into a `DateTime` object, ensuring the method returns `true` and the correct parsed value.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if parsing fails or the output does not match expectations.

### `TryParseIso8601_InvalidString_ReturnsFalse`
**Purpose**: Ensures that an invalid or malformed string fails to parse, returning `false` without throwing an exception.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the method returns `true` or throws an exception.

### `FormatForDisplay_DefaultFormat_ReturnsExpectedPattern`
**Purpose**: Validates that a `DateTime` object is formatted into a default human-readable string pattern (e.g., for UI display).
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected pattern.

### `FormatForDisplay_CustomFormat_ReturnsFormattedString`
**Purpose**: Tests custom date/time formatting using a specified format string, ensuring the output adheres to the provided pattern.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected custom format.

### `GetRelativeTime_VariousIntervals_ReturnsCorrectSuffix`
**Purpose**: Checks the generation of relative time descriptions (e.g., "5 minutes ago") for various time intervals, ensuring correct suffixes are applied.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the relative time string does not match the expected output.

### `GetRelativeTime_OneMonthAgo_ReturnsMoAgoSuffix`
**Purpose**: Specifically verifies the "month ago" suffix for a time interval of approximately one month.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the suffix is incorrect.

### `GetRelativeTime_OneYearAgo_ReturnsYAgoSuffix`
**Purpose**: Specifically verifies the "year ago" suffix for a time interval of approximately one year.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the suffix is incorrect.

### `FormatDuration_LessThanOneMinute_ReturnsSFormat`
**Purpose**: Tests duration formatting for intervals under one minute, ensuring the output uses seconds (e.g., "30s").
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected seconds format.

### `FormatDuration_BetweenOneAndSixtyMinutes_ReturnsMSFormat`
**Purpose**: Validates duration formatting for intervals between one and sixty minutes, ensuring the output combines minutes and seconds (e.g., "5m 30s").
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected pattern.

### `FormatDuration_BetweenOneAndTwentyFourHours_ReturnsHMFormat`
**Purpose**: Tests duration formatting for intervals between one and twenty-four hours, ensuring the output combines hours and minutes (e.g., "2h 30m").
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected pattern.

### `FormatDuration_MoreThanOneDay_ReturnsDHFormat`
**Purpose**: Validates duration formatting for intervals exceeding one day, ensuring the output combines days and hours (e.g., "2d 3h").
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the formatted string does not match the expected pattern.

### `GetDayStart_SpecificDate_ReturnsDateWithZeroTime`
**Purpose**: Ensures that retrieving the start of a day for a given `DateTime` returns the same date with time components set to midnight (00:00:00).
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the returned `DateTime` does not match the expected start of day.

### `GetDayEnd_SpecificDate_ReturnsLastTickOfDay`
**Purpose**: Tests retrieval of the end of a day, ensuring the returned `DateTime` represents the last possible tick before midnight (23:59:59.999...).
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the returned `DateTime` does not match the expected end of day.

### `GetMonthStart_SpecificDate_ReturnsFirstDayOfMonth`
**Purpose**: Verifies that retrieving the start of a month for a given `DateTime` returns the first day of the month at midnight.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the returned `DateTime` does not match the expected start of month.

### `GetMonthEnd_SpecificDate_ReturnsLastMomentOfMonth`
**Purpose**: Ensures that retrieving the end of a month returns the last tick of the last day of the month.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the returned `DateTime` does not match the expected end of month.

### `RoundDown_ToHourInterval_ReturnsFlooredHour`
**Purpose**: Tests rounding a `DateTime` down to the nearest hour, ensuring the minutes and seconds are truncated.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the rounded `DateTime` does not match the expected floored value.

### `RoundUp_ToHourInterval_ReturnsCeiledHour`
**Purpose**: Validates rounding a `DateTime` up to the nearest hour, ensuring the minutes and seconds are incremented to the next hour if necessary.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the rounded `DateTime` does not match the expected ceiled value.

### `RoundUp_AlreadyAligned_ReturnsUnchanged`
**Purpose**: Ensures that rounding a `DateTime` already aligned to the target interval (e.g., exact hour) returns the same value unchanged.
**Parameters**: None.
**Return Value**: None.
**Throws**: Assertion failure if the returned `DateTime` differs from the input.

## Usage

### Example 1: Parsing and Formatting ISO 8601 Dates
