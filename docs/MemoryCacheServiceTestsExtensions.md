# MemoryCacheServiceTestsExtensions

Extension methods for testing implementations of `IMemoryCacheService` that provide assertions for common cache operations. These helpers simplify test code by encapsulating expected behaviors for cache interactions, including synchronous and asynchronous set operations, existence checks, and default value handling.

## API

### `Get_ShouldReturnDefaultForNullValue<T>`

Ensures that retrieving a non-existent or null key from the cache returns the default value for type `T`.

- **Parameters**
  - `IMemoryCacheService cacheService`: The cache service instance under test.
  - `string key`: The cache key to retrieve.
- **Throws**
  - `ArgumentNullException`: If `cacheService` or `key` is `null`.
  - `AssertionException`: If the retrieved value is not equal to `default(T)`.

### `Set_ShouldStoreValue<T>`

Verifies that a synchronous `Set` operation correctly stores a value in the cache under the specified key.

- **Parameters**
  - `IMemoryCacheService cacheService`: The cache service instance under test.
  - `string key`: The cache key to store the value under.
  - `T value`: The value to store.
- **Throws**
  - `ArgumentNullException`: If `cacheService` or `key` is `null`.
  - `AssertionException`: If the value retrieved from the cache does not match the stored `value`.

### `SetAsync_ShouldStoreValueAsync<T>`

Ensures that an asynchronous `SetAsync` operation correctly stores a value in the cache under the specified key.

- **Parameters**
  - `IMemoryCacheService cacheService`: The cache service instance under test.
  - `string key`: The cache key to store the value under.
  - `T value`: The value to store.
- **Returns**
  - `Task`: A task representing the asynchronous operation.
- **Throws**
  - `ArgumentNullException`: If `cacheService` or `key` is `null`.
  - `AssertionException`: If the value retrieved from the cache does not match the stored `value`.

### `Exists_ShouldReturnCorrectState`

Checks that the `Exists` method accurately reflects whether a key is present in the cache.

- **Parameters**
  - `IMemoryCacheService cacheService`: The cache service instance under test.
  - `string key`: The cache key to check.
  - `bool expectedExists`: The expected result of the existence check.
- **Throws**
  - `ArgumentNullException`: If `cacheService` or `key` is `null`.
  - `AssertionException`: If the result of `Exists` does not match `expectedExists`.

## Usage
