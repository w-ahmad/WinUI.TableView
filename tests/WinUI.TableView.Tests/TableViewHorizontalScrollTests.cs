using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Horizontal scrolling goes through <see cref="TableView.HorizontalOffset"/>, so rows, headers and the scrollbar move together.
/// </summary>
[TestClass]
public class TableViewHorizontalScrollTests
{
    private TableView? _tableView;

    [TestCleanup]
    public async Task CleanupAsync()
    {
        if (_tableView is not null)
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(_tableView);
        }
    }

    [UITestMethod]
    public async Task ScrollViewer_DoesNotPanHorizontallyByItself()
    {
        var tableView = await CreateTableViewAsync();
        var scrollViewer = tableView.FindDescendant<ScrollViewer>()!;

        Assert.AreEqual(ScrollMode.Disabled, scrollViewer.HorizontalScrollMode,
            "Native horizontal panning would move the rows without the headers");
        Assert.IsTrue(scrollViewer.ScrollableWidth > 0, "The scrollable width must still reflect the wide columns");
    }

    [UITestMethod]
    public async Task HorizontalOffset_MovesHeadersWithCells()
    {
        var tableView = await CreateTableViewAsync();

        tableView.SetValue(TableView.HorizontalOffsetProperty, 100d);
        await WaitForRenderedFramesAsync(10);

        AssertHeadersLineUpWithCells(tableView);
        Assert.AreEqual(0d, tableView.FindDescendant<ScrollViewer>()!.HorizontalOffset, 0.01,
            "The scroll viewer itself stays at zero; the panels move by HorizontalOffset");
    }

    [UITestMethod]
    public async Task ResizingAColumn_WhileScrolled_KeepsHeadersAlignedWithCells()
    {
        var tableView = await CreateTableViewAsync();
        tableView.SetValue(TableView.HorizontalOffsetProperty, 100d);
        await WaitForRenderedFramesAsync(10);

        tableView.Columns[0].Width = new GridLength(260);
        await WaitForRenderedFramesAsync(10);

        AssertHeadersLineUpWithCells(tableView);
    }

#if !WINDOWS
    // WinUI shows the indicator from its own input handling; Uno needs TableView to drive it.
    [UITestMethod]
    public async Task HorizontalOffset_ShowsTheScrollIndicator_WithoutVerticalOverflow()
    {
        var tableView = await CreateTableViewAsync();
        var scrollViewer = tableView.FindDescendant<ScrollViewer>()!;
        Assert.AreEqual(0d, scrollViewer.ScrollableHeight, 0.01, "Precondition: the rows fit vertically");
        Assert.AreEqual(Visibility.Visible, scrollViewer.ComputedHorizontalScrollBarVisibility, "Precondition: the columns overflow");

        tableView.SetValue(TableView.HorizontalOffsetProperty, 50d);
        await WaitForRenderedFramesAsync(5);

        Assert.AreEqual("MouseIndicator", GetIndicatorState(scrollViewer),
            "Scrolling must show the horizontal scrollbar even when there is no vertical scrollbar");
    }

    [UITestMethod]
    public async Task ScrollViewer_HandlesHorizontalTouchPanningOnUno()
    {
        var tableView = await CreateTableViewAsync();
        var cell = ((TableViewRow)tableView.ContainerFromIndex(0)!).Cells[0];

        Assert.AreEqual(ManipulationModes.System, cell.ManipulationMode,
            "A cell requesting X and Y manipulation cancels the scroll viewer's vertical panning on Uno");
        Assert.AreEqual(ManipulationModes.TranslateX | ManipulationModes.TranslateInertia,
            tableView.FindDescendant<ScrollViewer>()!.ManipulationMode,
            "The scroll viewer handles horizontal touch panning itself");
    }
#endif

    private static string? GetIndicatorState(ScrollViewer scrollViewer)
    {
        var root = (FrameworkElement)VisualTreeHelper.GetChild(scrollViewer, 0);
        return VisualStateManager.GetVisualStateGroups(root)
            .FirstOrDefault(g => g.Name == "ScrollingIndicatorStates")?
            .CurrentState?.Name;
    }

    private static void AssertHeadersLineUpWithCells(TableView tableView)
    {
        var row = tableView.ContainerFromIndex(0) as TableViewRow;
        Assert.IsNotNull(row, "Precondition: a row is realized");

        foreach (var cell in row!.Cells)
        {
            var header = cell.Column!.HeaderControl!;

            Assert.AreEqual(GetLeft(cell, tableView), GetLeft(header, tableView), 1d,
                $"Header '{cell.Column.Header}' must start where its cells start");
        }
    }

    private static double GetLeft(FrameworkElement element, TableView tableView)
    {
        return element.TransformToVisual(tableView).TransformPoint(default).X;
    }

    private async Task<TableView> CreateTableViewAsync()
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
            ItemsSource = Enumerable.Range(0, 3).Select(i => new Item { Name = $"Item {i}" }).ToList()
        };

        foreach (var header in new[] { "A", "B", "C", "D", "E" })
        {
            tableView.Columns.Add(new TableViewTextColumn
            {
                Header = header,
                Width = new GridLength(150),
                Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) }
            });
        }

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForRenderedFramesAsync(10);

        return tableView;
    }

    private static async Task WaitForRenderedFramesAsync(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var taskCompletionSource = new TaskCompletionSource<object?>();

            void Callback(object? sender, object args)
            {
                CompositionTarget.Rendering -= Callback;
                taskCompletionSource.SetResult(null);
            }

            CompositionTarget.Rendering += Callback;
            await taskCompletionSource.Task;
        }
    }

    private sealed class Item
    {
        public string Name { get; set; } = string.Empty;
    }
}
