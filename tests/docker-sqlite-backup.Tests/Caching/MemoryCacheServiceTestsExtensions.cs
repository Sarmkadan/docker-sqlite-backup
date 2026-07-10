// Author: Vladyslav Zaiets

using DockerSqliteBackup.Caching;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Caching;

public static class MemoryCacheServiceTestsExtensions
{
    public static void Get_ShouldReturnDefaultForNullValue<T>(this MemoryCacheServiceTests _, string key)
    {
        // Arrange
        var sut = new MemoryCacheService();

        // Act
        var result = sut.Get<T>(key);

        // Assert
        result.Should().BeNull();
    }

    public static void Set_ShouldStoreValue<T>(this MemoryCacheServiceTests _, string key, T value)
    {
        // Arrange
        var sut = new MemoryCacheService();

        // Act
        sut.Set(key, value);
        var result = sut.Get<T>(key);

        // Assert
        result.Should().Be(value);
    }

    public static async Task SetAsync_ShouldStoreValueAsync<T>(this MemoryCacheServiceTests _, string key, T value)
    {
        // Arrange
        var sut = new MemoryCacheService();

        // Act
        await sut.SetAsync(key, value);
        var result = await sut.GetAsync<T>(key);

        // Assert
        result.Should().Be(value);
    }

    public static void Exists_ShouldReturnCorrectState(this MemoryCacheServiceTests _, string key, bool expectedExists)
    {
        // Arrange
        var sut = new MemoryCacheService();

        // Act
        var exists = sut.Exists(key);

        // Assert
        exists.Should().Be(expectedExists);
    }
}