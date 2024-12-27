using System;

namespace WinUI.TableView.Extensions;

internal static class TypeExtensions
{
    public static bool IsTimeSpan(this Type? type)
    {
        return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
    }

    public static bool IsTimeOnly(this Type? type)
    {
        return type == typeof(TimeOnly) || type == typeof(TimeOnly?);
    }

    public static bool IsDateOnly(this Type? type)
    {
        return type == typeof(DateOnly) || type == typeof(DateOnly?);
    }

    public static bool IsDateTime(this Type? type)
    {
        return type == typeof(DateTime) || type == typeof(DateTime?);
    }

    public static bool IsDateTimeOffset(this Type? type)
    {
        return type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
    }
}
