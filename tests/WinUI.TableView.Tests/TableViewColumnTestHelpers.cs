using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using System;

namespace WinUI.TableView.Tests;

internal static class TableViewColumnTestHelpers
{
    public static DataTemplate CreateTemplate(string text)
    {
        return (DataTemplate)XamlReader.Load(
            $"""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <TextBlock Text="{text}" />
            </DataTemplate>
            """);
    }
}

internal sealed class ColumnTestColumn : TableViewColumn
{
    public override FrameworkElement GenerateElement(TableViewCell cell, object? dataItem) => new TextBlock();

    public override FrameworkElement GenerateEditingElement(TableViewCell cell, object? dataItem) => new TextBox();
}

internal sealed class ConstantTemplateSelector(DataTemplate template) : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item) => template;
}

internal sealed class PrefixConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => $"{parameter}{value}";

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
}

internal sealed class ColumnTestItem
{
    public string? Name { get; set; }

    public string? ClipboardText { get; set; }

    public ColumnTestNestedItem? Nested { get; set; }

    public bool IsEnabled { get; set; }

    public ColumnTestOptionItem? SelectedOption { get; set; }

    public int SelectedOptionId { get; set; }

    public string? SelectedOptionText { get; set; }

    public DateOnly DueDate { get; set; }

    public TimeOnly AppointmentTime { get; set; }

    public double Amount { get; set; }

    public Uri? Link { get; set; }

    public object? UnknownDateLikeValue { get; set; }

    public object? UnknownTimeLikeValue { get; set; }
}

internal sealed class ColumnTestNestedItem
{
    public string? Value { get; set; }
}

internal sealed class ColumnTestOptionItem
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
