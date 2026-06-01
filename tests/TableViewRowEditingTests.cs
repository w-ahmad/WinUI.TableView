using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests for the row-editing feature: editing highlight via cell lifecycle,
/// TapToEdit property, overlay independence from alternate colors, and virtualization.
/// </summary>
[TestClass]
public class TableViewRowEditingTests
{
    [UITestMethod]
    public async Task TapToEdit_DefaultsToFalse()
    {
        var items = CreateTestItems(3);
        var tableView = CreateTableView(TableViewSelectionUnit.Cell, items);
        await LoadAsync(tableView);

        try
        {
            Assert.IsFalse(tableView.TapToEdit);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task Lifecycle_BeginEditing_ShowsHighlight_RowMode()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            var row = tableView.ContainerFromIndex(1) as TableViewRow;
            Assert.IsNotNull(row);
            Assert.IsTrue(row.Cells.Count > 0);

            var cell = row.Cells[0];
            var overlay = row.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);

            var started = await cell.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(started);
            Assert.IsTrue(tableView.IsEditing);
            Assert.AreEqual(Visibility.Visible, overlay.Visibility);

            tableView.EndCellEditing(TableViewEditAction.Cancel, cell);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task Lifecycle_BeginEditing_ShowsHighlight_CellOrRowMode()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.CellOrRow, items);
        await LoadAsync(tableView);

        try
        {
            var row = tableView.ContainerFromIndex(2) as TableViewRow;
            Assert.IsNotNull(row);

            var cell = row.Cells[0];
            var overlay = row.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);

            var started = await cell.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(started);
            Assert.AreEqual(Visibility.Visible, overlay.Visibility);

            tableView.EndCellEditing(TableViewEditAction.Cancel, cell);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task Lifecycle_CellMode_NoHighlight()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Cell, items);
        await LoadAsync(tableView);

        try
        {
            var row = tableView.ContainerFromIndex(1) as TableViewRow;
            Assert.IsNotNull(row);

            var cell = row.Cells[0];
            var overlay = row.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay);

            var started = await cell.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(started);
            Assert.IsTrue(tableView.IsEditing);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);

            tableView.EndCellEditing(TableViewEditAction.Cancel, cell);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);
        }
        finally
        {
            await UnloadAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task AlternateColors_PreservedDuringEditingHighlight()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        var alternateBrush = new SolidColorBrush(Colors.LightGray);
        tableView.AlternateRowBackground = alternateBrush;
        await LoadAsync(tableView);

        try
        {
            var oddRow = tableView.ContainerFromIndex(1) as TableViewRow;
            Assert.IsNotNull(oddRow);

            oddRow.EnsureAlternateColors();
            Assert.AreEqual(alternateBrush, oddRow.RowPresenter?.Background);

            var cell = oddRow.Cells[0];
            var overlay = oddRow.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay);

            var started = await cell.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(started);
            Assert.AreEqual(Visibility.Visible, overlay.Visibility);
            Assert.AreEqual(alternateBrush, oddRow.RowPresenter?.Background);

            tableView.EndCellEditing(TableViewEditAction.Cancel, cell);
            Assert.AreEqual(Visibility.Collapsed, overlay.Visibility);
            Assert.AreEqual(alternateBrush, oddRow.RowPresenter?.Background);
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
            Assert.IsFalse(tableView.TapToEdit);
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
    public async Task EditingState_SwitchBetweenRows()
    {
        var items = CreateTestItems(5);
        var tableView = CreateTableView(TableViewSelectionUnit.Row, items);
        await LoadAsync(tableView);

        try
        {
            var row0 = tableView.ContainerFromIndex(0) as TableViewRow;
            var row3 = tableView.ContainerFromIndex(3) as TableViewRow;
            Assert.IsNotNull(row0);
            Assert.IsNotNull(row3);

            var cell0 = row0.Cells[0];
            var overlay0 = row0.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay0);

            await cell0.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(tableView.IsEditing);
            Assert.AreEqual(Visibility.Visible, overlay0.Visibility);

            tableView.EndCellEditing(TableViewEditAction.Commit, cell0);
            tableView.SetIsEditing(false);
            Assert.IsFalse(tableView.IsEditing);
            Assert.AreEqual(Visibility.Collapsed, overlay0.Visibility);

            var cell3 = row3.Cells[1];
            var overlay3 = row3.FindDescendant<Border>(b => b.Name is "EditingHighlightOverlay");
            Assert.IsNotNull(overlay3);

            await cell3.BeginCellEditing(new RoutedEventArgs());
            Assert.IsTrue(tableView.IsEditing);
            Assert.AreEqual(Visibility.Visible, overlay3.Visibility);
            Assert.AreEqual(Visibility.Collapsed, overlay0.Visibility);

            tableView.EndCellEditing(TableViewEditAction.Commit, cell3);
            tableView.SetIsEditing(false);
            Assert.IsFalse(tableView.IsEditing);
            Assert.AreEqual(Visibility.Collapsed, overlay3.Visibility);
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
