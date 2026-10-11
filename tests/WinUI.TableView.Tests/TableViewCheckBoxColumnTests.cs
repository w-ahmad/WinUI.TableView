using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewCheckBoxColumnTests
{
    [UITestMethod]
    public void TableViewCheckBoxColumn_UsesSingleElementAndTracksReadOnlyState()
    {
        var tableView = new TableView { IsReadOnly = false };
        var column = new TableViewCheckBoxColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.IsEnabled)) }
        };
        column.SetOwningTableView(tableView);

        var checkBox = (CheckBox)column.GenerateElement(new TableViewCell(), new ColumnTestItem());
        var cell = new TableViewCell { Content = checkBox };

        Assert.IsTrue(column.UseSingleElement);
        Assert.IsTrue(checkBox.IsHitTestVisible);

        column.IsReadOnly = true;
        column.UpdateElementState(cell, null);

        Assert.IsFalse(checkBox.IsHitTestVisible);
        checkBox.IsChecked = true;
        Assert.AreEqual(true, column.PrepareCellForEdit(cell, new RoutedEventArgs()));
        Assert.ThrowsExactly<NotImplementedException>(() => column.GenerateEditingElement(new TableViewCell(), null));
    }

    [UITestMethod]
    public void TableViewCheckBoxColumn_RespectsTableViewReadOnlyState()
    {
        var tableView = new TableView { IsReadOnly = true };
        var column = new TableViewCheckBoxColumn();
        column.SetOwningTableView(tableView);

        var checkBox = (CheckBox)column.GenerateElement(new TableViewCell(), null);

        Assert.IsFalse(checkBox.IsHitTestVisible);
        Assert.AreEqual(new Thickness(12, 0, 12, 0), checkBox.Margin);
        Assert.AreEqual(20d, checkBox.MinWidth);
        Assert.AreEqual(20d, checkBox.MaxWidth);
    }
}
