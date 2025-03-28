using System;

namespace WinUI.TableView.Extensions;

/// <summary>
/// Provides extension methods for Date and Time types.
/// </summary>
internal static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to a DateTimeOffset using the local time zone.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A DateTimeOffset representing the same point in time as the DateTime.</returns>
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    /// <summary>
    /// Converts a TimeSpan to a DateTimeOffset using the current date.
    /// </summary>
    /// <param name="timeSpan">The TimeSpan to convert.</param>
    /// <returns>A DateTimeOffset representing the same time of day as the TimeSpan on the current date.</returns>
    public static DateTimeOffset ToDateTimeOffset(this TimeSpan timeSpan)
    {
        return DateTime.Today.Add(timeSpan).ToDateTimeOffset();
    }

    /// <summary>
    /// Converts a DateOnly to a DateTimeOffset using the minimum time of day.
    /// </summary>
    /// <param name="dateOnly">The DateOnly to convert.</param>
    /// <returns>A DateTimeOffset representing the same date as the DateOnly with the minimum time of day.</returns>
    public static DateTimeOffset ToDateTimeOffset(this DateOnly dateOnly)
    {
        return dateOnly.ToDateTime(TimeOnly.MinValue).ToDateTimeOffset();
    }

    /// <summary>
    /// Converts a TimeOnly to a DateTimeOffset using the current date.
    /// </summary>
    /// <param name="timeOnly">The TimeOnly to convert.</param>
    /// <returns>A DateTimeOffset representing the same time of day as the TimeOnly on the current date.</returns>
    public static DateTimeOffset ToDateTimeOffset(this TimeOnly timeOnly)
    {
        return timeOnly.ToTimeSpan().ToDateTimeOffset();
    }
}
