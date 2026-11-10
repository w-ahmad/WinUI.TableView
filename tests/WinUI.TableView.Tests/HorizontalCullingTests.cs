using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Cells and headers scrolled out of the horizontal viewport are hidden from the renderer (opacity 0),
/// and layout is untouched. Uno's Skia renderer walks every visual each frame and does not skip clipped ones, so a
/// 177-column table cost 0.3-1.5 s per frame with every cell drawn.
/// </summary>
[TestClass]
public class HorizontalCullingTests
{
    private const int ColumnCount = 40;
    private const double ColumnWidth = 100;

    [TestMethod]
    public void IsInView_IsTheIntersectionWithTheViewportWidenedByTheMargin()
    {
        // (left, width, viewLeft, viewWidth, margin, expected, case)
        var cases = new (double Left, double Width, double ViewLeft, double ViewWidth, double Margin, bool Expected, string Case)[]
        {
            (0, 100, 0, 400, 0, true, "first column, at the left edge"),
            (399, 100, 0, 400, 0, true, "starts inside the right edge"),
            (401, 100, 0, 400, 0, false, "starts past the right edge"),
            (401, 100, 0, 400, 10, true, "past the right edge, within the margin"),
            (0, 100, 101, 400, 0, false, "scrolled past on the left"),
            (0, 100, 100, 400, 0, true, "its right edge touches the viewport"),
            (0, 100, 150, 400, 60, true, "scrolled past on the left, within the margin"),
            (1000, 5000, 2000, 400, 0, true, "wider than the viewport, spanning it"),
        };

        foreach (var c in cases)
        {
            Assert.AreEqual(c.Expected, HorizontalCulling.IsInView(c.Left, c.Width, c.ViewLeft, c.ViewWidth, c.Margin), c.Case);
        }
    }

    [UITestMethod]
    public async Task OffscreenHeadersAndCells_AreHidden_AndTheVisibleOnesAreNot()
    {
        var tableView = await CreateTableViewAsync();
        var (headers, cells) = Panels(tableView);

        AssertCulledExactlyOutsideTheViewport(tableView, headers, "headers");
        AssertCulledExactlyOutsideTheViewport(tableView, cells, "cells");
        Assert.IsFalse(HorizontalCulling.IsCulled(headers.Children[0]), "the first header is on screen");
        Assert.IsTrue(HorizontalCulling.IsCulled(headers.Children[ColumnCount - 1]), "the last header is far off screen");
        Assert.AreEqual(0d, headers.Children[ColumnCount - 1].Opacity, "a culled element is drawn at opacity 0");
    }

    [UITestMethod]
    public async Task Scrolling_ShowsWhatComesIntoView_AndHidesWhatLeaves()
    {
        var tableView = await CreateTableViewAsync();
        var (headers, cells) = Panels(tableView);
        var widthBefore = cells.ActualWidth;

        tableView.SetValue(TableView.HorizontalOffsetProperty, (ColumnCount - 4) * ColumnWidth);
        tableView.UpdateLayout();

        AssertCulledExactlyOutsideTheViewport(tableView, headers, "headers after the scroll");
        AssertCulledExactlyOutsideTheViewport(tableView, cells, "cells after the scroll");
        Assert.IsTrue(HorizontalCulling.IsCulled(cells.Children[0]), "the first cell has scrolled off");
        Assert.IsFalse(HorizontalCulling.IsCulled(cells.Children[ColumnCount - 1]), "the last cell has scrolled on");
        Assert.AreEqual(1d, cells.Children[ColumnCount - 1].Opacity, "a cell back in view gets its own opacity back");
        Assert.AreEqual(widthBefore, cells.ActualWidth, 0.01, "culling never changes layout");
    }

    [UITestMethod]
    public async Task WideningTheTable_ShowsTheColumnsThatComeIntoView()
    {
        var tableView = await CreateTableViewAsync();
        var (headers, cells) = Panels(tableView);
        Assert.IsTrue(HorizontalCulling.IsCulled(cells.Children[10]), "Precondition: column 10 is off screen at 400 DIPs");

        tableView.Width = 1600;
        tableView.UpdateLayout();

        Assert.IsFalse(HorizontalCulling.IsCulled(cells.Children[10]), "column 10 is on screen once the table is 1,600 DIPs wide");
        Assert.IsFalse(HorizontalCulling.IsCulled(headers.Children[10]), "its header too");
        AssertCulledExactlyOutsideTheViewport(tableView, headers, "headers after widening");
        AssertCulledExactlyOutsideTheViewport(tableView, cells, "cells after widening");
    }

    [UITestMethod]
    public async Task TurningCullingOff_ShowsEverything()
    {
        var tableView = await CreateTableViewAsync();
        var (headers, cells) = Panels(tableView);
        Assert.IsTrue(cells.Children.Any(HorizontalCulling.IsCulled), "Precondition: something is culled");

        tableView.CullsOffscreenColumns = false;
        tableView.UpdateLayout();

        Assert.IsFalse(headers.Children.Any(HorizontalCulling.IsCulled), "no header is culled with culling off");
        Assert.IsFalse(cells.Children.Any(HorizontalCulling.IsCulled), "no cell is culled with culling off");
        Assert.IsTrue(cells.Children.All(c => c.Opacity == 1d), "every cell is drawn with culling off");
    }

    private static void AssertCulledExactlyOutsideTheViewport(TableView tableView, StackPanel panel, string what)
    {
        var wrong = new List<string>();
        foreach (var child in panel.Children.OfType<FrameworkElement>())
        {
            var inView = HorizontalCulling.IsInView(child.ActualOffset.X, child.ActualWidth, tableView.HorizontalOffset, tableView.ActualWidth);
            if (inView == HorizontalCulling.IsCulled(child))
                wrong.Add($"{child.ActualOffset.X:F0}+{child.ActualWidth:F0} (in view: {inView})");
        }

        Assert.AreEqual(0, wrong.Count, $"{what} culled wrongly at offset {tableView.HorizontalOffset}: {string.Join("; ", wrong)}");
        Assert.IsTrue(panel.Children.Any(HorizontalCulling.IsCulled), $"Precondition: some {what} are off screen");
    }

    private static (StackPanel Headers, StackPanel Cells) Panels(TableView tableView)
    {
        var headers = tableView.FindDescendants().OfType<TableViewHeaderRow>().First()
            .FindDescendant<StackPanel>(x => x.Name is "ScrollableHeadersPanel");
        var cells = tableView.FindDescendants().OfType<TableViewRowPresenter>().First()
            .FindDescendant<StackPanel>(x => x.Name is "ScrollableCellsPanel");
        Assert.IsNotNull(headers, "Precondition: the scrollable headers panel is in the visual tree");
        Assert.IsNotNull(cells, "Precondition: a row's scrollable cells panel is in the visual tree");
        Assert.AreEqual(ColumnCount, headers!.Children.Count, "Precondition: one header per column");
        Assert.AreEqual(ColumnCount, cells!.Children.Count, "Precondition: one cell per column");
        return (headers, cells);
    }

    private static async Task<TableView> CreateTableViewAsync(System.Collections.IEnumerable? items = null)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
        };

        for (var i = 0; i < ColumnCount; i++)
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = $"Column {i}",
                Width = new GridLength(ColumnWidth),
                Binding = new Binding { Path = new PropertyPath(nameof(CullingTestItem.Name)) }
            });
        }

        tableView.ItemsSource = items ?? new[] { new CullingTestItem { Name = "Alpha" }, new CullingTestItem { Name = "Beta" } };
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();
        return tableView;
    }

    private sealed class CullingTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
