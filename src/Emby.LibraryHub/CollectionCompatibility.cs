using System;
using System.Collections.Generic;

namespace Emby.LibraryHub;

/// <summary>Supplies the collection operations used by browsing on .NET Standard 2.0.</summary>
internal static class CollectionCompatibility
{
    /// <summary>Retains the first element for each key and preserves source order.</summary>
    internal static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> key)
    {
        var seen = new HashSet<TKey>();
        foreach (var item in source) if (seen.Add(key(item))) yield return item;
    }

    /// <summary>Builds a set using the default equality comparer for browse filters.</summary>
    internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new(source);

    /// <summary>Leaves the first group unchanged when the key has already been added.</summary>
    internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> target, TKey key, TValue value)
        where TKey : notnull
    {
        if (target.ContainsKey(key)) return false;
        target.Add(key, value);
        return true;
    }

    /// <summary>Splits an inventory into bounded permission queries, with no empty final batch.</summary>
    internal static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        using var items = source.GetEnumerator();
        while (items.MoveNext())
        {
            var batch = new T[size];
            var count = 0;
            do { batch[count++] = items.Current; } while (count < size && items.MoveNext());
            if (count != size) Array.Resize(ref batch, count);
            yield return batch;
        }
    }
}
