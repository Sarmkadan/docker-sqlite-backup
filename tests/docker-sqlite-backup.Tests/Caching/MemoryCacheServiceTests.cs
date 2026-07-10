// Author: Vladyslav Zaiets

using DockerSqliteBackup.Caching;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the MemoryCacheService class.
/// </summary>
public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCacheService _sut;

    /// <summary>
    /// Initializes a new instance of the MemoryCacheServiceTests class.
    /// </summary>
    public MemoryCacheServiceTests()
    {
        _sut = new MemoryCacheService(cleanupInterval: TimeSpan.FromHours(1));
    }

    /// <summary>
    /// Releases all resources used by the MemoryCacheServiceTests class.
    /// </summary>
    public void Dispose()
    {
        _sut.Clear();
    }

    /// <summary>
    /// Verifies that the Get method returns the default value when the key does not exist.
    /// </summary>
    [Fact]
    public void Get_NonExistentKey_ReturnsDefault()
    {
        var result = _sut.Get<string>("missing-key");

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Set method and the Get method return the stored value.
    /// </summary>
    [Fact]
    public void Set_ThenGet_ReturnsStoredValue()
    {
        _sut.Set("my-key", "my-value");

        var result = _sut.Get<string>("my-key");

        result.Should().Be("my-value");
    }

    /// <summary>
    /// Verifies that the Set method can store and retrieve complex types.
    /// </summary>
    [Fact]
    public void Set_ComplexType_RoundTripsCorrectly()
    {
        var data = new { Name = "test", Count = 42 };
        _sut.Set("complex-key", data);

        var result = _sut.Get<object>("complex-key");

        result.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that the Set method with expiration times out after the specified interval.
    /// </summary>
    [Fact]
    public void Set_WithExpiration_ValueExpires()
    {
        _sut.Set("expiring-key", "value", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(20);

        var result = _sut.Get<string>("expiring-key");

        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Set method without expiration persists indefinitely.
    /// </summary>
    [Fact]
    public void Set_WithoutExpiration_ValuePersists()
    {
        _sut.Set("persistent-key", "persistent-value");
        Thread.Sleep(20);

        var result = _sut.Get<string>("persistent-key");

        result.Should().Be("persistent-value");
    }

    /// <summary>
    /// Verifies that the Remove method removes the specified key from the cache.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_RemovesEntry()
    {
        _sut.Set("to-remove", "value");

        _sut.Remove("to-remove");

        _sut.Get<string>("to-remove").Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Remove method does not throw an exception when the key does not exist.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_DoesNotThrow()
    {
        var act = () => _sut.Remove("does-not-exist");

        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that the Exists method returns true for existing keys.
    /// </summary>
    [Fact]
    public void Exists_ExistingKey_ReturnsTrue()
    {
        _sut.Set("exists-key", "value");

        _sut.Exists("exists-key").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that the Exists method returns false for non-existent keys.
    /// </summary>
    [Fact]
    public void Exists_NonExistentKey_ReturnsFalse()
    {
        _sut.Exists("missing-key").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the Exists method returns false for expired keys.
    /// </summary>
    [Fact]
    public void Exists_ExpiredKey_ReturnsFalse()
    {
        _sut.Set("expired-key", "value", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(20);

        _sut.Exists("expired-key").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the Clear method removes all entries from the cache.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _sut.Set("key1", "value1");
        _sut.Set("key2", "value2");
        _sut.Set("key3", "value3");

        _sut.Clear();

        _sut.Get<string>("key1").Should().BeNull();
        _sut.Get<string>("key2").Should().BeNull();
        _sut.Get<string>("key3").Should().BeNull();
    }

    /// <summary>
    /// Verifies that the GetOrSet method calls the factory when the key does not exist.
    /// </summary>
    [Fact]
    public void GetOrSet_CacheMiss_CallsFactory()
    {
        var factoryCalled = false;

        var result = _sut.GetOrSet("new-key", () =>
        {
            factoryCalled = true;
            return "factory-value";
        });

        factoryCalled.Should().BeTrue();
        result.Should().Be("factory-value");
    }

    /// <summary>
    /// Verifies that the GetOrSet method does not call the factory when the key exists.
    /// </summary>
    [Fact]
    public void GetOrSet_CacheHit_DoesNotCallFactory()
    {
        _sut.Set("existing-key", "cached-value");
        var factoryCalled = false;

        var result = _sut.GetOrSet("existing-key", () =>
        {
            factoryCalled = true;
            return "factory-value";
        });

        factoryCalled.Should().BeFalse();
        result.Should().Be("cached-value");
    }

    /// <summary>
    /// Verifies that the GetOrSetAsync method calls the async factory when the key does not exist.
    /// </summary>
    [Fact]
    public async Task GetOrSetAsync_CacheMiss_CallsAsyncFactory()
    {
        var factoryCalled = false;

        var result = await _sut.GetOrSetAsync(
            "async-key",
            async _ =>
            {
                factoryCalled = true;
                await Task.Delay(1);
                return "async-value";
            });

        factoryCalled.Should().BeTrue();
        result.Should().Be("async-value");
    }

    /// <summary>
    /// Verifies that the GetOrSetAsync method does not call the async factory when the key exists.
    /// </summary>
    [Fact]
    public async Task GetOrSetAsync_CacheHit_DoesNotCallFactory()
    {
        _sut.Set("cached-async-key", "cached-value");
        var factoryCalled = false;

        var result = await _sut.GetOrSetAsync(
            "cached-async-key",
            async _ =>
            {
                factoryCalled = true;
                await Task.Delay(1);
                return "new-value";
            });

        factoryCalled.Should().BeFalse();
        result.Should().Be("cached-value");
    }

    /// <summary>
    /// Verifies that the SetAsync method and the GetAsync method return the stored value.
    /// </summary>
    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        await _sut.SetAsync("async-set-key", 42);

        var result = await _sut.GetAsync<int>("async-set-key");

        result.Should().Be(42);
    }

    /// <summary>
    /// Verifies that the RemoveAsync method removes the specified key from the cache.
    /// </summary>
    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesEntry()
    {
        _sut.Set("async-remove-key", "value");

        await _sut.RemoveAsync("async-remove-key");

        _sut.Get<string>("async-remove-key").Should().BeNull();
    }

    /// <summary>
    /// Verifies that the Set method overwrites existing entries.
    /// </summary>
    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        _sut.Set("overwrite-key", "original");
        _sut.Set("overwrite-key", "updated");

        _sut.Get<string>("overwrite-key").Should().Be("updated");
    }
}
