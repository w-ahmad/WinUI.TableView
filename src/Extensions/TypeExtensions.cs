using System;
using System.Linq;
using System.Reflection;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the Type class.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether the specified type is a Boolean.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is Boolean; otherwise, false.</returns>
    public static bool IsBoolean(this Type type)
    {
        return type == typeof(bool) || type == typeof(bool?);
    }

    /// <summary>
    /// Determines whether the specified type is a numeric.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is numeric; otherwise, false.</returns>
    public static bool IsNumeric(this Type type)
    {
        return type == typeof(byte) || type == typeof(byte?) ||
               type == typeof(sbyte) || type == typeof(sbyte?) ||
               type == typeof(short) || type == typeof(short?) ||
               type == typeof(ushort) || type == typeof(ushort?) ||
               type == typeof(int) || type == typeof(int?) ||
               type == typeof(uint) || type == typeof(uint?) ||
               type == typeof(long) || type == typeof(long?) ||
               type == typeof(ulong) || type == typeof(ulong?) ||
               type == typeof(float) || type == typeof(float?) ||
               type == typeof(double) || type == typeof(double?) ||
               type == typeof(decimal) || type == typeof(decimal?);
    }

    /// <summary>
    /// Determines whether the specified type is a TimeSpan or nullable TimeSpan.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is TimeSpan or nullable TimeSpan; otherwise, false.</returns>
    public static bool IsTimeSpan(this Type? type)
    {
        return type == typeof(TimeSpan) || type == typeof(TimeSpan?);
    }

    /// <summary>
    /// Determines whether the specified type is a TimeOnly or nullable TimeOnly.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is TimeOnly or nullable TimeOnly; otherwise, false.</returns>
    public static bool IsTimeOnly(this Type? type)
    {
        return type == typeof(TimeOnly) || type == typeof(TimeOnly?);
    }

    /// <summary>
    /// Determines whether the specified type is a DateOnly or nullable DateOnly.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is DateOnly or nullable DateOnly; otherwise, false.</returns>
    public static bool IsDateOnly(this Type? type)
    {
        return type == typeof(DateOnly) || type == typeof(DateOnly?);
    }

    /// <summary>
    /// Determines whether the specified type is a DateTime or nullable DateTime.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is DateTime or nullable DateTime; otherwise, false.</returns>
    public static bool IsDateTime(this Type? type)
    {
        return type == typeof(DateTime) || type == typeof(DateTime?);
    }

    /// <summary>
    /// Determines whether the specified type is a DateTimeOffset or nullable DateTimeOffset.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is DateTimeOffset or nullable DateTimeOffset; otherwise, false.</returns>
    public static bool IsDateTimeOffset(this Type? type)
    {
        return type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
    }

    /// <summary>
    /// Determines whether the specified type is a nullable type.
    /// </summary>
    public static bool IsNullableType(this Type? type)
    {
        return type is not null && type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Gets the underlying type argument of a nullable type.
    /// </summary>
    public static Type GetNonNullableType(this Type type)
    {
        return type.IsNullableType() ? Nullable.GetUnderlyingType(type)! : type;
    }

    /// <summary>
    /// Determines whether the specified type is a primitive type.
    /// </summary>
    public static bool IsPrimitive(this Type? dataType)
    {
        return dataType is not null &&
            (dataType.GetTypeInfo().IsPrimitive ||
             dataType == typeof(string) ||
             dataType == typeof(decimal) ||
             dataType == typeof(DateTime));
    }

    /// <summary>
    /// Determines whether the specified type is inherited from <see cref="IComparable"/>.
    /// </summary>
    public static bool IsInheritedFromIComparable(this Type type)
    {
        return type.GetInterfaces().Any(i => i == typeof(IComparable));
    }
}