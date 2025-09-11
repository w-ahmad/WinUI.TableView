using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Globalization.DateTimeFormatting;
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays time.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(TextBlock))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(TableViewTimePicker))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewTimeColumn : TableViewBoundColumn
{
    /// <summary>
    /// Initializes a new instance of the TableViewTimeColumn class.
    /// </summary>
    public TableViewTimeColumn()
    {
        ClockIdentifier = DateTimeFormatter.LongTime.Clock;
    }

    /// <summary>
    /// Generates a TextBlock element for the cell.
    /// </summary>
    /// <param name="cell">The cell for which the element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TextBlock element.</returns>
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem)
    {
        var textBlock = new TextBlock
        {
            Margin = new Thickness(12, 0, 12, 0),
        };

        textBlock.SetBinding(DateTimeFormatHelper.ValueProperty, Binding);
        textBlock.SetBinding(DateTimeFormatHelper.FormatProperty, new Binding
        {
            Path = new PropertyPath(nameof(ClockIdentifier)),
            Source = this
        });

        return textBlock;
    }

    /// <summary>
    /// Generates a TableViewTimePicker element for editing the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TableViewTimePicker element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        var timePicker = new TableViewTimePicker
        {
            ClockIdentifier = ClockIdentifier,
            MinuteIncrement = MinuteIncrement,
            PlaceholderText = PlaceholderText ?? TableViewLocalizedStrings.TimePickerPlaceholder,
            SourceType = GetSourcePropertyType(dataItem),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        timePicker.SetBinding(TableViewTimePicker.SelectedTimeProperty, Binding);

        return timePicker;
    }

    /// <summary>
    /// Gets the type of the source property.
    /// </summary>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>The type of the source property.</returns>
    private Type? GetSourcePropertyType(object? dataItem)
    {
        if (Binding is not null && dataItem is not null)
        {
            var type = dataItem.GetType();
            var propertyPath = Binding.Path?.Path;

            if (!string.IsNullOrEmpty(propertyPath))
            {
                var propertyInfo = type.GetProperty(propertyPath);
                if (propertyInfo is not null)
                {
                    type = propertyInfo.PropertyType;
                }
            }

            if (type.IsTimeSpan() || type.IsTimeOnly() || type.IsDateTime() || type.IsDateTimeOffset())
            {
                return type;
            }
        }

        return typeof(TimeSpan);
    }

    /// <summary>
    /// Gets or sets the clock identifier for the time picker.
    /// </summary>
    public string ClockIdentifier
    {
        get => (string)GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    /// <summary>
    /// Gets or sets the minute increment for the time picker.
    /// </summary>
    public int MinuteIncrement
    {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text for the time picker.
    /// </summary>
    public string? PlaceholderText
    {
        get => (string?)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Identifies the MinuteIncrement dependency property.
    /// </summary>
    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(nameof(MinuteIncrement), typeof(int), typeof(TableViewTimeColumn), new PropertyMetadata(1));

    /// <summary>
    /// Identifies the ClockIdentifier dependency property.
    /// </summary>
    public static readonly DependencyProperty ClockIdentifierProperty = DependencyProperty.Register(nameof(ClockIdentifier), typeof(string), typeof(TableViewTimeColumn), new PropertyMetadata(default));

    /// <summary>
    /// Identifies the PlaceholderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(TableViewTimeColumn), new PropertyMetadata(null));


}
