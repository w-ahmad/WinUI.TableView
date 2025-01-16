using System;
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
}
