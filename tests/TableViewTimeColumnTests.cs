using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using Windows.Globalization.DateTimeFormatting;
using WinUI.TableView.Controls;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewTimeColumnTests
{
    [UITestMethod]
    public void TableViewTimeColumn_GeneratesEditingElementWithConfiguredProperties()
    {
        var item = new ColumnTestItem { AppointmentTime = new TimeOnly(9, 30) };
        var column = new TableViewTimeColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.AppointmentTime)) },
            ClockIdentifier = "24HourClock",
            MinuteIncrement = 15
        };

        var element = (TableViewTimePicker)column.GenerateEditingElement(new TableViewCell(), item);
        var cell = new TableViewCell { Content = element };
        element.SelectedTime = item.AppointmentTime;

        Assert.AreEqual("24HourClock", element.ClockIdentifier);
        Assert.AreEqual(15, element.MinuteIncrement);
        Assert.AreEqual(TableViewLocalizedStrings.TimePickerPlaceholder, element.PlaceholderText);
        Assert.AreEqual(typeof(TimeOnly), element.SourceType);
        Assert.AreEqual(item.AppointmentTime, column.PrepareCellForEdit(cell, new RoutedEventArgs()));
        Assert.AreEqual(DateTimeFormatter.LongTime.Clock, new TableViewTimeColumn().ClockIdentifier);
    }

    [UITestMethod]
    public void TableViewTimeColumn_GeneratesDisplayElement_WithFormatBindings()
    {
        var column = new TableViewTimeColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.AppointmentTime)) }
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), new ColumnTestItem());

        Assert.IsNotNull(element.GetBindingExpression(DateTimeFormatHelper.ValueProperty));
        Assert.IsNotNull(element.GetBindingExpression(DateTimeFormatHelper.FormatProperty));
        Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);
    }

    [UITestMethod]
    public void TableViewTimeColumn_UsesCustomPlaceholderAndTimeSpanFallback()
    {
        var column = new TableViewTimeColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.UnknownTimeLikeValue)) },
            PlaceholderText = "Pick a time"
        };

        var element = (TableViewTimePicker)column.GenerateEditingElement(new TableViewCell(), new ColumnTestItem());

        Assert.AreEqual("Pick a time", element.PlaceholderText);
        Assert.AreEqual(typeof(TimeSpan), element.SourceType);
    }
}
