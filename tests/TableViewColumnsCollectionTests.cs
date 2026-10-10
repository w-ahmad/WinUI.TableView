using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections.Specialized;

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

        // The first FrozenColumnCount VISIBLE columns are frozen; the hidden one does not count.
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
    public void Add_ManyColumns_ShouldNotBeSuperLinear()
    {
        // Guards the fix for UpdateFrozenColumns evaluating VisibleColumns inside its loop on every
        // Add, which made building an n-column table O(n^3 log n): ~1.9 s for 350 columns.
        // Generous bound so it holds on a slow CI agent; the fixed code takes a few milliseconds.
        var tableView = new TableView();
        var collection = new TableViewColumnsCollection(tableView);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < 350; i++)
        {
            collection.Add(new TableViewTextColumn { Header = $"Column {i}" });
        }
        stopwatch.Stop();

        Assert.AreEqual(350, collection.Count);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds < 500,
            $"Adding 350 columns took {stopwatch.ElapsedMilliseconds} ms; expected well under 500 ms.");
    }
}
