# ScheduleServiceTests

`ScheduleServiceTests` is the unit test class for the `ScheduleService` component in the `docker-sqlite-backup` project. It validates the correctness of cron expression parsing, schedule creation and retrieval, and lifecycle management operations. The tests ensure that valid inputs produce expected outcomes, invalid inputs are rejected with appropriate exceptions, and repository interactions are correctly delegated.

## API

### public ScheduleServiceTests

The parameterless constructor for the test class. Initializes the test fixture, typically setting up mock dependencies for the `ScheduleService` instance under test. No parameters, no return value, does not throw.

### public void ValidateCronExpression_ValidExpression_ReturnsTrue

Tests that `ValidateCronExpression` returns `true` when supplied with a syntactically correct and semantically valid cron expression (e.g., `"0 0 * * *"`). No parameters. Does not throw; assertions verify the boolean result.

### public void ValidateCronExpression_InvalidExpression_ReturnsFalse

Tests that `ValidateCronExpression` returns `false` when supplied with a malformed or unsupported cron string (e.g., `"invalid"` or `"70 * * * *"`). No parameters. Does not throw; assertions verify the boolean result.

### public void GetNextExecutionTime_ValidSchedule_ReturnsDateInFuture

Tests that `GetNextExecutionTime` returns a `DateTime` value strictly greater than the current UTC time when given a valid cron expression. No parameters. Does not throw; assertions verify the returned value is in the future.

### public void GetNextExecutionTime_InvalidCronExpression_ReturnsNull

Tests that `GetNextExecutionTime` returns `null` when the provided cron expression is invalid or cannot be parsed. No parameters. Does not throw; assertions verify a null result.

### public async Task CreateScheduleAsync_ValidSchedule_DelegatesToRepository

Verifies that calling `CreateScheduleAsync` with a valid schedule object correctly delegates the persistence operation to the underlying repository. The test confirms the repository’s `AddAsync` or equivalent method is invoked exactly once with the expected schedule data. No parameters. Does not throw; assertions verify mock invocations.

### public async Task CreateScheduleAsync_InvalidSchedule_ThrowsInvalidScheduleException

Tests that `CreateScheduleAsync` throws an `InvalidScheduleException` when the provided schedule object fails validation rules (e.g., missing required fields, invalid backup target). No parameters. The test expects the exception to be thrown and verifies its type.

### public async Task CreateScheduleAsync_InvalidCronExpression_ThrowsInvalidCronExpressionException

Tests that `CreateScheduleAsync` throws an `InvalidCronExpressionException` when the schedule object contains a cron expression that is syntactically invalid or unsupported. No parameters. The test expects the exception to be thrown and verifies its type.

### public async Task GetScheduleAsync_ExistingId_ReturnsScheduleFromRepository

Tests that `GetScheduleAsync` returns the expected schedule object when queried with an identifier that exists in the repository. The test sets up the mock repository to return a known schedule and verifies the service passes it through correctly. No parameters. Does not throw; assertions verify the returned object matches the mock.

### public async Task GetScheduleAsync_NonExistingId_ReturnsNull

Tests that `GetScheduleAsync` returns `null` when queried with an identifier that does not correspond to any persisted schedule. No parameters. Does not throw; assertions verify a null result.

### public async Task DeactivateScheduleAsync_ExistingSchedule_UpdatesIsActiveFalse

Tests that `DeactivateScheduleAsync` retrieves an existing schedule by its identifier, sets its `IsActive` property to `false`, and persists the change through the repository. The test verifies both the state mutation and the repository save invocation. No parameters. Does not throw; assertions verify mock interactions and property state.

## Usage

```csharp
// Example 1: Testing schedule creation with a valid cron expression
[Fact]
public async Task CreateSchedule_WithDailyMidnightCron_Succeeds()
{
    var scheduleServiceTests = new ScheduleServiceTests();
    
    // The test internally arranges a valid schedule with cron "0 0 * * *"
    await scheduleServiceTests.CreateScheduleAsync_ValidSchedule_DelegatesToRepository();
    
    // If no exception is thrown, the test passes — confirming
    // the service accepts the schedule and delegates to the repository.
}
```

```csharp
// Example 2: Testing deactivation of an existing schedule
[Fact]
public async Task DeactivateSchedule_ExistingActiveSchedule_BecomesInactive()
{
    var scheduleServiceTests = new ScheduleServiceTests();
    
    // This test verifies that an active schedule is correctly deactivated
    await scheduleServiceTests.DeactivateScheduleAsync_ExistingSchedule_UpdatesIsActiveFalse();
    
    // Assertions inside the test confirm IsActive becomes false
    // and that the repository save method was called.
}
```

## Notes

- **Edge cases**: The cron validation tests should cover boundary values such as `"59 23 31 12 6"` (maximum valid fields) and expressions with step values (`"*/5 * * * *"`). The `GetNextExecutionTime_InvalidCronExpression_ReturnsNull` test must handle both completely unparseable strings and expressions that are structurally valid but semantically out of range.
- **Thread safety**: These tests are designed to run in isolation via xUnit’s test runner. Each test method operates on its own instance of the test class with independently configured mocks. No shared state exists between tests, so concurrent execution is safe. The underlying `ScheduleService` methods being tested should be thread-safe if they rely on thread-safe repository implementations, but the tests themselves do not validate concurrent access patterns.
- **Async behavior**: All `async Task` test methods should be awaited properly. Test frameworks that support asynchronous tests will handle the synchronization context correctly. Ensure mock setups for asynchronous repository methods use `ReturnsAsync` rather than blocking returns to avoid deadlocks.
- **Exception specificity**: Tests that verify exception throwing (`InvalidScheduleException`, `InvalidCronExpressionException`) should use `Assert.ThrowsAsync` or equivalent to catch the exact exception type. Derived exception types or generic `Exception` catches would weaken the test’s precision.
