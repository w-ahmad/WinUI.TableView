using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for object types.
/// </summary>
internal static class ObjectExtensions
{
    /// <summary>
    /// Gets the value of a property from an object using a sequence of property info and index pairs.
    /// </summary>
    /// <param name="obj">The object from which to get the value.</param>
    /// <param name="members">An array of member info and index pairs.</param>
    /// <returns>The value of the property, or null if the object is null.</returns>
    internal static object? GetValue(this object? obj, (MemberInfo info, object? index)[] members)
    {
        foreach (var (info, index) in members)
        {
            if (obj is null)
            {
                break;
            }

            if (info is PropertyInfo pi)
                obj = index is not null ? pi.GetValue(obj, [index]) : pi.GetValue(obj);
            else if (info is MethodInfo mi)
                obj = index is not null ? mi.Invoke(obj, [index]) : mi.Invoke(obj, []);
        }

        return obj;
    }

    /// <summary>
    /// Gets the value of a property from an object using a type and a property path.
    /// </summary>
    /// <param name="obj">The object from which to get the value.</param>
    /// <param name="type">The type of the object.</param>
    /// <param name="path">The property path.</param>
    /// <param name="members">An array of member info and index pairs.</param>
    /// <returns>The value of the property, or null if the object is null.</returns>
    internal static object? GetValue(this object? obj, Type? type, string? path, out (MemberInfo info, object? index)[] members)
    {
        var parts = path?.Split('.');

        if (parts is null)
        {
            members = [];
            return obj;
        }

        members = new (MemberInfo, object?)[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var index = default(object?);
            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                index = int.TryParse(part[1..^1], out var ind) ? ind : index;
                part = "Item";
            }

            if (type?.GetProperty(part) is { } pi)
            {
                members[i] = (pi, index);
                obj = index is not null ? pi?.GetValue(obj, [index]) : pi?.GetValue(obj);
                type = obj?.GetType();
            }
            else if (type?.IsArray is true && type.GetMethod("GetValue", [typeof(int)]) is { } mi)
            {
                members[i] = (mi, index);
                obj = index is not null ? mi?.Invoke(obj, [index]) : mi?.Invoke(obj, []);
                type = obj?.GetType();

            }
            else
            {
                members = null!;
                return null;
            }
        }

        return obj;
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
