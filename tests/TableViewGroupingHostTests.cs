using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinUI.TableView.Extensions;
using static WinUI.TableView.Tests.TableViewRowHostTestHelpers;

namespace WinUI.TableView.Tests;

/// <summary>
/// Tests grouping as displayed by the ItemsRepeater row host: the flattened visual row sequence, group header
/// realization and templates, expand/collapse through the header, sticky headers, and selection across the
/// resets grouping causes.
/// </summary>
[TestClass]
public class TableViewGroupingHostTests
{
    private const double ViewportHeight = 400d;

    [UITestMethod]
    public void VisualRows_Ungrouped_IsTheItemView()
    {
        var collectionView = new CollectionView(CreateItems(6));
        var visualRows = new TableViewVisualRows(collectionView);

        Assert.IsFalse(visualRows.HasGroups);
        Assert.AreEqual(6, visualRows.Count);

        for (var i = 0; i < 6; i++)
        {
            Assert.AreEqual(i, visualRows.GetItemIndex(i));
            Assert.AreEqual(i, visualRows.GetVisualIndex(i));
            Assert.AreSame(collectionView[i], visualRows[i]);
        }

        Assert.AreEqual(-1, visualRows.GetItemIndex(6));
        Assert.AreEqual(-1, visualRows.FindHeaderAtOrBefore(3));
    }

    [UITestMethod]
    public void VisualRows_Grouped_InterleavesHeadersWithTheirRows()
    {
        var collectionView = CreateGroupedView(TableViewGroupState.Expanded, nameof(GroupItem.Category));
        var visualRows = new TableViewVisualRows(collectionView);

        // [A, a0, a1, B, b0, b1, C, c0, c1]
        Assert.IsTrue(visualRows.HasGroups);
        Assert.AreEqual(9, visualRows.Count);
        CollectionAssert.AreEqual(new object?[] { "A", null, null, "B", null, null, "C", null, null },
            Enumerable.Range(0, 9).Select(i => visualRows.GetGroupHeader(i)?.Key).ToArray());

        Assert.AreEqual(-1, visualRows.GetItemIndex(3), "a header row has no item index");
        Assert.AreEqual(2, visualRows.GetItemIndex(4));
        Assert.AreEqual(4, visualRows.GetVisualIndex(2));
        Assert.AreSame(collectionView[2], visualRows[4]);

        Assert.AreEqual(3, visualRows.FindHeaderAtOrBefore(5), "a row belongs to the nearest header above it");
        Assert.AreEqual(3, visualRows.FindHeaderAtOrBefore(3));
        Assert.AreEqual(6, visualRows.FindHeaderAfter(3));
        Assert.AreEqual(-1, visualRows.FindHeaderAfter(6));
    }

    [UITestMethod]
    public void VisualRows_CollapsedGroups_ShowOnlyTheirHeaders()
    {
        var collectionView = CreateGroupedView(TableViewGroupState.Collapsed, nameof(GroupItem.Category));
        var visualRows = new TableViewVisualRows(collectionView);

        Assert.AreEqual(0, collectionView.Count);
        Assert.AreEqual(3, visualRows.Count);
        Assert.IsTrue(Enumerable.Range(0, 3).All(i => visualRows.GetItemIndex(i) is -1));

        SetGroupExpanded(collectionView, "B", level: 0, isExpanded: true);
        visualRows.Reset();

        // [A, B, b0, b1, C]
        Assert.AreEqual(5, visualRows.Count);
        Assert.AreEqual(0, visualRows.GetItemIndex(2));
        Assert.AreEqual("B", ((GroupItem)visualRows[3]!).Category);
    }

    [UITestMethod]
    public void VisualRows_NestedGroups_HideDescendantsOfACollapsedGroup()
    {
        var collectionView = CreateGroupedView(TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));
        var visualRows = new TableViewVisualRows(collectionView);

        // [A, A/x, a0, A/y, a1, B, B/x, b0, B/y, b1, C, C/x, c0, C/y, c1]
        Assert.AreEqual(15, visualRows.Count);
        Assert.AreEqual(1, visualRows.GetGroupHeader(1)!.Level);
        Assert.AreEqual(6, visualRows.FindHeaderAtOrBefore(7), "a leaf row belongs to its own subgroup's header");

