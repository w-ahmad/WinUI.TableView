using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// <see cref="TableView.DetachItemsSource"/> stops following the items source without resetting what is shown, so a
/// host can take a large table apart before releasing it instead of paying for a reset that tears every row down.
/// </summary>
[TestClass]
public class TableViewDetachItemsSourceTests
{
    [UITestMethod]
    public async Task DetachItemsSource_KeepsTheRows_AndIgnoresLaterChangesToTheSource()
    {
        var source = new ObservableCollection<DetachTestItem> { new() { Name = "Alpha" }, new() { Name = "Beta" } };
        var tableView = await CreateTableViewAsync(source);
        Assert.AreEqual(2, tableView.FindDescendants().OfType<TableViewRow>().Count(), "Precondition: two realized rows");

        tableView.DetachItemsSource();
        source.Clear();
        source.Add(new DetachTestItem { Name = "Gamma" });
        tableView.UpdateLayout();

        Assert.AreEqual(2, tableView.Items.Count, "a detached table keeps the rows it had");
        Assert.AreEqual(2, tableView.FindDescendants().OfType<TableViewRow>().Count(), "and their containers");
    }

    [UITestMethod]
    public async Task SettingItemsSourceAgain_FollowsTheNewSource()
    {
        var tableView = await CreateTableViewAsync(new ObservableCollection<DetachTestItem> { new() { Name = "Alpha" } });
        tableView.DetachItemsSource();

        var next = new ObservableCollection<DetachTestItem> { new() { Name = "Beta" }, new() { Name = "Gamma" } };
        tableView.ItemsSource = next;
        tableView.UpdateLayout();
        next.Add(new DetachTestItem { Name = "Delta" });
        tableView.UpdateLayout();

        Assert.AreEqual(3, tableView.Items.Count, "a new items source is followed as usual");
    }

    private static async Task<TableView> CreateTableViewAsync(ObservableCollection<DetachTestItem> items)
    {
        var tableView = new TableView
        {
            Width = 400,
            Height = 300,
            AutoGenerateColumns = false,
        };
        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Name",
            Binding = new Binding { Path = new PropertyPath(nameof(DetachTestItem.Name)) }
        });
        tableView.ItemsSource = items;
        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        tableView.UpdateLayout();
        return tableView;
    }

    private sealed class DetachTestItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
