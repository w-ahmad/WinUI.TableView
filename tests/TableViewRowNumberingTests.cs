using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

/// <summary>
/// Covers <see cref="TableViewRow.RowNumber"/> and <see cref="TableView.ShowRowNumbers"/>, both ungrouped and
/// grouped. <see cref="TableViewRow.RowNumber"/> is meant to stay the row's real, stable position among all
/// rows once <see cref="TableView.ShowRowNumbers"/> is enabled - unlike <see cref="TableViewRow.Index"/>, it
/// must not shift just because a collapsed group hid other rows.
/// </summary>
[TestClass]
public class TableViewRowNumberingTests
{
    [UITestMethod]
    public async Task RowNumber_Ungrouped_MatchesDisplayIndexPlusOne()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        var row = await tableView.ScrollRowIntoView(2);

        Assert.IsNotNull(row);
        Assert.AreEqual(3, row!.RowNumber);
    }

    [UITestMethod]
    public async Task RowNumber_WithShowRowNumbersOff_UsesDisplayIndex_EvenWhileGrouped()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var collectionView = await GroupByCategoryAsync(tableView);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // "B" shifts down to display index 0.

        var row = await tableView.ScrollRowIntoView(0);

        Assert.IsNotNull(row);
        Assert.AreEqual("B", ((RowNumberingTestItem)row!.Content!).Category);
        Assert.AreEqual(1, row.RowNumber); // Same as Index + 1 - ShowRowNumbers defaults to off.
    }

    [UITestMethod]
    public async Task RowNumber_WithShowRowNumbersOn_StaysRealAfterACollapseHidesEarlierRows()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        tableView.ShowRowNumbers = true;
        var collectionView = await GroupByCategoryAsync(tableView);

        SetGroupExpanded(collectionView, "A", isExpanded: false); // Hides rows 0-1 ("A"'s items).

        var row = await tableView.ScrollRowIntoView(0); // Display index 0 is now "B"'s first item.

        Assert.IsNotNull(row);
        Assert.AreEqual("B", ((RowNumberingTestItem)row!.Content!).Category);
        // "B"'s first item is at index 2 (0-based) in the full, ungrouped source order - its real number
        // must reflect that, not the display index (0) a collapsed "A" group shifted it down to.
        Assert.AreEqual(3, row.RowNumber);
    }

    [UITestMethod]
    public async Task ShowRowNumbers_TogglesTheRowNumberTextBlocksVisibilityAndText()
    {
        var tableView = await CreateTableViewAsync(CreateItems());
        var row = await tableView.ScrollRowIntoView(0);
        Assert.IsNotNull(row);

        var rowNumberText = row!.FindDescendant<TextBlock>(x => x.Name == "RowNumberText");
        Assert.IsNotNull(rowNumberText);
        Assert.AreEqual(Visibility.Collapsed, rowNumberText!.Visibility);

        tableView.ShowRowNumbers = true;
        await Task.Yield();

        Assert.AreEqual(Visibility.Visible, rowNumberText.Visibility);
        Assert.AreEqual("1", rowNumberText.Text);
    }

    [UITestMethod]
    public async Task RowHeaderTag_IsSetToTheRowNumber_RegardlessOfShowRowNumbers()
    {
        var tableView = await CreateTableViewAsync(CreateItems());

        var row = await tableView.ScrollRowIntoView(1);

        Assert.IsNotNull(row);
        Assert.AreEqual(row!.RowNumber, row.RowPresenter?.RowHeader?.Tag);
    }

    private static async Task<TableView> CreateTableViewAsync(RowNumberingTestItem[] items)
    {
        var tableView = new TableView();

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Binding = new Binding { Path = new PropertyPath(nameof(RowNumberingTestItem.Category)) }
        });

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);

        return tableView;
    }

    /// <summary>
    /// 3 categories ("A", "B", "C"), 2 items each, laid out contiguously (rows 0-1 = A, 2-3 = B, 4-5 = C).
    /// </summary>
    private static RowNumberingTestItem[] CreateItems()
    {
        var categories = new[] { "A", "B", "C" };

        return Enumerable.Range(0, 6)
            .Select(i => new RowNumberingTestItem { Category = categories[i / 2] })
            .ToArray();
    }

    /// <summary>
    /// Adding a <see cref="GroupDescription"/> to an already-loaded <see cref="TableView"/> makes it
    /// asynchronously unbind and rebind the base <c>ItemsSource</c>, so callers must await this before
    /// reading <see cref="TableView.Items"/> - otherwise it's transiently empty.
    /// </summary>
    private static async Task<CollectionView> GroupByCategoryAsync(TableView tableView, TableViewGroupState defaultGroupState = TableViewGroupState.Expanded)
    {
        var collectionView = (CollectionView)tableView.CollectionView;
        collectionView.DefaultGroupState = defaultGroupState;
        collectionView.GroupDescriptions.Add(new GroupDescription(nameof(RowNumberingTestItem.Category)));

        await Task.Yield();

        return collectionView;
    }

    private static void SetGroupExpanded(CollectionView collectionView, object key, bool isExpanded)
    {
        foreach (var groupObject in collectionView.CollectionGroups!)
        {
            var group = (CollectionViewGroup)groupObject;
            var info = (TableViewGroupInfo)group.Group!;
            if (Equals(info.Key, key))
            {
                info.IsExpanded = isExpanded;
                return;
            }
        }

        Assert.Fail($"No group found for key '{key}'.");
    }

    private sealed class RowNumberingTestItem
    {
        public string Category { get; set; } = string.Empty;
    }
}
