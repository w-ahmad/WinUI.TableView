using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Linq;
using System.Threading.Tasks;
#if WINDOWS
using Windows.System.UserProfile;
#endif
using WinUI.TableView.Controls;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

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
#if WINDOWS
        var clocks = GlobalizationPreferences.Clocks;
        Assert.AreEqual(clocks.Count > 0 ? clocks[0] : "24HourClock", new TableViewTimeColumn().ClockIdentifier);
#endif
    }

    [UITestMethod]
    public void TableViewTimeColumn_GeneratesDisplayElement_WithValueBindingAndFormat()
    {
        var column = new TableViewTimeColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.AppointmentTime)) },
            ClockIdentifier = "24HourClock"
        };

        var element = (TextBlock)column.GenerateElement(new TableViewCell(), new ColumnTestItem());

        Assert.IsNotNull(element.GetBindingExpression(DateTimeFormatHelper.ValueProperty));
        Assert.AreEqual("24HourClock", DateTimeFormatHelper.GetFormat(element));
        Assert.AreEqual(new Thickness(12, 0, 12, 0), element.Margin);
    }

    [UITestMethod]
    public async Task TableViewTimeColumn_ClockIdentifierChange_UpdatesRealizedAndRecycledCells()
    {
        var time = new TimeOnly(15, 30);
        var column = new TableViewTimeColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.AppointmentTime)) },
            ClockIdentifier = "12HourClock"
        };
        var tableView = new TableView { Width = 400, Height = 300, AutoGenerateColumns = false };
        tableView.Columns.Add(column);
        tableView.ItemsSource = Enumerable.Range(0, 500).Select(_ => new ColumnTestItem { AppointmentTime = time }).ToList();

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var expected = DateTimeFormatHelper.GetDateTimeFormatter("shorttime", "24HourClock").Format(time.ToDateTimeOffset());
        Assert.AreNotEqual(expected, GetDisplayElements(tableView, column).First().Text);

        column.ClockIdentifier = "24HourClock";
        AssertDisplayElements();

        await ScrollToAsync(tableView, 10_000d);
        AssertDisplayElements();

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);

        void AssertDisplayElements()
        {
            var elements = GetDisplayElements(tableView, column);
            Assert.IsTrue(elements.Count > 1);

            foreach (var element in elements)
            {
                Assert.AreEqual("24HourClock", DateTimeFormatHelper.GetFormat(element));
                Assert.AreEqual(expected, element.Text);
            }
        }
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
