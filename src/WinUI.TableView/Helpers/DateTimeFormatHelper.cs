using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using Windows.Globalization.DateTimeFormatting;
using Windows.System.UserProfile;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Helpers;

internal static class DateTimeFormatHelper
{
    private const string _12HourClock = "12HourClock";
    private const string _24HourClock = "24HourClock";

    internal static DateTimeFormatter _12HourClockFormatter { get; } = GetClockFormatter(_12HourClock);

    internal static DateTimeFormatter _24HourClockFormatter { get; } = GetClockFormatter(_24HourClock);

    private static DateTimeFormatter GetClockFormatter(string clock)
    {
        var languages = GlobalizationPreferences.Languages;
        var geographicRegion = GlobalizationPreferences.HomeGeographicRegion;
        var calendar = GlobalizationPreferences.Calendars[0];

        return new DateTimeFormatter("shorttime", languages, geographicRegion, calendar, clock);
    }

    private static void SetFormattedText(TextBlock textBlock)
    {
        var value = GetValue(textBlock);
        var format = GetFormat(textBlock);

        try
        {
            if (value is not null && format is _12HourClock or _24HourClock)
            {
                var formatter = format is _24HourClock ? _24HourClockFormatter : _12HourClockFormatter;
                var dateTimeOffset = value switch
                {
                    TimeSpan timeSpan => timeSpan.ToDateTimeOffset(),
                    TimeOnly timeOnly => timeOnly.ToDateTimeOffset(),
                    DateTime dateTime => dateTime.ToDateTimeOffset(),
                    DateTimeOffset => (DateTimeOffset)value,
                    _ => throw new FormatException()
                };

                textBlock.Text = formatter.Format(dateTimeOffset);
            }
            else if (value is not null)
            {
                var formatter = new DateTimeFormatter(format);
                var dateTimeOffset = value switch
                {
                    DateOnly dateOnly => dateOnly.ToDateTimeOffset(),
                    DateTime dateTime => dateTime.ToDateTimeOffset(),
                    DateTimeOffset => (DateTimeOffset)value,
                    _ => throw new FormatException()
                };

                textBlock.Text = formatter.Format(dateTimeOffset);
            }
            else
            {
                textBlock.Text = value?.ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to format value. Error: {ex.Message}");
            throw;
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            SetFormattedText(textBlock);
        }
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            SetFormattedText(textBlock);
        }
    }

    public static object GetValue(DependencyObject obj)
    {
        return obj.GetValue(ValueProperty);
    }

    public static void SetValue(DependencyObject obj, object value)
    {
        obj.SetValue(ValueProperty, value);
    }

    public static string GetFormat(DependencyObject obj)
    {
        return (string)obj.GetValue(FormatProperty);
    }

    public static void SetFormat(DependencyObject obj, string value)
    {
        obj.SetValue(FormatProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(object), typeof(DateTimeFormatHelper), new PropertyMetadata(default, OnValueChanged));
    public static readonly DependencyProperty FormatProperty = DependencyProperty.RegisterAttached("Format", typeof(string), typeof(DateTimeFormatHelper), new PropertyMetadata(default, OnFormatChanged));
}
