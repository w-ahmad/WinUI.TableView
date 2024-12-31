using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Controls;

public partial class TableViewDatePicker : CalendarDatePicker
{
    private bool _deferUpdate;

    public TableViewDatePicker()
    {
        DateChanged += OnDateChanged;
    }

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

    internal Type? SourceType { get; set; }

    public object? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(nameof(SelectedDate), typeof(object), typeof(TableViewDatePicker), new PropertyMetadata(default, OnSelectedDateChanged));
}
