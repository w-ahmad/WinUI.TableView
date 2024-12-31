using System;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for the Type class.
/// </summary>
internal static class TypeExtensions
{
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
}
