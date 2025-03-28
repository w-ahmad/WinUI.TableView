using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Windows.Globalization.DateTimeFormatting;
using Windows.System.UserProfile;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Helpers;

/// <summary>
/// Provides helper methods for formatting Date and Time values.
/// </summary>
internal static class DateTimeFormatHelper
{
    private const string _12HourClock = "12HourClock";
    private const string _24HourClock = "24HourClock";

    /// <summary>
    /// Gets the DateTimeFormatter for 12-hour clock format.
    /// </summary>
    internal static DateTimeFormatter _12HourClockFormatter { get; } = CreateNewFormatter("shorttime", _12HourClock);

    /// <summary>
    /// Gets the DateTimeFormatter for 24-hour clock format.
    /// </summary>
    internal static DateTimeFormatter _24HourClockFormatter { get; } = CreateNewFormatter("shorttime", _24HourClock);

    /// <summary>
    /// Gets the DateTimeFormatter for the short date format.
    /// </summary>
    internal static DateTimeFormatter DateFormatter { get; } = CreateNewFormatter("shortdate");

    /// <summary>
    /// Gets a DateTimeFormatter for the specified clock format.
    /// </summary>
    /// <param name="clock">The clock format ("12HourClock" or "24HourClock").</param>
    /// <returns>A DateTimeFormatter for the specified clock format.</returns>
    private static DateTimeFormatter GetClockFormatter(string clock)
    {
#if WINDOWS
        var languages = GlobalizationPreferences.Languages;
        var geographicRegion = GlobalizationPreferences.HomeGeographicRegion;
        var calendar = GlobalizationPreferences.Calendars[0];


        return new DateTimeFormatter("shorttime", languages, geographicRegion, calendar, clock);
#else
        return new DateTimeFormatter("shorttime");
#endif
    }

    /// <summary>
    /// Sets the formatted text for a TextBlock based on its value and format.
    /// </summary>
    /// <param name="textBlock">The TextBlock to set the formatted text for.</param>
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
                var dateTimeOffset = value switch
                {
                    DateOnly dateOnly => dateOnly.ToDateTimeOffset(),
                    DateTime dateTime => dateTime.ToDateTimeOffset(),
                    DateTimeOffset => (DateTimeOffset)value,
                    _ => throw new FormatException()
                };

                textBlock.Text = DateFormatter.Format(dateTimeOffset);
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

    private static DateTimeFormatter CreateNewFormatter(string strFormat, string? strClock = null)
    {
        DateTimeFormatter spFormatter;
        IReadOnlyList<string> spLanguages;
        string strGeographicRegion;
        string strCalendarIdentifier;

        spFormatter = new DateTimeFormatter(strFormat);

        strGeographicRegion = spFormatter.GeographicRegion;
        strCalendarIdentifier = spFormatter.Calendar;
        spLanguages = spFormatter.Languages;
        strClock ??= spFormatter.Clock;

        return new DateTimeFormatter(
            strFormat,
            spLanguages,
            strGeographicRegion,
            strCalendarIdentifier,
            strClock);
    }

    /// <summary>
    /// Handles changes to the Value attached property.
    /// </summary>
    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            SetFormattedText(textBlock);
        }
    }

    /// <summary>
    /// Handles changes to the Format attached property.
    /// </summary>
    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            SetFormattedText(textBlock);
        }
    }

    /// <summary>
    /// Gets the value of the Value attached property.
    /// </summary>
    public static object GetValue(DependencyObject obj)
    {
        return obj.GetValue(ValueProperty);
    }

    /// <summary>
    /// Sets the value of the Value attached property.
    /// </summary>
    public static void SetValue(DependencyObject obj, object value)
    {
        obj.SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets the value of the Format attached property.
    /// </summary>
    public static string GetFormat(DependencyObject obj)
    {
        return (string)obj.GetValue(FormatProperty);
    }

    /// <summary>
    /// Sets the value of the Format attached property.
    /// </summary>
    public static void SetFormat(DependencyObject obj, string value)
    {
        obj.SetValue(FormatProperty, value);
    }

    /// <summary>
    /// Identifies the Value attached property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached("Value", typeof(object), typeof(DateTimeFormatHelper), new PropertyMetadata(default, OnValueChanged));

    /// <summary>
    /// Identifies the Format attached property.
    /// </summary>
    public static readonly DependencyProperty FormatProperty = DependencyProperty.RegisterAttached("Format", typeof(string), typeof(DateTimeFormatHelper), new PropertyMetadata(default, OnFormatChanged));
}
