using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewLifecycleTests
{
    // Content of a ContentDialog gets an extra Unloaded after its Loaded while it is still in the live tree
    // (microsoft-ui-xaml#8342); that must not suspend the items source (#461).
    [UITestMethod]
    public async Task TableView_InContentDialog_KeepsItems()
    {
        var root = new Grid();
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(root);

        var items = CreateItems();
        var tableView = CreateTableView(items);
        var dialog = new ContentDialog { Content = tableView, XamlRoot = root.XamlRoot, CloseButtonText = "Close" };

        var loaded = new TaskCompletionSource<object?>();
        var unloadedCount = 0;
        tableView.Loaded += (_, _) => loaded.TrySetResult(null);
        tableView.Unloaded += (_, _) => unloadedCount++;

        var closed = new TaskCompletionSource<object?>();
        dialog.Closed += (_, _) => closed.TrySetResult(null);

        try
        {
            _ = dialog.ShowAsync();

            Assert.AreSame(loaded.Task, await Task.WhenAny(loaded.Task, Task.Delay(5000)),
                "Precondition: the table is loaded once the dialog opens");

            // Give a stale Unloaded time to arrive.
            await WaitForRenderedFramesAsync(10);

            Assert.IsTrue(tableView.IsLoaded, "Precondition: the table is still in the live tree");
            Assert.AreEqual(items.Count, tableView.Items.Count,
                $"A table shown in a ContentDialog must keep its items (Unloaded was raised {unloadedCount} time(s))");
            Assert.IsInstanceOfType<TableViewRow>(tableView.ContainerFromIndex(0),
                "A table shown in a ContentDialog must realize its rows");
        }
        finally
        {
            dialog.Hide();
            await Task.WhenAny(closed.Task, Task.Delay(5000));
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(root);
        }
    }

    [UITestMethod]
    public async Task TableView_UnloadAndReload_DetachesFromSourceAndResyncs()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        Assert.AreEqual(items.Count, tableView.Items.Count, "Precondition: the loaded table shows its items");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        items.Add(new LifecycleTestItem { Name = "Added while unloaded" });
        Assert.AreEqual(items.Count - 1, tableView.Items.Count, "An unloaded table must not listen to its items source");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            Assert.AreEqual(items.Count, tableView.Items.Count, "A reloaded table must pick up changes made while it was unloaded");

            items.Add(new LifecycleTestItem { Name = "Added after reload" });
            Assert.AreEqual(items.Count, tableView.Items.Count, "A reloaded table must listen to its items source again");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    // Tables hosted in TabViewItems are unloaded on every tab switch; that must not clear the selection (#469).
    [UITestMethod]
    public async Task TableView_UnloadAndReload_KeepsSelection()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.SelectedItem = items[3];

        var selectionChangedCount = 0;
        tableView.SelectionChanged += (_, _) => selectionChangedCount++;

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        Assert.AreSame(items[3], tableView.SelectedItem, "An unloaded table must keep its selection");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            Assert.AreSame(items[3], tableView.SelectedItem, "A reloaded table must keep its selection");
            Assert.AreEqual(0, selectionChangedCount, "Unloading and reloading must not change the selection");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task TableView_SourceChangedWhileUnloaded_RestoresSelectionOnReload()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);
        tableView.SelectionMode = ListViewSelectionMode.Extended;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        var removed = items[4];
        var expected = new object[] { items[3], items[5] };

        foreach (var item in items.Skip(3).Take(3).ToList())
        {
            tableView.SelectedItems.Add(item);
        }

        Assert.AreEqual(3, tableView.SelectedItems.Count, "Precondition: three rows are selected");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        items.Remove(removed);
        items.Insert(0, new LifecycleTestItem { Name = "Added while unloaded" });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            CollectionAssert.AreEquivalent(expected, tableView.SelectedItems.ToArray(),
                "A reloaded table must reselect the items that are still in its source");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    // A sort key changed while the table is unloaded resets the view on reload; rows detached by that reset
    // must not touch their table afterwards (#469).
    [UITestMethod]
    public async Task TableView_SortKeyChangedWhileUnloaded_ResortsAndKeepsSelectionOnReload()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);
        tableView.SortDescriptions.Add(new SortDescription(nameof(LifecycleTestItem.Name), SortDirection.Ascending));

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.SelectedItem = items[3];
        await WaitForRenderedFramesAsync(5);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        items[3].Name = "Item 8a";

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            await WaitForRenderedFramesAsync(10);

            Assert.AreSame(items[3], tableView.Items[8], "A reloaded table must apply its sorting to changed items");
            Assert.AreSame(items[3], tableView.SelectedItem, "A reloaded table must keep its selection");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    // Rows are hidden while the table is unloaded so that it re-enters the tree quickly; they must come
    // back, at the scroll position the table was left at.
    [UITestMethod]
    public async Task TableView_UnloadAndReload_RestoresRowsAndScrollPosition()
    {
        var items = new ObservableCollection<LifecycleTestItem>(
            Enumerable.Range(0, 200).Select(i => new LifecycleTestItem { Name = $"Item {i}" }));
        var tableView = CreateTableView(items);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        var scrollViewer = tableView.FindDescendant<ScrollViewer>();
        Assert.IsNotNull(scrollViewer);

        scrollViewer!.ChangeView(null, 1000d, null, true);
        await WaitForRenderedFramesAsync(10);
        var offset = scrollViewer.VerticalOffset;
        Assert.IsTrue(offset > 0, "Precondition: the table is scrolled down");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            await WaitForRenderedFramesAsync(20);

            Assert.AreEqual(Visibility.Visible, tableView.ItemsPanelRoot.Visibility, "A reloaded table must show its rows again");
            Assert.AreEqual(offset, scrollViewer.VerticalOffset, 1d, "A reloaded table must keep its scroll position");

            var realizedRows = tableView.FindDescendants().OfType<TableViewRow>().Where(row => row.ActualHeight > 0).ToList();
            Assert.IsTrue(realizedRows.Count > 0, "A reloaded table must lay out its rows");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    // Rows are kept while the table is unloaded, when nothing listens to their items; conditional cell styles
    // have to be evaluated again on reload even if the view itself did not change.
    [UITestMethod]
    public async Task TableView_ItemChangedWhileUnloaded_RefreshesConditionalCellStylesOnReload()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);
        var style = new Style(typeof(TableViewCell));
        tableView.ConditionalCellStyles.Add(new TableViewConditionalCellStyle
        {
            Style = style,
            Predicate = context => context.DataItem is LifecycleTestItem { Name: "Changed" }
        });

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForRenderedFramesAsync(5);

        var cell = (tableView.ContainerFromIndex(2) as TableViewRow)?.FindDescendant<TableViewCell>();
        Assert.IsNotNull(cell, "Precondition: the row's cell is realized");
        Assert.AreNotSame(style, cell!.Style, "Precondition: the conditional style does not apply yet");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        items[2].Name = "Changed";

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            await WaitForRenderedFramesAsync(10);

            cell = (tableView.ContainerFromIndex(2) as TableViewRow)?.FindDescendant<TableViewCell>();
            Assert.IsNotNull(cell, "Precondition: the row's cell is realized");
            Assert.AreSame(style, cell!.Style, "A reloaded table must re-evaluate its conditional cell styles");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    // A height change makes the current row scroll itself into view, which is awaited. The row can be
    // detached from its table before that completes, and must not use the table afterwards (#469, a guard
    // lost in #455).
    [UITestMethod]
    public async Task Row_DetachedWhileScrollingItselfIntoView_DoesNotThrow()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);
        Exception? unhandled = null;

        // The row's size handler runs outside the test, so what it throws only shows up as an unhandled exception.
        void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            unhandled ??= e.Exception;
            e.Handled = true;
        }

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        Application.Current.UnhandledException += OnUnhandledException;
        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
            await WaitForRenderedFramesAsync(5);

            var row = tableView.ContainerFromIndex(0) as TableViewRow;
            Assert.IsNotNull(row, "Precondition: the current row is realized");

            // Runs right after the row's own handler has started awaiting the scroll.
            var sizeChanged = false;
            row!.SizeChanged += (_, _) =>
            {
                sizeChanged = true;
                items.Clear();
                tableView.UpdateLayout();
            };

            row.MinHeight = row.ActualHeight + 40;
            await WaitForRenderedFramesAsync(10);
            await Task.Delay(200);

            Assert.IsTrue(sizeChanged, "Precondition: the row's height changed");
            Assert.IsNull(row.TableView, "Precondition: the row was detached from its table");
            Assert.IsNull(unhandled, $"The detached row threw: {unhandled}");
        }
        finally
        {
            Application.Current.UnhandledException -= OnUnhandledException;
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    private static ObservableCollection<LifecycleTestItem> CreateItems()
    {
        return new(Enumerable.Range(0, 10).Select(i => new LifecycleTestItem { Name = $"Item {i}" }));
    }

    private static TableView CreateTableView(ObservableCollection<LifecycleTestItem> items)
    {
        var tableView = new TableView
        {
            Width = 300,
            Height = 300,
            AutoGenerateColumns = false,
            ItemsSource = items
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(LifecycleTestItem.Name)) }
        });

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

    private sealed class LifecycleTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
