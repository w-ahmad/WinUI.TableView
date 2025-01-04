using System;
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
    /// <param name="pis">An array of property info and index pairs.</param>
    /// <returns>The value of the property, or null if the object is null.</returns>
    internal static object? GetValue(this object? obj, (PropertyInfo pi, object? index)[] pis)
    {
        foreach (var pi in pis)
        {
            if (obj is null)
            {
                break;
            }

            obj = pi.index is not null ? pi.pi.GetValue(obj, new[] { pi.index }) : pi.pi.GetValue(obj);
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
        var parts = path?.Split('.');

        if (parts is null)
        {
            pis = Array.Empty<(PropertyInfo, object?)>();
            return obj;
        }

        pis = new (PropertyInfo, object?)[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var index = default(object?);
            if (part.StartsWith('[') && part.EndsWith(']'))
            {
                index = int.TryParse(part[1..^1], out var ind) ? ind : index;
                part = "Item";
            }

            var pi = type?.GetProperty(part);
            if (pi is not null)
            {
                pis[i] = (pi, index);
                obj = index is not null ? pi?.GetValue(obj, new[] { index }) : pi?.GetValue(obj);
                type = obj?.GetType();
            }
            else
            {
                pis = null!;
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
}
