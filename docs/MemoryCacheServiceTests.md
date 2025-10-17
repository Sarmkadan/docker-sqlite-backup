# MemoryCacheServiceTests

`MemoryCacheServiceTests` is the unit test suite for the `MemoryCacheService` class in the `docker-sqlite-backup` project. It validates the correctness of the in-memory caching layer, covering synchronous and asynchronous operations, expiration semantics, complex type round-tripping, and edge cases such as missing keys and cache clearing. The class implements `IDisposable` to clean up test resources.

## API

### MemoryCacheServiceTests
Constructor. Initializes a new test instance, typically setting up a fresh `MemoryCacheService` and any required test data before each test method runs.

### void Dispose
Releases all resources held by the test fixture, including the underlying `MemoryCacheService` instance and any disposable test doubles. Called by the test framework after each test execution.

### void Get_NonExistentKey_ReturnsDefault
Verifies that calling `Get<T>` with a key that has never been stored returns the default value for the type (e.g., `null` for reference types, `0` for integers). Does not throw.

### void Set_ThenGet_ReturnsStoredValue
Confirms that after storing a value via `Set`, a subsequent `Get` with the same key returns the exact value that was stored. Covers the basic store/retrieve contract.

### void Set_ComplexType_RoundTripsCorrectly
Ensures that complex objects (e.g., custom classes with nested properties) can be stored and retrieved without data loss or corruption. Validates deep equality after round-tripping.

### void Set_WithExpiration_ValueExpires
Verifies that a value stored with an absolute or sliding expiration is no longer retrievable after the expiration period elapses. The test typically advances time or waits for the expiry.

### void Set_WithoutExpiration_ValuePersists
Confirms that a value stored without an expiration policy remains available indefinitely and is not evicted prematurely.

### void Remove_ExistingKey_RemovesEntry
Validates that calling `Remove` on a key that exists in the cache deletes the entry, and a subsequent `Get` returns the default value.

### void Remove_NonExistentKey_DoesNotThrow
Ensures that attempting to remove a key that is not present in the cache completes without throwing an exception.

### void Exists_ExistingKey_ReturnsTrue
Verifies that `Exists` returns `true` when the specified key is currently present and not expired.

### void Exists_NonExistentKey_ReturnsFalse
Verifies that `Exists` returns `false` when the key has never been added.

### void Exists_ExpiredKey_ReturnsFalse
Confirms that `Exists` returns `false` for a key that was added with an expiration that has since passed.

### void Clear_RemovesAllEntries
Validates that after calling `Clear`, all previously stored entries are removed and `Exists` returns `false` for every key.

### void GetOrSet_CacheMiss_CallsFactory
Ensures that when `GetOrSet` is called for a key not in the cache, the provided factory delegate is invoked, its result is stored, and the result is returned.

### void GetOrSet_CacheHit_DoesNotCallFactory
Confirms that when the key already exists, `GetOrSet` returns the cached value without invoking the factory delegate.

### async Task GetOrSetAsync_CacheMiss_CallsAsyncFactory
Asynchronous equivalent of the cache-miss test. Verifies that the async factory is awaited and its result is stored and returned.

### async Task GetOrSetAsync_CacheHit_DoesNotCallFactory
Asynchronous equivalent of the cache-hit test. Ensures the async factory is not invoked when the key is already present.

### async Task SetAsync_ThenGetAsync_ReturnsStoredValue
Validates the asynchronous store/retrieve path: a value set via `SetAsync` is correctly returned by a subsequent `GetAsync`.

### async Task RemoveAsync_ExistingKey_RemovesEntry
Confirms that `RemoveAsync` deletes an existing entry and that a following `GetAsync` returns the default value.

### void Set_OverwritesExistingEntry
Verifies that calling `Set` with a key that already exists replaces the old value with the new one, and a subsequent `Get` returns the updated value.

## Usage

### Example 1: Running a subset of tests with a test framework
```csharp
using Xunit;

public class CacheValidationTests
{
    private readonly MemoryCacheServiceTests _tests;

    public CacheValidationTests()
    {
        _tests = new MemoryCacheServiceTests();
    }

    [Fact]
    public void BasicStoreAndRetrieve_ShouldPass()
    {
        _tests.Set_ThenGet_ReturnsStoredValue();
    }

    [Fact]
    public void RemoveMissingKey_ShouldNotThrow()
    {
        _tests.Remove_NonExistentKey_DoesNotThrow();
    }

    public void Cleanup()
    {
        _tests.Dispose();
    }
}
```

### Example 2: Integrating expiration tests into a CI pipeline
```csharp
using System.Threading.Tasks;
using Xunit;

public class CacheExpirationTestSuite
{
    [Fact]
    public void Expiration_Scenarios_AllPass()
    {
        var tests = new MemoryCacheServiceTests();

        try
        {
            tests.Set_WithExpiration_ValueExpires();
            tests.Set_WithoutExpiration_ValuePersists();
            tests.Exists_ExpiredKey_ReturnsFalse();
        }
        finally
        {
            tests.Dispose();
        }
    }

    [Fact]
    public async Task AsyncOperations_ShouldBeConsistent()
    {
        var tests = new MemoryCacheServiceTests();

        try
        {
            await tests.SetAsync_ThenGetAsync_ReturnsStoredValue();
            await tests.GetOrSetAsync_CacheMiss_CallsAsyncFactory();
            await tests.GetOrSetAsync_CacheHit_DoesNotCallFactory();
        }
        finally
        {
            tests.Dispose();
        }
    }
}
```

## Notes

- **Test isolation:** Each test method assumes a clean cache state. The constructor and `Dispose` method are expected to be called before and after each test, respectively, to prevent state leakage between tests.
- **Time manipulation:** Tests involving expiration (`Set_WithExpiration_ValueExpires`, `Exists_ExpiredKey_ReturnsFalse`) may rely on simulated time or real delays. In CI environments with high clock skew, real-time waits can cause flakiness; prefer time abstraction if available.
- **Thread safety:** The test suite does not explicitly validate concurrent access patterns. All tests are single-threaded by design. Thread-safety guarantees of the underlying `MemoryCacheService` must be verified separately if required.
- **Complex type equality:** `Set_ComplexType_RoundTripsCorrectly` depends on correct equality semantics for the stored type. Reference-type mutations after storage are not covered and may lead to unexpected behavior in production.
- **Async continuations:** The async tests assume a synchronization context that allows safe awaiting (e.g., `Task`-returning test methods). Deadlocks may occur if the underlying cache implementation uses blocking calls on async paths.
- **Overwrite behavior:** `Set_OverwritesExistingEntry` confirms replacement but does not test expiration reset behavior. Whether overwriting resets or preserves the original expiration policy is implementation-specific and should be verified separately if relied upon.
