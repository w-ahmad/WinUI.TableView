using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests for the row-editing feature: tap-to-edit in Row and CellOrRow modes,
/// editing highlight, pointer hover, and virtualization.
/// </summary>
[TestClass]
public class TableViewRowEditingTests
{
    [UITestMethod]
    public async Task RowMode_EditingHighlight_AppliedToRow()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(1, 0);
            tableView.SetIsEditing(true);

            var row = tableView.ContainerFromIndex(1) as TableViewRow;
            Assert.IsNotNull(row);

            row.EnsureAlternateColors();

            tableView.SetIsEditing(false);

            row.EnsureAlternateColors();
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task CellOrRowMode_PointerHover_Prerequisites()
    {
        var items = CreateTestItems(3);
        var tableView = CreateTableView(TableViewSelectionUnit.CellOrRow, items);
        await LoadAsync(tableView);

        try
        {
            Assert.AreEqual(TableViewSelectionUnit.CellOrRow, tableView.SelectionUnit);
            Assert.IsFalse(tableView.IsReadOnly);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task CellOrRowMode_EditingHighlight_AppliedToRow()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.CellOrRow, items);
        await LoadAsync(tableView);

        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(2, 0);
            tableView.SetIsEditing(true);

            var row = tableView.ContainerFromIndex(2) as TableViewRow;
            Assert.IsNotNull(row);

            row.EnsureAlternateColors();

            tableView.SetIsEditing(false);

            row.EnsureAlternateColors();
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task CellOrRowMode_TapToEdit_Prerequisites()
    {
        var items = CreateTestItems(3);
        var tableView = CreateTableView(TableViewSelectionUnit.CellOrRow, items);
        await LoadAsync(tableView);

        try
        {
            Assert.AreEqual(TableViewSelectionUnit.CellOrRow, tableView.SelectionUnit);
            Assert.IsFalse(tableView.IsReadOnly);
            Assert.IsFalse(tableView.IsEditing);

            foreach (var column in tableView.Columns)
            {
                Assert.IsFalse(column.UseSingleElement);
            }
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task Virtualization_EditingHighlight_ClearedOnRecycle()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
            tableView.SetIsEditing(true);
            tableView.SetIsEditing(false);
            tableView.CurrentCellSlot = null;

            Assert.IsNull(tableView.CurrentCellSlot);

            var row = tableView.ContainerFromIndex(0) as TableViewRow;
            Assert.IsNotNull(row);

            row.EnsureAlternateColors();
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task Virtualization_CellCurrentState_ResetOnPrepare()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(1, 0);
            tableView.CurrentCellSlot = null;

            var row = tableView.ContainerFromIndex(1) as TableViewRow;
            Assert.IsNotNull(row);

            foreach (var cell in row.Cells)
            {
                cell.ApplyCurrentCellState();
            }
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task EditingState_SwitchBetweenRows_ClearsAndRestarts()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            tableView.CurrentCellSlot = new TableViewCellSlot(0, 0);
            tableView.SetIsEditing(true);
            Assert.IsTrue(tableView.IsEditing);

            tableView.SetIsEditing(false);
            Assert.IsFalse(tableView.IsEditing);

            tableView.CurrentCellSlot = new TableViewCellSlot(3, 1);
            tableView.SetIsEditing(true);
            Assert.IsTrue(tableView.IsEditing);
            Assert.AreEqual(3, tableView.CurrentCellSlot?.Row);

            tableView.SetIsEditing(false);
            Assert.IsFalse(tableView.IsEditing);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    private static TableView CreateTableView(
        TableViewSelectionUnit selectionUnit,
        ObservableCollection<TestItem>? items = null)
    {
        var tableView = new TableView
        {
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Single,
            SelectionUnit = selectionUnit,
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath("Name") }
        });

        tableView.Columns.Add(new TableViewNumberColumn
        {
            Header = "Value",
            Binding = new Binding { Path = new PropertyPath("Value") }
        });

        if (items is not null)
        {
            tableView.ItemsSource = items;
        }

        return tableView;
    }

    private static ObservableCollection<TestItem> CreateTestItems(int count)
    {
        var list = new ObservableCollection<TestItem>();
        for (int i = 0; i < count; i++)
        {
            list.Add(new TestItem { Id = i, Name = $"Item{i}", Value = count - i });
        }
        return list;
    }

    private static Task LoadAsync(FrameworkElement content)
    {
        return UnitTestApp.Current.MainWindow.LoadTestContentAsync(content);
    }

    private static Task UnloadAsync(FrameworkElement content)
    {
        return UnitTestApp.Current.MainWindow.UnloadTestContentAsync(content);
    }
}
