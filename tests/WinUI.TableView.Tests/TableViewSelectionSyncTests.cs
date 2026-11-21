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
using WinUI.TableView.AutomationPeers;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// The selection properties stay in step with the selection when items come and go.
/// </summary>
[TestClass]
public class TableViewSelectionSyncTests
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
    public async Task RemovingTheSelectedItem_ClearsTheSelectionProperties()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Single);
        var selected = items[2];
        tableView.SelectedItem = selected;

        var removed = new List<object>();
        tableView.SelectionChanged += (_, e) => removed.AddRange(e.RemovedItems);

        items.Remove(selected);

        Assert.AreEqual(0, tableView.SelectedItems.Count);
        Assert.IsNull(tableView.SelectedItem);
        Assert.AreEqual(-1, tableView.SelectedIndex);
        CollectionAssert.AreEqual(new object[] { selected }, removed);
    }

    [UITestMethod]
    public async Task InsertingBeforeTheSelectedItem_MovesSelectedIndex()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Single);
        var selected = items[2];
        tableView.SelectedItem = selected;

        var changes = 0;
        tableView.SelectionChanged += (_, _) => changes++;

        items.Insert(0, new Item { Name = "New" });

        Assert.AreEqual(3, tableView.SelectedIndex);
        Assert.AreSame(selected, tableView.SelectedItem);
        Assert.AreEqual(0, changes);

        items.RemoveAt(0);

        Assert.AreEqual(2, tableView.SelectedIndex);
        Assert.AreSame(selected, tableView.SelectedItem);
        Assert.AreEqual(0, changes);
    }

    [UITestMethod]
    public async Task RemovingOneOfSeveralSelectedItems_ReportsItAndKeepsTheRest()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        tableView.SelectRange(new ItemIndexRange(2, 3));
        var gone = items[3];

        var removed = new List<object>();
        tableView.SelectionChanged += (_, e) => removed.AddRange(e.RemovedItems);

        items.Remove(gone);

        CollectionAssert.AreEqual(new object[] { gone }, removed);
        CollectionAssert.AreEqual(new object[] { items[2], items[3] }, tableView.SelectedItems.ToArray());
        Assert.AreEqual(2, tableView.SelectedIndex);
    }

    [UITestMethod]
    public async Task SelectRange_BeyondTheItems_IsLimitedToThem()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);

        tableView.SelectRange(new ItemIndexRange(7, 500));

        Assert.AreEqual(3, tableView.SelectedItems.Count);
        Assert.IsFalse(tableView.SelectedItems.Contains(null!));

        tableView.DeselectAll();
        tableView.SelectRange(new ItemIndexRange(50, 5));

        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    [UITestMethod]
    public async Task SelectedIndex_BeyondTheItems_SelectsNothing()
    {
        var (tableView, _) = await CreateTableViewAsync(ListViewSelectionMode.Single);

        tableView.SelectedIndex = 50;

        Assert.AreEqual(0, tableView.SelectedItems.Count);
        Assert.AreEqual(-1, tableView.SelectedIndex);
        Assert.IsNull(tableView.SelectedItem);
    }

    [UITestMethod]
    public async Task ShiftSelection_AfterTheViewShrank_StaysWithinTheItems()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended, itemCount: 100);
        tableView.MakeSelection(new TableViewCellSlot(80, -1), shiftKey: false);

        tableView.FilterDescriptions.Add(new FilterDescription(nameof(Item.Name), item => ((Item)item!).Index < 20));
        await WaitForLayoutAsync(tableView);
        Assert.AreEqual(20, tableView.Items.Count);

        tableView.MakeSelection(new TableViewCellSlot(5, -1), shiftKey: true);

        Assert.IsTrue(tableView.SelectedItems.Count <= 20);
        Assert.IsFalse(tableView.SelectedItems.Contains(null!));
        Assert.IsTrue(tableView.SelectedRanges.All(range => range.LastIndex < 20));
    }

    [UITestMethod]
    public async Task PressOnASelectedRow_InMultipleMode_DeselectsIt()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Multiple);
        tableView.SelectRange(new ItemIndexRange(2, 2));

        Assert.IsTrue(tableView.DeselectRowOnPress(2, ctrlKey: false));
        CollectionAssert.AreEqual(new object[] { items[3] }, tableView.SelectedItems.ToArray());

        Assert.IsFalse(tableView.DeselectRowOnPress(5, ctrlKey: false), "a press on an unselected row selects it");
    }

    [UITestMethod]
    public async Task PressOnASelectedRow_InExtendedMode_DeselectsItOnlyWithCtrl()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        tableView.SelectRange(new ItemIndexRange(2, 2));

        Assert.IsFalse(tableView.DeselectRowOnPress(2, ctrlKey: false));
        Assert.AreEqual(2, tableView.SelectedItems.Count);

        Assert.IsTrue(tableView.DeselectRowOnPress(2, ctrlKey: true));
        CollectionAssert.AreEqual(new object[] { items[3] }, tableView.SelectedItems.ToArray());
    }

    [UITestMethod]
    public async Task RowSelectionItemPattern_ChangesOnlyItsOwnRow()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        var row2 = new TableViewRowAutomationPeer((TableViewRow)tableView.ContainerFromIndex(2)!);
        var row6 = new TableViewRowAutomationPeer((TableViewRow)tableView.ContainerFromIndex(6)!);

        row2.Select();
        row6.AddToSelection();
        CollectionAssert.AreEqual(new object[] { items[2], items[6] }, tableView.SelectedItems.ToArray());

        row6.AddToSelection();
        CollectionAssert.AreEqual(new object[] { items[2], items[6] }, tableView.SelectedItems.ToArray());

        row6.Select();
        CollectionAssert.AreEqual(new object[] { items[6] }, tableView.SelectedItems.ToArray());

        tableView.SelectionMode = ListViewSelectionMode.Multiple;
        row2.AddToSelection();
        row6.Select();
        CollectionAssert.AreEqual(new object[] { items[6] }, tableView.SelectedItems.ToArray());
    }

    [UITestMethod]
    public async Task SelectingARowWhileUnloaded_DoesNotThrow()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Single, itemCount: 500);
        Exception? unhandled = null;

        void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            unhandled ??= e.Exception;
            e.Handled = true;
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        Application.Current.UnhandledException += OnUnhandledException;

        try
        {
            tableView.SelectedItem = items[400];
            await Task.Delay(200);
            tableView.CurrentCellSlot = new TableViewCellSlot(450, 0);
            await Task.Delay(200);
            Assert.IsNull(await tableView.ScrollRowIntoView(300));
            Assert.IsNull(tableView.RealizeRow(300));
        }
        finally
        {
            Application.Current.UnhandledException -= OnUnhandledException;
        }

        Assert.IsNull(unhandled, $"Selecting while unloaded threw: {unhandled}");
        Assert.AreSame(items[400], tableView.SelectedItem);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);
    }

    [UITestMethod]
    public async Task AddingToSelectedItems_WhileSelectionIsOff_SelectsNothing()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.None);

        tableView.SelectedItems.Add(items[2]);

        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    [UITestMethod]
    public async Task SelectedItems_FindAnItemSelectedAtItsSecondOccurrence()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Multiple);
        items.Add(items[1]);
        await WaitForLayoutAsync(tableView);

        tableView.SelectRange(new Microsoft.UI.Xaml.Data.ItemIndexRange(items.Count - 1, 1));

        Assert.IsTrue(tableView.SelectedItems.Contains(items[1]));
        Assert.AreEqual(0, tableView.SelectedItems.IndexOf(items[1]));
        Assert.IsTrue(tableView.SelectedItems.Remove(items[1]));
        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    [UITestMethod]
    public async Task SelectionChangedHandler_RemovingTheAddedItems_RemovesExactlyThem()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        var expected = items.Skip(2).Take(3).ToArray();
        var kept = items.Except(expected).ToArray();

        tableView.SelectionChanged += (_, e) =>
        {
            foreach (var item in e.AddedItems)
            {
                items.Remove((Item)item);
            }
        };

        tableView.SelectRange(new ItemIndexRange(2, 3));

        CollectionAssert.AreEqual(kept, items.ToArray());
        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    [UITestMethod]
    public async Task SelectionChangedItems_ReadAfterTheSourceChanged_AreTheItemsThatChanged()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        var expected = items.Skip(2).Take(3).ToArray();
        IList<object>? added = null;
        tableView.SelectionChanged += (_, e) => added ??= e.AddedItems;

        tableView.SelectRange(new ItemIndexRange(2, 3));
        items.Insert(0, new Item { Name = "New" });

        CollectionAssert.AreEqual(expected, added!.ToArray());
    }

    [UITestMethod]
    public async Task SelectedIndexAndItem_WhileRowsCannotBeSelected_SelectNothing()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.None);

        tableView.SelectedIndex = 3;
        Assert.AreEqual(-1, tableView.SelectedIndex);
        Assert.AreEqual(0, tableView.SelectedItems.Count);

        tableView.SelectionMode = ListViewSelectionMode.Extended;
        tableView.SelectionUnit = TableViewSelectionUnit.Cell;

        tableView.SelectedIndex = 3;
        Assert.AreEqual(-1, tableView.SelectedIndex);

        tableView.SelectedItem = items[4];
        Assert.IsNull(tableView.SelectedItem);
        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    [UITestMethod]
    public async Task SelectedItem_NotInTheView_GoesBackToNull()
    {
        var (tableView, _) = await CreateTableViewAsync(ListViewSelectionMode.Single);

        tableView.SelectedItem = new Item { Name = "Foreign" };

        Assert.IsNull(tableView.SelectedItem);
        Assert.AreEqual(-1, tableView.SelectedIndex);
    }

    [UITestMethod]
    public async Task SettingARowIsSelected_SelectsItsItem()
    {
        var (tableView, items) = await CreateTableViewAsync(ListViewSelectionMode.Extended);
        var row = (TableViewRow)tableView.ContainerFromIndex(2)!;

        row.IsSelected = true;
        CollectionAssert.AreEqual(new object[] { items[2] }, tableView.SelectedItems.ToArray());
        Assert.AreEqual(2, tableView.SelectedIndex);

        row.IsSelected = false;
        Assert.AreEqual(0, tableView.SelectedItems.Count);

        tableView.SelectionMode = ListViewSelectionMode.None;
        row.IsSelected = true;
        Assert.IsFalse(row.IsSelected);
        Assert.AreEqual(0, tableView.SelectedItems.Count);
    }

    private async Task<(TableView, ObservableCollection<Item>)> CreateTableViewAsync(ListViewSelectionMode selectionMode, int itemCount = 10)
    {
        var items = new ObservableCollection<Item>(Enumerable.Range(0, itemCount).Select(i => new Item { Index = i, Name = $"Item {i}" }));
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
            SelectionMode = selectionMode,
            SelectionUnit = TableViewSelectionUnit.Row,
            ItemsSource = items
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(Item.Name)) }
        });

        _tableView = tableView;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return (tableView, items);
    }

    private sealed class Item
    {
        public int Index { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
