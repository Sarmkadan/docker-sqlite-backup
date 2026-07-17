// ... existing content ...

## MemoryCacheServiceTestsExtensions
The `MemoryCacheServiceTestsExtensions` class provides extension methods for testing `MemoryCacheService` behavior. It offers a set of assertions to verify the service's functionality, including getting and setting values, checking existence, removing keys, clearing the cache, and executing factories.

Here's an example of how to use some of its public members:
```csharp
using Docker.Sqlite.Backup.Caching;

// Arrange
var sut = new MemoryCacheService();

// Act & Assert
sut.Get_ShouldReturnDefaultForNullValue<string>("key");
sut.Set_ShouldStoreValue<string>("key", "value");
await sut.SetAsync_ShouldStoreValueAsync<string>("key", "value");
sut.Exists_ShouldReturnCorrectState("key", true);
sut.Remove_ShouldNotThrowForNonExistentKey("key");
sut.Clear_ShouldRemoveAllEntries();
sut.GetOrSet_ShouldExecuteFactoryOnlyWhenMissing<string>("key", 1);
```

// ... rest of the content ...
