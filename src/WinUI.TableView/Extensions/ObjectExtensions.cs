using System;
using System.Reflection;

namespace WinUI.TableView.Extensions;

internal static class ObjectExtensions
{
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

    internal static object? GetValue(this object? obj, Type? type, string path, out (PropertyInfo pi, object? index)[] pis)
    {
        var parts = path.Split('.');
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
                return obj;
            }
        }

        return obj;
    }
}
