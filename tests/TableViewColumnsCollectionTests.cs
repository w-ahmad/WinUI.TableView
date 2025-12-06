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
}
