using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for object types.
/// </summary>
internal static partial class ObjectExtensions
{
    // Regex to split property paths into property names and indexers (for cases like e.g. "[2].Foo[0].Bar", where Foo might be a Property that returns an array)
    [GeneratedRegex(@"([^.[]+)|(\[[^\]]+\])", RegexOptions.Compiled)]
    private static partial Regex PropertyPathRegex();

    /// <summary>
    /// Gets the value of a property from an object using a sequence of property info and index pairs.
    /// </summary>
    /// <param name="obj">The object from which to get the value.</param>
    /// <param name="pis">An array of property info and index pairs.</param>
    /// <returns>The value of the property, or null if the object is null.</returns>
    internal static object? GetValue(this object? obj, (PropertyInfo pi, object? index)[] pis)
    {
        if (pis is null || pis.Length == 0)
        {
            return null;
        }

        foreach (var (pi, index) in pis)
        {
            if (obj is null)
                break;

            if (pi != null)
            {
                // Use property getter, with or without index
                obj = index is not null ? pi.GetValue(obj, [index]) : pi.GetValue(obj);
            }
            else if (obj is IDictionary dictionary && dictionary.Contains(index!))
            {
                obj = dictionary[index!];
            }
            else if (index is int idx)
            {
                // Array
                if (obj is Array arr && idx >= 0 && idx < arr.Length)
                {
                    obj = arr.GetValue(idx);
                }
                // IList
                else if (obj is IList list && idx >= 0 && list.Count < idx)
                {
                    obj = list[idx];
                }
            }
            else
            {
                // Not a supported path segment
                return null;
            }
        }

        return obj;
    }

    /// <summary>
    /// Gets the value of a property from an object using a type and a property path.
    /// </summary>
    /// <param name="obj">The object from which to get the value.</param>
    /// <param name="type">The type of the object.</param>
    /// <param name="path">The property path.</param>
    /// <param name="pis">An array of property info and index pairs.</param>
    /// <returns>The value of the property, or null if the object is null.</returns>
    internal static object? GetValue(this object? obj, Type? type, string? path, out (PropertyInfo pi, object? index)[] pis)
    {
        if (obj == null || string.IsNullOrWhiteSpace(path) || type == null)
        {
            pis = [];
            return obj;
        }

        var matches = PropertyPathRegex().Matches(path);
        if (matches.Count == 0)
        {
            pis = [];
            return obj;
        }

        // Pre-size the steps array to the number of matches
        pis = new (PropertyInfo, object?)[matches.Count];
        var i = 0;

        foreach (Match match in matches)
        {
            var part = match.Value;
            object? index = null;

            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                index = part[1..^1];

                // Try IDictionary
                if (obj is IDictionary dictionary && dictionary.Contains(index!))
                {
                    obj = dictionary[index!];
                    pis[i++] = (null!, index);
                    type = obj?.GetType();
                    continue;
                }
                else if (int.TryParse(part[1..^1], out var idx))
                {
                    index = idx;

                    // Try array
                    if (obj is Array arr && idx >= 0 && idx < arr.Length)
                    {
                        obj = arr.GetValue(idx);
                        pis[i++] = (null!, idx);
                        type = obj?.GetType();
                        continue;
                    }
                    // Try IList
                    else if (obj is IList list && idx >= 0 && list.Count < idx)
                    {
                        obj = list[idx];
                        pis[i++] = (null!, idx);
                        type = obj?.GetType();
                        continue;
                    }

                }

                part = "Item"; // Default indexer property name
            }

            if (TryGetPropertyValue(ref obj, ref type, part, index, out var pi))
            {
                pis[i++] = (pi!, index);
            }
            else
            {
                pis = null!;
                return null;
            }
        }

        static bool TryGetPropertyValue(ref object? obj, ref Type? type, string propertyName, object? index, out PropertyInfo? pi)
        {
            try
            {
                pi = index is null ? type?.GetProperty(propertyName) : type?.GetProperty(propertyName, [index.GetType()]);

                if (pi != null)
                {
                    obj = index is null ? pi.GetValue(obj) : pi.GetValue(obj, [index]);
                    type = obj?.GetType();

                    return true;
                }

                return false;
            }
            catch
            {
                pi = null!;
                return false;
            }
        }

        return obj;
    }

    /// <summary>
    /// Determines the type of items contained within the specified <see cref="IEnumerable"/>.
    /// </summary>
    /// <remarks>This method attempts to determine the item type of the provided <see cref="IEnumerable"/>
    /// using the following strategies: <list type="bullet"> <item> If the <paramref name="list"/> is a generic
    /// enumerable, the generic type is returned. </item> <item> If the item type implements <see
    /// cref="ICustomTypeProvider"/>, the method may attempt to retrieve a custom type from the list's items. </item>
    /// <item> If the item type cannot be determined directly, the method inspects the first item in the list to infer
    /// its type. </item> </list> If the list is empty or the item type is <see cref="object"/>, the method may return
    /// <see langword="null"/>.</remarks>
    /// <param name="list">The <see cref="IEnumerable"/> instance to analyze.</param>
    /// <returns>The <see cref="Type"/> of the items in the <paramref name="list"/>, or <see langword="null"/> if the type cannot
    /// be determined. If the list is empty or contains items of type <see cref="object"/>, additional heuristics may be
    /// applied to infer a more specific type.</returns>
    internal static Type? GetItemType(this IEnumerable list)
    {
        var listType = list.GetType();
        Type? itemType = null;
        var isICustomTypeProvider = false;

        // If it's a generic enumerable, get the generic type.

        // Unfortunately, if data source is fed from a bare IEnumerable, TypeHelper will report an element type of object,
        // which is not particularly interesting.  It is dealt with it further on.
        itemType = listType.GetEnumerableItemType();

        if (itemType != null)
        {
            isICustomTypeProvider = typeof(ICustomTypeProvider).IsAssignableFrom(itemType);
        }

        // Bare IEnumerables mean that result type will be object.  In that case, try to get something more interesting.
        // Or, if the itemType implements ICustomTypeProvider, try to retrieve the custom type from one of the object instances.
        if (itemType == null || itemType == typeof(object) || isICustomTypeProvider)
        {
            // No type was located yet. Does the list have anything in it?
            Type? firstItemType = null;
            var en = list.GetEnumerator();
            if (en.MoveNext() && en.Current != null)
            {
                firstItemType = en.Current.GetCustomOrCLRType();
            }
            else
            {
                firstItemType = list
                    .Cast<object>() // cast to convert IEnumerable to IEnumerable<object>
                    .Select(x => x.GetType()) // get the type
                    .FirstOrDefault(); // get only the first thing to come out of the sequence, or null if empty
            }

            if (firstItemType != typeof(object))
            {
                return firstItemType;
            }
        }

        // Couldn't get the CustomType because there were no items.
        if (isICustomTypeProvider)
        {
            return null;
        }

        return itemType;
    }

    /// <summary>
    /// Returns instance.GetCustomType() if the instance implements ICustomTypeProvider; otherwise,
    /// returns instance.GetType().
    /// </summary>
    /// <param name="instance">Object to return the type of</param>
    /// <returns>Type of the instance</returns>
    internal static Type? GetCustomOrCLRType(this object? instance)
    {
        if (instance is ICustomTypeProvider customTypeProvider)
        {
            return customTypeProvider.GetCustomType() ?? instance.GetType();
        }

        return instance?.GetType();
    }
}