        SetGroupExpanded(collectionView, "B", level: 0, isExpanded: false);
        visualRows.Reset();

        // [A, A/x, a0, A/y, a1, B, C, C/x, c0, C/y, c1]
        Assert.AreEqual(11, visualRows.Count);
        Assert.AreEqual("B", visualRows.GetGroupHeader(5)!.Key);
        Assert.AreEqual("C", visualRows.GetGroupHeader(6)!.Key);
        Assert.AreEqual(2, visualRows.GetItemIndex(8), "C's first item follows A's two in the item view");
    }

    [UITestMethod]
    public async Task Grouping_RealizesHeaderRows_AndExpandingAGroupRealizesItsRows()
    {
        var tableView = await CreateTableViewAsync(CreateItems(30));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        Assert.AreEqual(3, tableView.GroupHeaderRows.Count, "every collapsed group shows its header");
        Assert.AreEqual(0, tableView.Rows.Count, "collapsed groups show no rows");
        CollectionAssert.AreEquivalent(new[] { "A", "B", "C" }, tableView.GroupHeaderRows.Select(x => x.GroupInfo!.Key).ToArray());

        tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "B")).ToggleExpanded();
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(10, tableView.Items.Count);
        Assert.AreEqual(10, tableView.Rows.Count);
        Assert.IsTrue(tableView.Rows.All(row => ((GroupItem)row.Content!).Category == "B"));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task Grouping_RealizationTracksTheViewport_NotTheItemCount()
    {
        var tableView = await CreateTableViewAsync(CreateItems(30_000));
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        var realized = tableView.Rows.Count + tableView.GroupHeaderRows.Count;
        var maxExpected = (int)(ViewportHeight / 36d) + 12;

        Assert.IsTrue(realized > 0);
        Assert.IsTrue(realized <= maxExpected, $"{realized} elements realized for a {ViewportHeight}px viewport");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GroupHeaderTemplate_ComesFromTheGroupStyleForItsLevel()
    {
        var levelZero = new DataTemplate();
        var levelOne = new DataTemplate();
        var tableView = await CreateTableViewAsync(CreateItems(6));

        tableView.GroupStyle.Add(new GroupStyle { HeaderTemplate = levelZero });
        tableView.GroupStyle.Add(new GroupStyle { HeaderTemplate = levelOne });
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));

        Assert.IsTrue(tableView.GroupHeaderRows.Any(x => x.GroupInfo!.Level is 1), "the nested level should be realized");

        foreach (var header in tableView.GroupHeaderRows)
        {
            Assert.AreSame(header.GroupInfo!.Level is 0 ? levelZero : levelOne, header.ContentTemplate);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GroupHeaderTemplate_FallsBackToDefaultGroupStyle()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        Assert.AreEqual(0, tableView.GroupStyle.Count, "the default style is not copied into GroupStyle");
        Assert.IsNotNull(tableView.DefaultGroupStyle?.HeaderTemplate);
        Assert.IsTrue(tableView.GroupHeaderRows.All(x => ReferenceEquals(x.ContentTemplate, tableView.DefaultGroupStyle!.HeaderTemplate)));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ShowGroupExpandCollapseButton_UpdatesRealizedHeaders()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        tableView.ShowGroupExpandCollapseButton = false;

        foreach (var header in tableView.GroupHeaderRows)
        {
            var button = header.FindDescendants().OfType<Button>().Single(x => x.Name is "ExpandCollapseButton");

            Assert.AreEqual(0d, button.Opacity);
            Assert.IsFalse(button.IsHitTestVisible);
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GroupHeaderAutomationPeer_ExpandsAndCollapsesItsGroup()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        var header = tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "A"));
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(header);
        var expandCollapse = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);

        Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.AreEqual("A (2)", peer.GetName());
        Assert.AreEqual(Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed, expandCollapse.ExpandCollapseState);

        expandCollapse.Expand();
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(2, tableView.Items.Count);
        Assert.IsTrue(tableView.Items.Cast<GroupItem>().All(x => x.Category == "A"));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_IsPinnedToTheViewportTop_ForTheGroupBeingScrolled()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        var scrollViewer = GetScrollViewer(tableView);

        Assert.IsFalse(tableView.GroupHeaderRows.Any(x => x.IsPinned), "nothing is pinned before scrolling");

        await ScrollToAsync(tableView, 2_000d);
        AssertPinned(tableView, scrollViewer, "A");

        // Halfway down, the second of three equally sized groups is at the top.
        await ScrollToAsync(tableView, scrollViewer.ExtentHeight / 2);
        AssertPinned(tableView, scrollViewer, "B");

        tableView.AreStickyGroupHeadersEnabled = false;
        await WaitForLayoutAsync(tableView);

        Assert.IsFalse(tableView.GroupHeaderRows.Any(x => x.IsPinned), "turning the feature off unpins the header");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_IsNotUsed_WhenDisabled()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        var scrollViewer = GetScrollViewer(tableView);
        await ScrollToAsync(tableView, 2_000d);

        Assert.IsFalse(tableView.GroupHeaderRows.Any(x => x.IsPinned));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ViewReset_ClearsTheSelection_AndReportsTheRemovedItems()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        tableView.SelectRange(new ItemIndexRange(1, 1));

        var selected = tableView.SelectedItem;
        IList<object>? removed = null;

        // Kept, not copied: the list has to stay right after the handler returns and the view has been rebuilt.
        tableView.SelectionChanged += (_, e) => removed = e.RemovedItems;

        // Sorting rebuilds the view, re-ordering every index.
        ((CollectionView)tableView.CollectionView).SortDescriptions.Add(
            new SortDescription(nameof(GroupItem.Id), SortDirection.Descending));

        Assert.IsNotNull(selected);
        Assert.IsNotNull(removed, "SelectionChanged should be raised");
        CollectionAssert.AreEqual(new[] { selected }, removed.ToArray());
        Assert.AreNotSame(selected, tableView.Items[1], "the sort should have moved another item onto the old index");
        Assert.IsNull(tableView.SelectedItem);
        Assert.AreEqual(-1, tableView.SelectedIndex);
        Assert.AreEqual(0, tableView.SelectedItems.Count);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task SelectionChangedLists_AreReadOnly()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        IList<object>? added = null;
        tableView.SelectionChanged += (_, e) => added = e.AddedItems;

        tableView.SelectRange(new ItemIndexRange(1, 2));

        Assert.IsNotNull(added);
        Assert.AreEqual(2, added.Count);
        Assert.IsTrue(added.IsReadOnly);
        Assert.ThrowsExactly<System.NotSupportedException>(() => added.Add(tableView.Items[4]));
        Assert.ThrowsExactly<System.NotSupportedException>(() => added.Clear());
        Assert.AreEqual(2, tableView.SelectedItems.Count, "the selection itself is untouched");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GroupHeader_VisualStates_FollowTheGroupAndPinning()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        var collapsed = tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "A"));
        Assert.AreEqual(-90d, GetChevronAngle(collapsed), "a collapsed group's chevron points sideways");
        Assert.IsNull(GetTemplatePart<Border>(collapsed, "PinnedBackground").Background, "an unpinned header has no backing fill");

        collapsed.ToggleExpanded();
        await WaitForLayoutAsync(tableView);

        var expanded = tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "A"));
        Assert.AreEqual(0d, GetChevronAngle(expanded), "an expanded group's chevron points down");

        await ScrollToAsync(tableView, 2_000d);

        var pinned = tableView.GroupHeaderRows.Single(x => x.IsPinned);
        Assert.IsNotNull(GetTemplatePart<Border>(pinned, "PinnedBackground").Background, "a pinned header is backed by an opaque fill");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task NestedGrouping_IndentsHeadersByLevel_AndRowsBelowTheFirstLevel()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));

        foreach (var header in tableView.GroupHeaderRows)
        {
            Assert.AreEqual(header.GroupInfo!.Level * 24d, GetTemplatePart<Border>(header, "IndentPlaceholder").Width);
        }

        // Two levels of grouping: rows sit one indent step in, under the innermost headers.
        Assert.IsTrue(tableView.Rows.Count > 0);
        Assert.IsTrue(tableView.Rows.All(row => GetTemplatePart<Border>(row, "IndentPlaceholder").Width is 24d));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task GroupHeaders_AreRecycled_AcrossExpandAndCollapse()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        var before = tableView.GroupHeaderRows.ToHashSet();

        tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "A")).ToggleExpanded();
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(3, tableView.GroupHeaderRows.Count);
        Assert.IsTrue(tableView.GroupHeaderRows.All(before.Contains), "the rebuild should reuse the pooled header elements");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task CollapsingAGroup_ClearsTheSelection()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Expanded);
        tableView.SelectRange(new ItemIndexRange(3, 1));

        tableView.GroupHeaderRows.First(x => Equals(x.GroupInfo!.Key, "A")).ToggleExpanded();
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(0, tableView.SelectedItems.Count);
        Assert.IsTrue(tableView.Rows.All(row => !row.IsSelected));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    private static double GetChevronAngle(TableViewGroupHeaderRow header)
    {
        return ((Microsoft.UI.Xaml.Media.RotateTransform)GetTemplatePart<FontIcon>(header, "ExpandCollapseIcon").RenderTransform).Angle;
    }

    private static T GetTemplatePart<T>(DependencyObject owner, string name) where T : FrameworkElement
    {
        return owner.FindDescendants().OfType<T>().First(x => x.Name == name);
    }

    private static void AssertPinned(TableView tableView, ScrollViewer scrollViewer, string expectedKey)
    {
        var pinned = tableView.GroupHeaderRows.Where(x => x.IsPinned).ToList();

        Assert.AreEqual(1, pinned.Count, "exactly one header should be pinned");
        Assert.AreEqual(expectedKey, pinned[0].GroupInfo!.Key);

        // Compared in the scrolled content's coordinates: the scroll offset itself is applied by the ScrollViewer
        // outside layout, so a transform up to the ScrollViewer does not reliably include it.
        Assert.AreEqual(scrollViewer.VerticalOffset, pinned[0].ActualOffset.Y, 1d,
            "the pinned header should be arranged at the top of the viewport");
    }

    private static CollectionView CreateGroupedView(TableViewGroupState defaultGroupState, params string[] properties)
    {
        var collectionView = new CollectionView(CreateItems(6)) { DefaultGroupState = defaultGroupState };

        foreach (var property in properties)
        {
            collectionView.GroupDescriptions.Add(new GroupDescription(property));
        }

        return collectionView;
    }

    private static void SetGroupExpanded(CollectionView collectionView, object key, int level, bool isExpanded)
    {
        var info = collectionView.CollectionGroups!
            .Cast<CollectionViewGroup>()
            .Select(x => (TableViewGroupInfo)x.Group!)
            .First(x => Equals(x.Key, key) && x.Level == level);

        info.IsExpanded = isExpanded;
    }

    private static async Task GroupAsync(TableView tableView, TableViewGroupState defaultGroupState, params string[] properties)
    {
        tableView.DefaultGroupState = defaultGroupState;

        foreach (var property in properties.Length > 0 ? properties : [nameof(GroupItem.Category)])
        {
            tableView.GroupDescriptions.Add(new GroupDescription(property));
        }

        await WaitForLayoutAsync(tableView);
    }

    private static async Task<TableView> CreateTableViewAsync(GroupItem[] items)
    {
        var tableView = new TableView
        {
            Width = 600,
            Height = ViewportHeight,
            RowHeight = 40,
            AutoGenerateColumns = false,
            SelectionMode = ListViewSelectionMode.Extended
        };

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Category",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(GroupItem.Category)) }
        });

        tableView.Columns.Add(new TableViewTextColumn
        {
            Header = "Kind",
            Width = new GridLength(200),
            Binding = new Binding { Path = new PropertyPath(nameof(GroupItem.Kind)) }
        });

        tableView.ItemsSource = items;

        await UnitTestApp.Current.MainWindow.LoadTestContentAsync(tableView);
        await WaitForLayoutAsync(tableView);

        return tableView;
    }

    /// <summary>
    /// Three categories, each a contiguous third of the items, alternating between two kinds.
    /// </summary>
    private static GroupItem[] CreateItems(int count)
    {
        var categories = new[] { "A", "B", "C" };
        var perCategory = (count + 2) / 3;

        return [.. Enumerable.Range(0, count).Select(i => new GroupItem
        {
            Id = i,
            Category = categories[i / perCategory],
            Kind = i % 2 is 0 ? "x" : "y"
        })];
    }

    private sealed class GroupItem
    {
        public int Id { get; set; }

        public string Category { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;
    }
}
