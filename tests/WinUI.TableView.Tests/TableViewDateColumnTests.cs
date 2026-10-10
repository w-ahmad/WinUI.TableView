using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using WinUI.TableView.Controls;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewDateColumnTests
{
    [UITestMethod]
    public void TableViewDateColumn_GeneratesEditingElementWithConfiguredProperties()
    {
        var minDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var maxDate = new DateTimeOffset(2024, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var item = new ColumnTestItem { DueDate = new DateOnly(2024, 6, 15) };
        var column = new TableViewDateColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.DueDate)) },
            MinDate = minDate,
            MaxDate = maxDate,
            DateFormat = "month day",
            IsTodayHighlighted = false,
            IsGroupLabelVisible = false,
            IsOutOfScopeEnabled = false,
            DayOfWeekFormat = "{dayofweek.abbreviated(2)}"
        };

        var element = (TableViewDatePicker)column.GenerateEditingElement(new TableViewCell(), item);
        var cell = new TableViewCell { Content = element };
        element.SelectedDate = item.DueDate;

        Assert.AreEqual(minDate, element.MinDate);
        Assert.AreEqual(maxDate, element.MaxDate);
        Assert.AreEqual("month day", element.DateFormat);
        Assert.IsFalse(element.IsTodayHighlighted);
        Assert.IsFalse(element.IsGroupLabelVisible);
        Assert.IsFalse(element.IsOutOfScopeEnabled);
        Assert.AreEqual("{dayofweek.abbreviated(2)}", element.DayOfWeekFormat);
        Assert.AreEqual(TableViewLocalizedStrings.DatePickerPlaceholder, element.PlaceholderText);
        Assert.AreEqual(typeof(DateOnly), element.SourceType);
        Assert.AreEqual(item.DueDate, column.PrepareCellForEdit(cell, new RoutedEventArgs()));
    }

    [UITestMethod]
    public void TableViewDateColumn_GeneratesDisplayElement_WithFormatBindings()
    {
        var column = new TableViewDateColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.DueDate)) },
            DateFormat = "shortdate"
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), new ColumnTestItem());

        Assert.IsNotNull(element.GetBindingExpression(DateTimeFormatHelper.ValueProperty));
        Assert.IsNotNull(element.GetBindingExpression(DateTimeFormatHelper.FormatProperty));
        Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);
    }

    [UITestMethod]
    public void TableViewDateColumn_UsesCustomPlaceholderAndDateTimeOffsetFallback()
    {
        var column = new TableViewDateColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.UnknownDateLikeValue)) },
            PlaceHolderText = "Choose a date"
        };

        var element = (TableViewDatePicker)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());

        Assert.AreEqual("Choose a date", element.PlaceholderText);
        Assert.AreEqual(typeof(DateTimeOffset), element.SourceType);
    }
}
