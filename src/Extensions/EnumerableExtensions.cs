// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DockerSqliteBackup.Extensions;

/// <summary>
/// Extension methods for IEnumerable operations.
/// Provides utilities for batching, filtering, and transforming collections.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Batches an enumerable into chunks of a specified size.
    /// </summary>
    public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0", nameof(batchSize));

        var batch = new List<T>(batchSize);

        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count == batchSize)
            {
                yield return batch;
                batch = new List<T>(batchSize);
            }
        }

        if (batch.Count > 0)
            yield return batch;
    }

    /// <summary>
    /// Checks if a collection is empty or null.
    /// </summary>
    public static bool IsEmpty<T>(this IEnumerable<T>? source)
    {
        return source == null || !source.Any();
    }

    /// <summary>
    /// Checks if a collection has any items.
    /// </summary>
    public static bool HasItems<T>(this IEnumerable<T>? source)
    {
        return source != null && source.Any();
    }

    /// <summary>
    /// Gets the first item or a default value.
    /// </summary>
    public static T? FirstOrNull<T>(this IEnumerable<T?> source) where T : class
    {
        return source.FirstOrDefault(x => x != null);
    }

    /// <summary>
    /// Filters out null values.
    /// </summary>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        return source.Where(x => x != null)!;
    }

    /// <summary>
    /// Converts an enumerable to a paginated result.
    /// </summary>
    public static (List<T> items, int total, int page, int pageSize) Paginate<T>(
        this IEnumerable<T> source,
        int page = 1,
        int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var list = source.ToList();
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, total, page, pageSize);
    }

    /// <summary>
    /// Groups items by a key and returns a count for each group.
    /// </summary>
    public static Dictionary<TKey, int> CountBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        return source
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Merges multiple enumerables into one.
    /// </summary>
    public static IEnumerable<T> Merge<T>(params IEnumerable<T>[] sources)
    {
        return sources.SelectMany(x => x);
    }

    /// <summary>
    /// Ensures an enumerable is materialized as a list.
    /// </summary>
    public static List<T> AsList<T>(this IEnumerable<T> source)
    {
        return source as List<T> ?? source.ToList();
    }

    /// <summary>
    /// Safely performs an action on each item.
    /// </summary>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Selects either from a true or false collection based on a predicate.
    /// </summary>
    public static IEnumerable<T> SelectFromOrElse<T>(
        this IEnumerable<T> source,
        IEnumerable<T> trueValues,
        IEnumerable<T> falseValues,
        Func<T, bool> predicate)
    {
        return source.Any(predicate) ? trueValues : falseValues;
    }

    /// <summary>
    /// Gets distinct items by a key selector.
    /// </summary>
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector) where TKey : notnull
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seen.Add(key))
                yield return item;
        }
    }
}
