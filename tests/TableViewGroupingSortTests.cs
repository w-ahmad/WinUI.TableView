using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers the interaction between grouping and sorting a different, unrelated column. A grouped column's
/// <see cref="TableViewColumn.SortDirection"/> mirrors its group's own order rather than an independent
/// sort, so <see cref="TableView.ClearAllSorting"/> - invoked whenever a plain header click single-sorts a
/// different column - must not clear it or desync it from the group.
/// </summary>
[TestClass]
public class TableViewGroupingSortTests
{
    [UITestMethod]
    public async Task ClearAllSorting_DoesNotClear_AGroupedColumnsSortDirection()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();
        var collectionView = (CollectionView)tableView.CollectionView;

        // Mirrors what TableViewColumnHeader.Group() does: add a ColumnGroupDescription for the column and
        // give it a SortDirection, since a grouped column's order and its SortDirection indicator are unified.
        collectionView.GroupDescriptions.Add(new ColumnGroupDescription(categoryColumn, nameof(SortTestItem.Category), SortDirection.Ascending));
        categoryColumn.SortDirection = SortDirection.Ascending;

        await Task.Yield();

        // What a plain click on a different column's header does: single-sort semantics clear sorting
        // everywhere else first.
        tableView.ClearAllSorting();

        Assert.AreEqual(SortDirection.Ascending, categoryColumn.SortDirection);
        Assert.IsTrue(collectionView.GroupDescriptions.OfType<ColumnGroupDescription>().Any(x => x.Column == categoryColumn));
    }

    [UITestMethod]
    public async Task ClearAllSorting_Clears_AnUngroupedColumnsSortDirection()
    {
        var (tableView, categoryColumn, _) = await CreateTableViewAsync();

        categoryColumn.SortDirection = SortDirection.Ascending;

        tableView.ClearAllSorting();

        Assert.IsNull(categoryColumn.SortDirection);
    }

    private static async Task<(TableView TableView, TableViewTextColumn CategoryColumn, TableViewTextColumn ValueColumn)> CreateTableViewAsync()
    {
        var items = Enumerable.Range(0, 4)
            .Select(i => new SortTestItem { Category = i % 2 == 0 ? "A" : "B", Value = i })
            .ToArray();

        var tableView = new TableView();
        var categoryColumn = new TableViewTextColumn { Header = "Category", Binding = new Binding { Path = new PropertyPath(nameof(SortTestItem.Category)) } };
        var valueColumn = new TableViewTextColumn { Header = "Value", Binding = new Binding { Path = new PropertyPath(nameof(SortTestItem.Value)) } };
        tableView.Columns.Add(categoryColumn);
        tableView.Columns.Add(valueColumn);
        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return (tableView, categoryColumn, valueColumn);
    }

    private sealed class SortTestItem
    {
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
