using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewToggleSwitchColumnTests
{
    [UITestMethod]
    public void TableViewToggleSwitchColumn_UsesSingleElementAndTracksReadOnlyState()
    {
        var tableView = new TableView { IsReadOnly = false };
        var column = new TableViewToggleSwitchColumn
        {
            Binding = new Binding { Path = new PropertyPath(nameof(ColumnTestItem.IsEnabled)) },
            OnContent = "On",
            OffContent = "Off"
        };
        column.SetOwningTableView(tableView);

        var toggleSwitch = (ToggleSwitch)column.GenerateElement(new TableViewCell(), new ColumnTestItem());
        var cell = new TableViewCell { Content = toggleSwitch };

        Assert.IsTrue(column.UseSingleElement);
        Assert.AreEqual("On", toggleSwitch.OnContent);
        Assert.AreEqual("Off", toggleSwitch.OffContent);
        Assert.IsTrue(toggleSwitch.IsHitTestVisible);

        tableView.IsReadOnly = true;
        column.UpdateElementState(cell, null);

        Assert.IsFalse(toggleSwitch.IsHitTestVisible);
        toggleSwitch.IsOn = true;
        Assert.AreEqual(true, column.PrepareCellForEdit(cell, new RoutedEventArgs()));
        Assert.ThrowsExactly<NotImplementedException>(() => column.GenerateEditingElement(new TableViewCell(), null));
    }

    [UITestMethod]
    public void TableViewToggleSwitchColumn_RespectsTableViewReadOnlyState()
    {
        var tableView = new TableView { IsReadOnly = true };
        var column = new TableViewToggleSwitchColumn();
        column.SetOwningTableView(tableView);

        var toggleSwitch = (ToggleSwitch)column.GenerateElement(new TableViewCell(), null);

        Assert.IsFalse(toggleSwitch.IsHitTestVisible);
        Assert.AreEqual(new Thickness(12, 0, 12, 0), toggleSwitch.Margin);
    }
}
