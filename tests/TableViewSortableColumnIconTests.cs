using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers <see cref="TableView.ShowSortableColumnIcon"/> - the opt-in hint icon that marks a sortable
/// column which has no active sort direction (issue #114). The feature defaults to off, must never show
/// on a column that isn't actually sortable, and must react live to CanSort/CanSortColumns changes.
/// </summary>
[TestClass]
public class TableViewSortableColumnIconTests
{
    [UITestMethod]
    public void ShowSortableColumnIcon_DefaultsToFalse()
    {
        var tableView = new TableView();

        Assert.IsFalse(tableView.ShowSortableColumnIcon);
    }

    [UITestMethod]
    public void ShowSortableColumnIcon_CanBeSetToTrue()
    {
        var tableView = new TableView { ShowSortableColumnIcon = true };

        Assert.IsTrue(tableView.ShowSortableColumnIcon);
    }

    [UITestMethod]
    public async Task UnsortedSortableColumn_HidesHintIcon_WhenFeatureIsOff()
    {
        var (_, column) = await CreateTableViewAsync();

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility);
    }

    [UITestMethod]
    public async Task UnsortedSortableColumn_ShowsHintIcon_WhenFeatureIsOn()
    {
        var (tableView, column) = await CreateTableViewAsync();

        tableView.ShowSortableColumnIcon = true;

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility);
        Assert.AreEqual("\ue8cb", sortIcon.Glyph);
        Assert.AreEqual(0.5, sortIcon.Opacity);
    }

    [UITestMethod]
    public async Task UnsortedNonSortableColumn_HidesHintIcon_EvenWhenFeatureIsOn()
    {
        var (tableView, column) = await CreateTableViewAsync();

        column.CanSort = false;
        tableView.ShowSortableColumnIcon = true;

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility);
    }

    [UITestMethod]
    public async Task SortedColumn_ShowsDirectionIcon_NotHintIcon_WhenFeatureIsOn()
    {
        var (tableView, column) = await CreateTableViewAsync();

        tableView.ShowSortableColumnIcon = true;
        column.SortDirection = SortDirection.Ascending;

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility);
        Assert.AreEqual(1d, sortIcon.Opacity, "Ascending/Descending icon should be full opacity, not the 0.5 hint opacity");
    }

    [UITestMethod]
    public async Task TogglingCanSort_AtRuntime_UpdatesHintIconImmediately()
    {
        var (tableView, column) = await CreateTableViewAsync();
        tableView.ShowSortableColumnIcon = true;
        var sortIcon = GetSortIcon(column);
        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility, "Precondition: hint icon starts visible");

        column.CanSort = false;

        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility);

        column.CanSort = true;

        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility);
    }

    [UITestMethod]
    public async Task TogglingCanSortColumns_AtRuntime_UpdatesHintIconImmediately()
    {
        var (tableView, column) = await CreateTableViewAsync();
        tableView.ShowSortableColumnIcon = true;
        var sortIcon = GetSortIcon(column);
        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility, "Precondition: hint icon starts visible");

        tableView.CanSortColumns = false;

        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility);
    }

    [UITestMethod]
    public async Task TogglingShowSortableColumnIcon_AtRuntime_UpdatesExistingHeader()
    {
        var (tableView, column) = await CreateTableViewAsync();
        var sortIcon = GetSortIcon(column);
        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility, "Precondition: hint icon starts hidden by default");

        tableView.ShowSortableColumnIcon = true;

        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility);
    }

    [UITestMethod]
    public async Task ShowSortableColumnIcon_SetBeforeLoad_StillAppliesOnFirstApplyTemplate()
    {
        var (_, column) = await CreateTableViewAsync((tableView, _) => tableView.ShowSortableColumnIcon = true);

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Visible, sortIcon.Visibility,
            "ShowSortableColumnIcon set before the TableView loads must still be reflected once the header's template is applied");
    }

    [UITestMethod]
    public async Task CanSortColumnsFalse_SetBeforeLoad_KeepsHintIconCollapsed()
    {
        var (_, column) = await CreateTableViewAsync((tableView, _) =>
        {
            tableView.ShowSortableColumnIcon = true;
            tableView.CanSortColumns = false;
        });

        var sortIcon = GetSortIcon(column);

        Assert.AreEqual(Visibility.Collapsed, sortIcon.Visibility,
            "CanSortColumns=False set before the TableView loads must still be reflected once the header's template is applied");
    }

    private static FontIcon GetSortIcon(TableViewColumn column)
    {
        var header = column.HeaderControl!;
        var sortIcon = header.FindDescendant<FontIcon>(f => f.Name == "SortIcon");

        Assert.IsNotNull(sortIcon, "Expected to find the header template's SortIcon FontIcon");

        return sortIcon!;
    }

    private static async Task<(TableView TableView, TableViewTextColumn Column)> CreateTableViewAsync(Action<TableView, TableViewTextColumn>? configure = null)
    {
        var items = new[]
        {
            new SortableIconTestItem { Name = "Alpha" },
            new SortableIconTestItem { Name = "Beta" },
        };

        var tableView = new TableView();
        var column = new TableViewTextColumn { Header = "Name", Binding = new Binding { Path = new PropertyPath(nameof(SortableIconTestItem.Name)) } };
        tableView.Columns.Add(column);
        tableView.ItemsSource = items;

        configure?.Invoke(tableView, column);

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return (tableView, column);
    }

    private sealed class SortableIconTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
