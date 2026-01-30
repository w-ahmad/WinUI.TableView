using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for collections.
/// </summary>
internal static class CollectionExtensions
{
    /// <summary>
    /// Adds a range of items to the collection.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to add items to.</param>
    /// <param name="items">The items to add to the collection.</param>
    public static void AddRange<T>(this IList<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Removes items from the collection that match the specified predicate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to remove items from.</param>
    /// <param name="predicate">The predicate to determine which items to remove.</param>
    public static void RemoveWhere<T>(this ICollection<T> collection, Predicate<T> predicate)
    {
        var itemsToRemove = collection.Where(x => predicate(x)).ToList();

        foreach (var item in itemsToRemove)
        {
            collection.Remove(item);
        }
    }

    /// <summary>
    /// Gets the index of the first occurrence of an item in the enumerable.
    /// </summary>
    /// <param name="enumerable">The enumerable to search.</param>
    /// <param name="item">The item to find.</param>
    /// <returns>The index of the item, or -1 if not found.</returns>
    public static int IndexOf(this IEnumerable enumerable, object? item)
    {
        if (enumerable is ICollection collection)
            return collection.IndexOf(item);

        if (enumerable is ICollectionView collectionView)
            return collectionView.IndexOf(item);


        var index = 0;
        foreach (var element in enumerable)
        {
            if (Equals(element, item))
                return index;

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Gets a value indicating whether the enumerable is read-only.
    /// </summary>
    /// <param name="enumerable">The enumerable to check.</param>
    /// <returns></returns>
    public static bool IsReadOnly(this IEnumerable enumerable)
    {
        if (enumerable is IList list)
            return list.IsReadOnly;

        if (enumerable is ICollectionView collectionView)
            return collectionView.IsReadOnly;

        return true;
    }

    public static void Add(this IEnumerable enumerable, object? item)
    {
        if (enumerable is ICollection collection)
            collection.Add(item);

        if (enumerable is ICollectionView collectionView)
            collectionView.Add(item);
    }

    public static void Insert(this IEnumerable enumerable, int index, object? item)
    {
        if (enumerable is ICollection collection)
            collection.Insert(index, item);

        if (enumerable is ICollectionView collectionView)
            collectionView.Insert(index, item);
    }

    public static void Remove(this IEnumerable enumerable, object? item)
    {
        if (enumerable is ICollection collection)
            collection.Remove(item);

        if (enumerable is ICollectionView collectionView)
            collectionView.Remove(item);
    }

    public static void Clear(this IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
            collection.Clear();

        if (enumerable is ICollectionView collectionView)
            collectionView.Clear();
    }
}
