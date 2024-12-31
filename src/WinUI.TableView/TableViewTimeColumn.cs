using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Windows.Globalization.DateTimeFormatting;
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView;

public partial class TableViewTimeColumn : TableViewBoundColumn
{
    public TableViewTimeColumn()
    {
        ClockIdentifier = DateTimeFormatter.ShortTime.Clock;
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
            Path = new PropertyPath(nameof(ClockIdentifier)),
            Source = this
        });

        return textBlock;
    }

    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem)
    {
        var timePicker = new TableViewTimePicker
        {
            ClockIdentifier = ClockIdentifier,
            MinuteIncrement = MinuteIncrement,
            SourceType = GetSourcePropertyType(dataItem),
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        timePicker.SetBinding(TableViewTimePicker.SelectedTimeProperty, Binding);

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

            if (type.IsTimeSpan() || type.IsTimeOnly() || type.IsDateTime() || type.IsDateTimeOffset())
            {
                return type;
            }
        }

        return default;
    }

    public string ClockIdentifier
    {
        get => (string)GetValue(ClockIdentifierProperty);
        set => SetValue(ClockIdentifierProperty, value);
    }

    public int MinuteIncrement
    {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(nameof(MinuteIncrement), typeof(int), typeof(TableViewTimeColumn), new PropertyMetadata(1));
    public static readonly DependencyProperty ClockIdentifierProperty = DependencyProperty.Register(nameof(ClockIdentifier), typeof(string), typeof(TableViewTimeColumn), new PropertyMetadata(default));
}
