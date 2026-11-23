using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewColumnsCollectionTests
{
    [UITestMethod]
    public void Constructor_ShouldInitializeTableViewProperty()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        Assert.AreEqual(tableView, collection.TableView);
    }

    [UITestMethod]
    public void Add_ShouldRaiseCollectionChangedEvent()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column = new TableViewTextColumn();

        var eventRaised = false;
        collection.CollectionChanged += (s, e) => eventRaised = true;

        collection.Add(column);

        Assert.IsTrue(eventRaised);
    }

    [UITestMethod]
    public void Remove_ShouldRaiseCollectionChangedEvent()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column = new TableViewTextColumn();

        collection.Add(column);

        var eventRaised = false;
        collection.CollectionChanged += (s, e) => eventRaised = true;

        collection.Remove(column);

        Assert.IsTrue(eventRaised);
    }

    [UITestMethod]
    public void VisibleColumns_ShouldReturnOnlyVisibleColumns()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);

        var visibleColumn = new TableViewTextColumn { Visibility = Visibility.Visible };
        var hiddenColumn = new TableViewTextColumn { Visibility = Visibility.Collapsed };

        collection.Add(visibleColumn);
        collection.Add(hiddenColumn);

        var visibleColumns = collection.VisibleColumns;

        Assert.AreEqual(1, visibleColumns.Count);
        Assert.AreEqual(visibleColumn, visibleColumns[0]);
    }

    [UITestMethod]
    public void HandleColumnPropertyChanged_ShouldRaiseColumnPropertyChangedEvent()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column = new TableViewTextColumn();

        collection.Add(column);

        var eventRaised = false;
        collection.ColumnPropertyChanged += (s, e) => eventRaised = true;

        collection.HandleColumnPropertyChanged(column, "TestProperty");

        Assert.IsTrue(eventRaised);
    }

    [UITestMethod]
    public void HandleColumnPropertyChanged_ShouldNotRaiseEvent_ForInvalidColumn()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column = new TableViewTextColumn();

        var eventRaised = false;
        collection.ColumnPropertyChanged += (s, e) => eventRaised = true;

        collection.HandleColumnPropertyChanged(column, "TestProperty");

        Assert.IsFalse(eventRaised);
    }

    [UITestMethod]
    public void ResetCollection_ShouldRaiseCollectionChangedEvent()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column1 = new TableViewTextColumn();
        var column2 = new TableViewTextColumn();

        collection.Add(column1);
        collection.Add(column2);

        var eventRaised = false;
        collection.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                eventRaised = true;
            }
        };

        collection.Clear();

        Assert.IsTrue(eventRaised);
    }


    [UITestMethod]
    public void VisibleColumns_ShouldReturnColumnsInCorrectOrder()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);

        var column1 = new TableViewTextColumn { Header = "column1", Visibility = Visibility.Visible, Order = 1 };
        var column2 = new TableViewTextColumn { Header = "column2", Visibility = Visibility.Visible, Order = 2 };
        var column3 = new TableViewTextColumn { Header = "column3", Visibility = Visibility.Visible, Order = 1 };

        collection.Add(column1);
        collection.Add(column2);
        collection.Add(column3);

        var visibleColumns = collection.VisibleColumns;

        Assert.AreEqual(column1, visibleColumns[0]);
        Assert.AreEqual(column3, visibleColumns[1]);
        Assert.AreEqual(column2, visibleColumns[2]);
    }

    [UITestMethod]
    public void AddDuplicateColumns_ShouldHandleCorrectly()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var column = new TableViewTextColumn();

        collection.Add(column);
        collection.Add(column);

        Assert.AreEqual(2, collection.Count);
    }

    [UITestMethod]
    public void ColumnVisibilityChange_ShouldUpdateVisibleColumns()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);

        var column = new TableViewTextColumn { Visibility = Visibility.Visible };
        collection.Add(column);

        Assert.AreEqual(1, collection.VisibleColumns.Count);

        column.Visibility = Visibility.Collapsed;

        Assert.AreEqual(0, collection.VisibleColumns.Count);
    }

    [UITestMethod]
    public void Add_ShouldThrowException_ForInvalidObjectType()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);

        var invalidObject = new TextBox();

        Assert.ThrowsExactly<InvalidCastException>(() => collection.Add(invalidObject));
    }

    [UITestMethod]
    public void UpdateFrozenColumns_ShouldFreezeLeadingVisibleColumnsOnly()
    {
        var tableView = new TableView { FrozenColumnCount = 2 };
        var collection = new TableViewColumnsCollection(tableView);

        var first = new TableViewTextColumn();
        var hidden = new TableViewTextColumn { Visibility = Visibility.Collapsed };
        var second = new TableViewTextColumn();
        var third = new TableViewTextColumn();

        collection.Add(first);
        collection.Add(hidden);
        collection.Add(second);
        collection.Add(third);

        Assert.IsTrue(first.IsFrozen);
        Assert.IsTrue(second.IsFrozen);
        Assert.IsFalse(third.IsFrozen);
    }

    [UITestMethod]
    public void UpdateFrozenColumns_ShouldHonourOrderOverInsertionOrder()
    {
        var tableView = new TableView { FrozenColumnCount = 1 };
        var collection = new TableViewColumnsCollection(tableView);

        var addedFirst = new TableViewTextColumn { Order = 2 };
        var addedSecond = new TableViewTextColumn { Order = 1 };

        collection.Add(addedFirst);
        collection.Add(addedSecond);

        Assert.IsFalse(addedFirst.IsFrozen);
        Assert.IsTrue(addedSecond.IsFrozen);
    }

    [UITestMethod]
    public void FrozenColumnCountChange_ShouldRefreezeColumns()
    {
        var tableView = new TableView();
        var columns = Enumerable.Range(0, 4).Select(_ => new TableViewTextColumn()).ToList();
        columns.ForEach(tableView.Columns.Add);

        Assert.IsFalse(columns.Any(x => x.IsFrozen));

        tableView.FrozenColumnCount = 3;
        CollectionAssert.AreEqual(new[] { true, true, true, false }, columns.Select(x => x.IsFrozen).ToArray());

        tableView.FrozenColumnCount = 1;
        CollectionAssert.AreEqual(new[] { true, false, false, false }, columns.Select(x => x.IsFrozen).ToArray());
    }

    [UITestMethod]
    public void UpdateFrozenColumns_ShouldFreezeADuplicatedColumnByItsFirstPlace()
    {
        var tableView = new TableView { FrozenColumnCount = 1 };
        var collection = new TableViewColumnsCollection(tableView);
        var duplicated = new TableViewTextColumn();
        var other = new TableViewTextColumn();

        collection.Add(duplicated);
        collection.Add(other);
        collection.Add(duplicated);

        Assert.IsTrue(duplicated.IsFrozen);
        Assert.IsFalse(other.IsFrozen);
    }

    [UITestMethod]
    public void Move_ThenRemove_KeepsTheMovedColumnOwned()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var a = new TableViewTextColumn();
        var b = new TableViewTextColumn();
        var c = new TableViewTextColumn();

        collection.Add(a);
        collection.Add(b);
        collection.Add(c);
        collection.Move(0, 2);
        collection.RemoveAt(0);

        Assert.AreEqual(2, collection.VisibleColumns.Count);

        a.Visibility = Visibility.Collapsed;

        Assert.AreEqual(1, collection.VisibleColumns.Count);
    }

    [UITestMethod]
    public void InsertRemoveAndReplace_KeepVisibleColumnsAndIndexesInStep()
    {
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);
        var a = new TableViewTextColumn();
        var b = new TableViewTextColumn();
        var c = new TableViewTextColumn();
        var d = new TableViewTextColumn();
        var index = -1;
        collection.ColumnPropertyChanged += (_, e) => index = e.Index;

        collection.Add(a);
        collection.Add(b);
        collection.Insert(0, c);
        CollectionAssert.AreEqual(new[] { c, a, b }, collection.VisibleColumns.ToArray());

        b.Width = new GridLength(120);
        Assert.AreEqual(2, index);

        collection.RemoveAt(1);
        collection[0] = d;
        CollectionAssert.AreEqual(new[] { d, b }, collection.VisibleColumns.ToArray());

        b.Width = new GridLength(130);
        Assert.AreEqual(1, index);

        index = -1;
        c.Width = new GridLength(130);
        Assert.AreEqual(-1, index, "a column that was replaced is not in the collection");
    }

    [UITestMethod]
    public void ColumnProperties_ReadBackWhatWasSet()
    {
        var column = new TableViewTextColumn();

        Assert.IsTrue(column.Width.IsAuto);
        Assert.AreEqual(Visibility.Visible, column.Visibility);
        Assert.IsNull(column.MinWidth);
        Assert.IsNull(column.Order);

        column.Width = new GridLength(2, GridUnitType.Star);
        column.MinWidth = 40;
        column.MaxWidth = 300;
        column.ActualWidth = 150;
        column.Visibility = Visibility.Collapsed;
        column.Order = 3;
        column.SetValue(TableViewColumn.MinWidthProperty, 50d);

        Assert.AreEqual(new GridLength(2, GridUnitType.Star), column.Width);
        Assert.AreEqual(50d, column.MinWidth);
        Assert.AreEqual(300d, column.MaxWidth);
        Assert.AreEqual(150d, column.ActualWidth);
        Assert.AreEqual(Visibility.Collapsed, column.Visibility);
        Assert.AreEqual(3, column.Order);

        column.ClearValue(TableViewColumn.OrderProperty);
        column.ClearValue(TableViewColumn.WidthProperty);

        Assert.IsNull(column.Order);
        Assert.IsTrue(column.Width.IsAuto);
    }
}
