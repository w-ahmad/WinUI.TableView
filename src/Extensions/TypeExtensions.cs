using System;
using System.Collections;
using System.Collections.Generic;
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
            (dataType.IsPrimitive ||
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

    private static Type? FindGenericType(Type definition, Type type)
    {
        var definitionTypeInfo = definition.GetTypeInfo();

        while (type != null && type != typeof(object))
        {
            var typeTypeInfo = type.GetTypeInfo();

            if (typeTypeInfo.IsGenericType && type.GetGenericTypeDefinition() == definition)
            {
                return type;
            }

            if (definitionTypeInfo.IsInterface)
            {
                foreach (var type2 in typeTypeInfo.ImplementedInterfaces)
                {
                    var type3 = FindGenericType(definition, type2);
                    if (type3 != null)
                    {
                        return type3;
                    }
                }
            }

            type = typeTypeInfo.BaseType!;
        }

        return null;
    }

    internal static Type GetEnumerableItemType(this Type enumerableType)
    {
        var type = FindGenericType(typeof(IEnumerable<>), enumerableType);
        if (type != null)
        {
            return type.GetGenericArguments()[0];
        }

        return enumerableType;
    }

    internal static bool IsEnumerableType(this Type enumerableType)
    {
        return FindGenericType(typeof(IEnumerable<>), enumerableType) != null;
    }

    /// <summary>
    /// Determines whether the specified type is a Uri.
    /// </summary>
    internal static bool IsUri(this Type type)
    {
        return type == typeof(Uri);
    }
}