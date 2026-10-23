using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;

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
    public async Task TableView_UnloadAndReload_SuspendsAndRestoresItems()
    {
        var items = CreateItems();
        var tableView = CreateTableView(items);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        Assert.AreEqual(items.Count, tableView.Items.Count, "Precondition: the loaded table shows its items");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        Assert.AreEqual(0, tableView.Items.Count, "An unloaded table must release its items source");

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        try
        {
            Assert.AreEqual(items.Count, tableView.Items.Count, "A reloaded table must restore its items");
        }
        finally
        {
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
