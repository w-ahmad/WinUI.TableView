using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests that the table's FontFamily and FontSize reach rows, cells and headers by property inheritance alone. The
/// ListViewItem-based row needed a binding for each, because its base style set both.
/// </summary>
[TestClass]
public class TableViewFontInheritanceTests
{
    private const string TableFontFamily = "Courier New";
    private const double TableFontSize = 20d;

    [UITestMethod]
    public async Task RowsCellsAndHeaders_UseTheTableViewFont()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50, tableView =>
        {
            tableView.FontFamily = new FontFamily(TableFontFamily);
            tableView.FontSize = TableFontSize;
        });

        AssertRealizedRowsUseTableFont(tableView);

        var columnHeaders = tableView.FindDescendants().OfType<TableViewColumnHeader>().ToList();
        Assert.AreEqual(tableView.Columns.VisibleColumns.Count, columnHeaders.Count);

        foreach (var header in columnHeaders)
        {
            AssertUsesTableFont(header, "column header");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ChangingTheFont_ReachesRealizedAndNewlyRealizedRows()
    {
        var tableView = await CreateTableViewAsync(itemCount: 5_000);

        tableView.FontFamily = new FontFamily(TableFontFamily);
        tableView.FontSize = TableFontSize;
        await WaitForLayoutAsync(tableView);

        AssertRealizedRowsUseTableFont(tableView);

        // Scrolling far away realizes rows for new items, recycled from the pool or built fresh. Both kinds have to
        // pick up the font the table has now.
        await ScrollToAsync(tableView, 100_000d);

        Assert.IsTrue(RealizedRows(tableView).All(row => row.Index > 50), "the far rows should be realized");
        AssertRealizedRowsUseTableFont(tableView);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ALargerFont_GrowsTheRows_WithoutOverlap()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        var heightBefore = RealizedRows(tableView).Max(row => row.ActualHeight);

        tableView.FontSize = 40d;
        await WaitForLayoutAsync(tableView);

        var rows = RealizedRows(tableView).OrderBy(row => row.Index).ToList();
        Assert.IsTrue(rows.Count > 1);
        Assert.IsTrue(rows[0].ActualHeight > heightBefore,
            $"a 40px font should make the rows taller than {heightBefore}px, got {rows[0].ActualHeight}px");

        // The layout has to pick up the new heights, so each row starts where the one before it ends.
        for (var i = 1; i < rows.Count; i++)
        {
            var expectedTop = rows[i - 1].ActualOffset.Y + rows[i - 1].ActualHeight;
            Assert.AreEqual(expectedTop, rows[i].ActualOffset.Y, 1d, $"row {rows[i].Index} is not stacked under row {rows[i - 1].Index}");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static void AssertRealizedRowsUseTableFont(TableView tableView)
    {
        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Count > 0, "some rows should be realized");

        foreach (var row in rows)
        {
            AssertUsesTableFont(row, $"row {row.Index}");

            var rowHeader = row.FindDescendants().OfType<TableViewRowHeader>().FirstOrDefault();
            Assert.IsNotNull(rowHeader, $"row {row.Index} should have a row header");
            AssertUsesTableFont(rowHeader, $"row {row.Index}'s header");

            Assert.AreEqual(tableView.Columns.VisibleColumns.Count, row.Cells.Count);

            foreach (var cell in row.Cells)
            {
                AssertUsesTableFont(cell, $"row {row.Index}'s cell");

                // The text a cell shows is what the user actually sees in the font.
                var text = cell.FindDescendants().OfType<TextBlock>().FirstOrDefault();
                Assert.IsNotNull(text, $"row {row.Index}'s cell should show a TextBlock");
                Assert.AreEqual(TableFontFamily, text.FontFamily.Source, $"row {row.Index}'s cell text font family");
                Assert.AreEqual(TableFontSize, text.FontSize, $"row {row.Index}'s cell text font size");
            }
        }
    }

    private static void AssertUsesTableFont(Control control, string description)
    {
        Assert.AreEqual(TableFontFamily, control.FontFamily.Source, $"{description} font family");
        Assert.AreEqual(TableFontSize, control.FontSize, $"{description} font size");
    }

    private static List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0)];
    }

    private static async Task<TableView> CreateTableViewAsync(int itemCount, System.Action<TableView>? configure = null)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = 400,
            AutoGenerateColumns = false
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Name)) }
        });

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(HostItem.Category)) }
        });

        configure?.Invoke(tableView);

        var items = new ObservableCollection<HostItem>();

        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new HostItem { Id = i, Name = $"Item {i}", Category = ((char)('A' + (i % 3))).ToString() });
        }

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }
}
