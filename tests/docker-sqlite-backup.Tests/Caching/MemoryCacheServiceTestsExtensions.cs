// Author: Vladyslav Zaiets
using System.Collections.Concurrent;
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

	/// <summary>
	/// Verifies that an exception thrown by the synchronous GetOrSet factory propagates and that nothing is cached for the key.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
	public static void GetOrSet_FactoryThrows_PropagatesAndLeavesKeyUnset(this MemoryCacheServiceTests _, string key)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);

		// Arrange
		using var sut = new MemoryCacheService();

		// Act
		Action act = () => sut.GetOrSet<string>(key, () => throw new InvalidOperationException("factory failed"));

		// Assert
		act.Should().Throw<InvalidOperationException>().WithMessage("factory failed");
		sut.Exists(key).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that an exception thrown by the asynchronous GetOrSetAsync factory propagates and that nothing is cached for the key.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
	public static async Task GetOrSetAsync_FactoryThrows_PropagatesAndLeavesKeyUnset(this MemoryCacheServiceTests _, string key)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);

		// Arrange
		using var sut = new MemoryCacheService();

		static async Task<string> FailingFactory(CancellationToken cancellationToken)
		{
			await Task.Yield();
			throw new InvalidOperationException("factory failed");
		}

		// Act
		Func<Task> act = () => sut.GetOrSetAsync<string>(key, FailingFactory);

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("factory failed");
		sut.Exists(key).Should().BeFalse();
	}

	/// <summary>
	/// Verifies that reading a stored value as an incompatible type returns the default value instead of throwing.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
	public static async Task Get_IncompatibleType_ReturnsDefaultWithoutThrowing(this MemoryCacheServiceTests _, string key)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);

		// Arrange
		using var sut = new MemoryCacheService();
		sut.Set(key, "not-a-number");

		// Act
		Action readAsInt = () => sut.Get<int?>(key);
		var asyncResult = await sut.GetAsync<int?>(key);

		// Assert
		readAsInt.Should().NotThrow();
		sut.Get<int?>(key).Should().BeNull();
		asyncResult.Should().BeNull();
		sut.Get<string>(key).Should().Be("not-a-number");
	}

	/// <summary>
	/// Verifies that concurrent Set, Remove, and Get calls on the same key never fail and only ever observe values that were written.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key shared by all threads.</param>
	/// <param name="iterations">The number of operations each thread performs.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="iterations"/> is zero or negative.</exception>
	public static async Task ConcurrentSetGetRemove_SameKey_NeverObservesForeignValue(this MemoryCacheServiceTests _, string key, int iterations)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(iterations);

		// Arrange
		using var sut = new MemoryCacheService();
		const int writerCount = 4;
		const int readerCount = 4;
		const int finalValue = 99;
		var observed = new ConcurrentBag<int>();
		using var start = new Barrier(writerCount + readerCount);

		// Dedicated threads with a shared start barrier so reads overlap writes instead of running after them.
		var writers = Enumerable.Range(1, writerCount).Select(writerId => Task.Factory.StartNew(() =>
		{
			start.SignalAndWait();
			for (var i = 0; i < iterations; i++)
			{
				sut.Set(key, writerId);
				if (i % 50 == 49)
					sut.Remove(key);
			}
		}, TaskCreationOptions.LongRunning)).ToArray();

		var writing = Task.WhenAll(writers);

		// Readers poll until the writers finish, so they cannot complete before the first write lands.
		var readers = Enumerable.Range(0, readerCount).Select(readerId => Task.Factory.StartNew(() =>
		{
			start.SignalAndWait();
			while (!writing.IsCompleted)
			{
				if (sut.Get<int?>(key) is { } value)
					observed.Add(value);
			}
		}, TaskCreationOptions.LongRunning)).ToArray();

		// Act
		await Task.WhenAll(writers.Concat(readers));
		sut.Set(key, finalValue);

		// Assert
		observed.Should().NotBeEmpty();
		observed.Should().NotContain(value => value < 1 || value > writerCount);
		sut.Get<int?>(key).Should().Be(finalValue);
	}

	/// <summary>
	/// Verifies current behavior: Clear does not cancel an in-flight GetOrSetAsync, so the factory result is stored after the clear.
	/// </summary>
	/// <param name="_">The test fixture instance (unused).</param>
	/// <param name="key">The cache key to use.</param>
	/// <exception cref="ArgumentException"><paramref name="key"/> is null or empty.</exception>
	public static async Task GetOrSetAsync_ClearWhileFactoryInFlight_StoresValueAfterClear(this MemoryCacheServiceTests _, string key)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);

		// Arrange
		using var sut = new MemoryCacheService();
		var factoryStarted = new TaskCompletionSource();
		var releaseFactory = new TaskCompletionSource<string>();

		var pending = sut.GetOrSetAsync<string>(key, async cancellationToken =>
		{
			factoryStarted.SetResult();
			return await releaseFactory.Task;
		});
		await factoryStarted.Task;

		// Act
		sut.Clear();
		releaseFactory.SetResult("late-value");
		await pending;

		// Assert
		sut.Get<string>(key).Should().Be("late-value");
	}
}
