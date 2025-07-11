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
        foreach (var (pi, index) in pis)
        {
            if (obj is null)
                break;

            if (pi != null)
            {
                // Use property getter, with or without index
                obj = index is not null ? pi.GetValue(obj, [index]) : pi.GetValue(obj);
            }
            else if (index is int i)
            {
                // Array
                if (obj is Array arr)
                {
                    obj = arr.GetValue(i);
                }
                // IList
                else if (obj is IList list)
                {
                    obj = list[i];
                }
                else
                {
                    // Not a supported indexer type
                    return null;
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
        int i = 0;
        object? current = obj;
        Type? currentType = type;

        foreach (Match match in matches)
        {
            string part = match.Value;
            object? index = null;
            PropertyInfo? pi = null;

            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                // Indexer: [int] or [string]
                string indexer = part[1..^1];
                if (int.TryParse(indexer, out int intIndex))
                    index = intIndex;
                else
                    index = indexer;

                // Try array
                if (current is Array arr && index is int idx)
                {
                    current = arr.GetValue(idx);
                    pis[i++] = (null!, idx);
                    currentType = current?.GetType();
                    continue;
                }

                // Try IList
                if (current is IList list && index is int idx2)
                {
                    current = list[idx2];
                    pis[i++] = (null!, idx2);
                    currentType = current?.GetType();
                    continue;
                }

                // Try to find a default indexer property "Item" (e.g., this[string]);
                // Note that only single argument indexers of type int or string are currently support
                pi = currentType?.GetProperty("Item", [index.GetType()]);
                if (pi != null)
                {
                    current = pi.GetValue(current, [index]);
                    pis[i++] = (pi, index);
                    currentType = current?.GetType();
                    continue;
                }

                // Not found
                pis = null!;
                return null;
            }
            else
            {
                // Property access
                pi = currentType?.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pi == null)
                {
                    pis = null!;
                    return null;
                }
                current = pi.GetValue(current);
                pis[i++] = (pi, null);
                currentType = current?.GetType();
            }
        }

        return current;
    }

    /// <summary>
    /// Determines whether the specified object is numeric.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static bool IsNumeric(this object obj)
    {
        return obj is byte
                   or sbyte
                   or short
                   or ushort
                   or int
                   or uint
                   or long
                   or ulong
                   or float
                   or double
                   or decimal;
    }

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

    internal static Type? GetCustomOrCLRType(this object? instance)
    {
        if (instance is ICustomTypeProvider customTypeProvider)
        {
            return customTypeProvider.GetCustomType() ?? instance.GetType();
        }

        return instance?.GetType();
    }
}
