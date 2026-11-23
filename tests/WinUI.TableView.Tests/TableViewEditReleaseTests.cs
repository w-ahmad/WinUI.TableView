using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// An open edit ends when its row is released, whatever releases it.
/// </summary>
[TestClass]
public class TableViewEditReleaseTests
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
    public async Task SortingWhileEditing_CommitsTheEdit()
    {
        var (tableView, items) = await CreateTableViewAsync(20);
        var edited = items[0];
        await BeginEditAsync(tableView, "Zzzz");

        // A header click clears the current cell before it sorts.
        tableView.CurrentCellSlot = null;
        tableView.SortDescriptions.Add(new SortDescription(nameof(Item.Name), SortDirection.Ascending));
        await SettleAsync(tableView);

        Assert.IsFalse(tableView.IsEditing);
        Assert.AreEqual("Zzzz", edited.Name);
        Assert.AreSame(edited, tableView.Items[19]);
        AssertRowsShowTheirItems(tableView);
    }

    [UITestMethod]
    public async Task ReplacingTheSourceWhileEditing_LeavesEditMode()
    {
        var (tableView, items) = await CreateTableViewAsync(20);
        var edited = items[0];
        await BeginEditAsync(tableView, "Edited");

        tableView.ItemsSource = new ObservableCollection<Item>(Enumerable.Range(0, 5).Select(i => new Item { Name = $"Other {i}" }));
        await SettleAsync(tableView);

        Assert.IsFalse(tableView.IsEditing);
        Assert.AreEqual("Edited", edited.Name);
        Assert.IsFalse(tableView.Rows.SelectMany(row => row.Cells).Any(cell => cell.Content is TextBox));
    }

    [UITestMethod]
    public async Task RefusedCommit_StillLeavesEditMode()
    {
        var (tableView, items) = await CreateTableViewAsync(20);
        var edited = items[0];
        tableView.CellEditEnding += (_, e) => e.Cancel = true;
        await BeginEditAsync(tableView, "Edited");

        tableView.SortDescriptions.Add(new SortDescription(nameof(Item.Name), SortDirection.Descending));
        await SettleAsync(tableView);

        Assert.IsFalse(tableView.IsEditing);
        Assert.AreEqual("Item 0000", edited.Name);
        Assert.IsFalse(tableView.Rows.SelectMany(row => row.Cells).Any(cell => cell.Content is TextBox));
    }

    [UITestMethod]
    public async Task RecycledRow_CommittingAnEditThatMovesItsItem_KeepsTheViewWhole()
    {
        var (tableView, items) = await CreateTableViewAsync(2_000);
        tableView.SortDescriptions.Add(new SortDescription(nameof(Item.Name), SortDirection.Ascending));
        await SettleAsync(tableView);

        var edited = (Item)tableView.Items[0];
        await BeginEditAsync(tableView, "Zzzz");

        await ScrollToAsync(tableView, 40_000d);
        await SettleAsync(tableView);

        if (tableView.IsEditing && tableView.Rows.Any(row => ReferenceEquals(row.Content, edited)))
        {
            return; // the repeater kept the row for the keyboard focus it holds
        }

        Assert.IsFalse(tableView.IsEditing);
        Assert.AreEqual("Zzzz", edited.Name);
        Assert.AreEqual(2_000, tableView.Items.Count);
        Assert.AreSame(edited, tableView.Items[1_999]);
        AssertRowsShowTheirItems(tableView);
    }

    private static void AssertRowsShowTheirItems(TableView tableView)
    {
        foreach (var row in tableView.Rows)
        {
            Assert.AreSame(tableView.Items[row.Index], row.Content, $"Row {row.Index} shows another item");
        }
    }

    private static async Task BeginEditAsync(TableView tableView, string text)
    {
        tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
        var cell = tableView.Rows.First(row => row.Index == 0).Cells[0];
        cell.PrepareForEdit(new RoutedEventArgs());
        await WaitForLayoutAsync(tableView);

        Assert.IsTrue(tableView.IsEditing, "Precondition: the cell is editing");
        ((TextBox)cell.Content).Text = text;
    }

    private static async Task SettleAsync(TableView tableView)
    {
        await WaitForLayoutAsync(tableView);
        await Task.Delay(100);
        await WaitForLayoutAsync(tableView);
    }

    private async Task<(TableView, ObservableCollection<Item>)> CreateTableViewAsync(int itemCount)
    {
        var items = new ObservableCollection<Item>(Enumerable.Range(0, itemCount).Select(i => new Item { Name = $"Item {i:D4}" }));
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended,
            ItemsSource = items
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)), Mode = BindingMode.TwoWay }
        });

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return (tableView, items);
    }

    private sealed class Item : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        } = string.Empty;
    }
}
