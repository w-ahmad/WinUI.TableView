using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Controls;

/// <summary>
/// Represents a time editing element for the TableViewTimeColumn.
/// </summary>
public partial class TableViewTimePicker : TimePicker
{
    /// <summary>
    /// Initializes a new instance of the TableViewTimePicker class.
    /// </summary>
    public TableViewTimePicker()
    {
        DefaultStyleKey = typeof(TableViewTimePicker);
    }

    ///// <summary>
    ///// Handles the TimePicked event of the flyout.
    ///// </summary>
    //private void OnTimePicked(TimePickerFlyout sender, TimePickedEventArgs args)
    //{
    //    var oldTime = SelectedTime is null ? TimeSpan.Zero : args.OldTime;

    //    if (SourceType.IsTimeSpan())
    //    {
    //        SelectedTime = args.NewTime;
    //    }
    //    else if (SourceType.IsTimeOnly())
    //    {
    //        SelectedTime = TimeOnly.FromTimeSpan(args.NewTime);
    //    }
    //    else if (SourceType.IsDateTime())
    //    {
    //        var dateTime = (DateTime?)SelectedTime ?? DateTime.Today;
    //        SelectedTime = dateTime.Subtract(oldTime).Add(args.NewTime);
    //    }
    //    else if (SourceType.IsDateTimeOffset())
    //    {
    //        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Today);
    //        var dateTimeOffset = (DateTimeOffset?)SelectedTime ?? new DateTimeOffset(DateTime.Today, offset);
    //        SelectedTime = dateTimeOffset.Subtract(oldTime).Add(args.NewTime);
    //    }
    //}

    /// <summary>
    /// Gets or sets the placeholder text for the time picker.
    /// </summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TableViewTimePicker), new PropertyMetadata("pick a time"));
}