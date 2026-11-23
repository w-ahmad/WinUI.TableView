using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests that the row headers share one width, which fits the widest header the table has shown, as the rows showing
/// them are reused for other items and as the template changes.
/// </summary>
[TestClass]
public class TableViewRowHeaderWidthTests
{
    private const string NarrowName = "A";
    private static readonly string WideName = new('W', 30);

    [UITestMethod]
    public async Task RowHeaderWidth_FitsTheWidestHeaderShown_AsRowsAreReused()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.RowHeaderTemplate = CreateTemplate("{Binding Name}");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var narrow = tableView.RowHeaderActualWidth;
        Assert.IsTrue(narrow > 0d, "the headers have no width");
        AssertHeadersShareTheWidth(tableView);

        // The first rows have short names; far down they are reused for items with long ones.
        await ScrollToAsync(tableView, 20_000d);
        await WaitForLayoutAsync(tableView);

        // The new width reaches the rows a pass later.
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        var wide = tableView.RowHeaderActualWidth;
        Assert.IsTrue(wide > narrow + 100d, $"the headers did not grow for the wider content ({narrow}px then {wide}px)");
        AssertHeadersShareTheWidth(tableView);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RowHeaderWidth_FollowsTheTemplate()
    {
        var tableView = CreateTableView(itemCount: 200);
        tableView.RowHeaderTemplate = CreateTemplate("{Binding Name}");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        var before = tableView.RowHeaderActualWidth;

        tableView.RowHeaderTemplate = (DataTemplate)XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='150' Height='10' /></DataTemplate>");
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(tableView.RowHeaderActualWidth >= 150d, $"the new template's 150px is not fitted ({before}px then {tableView.RowHeaderActualWidth}px)");
        AssertHeadersShareTheWidth(tableView);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RowHeaderWidth_GrowsAgain_WhenTheTemplateIsRemovedAndSetAgain()
    {
        const string wide = "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='150' Height='10' /></DataTemplate>";
        var tableView = CreateTableView(itemCount: 200);
        tableView.RowHeaderTemplate = (DataTemplate)XamlReader.Load(wide);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        tableView.RowHeaderTemplate = null;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);
        Assert.IsTrue(tableView.RowHeaderActualWidth < 150d, $"the headers kept the removed template's width ({tableView.RowHeaderActualWidth}px)");

        tableView.RowHeaderTemplate = (DataTemplate)XamlReader.Load(wide);
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(tableView.RowHeaderActualWidth >= 150d, $"the headers did not grow back ({tableView.RowHeaderActualWidth}px)");
        AssertHeadersShareTheWidth(tableView);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RowHeaderWidth_FollowsAFixedWidth()
    {
        var tableView = CreateTableView(itemCount: 200);
        tableView.RowHeaderTemplate = CreateTemplate("{Binding Name}");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        tableView.RowHeaderWidth = 120d;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        foreach (var row in RealizedRows(tableView))
        {
            Assert.AreEqual(120d, row.RowPresenter!.RowHeader!.ActualWidth, 1d, $"row {row.Index}'s header");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task RowHeaderWidth_ReachesRowsThatWerePooledWhenItChanged()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.Height = 800d;
        tableView.RowHeaderTemplate = CreateTemplate("{Binding Name}");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        var realizedWhenTall = RealizedRows(tableView).Count;

        // A shorter table shows fewer rows, and the rest wait in the repeater's pool while the width changes.
        tableView.Height = 150d;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);
        Assert.IsTrue(RealizedRows(tableView).Count < realizedWhenTall, "the shorter table should realize fewer rows");

        tableView.RowHeaderWidth = 120d;
        tableView.RowHeaderMinWidth = 30d;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        tableView.Height = 800d;
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);

        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Count >= realizedWhenTall, "the rows that were pooled should be realized again");

        foreach (var row in rows)
        {
            var header = row.RowPresenter!.RowHeader!;

            Assert.AreEqual(120d, header.ActualWidth, 1d, $"row {row.Index}'s header kept the width it had before");
            Assert.AreEqual(30d, header.MinWidth, $"row {row.Index}'s header kept its old minimum width");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static void AssertHeadersShareTheWidth(TableView tableView)
    {
        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Count > 0);

        // The headers are as wide as the table's shared width asks, or as the minimum width they have, whichever is more.
        var expected = System.Math.Max(tableView.RowHeaderActualWidth, tableView.RowHeaderMinWidth);

        foreach (var row in rows)
        {
            Assert.AreEqual(expected, row.RowPresenter!.RowHeader!.ActualWidth, 1d, $"row {row.Index}'s header is not as wide as the others");
        }
    }

    private static System.Collections.Generic.List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0)];
    }

    private static DataTemplate CreateTemplate(string textBinding)
    {
        return (DataTemplate)XamlReader.Load(
            $"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text=\"{textBinding}\" /></DataTemplate>");
    }

    private static TableView CreateTableView(int itemCount)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = 400,
            RowHeight = 40,
            AutoGenerateColumns = false
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        tableView.ItemsSource = new ObservableCollection<HostItem>(
            Enumerable.Range(0, itemCount).Select(i => new HostItem { Id = i, Name = i < 300 ? NarrowName : WideName, Category = "A" }));

        return tableView;
    }
}
