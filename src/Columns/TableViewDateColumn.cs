using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace WinUI.TableView;

/// <summary>
/// Represents a column in a TableView that displays a date.
/// </summary>
[StyleTypedProperty(Property = nameof(ElementStyle), StyleTargetType = typeof(TextBlock))]
[StyleTypedProperty(Property = nameof(EditingElementStyle), StyleTargetType = typeof(TableViewDatePicker))]
#if WINDOWS
[WinRT.GeneratedBindableCustomProperty]
#endif
public partial class TableViewDateColumn : TableViewBoundColumn
{
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
            Path = new PropertyPath(nameof(DateFormat)),
            Source = this
        });

        return textBlock;
    }

    /// <summary>
    /// Generates a TableViewDatePicker element for editing the cell.
    /// </summary>
    /// <param name="cell">The cell for which the editing element is generated.</param>
    /// <param name="dataItem">The data item associated with the cell.</param>
    /// <returns>A TableViewDatePicker element.</returns>
    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        var timePicker = new TableViewDatePicker
        {
            MinDate = MinDate,
            MaxDate = MaxDate,
            DateFormat = DateFormat,
            IsTodayHighlighted = IsTodayHighlighted,
            IsGroupLabelVisible = IsGroupLabelVisible,
            IsOutOfScopeEnabled = IsOutOfScopeEnabled,
            PlaceholderText = PlaceHolderText ?? TableViewLocalizedStrings.DatePickerPlaceholder,
            DayOfWeekFormat = DayOfWeekFormat,
            FirstDayOfWeek = FirstDayOfWeek,
            SourceType = GetSourcePropertyType(dataItem),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        timePicker.SetBinding(TableViewDatePicker.SelectedDateProperty, Binding);

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

            if (type.IsDateOnly() || type.IsDateTime() || type.IsDateTimeOffset())
            {
                return type;
            }
        }

        return typeof(DateTimeOffset);
    }

    /// <summary>
    /// Gets or sets the date format for the column.
    /// </summary>
    public string? DateFormat
    {
        get => (string?)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum date for the date picker.
    /// </summary>
    public DateTimeOffset MinDate
    {
        get => (DateTimeOffset)GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum date for the date picker.
    /// </summary>
    public DateTimeOffset MaxDate
    {
        get => (DateTimeOffset)GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether today's date is highlighted in date picker.
    /// </summary>
    public bool IsTodayHighlighted
    {
        get => (bool)GetValue(IsTodayHighlightedProperty);
        set => SetValue(IsTodayHighlightedProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether out-of-scope dates are enabled in date picker.
    /// </summary>
    public bool IsOutOfScopeEnabled
    {
        get => (bool)GetValue(IsOutOfScopeEnabledProperty);
        set => SetValue(IsOutOfScopeEnabledProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the group label is visible in date picker.
    /// </summary>
    public bool IsGroupLabelVisible
    {
        get => (bool)GetValue(IsGroupLabelVisibleProperty);
        set => SetValue(IsGroupLabelVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text for the date picker in date picker.
    /// </summary>
    public string? PlaceHolderText
    {
        get => (string?)GetValue(PlaceHolderTextProperty);
        set => SetValue(PlaceHolderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the format for the day of the week in date picker.
    /// </summary>
    public string DayOfWeekFormat
    {
        get => (string)GetValue(DayOfWeekFormatProperty);
        set => SetValue(DayOfWeekFormatProperty, value);
    }

    /// <summary>
    /// Gets or sets the first day of the week in date picker.
    /// </summary>
    public DayOfWeek FirstDayOfWeek
    {
        get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    /// <summary>
    /// Identifies the FirstDayOfWeek dependency property.
    /// </summary>
    public static readonly DependencyProperty FirstDayOfWeekProperty = DependencyProperty.Register(nameof(FirstDayOfWeek), typeof(DayOfWeek), typeof(TableViewDateColumn), new PropertyMetadata(DayOfWeek.Sunday));

    /// <summary>
    /// Identifies the DayOfWeekFormat dependency property.
    /// </summary>
    public static readonly DependencyProperty DayOfWeekFormatProperty = DependencyProperty.Register(nameof(DayOfWeekFormat), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata(string.Empty));

    /// <summary>
    /// Identifies the PlaceHolderText dependency property.
    /// </summary>
    public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.Register(nameof(PlaceHolderText), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata(null));

    /// <summary>
    /// Identifies the IsGroupLabelVisible dependency property.
    /// </summary>
    public static readonly DependencyProperty IsGroupLabelVisibleProperty = DependencyProperty.Register(nameof(IsGroupLabelVisible), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the IsOutOfScopeEnabled dependency property.
    /// </summary>
    public static readonly DependencyProperty IsOutOfScopeEnabledProperty = DependencyProperty.Register(nameof(IsOutOfScopeEnabled), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the IsTodayHighlighted dependency property.
    /// </summary>
    public static readonly DependencyProperty IsTodayHighlightedProperty = DependencyProperty.Register(nameof(IsTodayHighlighted), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));

    /// <summary>
    /// Identifies the MinDate dependency property.
    /// </summary>
    public static readonly DependencyProperty MinDateProperty = DependencyProperty.Register(nameof(MinDate), typeof(DateTimeOffset), typeof(TableViewDateColumn), new PropertyMetadata(DateTimeOffset.MinValue));

    /// <summary>
    /// Identifies the MaxDate dependency property.
    /// </summary>
    public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(nameof(MaxDate), typeof(DateTimeOffset), typeof(TableViewDateColumn), new PropertyMetadata(DateTimeOffset.MaxValue));

    /// <summary>
    /// Identifies the DateFormat dependency property.
    /// </summary>
    public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(nameof(DateFormat), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata("shortdate"));
}
