using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Controls;

/// <summary>
/// Represents a date editing element for the TableViewDateColumn.
/// </summary>
public partial class TableViewDatePicker : CalendarDatePicker
{
    private bool _deferUpdate;

    /// <summary>
    /// Initializes a new instance of the TableViewDatePicker class.
    /// </summary>
    public TableViewDatePicker()
    {
        DateChanged += OnDateChanged;
    }

    /// <summary>
    /// Handles the DateChanged event.
    /// </summary>
    private void OnDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_deferUpdate) return;

        _deferUpdate = true;

        if (Date is null)
        {
            SelectedDate = null;
        }
        else if (SourceType.IsDateOnly())
        {
            SelectedDate = DateOnly.FromDateTime(Date.Value.DateTime);
        }
        else if (SourceType.IsDateTime())
        {
            var newDate = Date.Value.DateTime;
            var selectedDate = (DateTime?)SelectedDate ?? DateTime.Now;
            SelectedDate = new DateTime(newDate.Year, newDate.Month, newDate.Day,
                                        selectedDate.Hour, selectedDate.Minute, selectedDate.Second);
        }
        else if (SourceType.IsDateTimeOffset())
        {
            var selectedDate = (DateTimeOffset?)SelectedDate ?? DateTimeOffset.Now;
            var newDate = Date.Value;
            SelectedDate = new DateTimeOffset(newDate.Year, newDate.Month, newDate.Day,
                                              selectedDate.Hour, selectedDate.Minute, selectedDate.Second, selectedDate.Offset);
        }

        _deferUpdate = false;
    }

    /// <summary>
    /// Handles changes to the SelectedDate property.
    /// </summary>
    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TableViewDatePicker datePicker && !datePicker._deferUpdate)
        {
            datePicker._deferUpdate = true;
            datePicker.Date = e.NewValue switch
            {
                DateOnly dateOnly => dateOnly.ToDateTimeOffset(),
                DateTime dateTime => dateTime.ToDateTimeOffset(),
                DateTimeOffset dateTimeOffset => dateTimeOffset,
                _ => throw new FormatException()
            };
            datePicker.SourceType ??= e.NewValue?.GetType();
            datePicker._deferUpdate = false;
        }
    }

    /// <summary>
    /// Gets or sets the source type of the date picker.
    /// This value could be DateOnly, DateTime, or DateTimeOffset.
    /// </summary>
    internal Type? SourceType { get; set; }

    /// <summary>
    /// Gets or sets the selected date.
    /// </summary>
    public object? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>
    /// Identifies the SelectedDate dependency property.
    /// </summary>
    public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(nameof(SelectedDate), typeof(object), typeof(TableViewDatePicker), new PropertyMetadata(default, OnSelectedDateChanged));
}
