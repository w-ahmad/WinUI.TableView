using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Globalization.DateTimeFormatting;
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using DayOfWeek = Windows.Globalization.DayOfWeek;

namespace WinUI.TableView;

public partial class TableViewDateColumn : TableViewBoundColumn
{
    public TableViewDateColumn()
    {
        DateFormat = DateTimeFormatter.ShortDate.Patterns[0];
    }

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
            PlaceholderText = PlaceHolderText,
            DayOfWeekFormat = DayOfWeekFormat,
            FirstDayOfWeek = FirstDayOfWeek,
            SourceType = GetSourcePropertyType(dataItem),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        timePicker.SetBinding(TableViewDatePicker.SelectedDateProperty, Binding);

        return timePicker;
    }

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

        return default;
    }

    public string? DateFormat
    {
        get => (string?)GetValue(DateFormatProperty);
        set => SetValue(DateFormatProperty, value);
    }

    public DateTimeOffset MinDate
    {
        get => (DateTimeOffset)GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    public DateTimeOffset MaxDate
    {
        get => (DateTimeOffset)GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
    }

    public bool IsTodayHighlighted
    {
        get => (bool)GetValue(IsTodayHighlightedProperty);
        set => SetValue(IsTodayHighlightedProperty, value);
    }

    public bool IsOutOfScopeEnabled
    {
        get => (bool)GetValue(IsOutOfScopeEnabledProperty);
        set => SetValue(IsOutOfScopeEnabledProperty, value);
    }

    public bool IsGroupLabelVisible
    {
        get => (bool)GetValue(IsGroupLabelVisibleProperty);
        set => SetValue(IsGroupLabelVisibleProperty, value);
    }

    public string PlaceHolderText
    {
        get => (string)GetValue(PlaceHolderTextProperty);
        set => SetValue(PlaceHolderTextProperty, value);
    }

    public string DayOfWeekFormat
    {
        get => (string)GetValue(DayOfWeekFormatProperty);
        set => SetValue(DayOfWeekFormatProperty, value);
    }

    public DayOfWeek FirstDayOfWeek
    {
        get => (DayOfWeek)GetValue(FirstDayOfWeekProperty);
        set => SetValue(FirstDayOfWeekProperty, value);
    }

    public static readonly DependencyProperty FirstDayOfWeekProperty = DependencyProperty.Register(nameof(FirstDayOfWeek), typeof(DayOfWeek), typeof(TableViewDateColumn), new PropertyMetadata(DayOfWeek.Sunday));
    public static readonly DependencyProperty DayOfWeekFormatProperty = DependencyProperty.Register(nameof(DayOfWeekFormat), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty PlaceHolderTextProperty = DependencyProperty.Register(nameof(PlaceHolderText), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata("pick a date"));
    public static readonly DependencyProperty IsGroupLabelVisibleProperty = DependencyProperty.Register(nameof(IsGroupLabelVisible), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));
    public static readonly DependencyProperty IsOutOfScopeEnabledProperty = DependencyProperty.Register(nameof(IsOutOfScopeEnabled), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));
    public static readonly DependencyProperty IsTodayHighlightedProperty = DependencyProperty.Register(nameof(IsTodayHighlighted), typeof(bool), typeof(TableViewDateColumn), new PropertyMetadata(true));
    public static readonly DependencyProperty MinDateProperty = DependencyProperty.Register(nameof(MinDate), typeof(DateTimeOffset), typeof(TableViewDateColumn), new PropertyMetadata(DateTimeOffset.MinValue));
    public static readonly DependencyProperty MaxDateProperty = DependencyProperty.Register(nameof(MaxDate), typeof(DateTimeOffset), typeof(TableViewDateColumn), new PropertyMetadata(DateTimeOffset.MaxValue));
    public static readonly DependencyProperty DateFormatProperty = DependencyProperty.Register(nameof(DateFormat), typeof(string), typeof(TableViewDateColumn), new PropertyMetadata(default));
}
