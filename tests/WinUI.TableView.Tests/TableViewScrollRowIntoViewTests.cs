using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewScrollRowIntoViewTests
{
    [UITestMethod]
    public async Task ScrollRowIntoView_ReturnsNull_WhenIndexIsPastTheLastRow()
    {
        var (tableView, items) = await CreateTableViewAsync(itemCount: 3);
        try
        {
            Assert.IsNull(await tableView.ScrollRowIntoView(items.Count));
            Assert.IsNull(await tableView.ScrollRowIntoView(items.Count + 10));
            Assert.IsNull(await tableView.ScrollRowIntoView(-1));
            Assert.IsNotNull(await tableView.ScrollRowIntoView(items.Count - 1), "A valid index still resolves its row");
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    [DataRow(ListViewSelectionMode.Single)]
    [DataRow(ListViewSelectionMode.Multiple)]
    public async Task SelectingARow_ThenRemovingItems_DoesNotThrowFromTheQueuedScroll(ListViewSelectionMode selectionMode)
    {
        var (tableView, items) = await CreateTableViewAsync(itemCount: 5);
        Exception? unhandled = null;

        // The queued scroll runs outside the test, so what it throws only shows up as an unhandled exception.
        void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            unhandled ??= e.Exception;
            e.Handled = true;
        }

        Application.Current.UnhandledException += OnUnhandledException;
        try
        {
            tableView.SelectionMode = selectionMode;

            // Selecting queues a scroll to the selected row; the items shrink below its index before it runs.
            tableView.SelectedItem = items[4];
            Assert.AreEqual(1, tableView.SelectedItems.Count, "Precondition: exactly one row is selected");

            items.Clear();

            await Task.Delay(300);

            Assert.AreEqual(0, tableView.Items.Count);
            Assert.IsNull(unhandled, $"The queued scroll threw: {unhandled}");
        }
        finally
        {
            Application.Current.UnhandledException -= OnUnhandledException;
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task SelectingARow_ThenRemovingRowsAboveIt_ScrollsToItsNewIndex()
    {
        var (tableView, items) = await CreateTableViewAsync(itemCount: 5);
        try
        {
            var selected = items[4];
            tableView.SelectedItem = selected;
            items.RemoveAt(0);
            items.RemoveAt(0);

            await Task.Delay(300);

            Assert.AreSame(selected, tableView.SelectedItem);
            Assert.AreEqual(2, tableView.Items.IndexOf(selected));
        }
        finally
        {
            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    private static async Task<(TableView TableView, ObservableCollection<ScrollTestItem> Items)> CreateTableViewAsync(int itemCount)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(100, GridUnitType.Pixel),
            Binding = new Binding { Path = new PropertyPath(nameof(ScrollTestItem.Name)) }
        });

        var items = new ObservableCollection<ScrollTestItem>(
            Enumerable.Range(0, itemCount).Select(i => new ScrollTestItem { Name = $"Item {i}" }));

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return (tableView, items);
    }

    private sealed class ScrollTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
