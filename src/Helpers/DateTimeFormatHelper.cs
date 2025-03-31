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
    private static readonly Dictionary<(string Format, string? Clock), DateTimeFormatter> _formatters = [];

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
                var formatter = GetDateTimeFormatter("shorttime", format);
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
                var formatter = GetDateTimeFormatter(format);

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

    /// <summary>
    /// Gets a DateTimeFormatter for the specified format and clock.
    /// </summary>
    internal static DateTimeFormatter GetDateTimeFormatter(string strFormat, string? strClock = null)
    {
        if (_formatters.TryGetValue((strFormat, strClock), out var cacheFormatter))
        {
            return cacheFormatter;
        }

        var formatter = new DateTimeFormatter(strFormat);
        var result = new DateTimeFormatter(
             strFormat,
             formatter.Languages,
             formatter.GeographicRegion,
             formatter.Calendar,
             strClock ?? formatter.Clock);

        _formatters[(strFormat, strClock)] = result;

        return result;
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
