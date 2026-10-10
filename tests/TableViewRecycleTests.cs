using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
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
/// Tests what a row carries over when the row host reuses it for another item: nothing from the item it showed
/// before, and no side effects such as moving focus.
/// </summary>
[TestClass]
public class TableViewRecycleTests
{
    private const double ViewportHeight = 400d;

    [UITestMethod]
    public async Task RecycledRow_DoesNotKeepAStaleCurrentCellVisual()
    {
        var tableView = await CreateTableViewAsync(itemCount: 2_000);
        tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
        await WaitForLayoutAsync(tableView);

        // Row 0 goes to the pool, and the current cell moves somewhere no realized row shows.
        await ScrollToAsync(tableView, 20_000d);
        tableView.CurrentCellSlot = new TableViewCellSlot(1_999, 1);
        await WaitForLayoutAsync(tableView);

        await ScrollToAsync(tableView, 0d);

        foreach (var cell in RealizedRows(tableView).SelectMany(row => row.Cells))
        {
            var expected = cell.IsCurrent ? VisualStates.StateCurrent : VisualStates.StateRegular;
            Assert.AreEqual(expected, GetCurrentState(cell, VisualStates.GroupCurrent), $"cell {cell.Slot} shows a stale current state");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollingTheCurrentRowBackIn_DoesNotMoveFocus()
    {
        var button = new Button { Content = "Elsewhere" };
        var tableView = CreateTableView(itemCount: 2_000);
        var root = new StackPanel { Children = { button, tableView } };

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(root);
        await WaitForLayoutAsync(tableView);

        tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
        await WaitForLayoutAsync(tableView);
        await Task.Delay(50);

        button.Focus(FocusState.Programmatic);
        await ScrollToAsync(tableView, 20_000d);
        await ScrollToAsync(tableView, 0d);
        await Task.Delay(50);

        // Preparing row 0 again happens inside a layout pass; it must show the current cell, not focus it.
        Assert.AreSame(button, FocusManager.GetFocusedElement(root.XamlRoot), "scrolling the current row back in took focus");
        Assert.AreEqual(0d, GetScrollViewer(tableView).VerticalOffset, 1d);
        Assert.AreEqual(VisualStates.StateCurrent, GetCurrentState(GetCell(tableView, 0, 0), VisualStates.GroupCurrent));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(root);
    }

    [UITestMethod]
    public async Task RowHeaderTemplateSelector_IsReevaluated_ForReusedRows()
    {
        var even = CreateTemplate("Even");
        var odd = CreateTemplate("Odd");
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.RowHeaderTemplateSelector = new ParitySelector(even, odd);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        // A 40px row scrolled by an odd number of rows puts reused rows on items of the other parity.
        await ScrollToAsync(tableView, 41 * 40d);
        await ScrollToAsync(tableView, 4_000d + 40d);

        foreach (var row in RealizedRows(tableView))
        {
            var expected = ((HostItem)row.Content).Id % 2 == 0 ? even : odd;
            Assert.AreSame(expected, row.RowPresenter!.RowHeader!.ContentTemplate, $"row {row.Index} has the template of an earlier item");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectedRowScrolledIntoView_ShowsItsSelectionWithoutAnimating()
    {
        var tableView = await CreateTableViewAsync(itemCount: 2_000);
        tableView.SelectRange(new ItemIndexRange(100, 50));

        await ScrollToAsync(tableView, 100 * 40d);

        foreach (var row in RealizedRows(tableView).Where(row => row.IsSelected))
        {
            var indicator = row.FindDescendants().OfType<Border>().First(x => x.Name is "SelectionIndicator");
            var scale = (ScaleTransform)indicator.RenderTransform;

            Assert.AreEqual(VisualStates.StateSelected, GetCurrentState(row, VisualStates.GroupCommon));
            Assert.AreEqual(1d, scale.ScaleY, $"row {row.Index}'s selection indicator is still growing in");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectingARow_ShowsItsHighlightAtOnce_WhileTheIndicatorGrowsIn()
    {
        var tableView = await CreateTableViewAsync(itemCount: 50);
        tableView.SelectionMode = ListViewSelectionMode.Multiple;
        await WaitForLayoutAsync(tableView);

        tableView.SelectRange(new ItemIndexRange(2, 1));
        await Task.Delay(50);

        var row = RealizedRows(tableView).First(x => x.Index == 2);
        var check = row.FindDescendants().OfType<FontIcon>().First(x => x.Name == "MultiSelectCheck");
        var scale = (ScaleTransform)row.FindDescendants().OfType<Border>().First(x => x.Name is "SelectionIndicator").RenderTransform;

        Assert.IsTrue(scale.ScaleY < 1d, "the indicator should still be growing in");
        Assert.AreEqual(1d, check.Opacity, "the rest of the selection should not wait for the indicator");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollingAfterWarmUp_CreatesNoRows()
    {
        var tableView = await CreateTableViewAsync(itemCount: 5_000);

        // The first jumps fill the pool, including rows for the cache above and below.
        await ScrollToAsync(tableView, 40_000d);
        await ScrollToAsync(tableView, 80_000d);
        var created = tableView.RowElementFactory!.CreatedRowCount;

        for (var offset = 120_000d; offset <= 190_000d; offset += 10_000d)
        {
            await ScrollToAsync(tableView, offset);
        }

        Assert.AreEqual(created, tableView.RowElementFactory.CreatedRowCount, "scrolling built rows instead of reusing the pooled ones");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task TemplateCell_KeepsItsElement_WhenItsRowShowsAnotherItem()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.Columns.Add(new TableViewTemplateColumn { Header = "Template", CellTemplate = CreateTemplate("{Binding Name}") });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var elements = RealizedRows(tableView).ToDictionary(row => row, row => row.Cells[2].Content);

        await ScrollToAsync(tableView, 20_000d);

        var reused = RealizedRows(tableView).Where(elements.ContainsKey).ToList();
        Assert.IsTrue(reused.Count > 0, "scrolling should have reused rows");

        foreach (var row in reused)
        {
            var cell = row.Cells[2];
            Assert.AreSame(elements[row], cell.Content, $"row {row.Index}'s template cell built a new element");
            Assert.AreEqual(((HostItem)row.Content).Name, ((TextBlock)((ContentControl)cell.Content).ContentTemplateRoot).Text);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CellTemplateSelector_IsReevaluated_ForReusedRows()
    {
        var even = CreateTemplate("Even");
        var odd = CreateTemplate("Odd");
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.Columns.Add(new TableViewTemplateColumn { Header = "Template", CellTemplateSelector = new ParitySelector(even, odd) });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        await ScrollToAsync(tableView, 41 * 40d);
        await ScrollToAsync(tableView, 4_000d + 40d);

        foreach (var row in RealizedRows(tableView))
        {
            var expected = ((HostItem)row.Content).Id % 2 == 0 ? even : odd;
            Assert.AreSame(expected, ((ContentControl)row.Cells[2].Content).ContentTemplate, $"row {row.Index} has the cell template of an earlier item");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task MultiSelectCheck_IsShownAtOnce_OnReusedRows()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        tableView.SelectionMode = ListViewSelectionMode.Multiple;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        // The first thousand items are selected; scrolling to the boundary reuses rows on both sides of it.
        tableView.SelectRange(new ItemIndexRange(0, 1_000));
        await ScrollToAsync(tableView, 1_000 * 41d - 200d);

        var rows = RealizedRows(tableView);
        Assert.IsTrue(rows.Any(x => x.IsSelected) && rows.Any(x => !x.IsSelected), "rows on both sides of the selection should be realized");

        foreach (var row in rows)
        {
            var check = row.FindDescendants().OfType<FontIcon>().First(x => x.Name == "MultiSelectCheck");
            Assert.AreEqual(row.IsSelected ? 1d : 0d, check.Opacity, $"row {row.Index}");
        }

        Assert.IsFalse(rows.SelectMany(x => x.FindDescendants()).OfType<AnimatedIcon>().Any(), "a row should draw no animated icon");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CheckBoxCells_ShowTheirValueAtOnce_UnlessGivenAnElementStyle()
    {
        var tableView = CreateTableView(itemCount: 2_000);
        var column = new TableViewCheckBoxColumn { Header = "Checked", Binding = new Binding { Path = new PropertyPath(nameof(HostItem.IsChecked)) } };
        tableView.Columns.Add(column);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
        await ScrollToAsync(tableView, 20_000d);

        Assert.IsNotNull(column.DefaultElementStyle);

        foreach (var row in RealizedRows(tableView))
        {
            var checkBox = (CheckBox)row.Cells[2].Content;
            var glyph = checkBox.FindDescendants().OfType<FontIcon>().First(x => x.Name == "CheckGlyph");

            Assert.AreSame(column.DefaultElementStyle, checkBox.Style);
            Assert.AreEqual(((HostItem)row.Content).IsChecked ? 1d : 0d, glyph.Opacity, $"row {row.Index}");
        }

        Assert.IsFalse(RealizedRows(tableView).SelectMany(x => x.FindDescendants()).OfType<AnimatedIcon>().Any(), "a check box cell should draw no animated icon");

        var style = new Style(typeof(CheckBox));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(2)));
        column.ElementStyle = style;
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(RealizedRows(tableView).All(row => ReferenceEquals(((CheckBox)row.Cells[2].Content).Style, style)), "the column's own style should win");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static string? GetCurrentState(Control control, string groupName)
    {
        var root = (FrameworkElement)VisualTreeHelper.GetChild(control, 0);
        return VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(x => x.Name == groupName)?.CurrentState?.Name;
    }

    private static TableViewCell GetCell(TableView tableView, int row, int column)
    {
        return RealizedRows(tableView).First(x => x.Index == row).Cells[column];
    }

    private static DataTemplate CreateTemplate(string text)
    {
        return (DataTemplate)XamlReader.Load(
            $"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text=\"{text}\" /></DataTemplate>");
    }

    private static List<TableViewRow> RealizedRows(TableView tableView)
    {
        return [.. tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.Index >= 0)];
    }

    private static async Task<TableView> CreateTableViewAsync(int itemCount)
    {
        var tableView = CreateTableView(itemCount);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    private static TableView CreateTableView(int itemCount)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = ViewportHeight,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended
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

        tableView.ItemsSource = new ObservableCollection<HostItem>(
            Enumerable.Range(0, itemCount).Select(i => new HostItem { Id = i, Name = $"Item {i}", Category = "A", IsChecked = i % 3 is 0 }));

        return tableView;
    }

    private sealed partial class ParitySelector(DataTemplate even, DataTemplate odd) : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item)
        {
            return item is HostItem { Id: var id } && id % 2 == 1 ? odd : even;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}
