using System;

namespace WinUI.TableView.Extensions;

internal static class DateTimeExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime)
    {
        return new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
    }

    public static DateTimeOffset ToDateTimeOffset(this TimeSpan timeSpan)
    {
        return DateTime.Today.Add(timeSpan).ToDateTimeOffset();
    }

    public static DateTimeOffset ToDateTimeOffset(this DateOnly dateOnly)
    {
        return dateOnly.ToDateTime(TimeOnly.MinValue).ToDateTimeOffset();
    }

    public static DateTimeOffset ToDateTimeOffset(this TimeOnly timeOnly)
    {
        return timeOnly.ToTimeSpan().ToDateTimeOffset();
    }
}
