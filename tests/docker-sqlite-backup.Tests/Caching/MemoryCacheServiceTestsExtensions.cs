// Author: Vladyslav Zaiets
using DockerSqliteBackup.Caching;
using FluentAssertions;
using Xunit;

namespace DockerSqliteBackup.Tests.Caching;

/// <summary>
/// Extension methods for testing <see cref="MemoryCacheService"/> behavior.
/// </summary>
public static class MemoryCacheServiceTestsExtensions
{
	/// <summary>
	/// Verifies that getting a non-existent key returns the default value for the type.
	/// </summary>
	/// <typeparam name="T">The type of value expected.</typeparam>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to retrieve.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static void Get_ShouldReturnDefaultForNullValue<T>(this MemoryCacheServiceTests _, string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();

		// Act
		var result = sut.Get<T>(key);

		// Assert
		result.Should().BeNull();
	}

	/// <summary>
	/// Verifies that setting a value allows it to be retrieved.
	/// </summary>
	/// <typeparam name="T">The type of value being stored.</typeparam>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <param name="value">The value to store.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static void Set_ShouldStoreValue<T>(this MemoryCacheServiceTests _, string key, T value)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();

		// Act
		sut.Set(key, value);
		var result = sut.Get<T>(key);

		// Assert
		result.Should().Be(value);
	}

	/// <summary>
	/// Verifies that setting a value asynchronously allows it to be retrieved asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of value being stored.</typeparam>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <param name="value">The value to store.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static async Task SetAsync_ShouldStoreValueAsync<T>(this MemoryCacheServiceTests _, string key, T value)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();

		// Act
		await sut.SetAsync(key, value);
		var result = await sut.GetAsync<T>(key);

		// Assert
		result.Should().Be(value);
	}

	/// <summary>
	/// Verifies that the Exists method returns the correct state for cached and non-cached keys.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to test.</param>
	/// <param name="expectedExists">Whether the key should exist in cache.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static void Exists_ShouldReturnCorrectState(this MemoryCacheServiceTests _, string key, bool expectedExists)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();

		// Act & Assert for non-existent key
		if (!expectedExists)
		{
			sut.Exists(key).Should().BeFalse();
			return;
		}

		// For existing key, set a value first
		sut.Set(key, "test-value");

		// Act
		var exists = sut.Exists(key);

		// Assert
		exists.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that removing a non-existent key doesn't throw.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to attempt removal on.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static void Remove_ShouldNotThrowForNonExistentKey(this MemoryCacheServiceTests _, string key)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();

		// Act
		Action act = () => sut.Remove(key);

		// Assert
		act.Should().NotThrow();
	}

	/// <summary>
	/// Verifies that clearing the cache removes all entries.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	public static void Clear_ShouldRemoveAllEntries(this MemoryCacheServiceTests _)
	{
		// Arrange
		var sut = new MemoryCacheService();
		sut.Set("key1", "value1");
		sut.Set("key2", "value2");

		// Act
		sut.Clear();

		// Assert
		sut.Exists("key1").Should().BeFalse();
		sut.Exists("key2").Should().BeFalse();
	}

	/// <summary>
	/// Verifies that GetOrSet executes the factory only when key is not present.
	/// </summary>
	/// <typeparam name="T">The type of value being retrieved.</typeparam>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <param name="expectedCallCount">Expected number of times factory should be called.</param>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
	public static void GetOrSet_ShouldExecuteFactoryOnlyWhenMissing<T>(this MemoryCacheServiceTests _, string key, int expectedCallCount)
	{
		ArgumentNullException.ThrowIfNull(key);

		// Arrange
		var sut = new MemoryCacheService();
		var callCount = 0;

		T Factory()
		{
			callCount++;
			return default!;
		}

		// Act - first call should execute factory
		sut.GetOrSet(key, Factory);

		// Assert - factory should have been called once
		callCount.Should().Be(expectedCallCount);
	}
}