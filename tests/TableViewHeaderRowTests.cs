using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewHeaderRowTests
{
    [UITestMethod]
    public async Task HeaderClip_StaysAValidRect_WhenOffsetExceedsTheHeadersPanelExtent()
    {
        // Frozen columns wider than the viewport let HorizontalOffset run past the scrollable
        // headers panel's own extent, which is what drives the clip width below zero.
        var tableView = await CreateTableViewAsync(frozenColumnCount: 2);
        try
        {
            var panel = GetScrollableHeadersPanel(tableView);

            tableView.SetValue(TableView.HorizontalOffsetProperty, 5000d);
            tableView.UpdateLayout();

            Assert.IsTrue(tableView.HorizontalOffset > panel.ActualWidth,
                $"Precondition: the offset ({tableView.HorizontalOffset}) is past the panel's extent ({panel.ActualWidth})");

            var clip = panel.Clip;
            Assert.IsNotNull(clip, "A clip is applied once the headers are scrolled");
            Assert.IsTrue(clip!.Rect.Width >= 0,
                $"Header clip width must never be negative (was {clip.Rect.Width})");
            Assert.IsTrue(clip.Rect.X >= 0 && clip.Rect.X <= panel.ActualWidth,
                $"Header clip X must stay inside the panel's extent (was {clip.Rect.X} for a panel {panel.ActualWidth} wide)");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task HeaderClip_IsUnchanged_WhenScrolledWithinTheHeadersPanelExtent()
    {
        var tableView = await CreateTableViewAsync(frozenColumnCount: 0);
        var panel = GetScrollableHeadersPanel(tableView);

        const double offset = 60d;
        Assert.IsTrue(panel.ActualWidth > offset, "Precondition: the offset is inside the panel's extent");

        tableView.SetValue(TableView.HorizontalOffsetProperty, offset);
        tableView.UpdateLayout();

        var clip = panel.Clip;
        Assert.IsNotNull(clip, "A clip is applied once the headers are scrolled");
        Assert.AreEqual(offset, clip!.Rect.X, 0.01, "Clip X is the horizontal offset");
        Assert.AreEqual(panel.ActualWidth - offset, clip.Rect.Width, 0.01, "Clip width is the unscrolled remainder");
    }

    [UITestMethod]
    public async Task EmptyTable_HeaderOffsetResets_WhenHeadersVisibilityChanges()
    {
        var tableView = await CreateTableViewAsync(
            frozenColumnCount: 0,
            items: Array.Empty<HeaderRowTestItem>(),
            selectionMode: ListViewSelectionMode.Single);

        try
        {
            tableView.CellsHorizontalOffset = 42;

            tableView.HeadersVisibility = TableViewHeadersVisibility.Rows;

            Assert.AreEqual(0d, tableView.CellsHorizontalOffset,
                "An empty table without column headers or multiple selection must clear the stale header offset");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task EmptyTable_HeaderOffsetResets_WhenSelectionUnitChanges()
    {
        var tableView = await CreateTableViewAsync(
            frozenColumnCount: 0,
            items: Array.Empty<HeaderRowTestItem>(),
            selectionMode: ListViewSelectionMode.Multiple,
            selectionUnit: TableViewSelectionUnit.Cell,
            headersVisibility: TableViewHeadersVisibility.Rows);

        try
        {
            tableView.CellsHorizontalOffset = 42;

            tableView.SelectionUnit = TableViewSelectionUnit.CellOrRow;

            Assert.IsTrue(double.IsNaN(tableView.CellsHorizontalOffset),
                "An empty table in multiple-selection mode must use an unconstrained corner width");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task EmptyTable_HeaderOffsetResets_WhenLastItemIsRemoved()
    {
        var items = new ObservableCollection<HeaderRowTestItem>
        {
            new() { Name = "Alpha" }
        };
        var tableView = await CreateTableViewAsync(frozenColumnCount: 0, items: items);

        try
        {
            Assert.AreEqual(1, tableView.Items.Count, "Precondition: the table has one item");
            tableView.RowHeaderWidth = 72;
            tableView.RowHeaderMinWidth = 36;
            await Task.Yield();
            tableView.UpdateLayout();
            tableView.CellsHorizontalOffset = 42;

            items.RemoveAt(0);
            await Task.Yield();
            tableView.UpdateLayout();

            Assert.AreEqual(0, tableView.Items.Count, "Precondition: the table is empty");
            Assert.AreEqual(72d, tableView.CellsHorizontalOffset, 0.01,
                "Removing the last item must reset the header offset to the configured row-header width");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task EmptyTable_NaNHeaderOffsetDoesNotAffectFrozenColumnScrollBarSpace()
    {
        var tableView = await CreateTableViewAsync(
            frozenColumnCount: 1,
            items: Array.Empty<HeaderRowTestItem>(),
            selectionMode: ListViewSelectionMode.Multiple);

        try
        {
            tableView.HeadersVisibility = TableViewHeadersVisibility.Columns;
            tableView.HeadersVisibility = TableViewHeadersVisibility.All;

            Assert.IsTrue(double.IsNaN(tableView.CellsHorizontalOffset),
                "Precondition: an empty table with multiple selection uses an unconstrained corner width");
            var scrollViewer = tableView.FindDescendant<ScrollViewer>();
            Assert.IsNotNull(scrollViewer, "Precondition: the table's scroll viewer is in the visual tree");

            tableView.UpdateHorizontalScrollBarMargin();

            var expectedSpace = tableView.Columns.VisibleColumns
                .Where(column => column.IsFrozen)
                .Sum(column => column.ActualWidth);
            Assert.IsTrue(expectedSpace > 0, "Precondition: the frozen column has a measured width");
            Assert.AreEqual(expectedSpace, AttachedPropertiesHelper.GetFrozenColumnScrollBarSpace(scrollViewer!),
                0.01, "Scrollbar space must remain finite and include the frozen column width");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public void EmptyTable_HeaderOffsetUsesConfiguredRowHeaderWidth()
    {
        var tableView = new TableView
        {
            SelectionMode = ListViewSelectionMode.Single,
            HeadersVisibility = TableViewHeadersVisibility.All,
            RowHeaderMinWidth = 36
        };
        var headerRow = new TableViewHeaderRow { TableView = tableView };

        foreach (var (width, expectedOffset) in new[]
        {
            (double.NaN, 36d),
            (24d, 36d),
            (72d, 72d)
        })
        {
            tableView.RowHeaderWidth = width;
            tableView.CellsHorizontalOffset = 42;

            headerRow.SetCellsHorizontalOffset();

            Assert.AreEqual(expectedOffset, tableView.CellsHorizontalOffset, 0.01,
                $"The empty-table offset must honor RowHeaderWidth ({width}) and RowHeaderMinWidth");
        }
    }

    [UITestMethod]
    public void PopulatedTable_HeaderOffsetIsPreserved()
    {
        var tableView = new TableView
        {
            ItemsSource = new[] { new HeaderRowTestItem { Name = "Alpha" } }
        };
        var headerRow = new TableViewHeaderRow { TableView = tableView };
        Assert.AreEqual(1, tableView.Items.Count, "Precondition: the table has one item");
        tableView.CellsHorizontalOffset = 42;

        headerRow.SetCellsHorizontalOffset();

        Assert.AreEqual(42d, tableView.CellsHorizontalOffset,
            "A populated table must retain its row-measured offset");
    }

    [UITestMethod]
    public async Task SelectAllCheckBox_HasSymmetricHorizontalMargin()
    {
        var tableView = await CreateTableViewAsync(
            frozenColumnCount: 0,
            items: Array.Empty<HeaderRowTestItem>(),
            selectionMode: ListViewSelectionMode.Multiple);

        try
        {
            var checkBox = tableView.FindDescendants()
                .OfType<CheckBox>()
                .FirstOrDefault(element => element.Name is "selectAllCheckBox");
            Assert.IsNotNull(checkBox, "Precondition: the select-all checkbox is in the header template");
            Assert.AreEqual(checkBox!.Margin.Left, checkBox.Margin.Right, 0.01,
                "The select-all checkbox should have symmetric horizontal spacing");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    private static StackPanel GetScrollableHeadersPanel(TableView tableView)
    {
        var headerRow = tableView.FindDescendants().OfType<TableViewHeaderRow>().FirstOrDefault();
        Assert.IsNotNull(headerRow, "Precondition: the header row is in the visual tree");

        var panel = headerRow!.FindDescendant<StackPanel>(x => x.Name is "ScrollableHeadersPanel");
        Assert.IsNotNull(panel, "Precondition: the scrollable headers panel is in the visual tree");

        return panel!;
    }

    private static async Task<TableView> CreateTableViewAsync(int frozenColumnCount)
    {
        return await CreateTableViewAsync(frozenColumnCount, new[]
        {
            new HeaderRowTestItem { Name = "Alpha" },
            new HeaderRowTestItem { Name = "Beta" },
            new HeaderRowTestItem { Name = "Gamma" }
        });
    }

    private static async Task<TableView> CreateTableViewAsync(
        int frozenColumnCount,
        IEnumerable<HeaderRowTestItem> items,
        ListViewSelectionMode selectionMode = ListViewSelectionMode.Extended,
        TableViewSelectionUnit selectionUnit = TableViewSelectionUnit.CellOrRow,
        TableViewHeadersVisibility headersVisibility = TableViewHeadersVisibility.All)
    {
        var tableView = new TableView
        {
            Width = 250,
            Height = 300,
            AutoGenerateColumns = false,
            FrozenColumnCount = frozenColumnCount,
            SelectionMode = selectionMode,
            SelectionUnit = selectionUnit,
            HeadersVisibility = headersVisibility
        };

        for (var i = 0; i < 5; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(i < frozenColumnCount ? 200 : 60),
                Binding = new Binding { Path = new PropertyPath(nameof(HeaderRowTestItem.Name)) }
            });
        }

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    private sealed class HeaderRowTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
