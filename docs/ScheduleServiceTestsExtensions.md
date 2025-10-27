# ScheduleServiceTestsExtensions

A static test utility class providing factory methods and assertion helpers for `BackupSchedule` objects. Designed to simplify unit testing of schedule-related functionality in the `docker-sqlite-backup` project.

## API

### `public static BackupSchedule CreateValidSchedule()`

Creates a valid `BackupSchedule` instance with default, well-formed values suitable for positive test cases.

- **Parameters**: None
- **Return value**: A new `BackupSchedule` instance with valid properties (e.g., valid cron expression, active status, non-empty name and database path).
- **Exceptions**: None

---

### `public static BackupSchedule CreateInvalidSchedule()`

Creates an invalid `BackupSchedule` instance with intentionally incorrect or missing values for negative test cases.

- **Parameters**: None
- **Return value**: A new `BackupSchedule` instance with invalid properties (e.g., empty name, invalid cron expression, inactive status).
- **Exceptions**: None

---
### `public static BackupSchedule CreateScheduleWithInvalidCron()`

Creates a `BackupSchedule` instance with a syntactically invalid cron expression.

- **Parameters**: None
- **Return value**: A new `BackupSchedule` instance with a malformed cron expression (e.g., `"invalid-cron"`).
- **Exceptions**: None

---
### `public static void AssertScheduleIsActive(BackupSchedule schedule)`

Asserts that the given schedule is marked as active.

- **Parameters**:
  - `schedule`: The `BackupSchedule` instance to validate.
- **Return value**: None
- **Exceptions**: Throws an assertion exception if `schedule` is `null` or if `schedule.IsActive` is `false`.

---
### `public static void AssertScheduleHasId(BackupSchedule schedule, string expectedId)`

Asserts that the given schedule has the expected identifier.

- **Parameters**:
  - `schedule`: The `BackupSchedule` instance to validate.
  - `expectedId`: The expected value of `schedule.Id`.
- **Return value**: None
- **Exceptions**: Throws an assertion exception if `schedule` is `null` or if `schedule.Id` does not match `expectedId`.

---
### `public static void AssertScheduleHasName(BackupSchedule schedule, string expectedName)`

Asserts that the given schedule has the expected name.

- **Parameters**:
  - `schedule`: The `BackupSchedule` instance to validate.
  - `expectedName`: The expected value of `schedule.Name`.
- **Return value**: None
- **Exceptions**: Throws an assertion exception if `schedule` is `null` or if `schedule.Name` does not match `expectedName`.

---
### `public static void AssertScheduleHasDatabasePath(BackupSchedule schedule, string expectedDatabasePath)`

Asserts that the given schedule has the expected database file path.

- **Parameters**:
  - `schedule`: The `BackupSchedule` instance to validate.
  - `expectedDatabasePath`: The expected value of `schedule.DatabasePath`.
- **Return value**: None
- **Exceptions**: Throws an assertion exception if `schedule` is `null` or if `schedule.DatabasePath` does not match `expectedDatabasePath`.

## Usage
