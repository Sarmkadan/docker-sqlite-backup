// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DockerSqliteBackup.Caching;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Caching;

public class MemoryCacheServiceTests : IDisposable
{
    private readonly MemoryCacheService _sut;

    public MemoryCacheServiceTests()
    {
        _sut = new MemoryCacheService(cleanupInterval: TimeSpan.FromHours(1));
    }

    public void Dispose()
    {
        _sut.Clear();
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsDefault()
    {
        var result = _sut.Get<string>("missing-key");

        result.Should().BeNull();
    }

    [Fact]
    public void Set_ThenGet_ReturnsStoredValue()
    {
        _sut.Set("my-key", "my-value");

        var result = _sut.Get<string>("my-key");

        result.Should().Be("my-value");
    }

    [Fact]
    public void Set_ComplexType_RoundTripsCorrectly()
    {
        var data = new { Name = "test", Count = 42 };
        _sut.Set("complex-key", data);

        var result = _sut.Get<object>("complex-key");

        result.Should().NotBeNull();
    }

    [Fact]
    public void Set_WithExpiration_ValueExpires()
    {
        _sut.Set("expiring-key", "value", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(20);

        var result = _sut.Get<string>("expiring-key");

        result.Should().BeNull();
    }

    [Fact]
    public void Set_WithoutExpiration_ValuePersists()
    {
        _sut.Set("persistent-key", "persistent-value");
        Thread.Sleep(20);

        var result = _sut.Get<string>("persistent-key");

        result.Should().Be("persistent-value");
    }

    [Fact]
    public void Remove_ExistingKey_RemovesEntry()
    {
        _sut.Set("to-remove", "value");

        _sut.Remove("to-remove");

        _sut.Get<string>("to-remove").Should().BeNull();
    }

    [Fact]
    public void Remove_NonExistentKey_DoesNotThrow()
    {
        var act = () => _sut.Remove("does-not-exist");

        act.Should().NotThrow();
    }

    [Fact]
    public void Exists_ExistingKey_ReturnsTrue()
    {
        _sut.Set("exists-key", "value");

        _sut.Exists("exists-key").Should().BeTrue();
    }

    [Fact]
    public void Exists_NonExistentKey_ReturnsFalse()
    {
        _sut.Exists("missing-key").Should().BeFalse();
    }

    [Fact]
    public void Exists_ExpiredKey_ReturnsFalse()
    {
        _sut.Set("expired-key", "value", TimeSpan.FromMilliseconds(1));
        Thread.Sleep(20);

        _sut.Exists("expired-key").Should().BeFalse();
    }

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

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsStoredValue()
    {
        await _sut.SetAsync("async-set-key", 42);

        var result = await _sut.GetAsync<int>("async-set-key");

        result.Should().Be(42);
    }

    [Fact]
    public async Task RemoveAsync_ExistingKey_RemovesEntry()
    {
        _sut.Set("async-remove-key", "value");

        await _sut.RemoveAsync("async-remove-key");

        _sut.Get<string>("async-remove-key").Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        _sut.Set("overwrite-key", "original");
        _sut.Set("overwrite-key", "updated");

        _sut.Get<string>("overwrite-key").Should().Be("updated");
    }
}
