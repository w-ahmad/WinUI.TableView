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
        var maxExpected = GetMaxRealizedRows(tableView, ViewportHeight, 36d);

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
    public async Task GroupHeaderTemplate_ReusesTheLastGroupStyleForDeeperLevels()
    {
        var levelZero = new DataTemplate();
        var levelOne = new DataTemplate();
        var tableView = await CreateTableViewAsync(CreateItems(6));

        tableView.GroupStyle.Add(new GroupStyle { HeaderTemplate = levelZero });
        tableView.GroupStyle.Add(new GroupStyle { HeaderTemplate = levelOne });
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind), nameof(GroupItem.Id));

        Assert.IsTrue(tableView.GroupHeaderRows.Any(x => x.GroupInfo!.Level is 2), "a third level should be realized");

        // Two styles for three levels: the last entry serves every level past the end of the list.
        foreach (var header in tableView.GroupHeaderRows)
        {
            Assert.AreSame(header.GroupInfo!.Level is 0 ? levelZero : levelOne, header.ContentTemplate, $"level {header.GroupInfo.Level}");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_IsNotAmongTheAutomationChildren()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);
        await ScrollToAsync(tableView, 2_000d);
        Assert.IsTrue(IsStickyHeaderShown(tableView), "precondition: a header is pinned");

        var children = FrameworkElementAutomationPeer.CreatePeerForElement(tableView).GetChildren();
        var owners = children.OfType<FrameworkElementAutomationPeer>().Select(x => x.Owner).ToList();

        // Screen reader users reach the header among the rows; the one drawn over them would be a duplicate.
        Assert.IsFalse(owners.Contains(tableView.StickyGroupHeader!), "the sticky header should not be exposed to automation");
        Assert.IsTrue(owners.OfType<TableViewRow>().Any(), "the realized rows should be");

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
    public async Task DefaultGroupHeaderTemplate_ShowsKeyAndCount()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Collapsed);

        Assert.AreEqual(3, tableView.GroupHeaderRows.Count);

        foreach (var header in tableView.GroupHeaderRows)
        {
            var group = header.GroupInfo!;
            var texts = header.FindDescendants().OfType<TextBlock>().Select(x => x.Text).ToList();

            Assert.AreEqual(group.Key, group.GetValue(TableViewGroupInfo.KeyProperty));
            Assert.AreEqual(group.Count, group.GetValue(TableViewGroupInfo.CountProperty));
            CollectionAssert.Contains(texts, (string)group.Key!);
            CollectionAssert.Contains(texts, "2");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public void CollectionGroups_Group_HasBindableKeyAndCount()
    {
        var collectionView = CreateGroupedView(TableViewGroupState.Expanded, nameof(GroupItem.Category));

        foreach (var entry in collectionView.CollectionGroups!.Cast<Microsoft.UI.Xaml.Data.ICollectionViewGroup>())
        {
            var group = (TableViewGroupInfo)entry.Group;

            Assert.IsNotNull(group.Key);
            Assert.AreEqual(group.Key, group.GetValue(TableViewGroupInfo.KeyProperty));
            Assert.AreEqual(2, group.GetValue(TableViewGroupInfo.CountProperty));
        }
    }

    [UITestMethod]
    public void GroupInfo_CreatedByAnApp_HasBindableKeyAndCount()
    {
        var group = new TableViewGroupInfo { Key = "A", Count = 2 };

        Assert.AreEqual("A", group.GetValue(TableViewGroupInfo.KeyProperty));
        Assert.AreEqual(2, group.GetValue(TableViewGroupInfo.CountProperty));
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
    public async Task StickyGroupHeader_ShowsTheGroupAtTheViewportTop()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        var scrollViewer = GetScrollViewer(tableView);

        Assert.IsFalse(IsStickyHeaderShown(tableView), "at the top, the first group's own header is in view");

        await ScrollToAsync(tableView, 2_000d);
        AssertStickyHeader(tableView, scrollViewer, "A");

        // Halfway down, the second of three equally sized groups is at the top.
        await ScrollToAsync(tableView, scrollViewer.ExtentHeight / 2);
        AssertStickyHeader(tableView, scrollViewer, "B");

        tableView.AreStickyGroupHeadersEnabled = false;
        await WaitForLayoutAsync(tableView);

        Assert.IsFalse(IsStickyHeaderShown(tableView), "turning the feature off hides the sticky header");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_IsNotUsed_WhenDisabled()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        await ScrollToAsync(tableView, 2_000d);

        Assert.IsFalse(IsStickyHeaderShown(tableView));
        Assert.IsFalse(tableView.GroupHeaderRows.Any(x => x.IsPinned));

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_IsPushedUpByTheNextGroupsHeader()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        // Group A's header, its 1,000 rows, then group B's header, realized just below A's last row.
        const int nextHeader = 1_001;
        await tableView.ScrollRowIntoView(999);
        await WaitForScrollToSettleAsync(tableView);

        var sticky = tableView.StickyGroupHeader!;
        Assert.IsTrue(tableView.RealizedRows.TryGetBounds(nextHeader, out var nextTop, out _), "group B's header should be realized");

        // Group B's header half a header below the top edge: the sticky header for A is half pushed out.
        await ScrollToAsync(tableView, nextTop - (sticky.ActualHeight / 2));

        Assert.IsTrue(tableView.RealizedRows.TryGetBounds(nextHeader, out nextTop, out _));
        var expected = nextTop - tableView.RowsViewportTop - sticky.ActualHeight;

        Assert.AreEqual("A", sticky.GroupInfo!.Key);
        Assert.IsTrue(expected < 0d && expected > -sticky.ActualHeight, $"group B's header should overlap the sticky header, by {-expected}px");
        Assert.AreEqual(expected, ((Microsoft.UI.Xaml.Media.TranslateTransform)sticky.RenderTransform).Y, 1d);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task StickyGroupHeader_Button_CollapsesTheGroupItShows()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        await ScrollToAsync(tableView, 2_000d);
        var group = tableView.StickyGroupHeader!.GroupInfo!;
        Assert.AreEqual("A", group.Key);

        var button = GetTemplatePart<Button>(tableView.StickyGroupHeader, "ExpandCollapseButton");
        Assert.IsFalse(button.IsTabStop, "keyboard users reach the header in the rows, not the one drawn over them");

        new ButtonAutomationPeer(button).Invoke();
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsFalse(group.IsExpanded);
        Assert.IsFalse(IsStickyHeaderShown(tableView) && Equals(tableView.StickyGroupHeader.GroupInfo?.Key, "A"),
            "a collapsed group has no rows left to keep its header stuck");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_FarAway_LandsUnderTheStickyHeader()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        await ScrollToAsync(tableView, 2_000d);
        var stickyHeight = tableView.StickyGroupHeader!.ActualHeight;
        await ScrollToAsync(tableView, GetScrollViewer(tableView).ScrollableHeight);

        // Item 1,500 is in group B, far above the bottom, so it is not realized.
        Assert.IsNull(tableView.ContainerFromIndex(1_500));

        var row = await tableView.ScrollRowIntoView(1_500);
        await WaitForScrollToSettleAsync(tableView);

        Assert.IsNotNull(row);
        Assert.AreSame(tableView.Items[1_500], row!.Content);
        Assert.AreEqual("B", tableView.StickyGroupHeader.GroupInfo!.Key);
        Assert.AreEqual(stickyHeight, row.ActualOffset.Y - tableView.RowsViewportTop, 1d,
            "the row should stop just under the sticky header, not underneath it");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task LongGroupKey_DoesNotWidenTheRows()
    {
        var items = CreateItems(30);

        // Groups are ordered by key, so this one stays first, where its header is realized.
        foreach (var item in items.Where(x => x.Category is "A"))
        {
            item.Category = new string('A', 2_000);
        }

        var tableView = await CreateTableViewAsync(items);
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        Assert.IsTrue(tableView.GroupHeaderRows.Any(x => x.GroupInfo!.Key is string { Length: 2_000 }), "the long key's header should be realized");
        Assert.AreEqual(0d, GetScrollViewer(tableView).ScrollableWidth, 1d, "two 200px columns fit the 600px table, whatever the group key");

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task ScrollRowIntoView_FromAbove_StopsUnderTheStickyHeader()
    {
        var tableView = await CreateTableViewAsync(CreateItems(3_000));
        tableView.AreStickyGroupHeadersEnabled = true;
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        // Item 1,500 is in group B, far below B's own header.
        await tableView.ScrollRowIntoView(1_500);
        await WaitForScrollToSettleAsync(tableView);
        var target = tableView.ContainerFromIndex(1_500) as TableViewRow;
        Assert.IsNotNull(target);

        // Leave the row half scrolled out of the top: B's sticky header covers it.
        await ScrollToAsync(tableView, target!.ActualOffset.Y + 20d);
        var stickyHeight = tableView.StickyGroupHeader!.ActualHeight;
        Assert.AreEqual("B", tableView.StickyGroupHeader.GroupInfo!.Key);

        var row = await tableView.ScrollRowIntoView(1_500);
        await WaitForScrollToSettleAsync(tableView);

        Assert.AreSame(target, row);
        Assert.AreEqual(stickyHeight, row!.ActualOffset.Y - tableView.RowsViewportTop, 1d,
            "the row should stop just under the sticky header, not underneath it");

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

        Assert.IsNotNull(GetTemplatePart<Border>(tableView.StickyGroupHeader!, "PinnedBackground").Background, "the sticky header is backed by an opaque fill");
        Assert.IsTrue(tableView.GroupHeaderRows.All(x => GetTemplatePart<Border>(x, "PinnedBackground").Background is null), "headers in the rows are never pinned");

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
    public async Task NestedGrouping_RowNumbers_FillTheIndentBeforeWideningTheRowHeader()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));

        double CellsLeft() => tableView.Rows[0].Cells[0].TransformToVisual(tableView).TransformPoint(default).X;

        var before = CellsLeft();

        tableView.ShowRowNumbers = true;
        await WaitForLayoutAsync(tableView);

        var numberWidth = GetTemplatePart<TextBlock>(tableView.Rows[0], "RowNumberText").DesiredSize.Width;

        Assert.IsTrue(numberWidth > 24d, "the number should be wider than the indent");
        // The grid line was inside the indent and now follows the number.
        Assert.AreEqual(before + numberWidth - 24d, CellsLeft(), 1.5);

        tableView.ShowRowNumbers = false;
        await WaitForLayoutAsync(tableView);

        Assert.AreEqual(before, CellsLeft(), 0.5);

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task NestedGrouping_RowHeaderTemplate_FillsTheIndentBeforeWideningTheRowHeader()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));

        double CellsLeft() => tableView.Rows[0].Cells[0].TransformToVisual(tableView).TransformPoint(default).X;

        var before = CellsLeft();
        var header = GetTemplatePart<TableViewRowHeader>(tableView.Rows[0], "RowHeader");
        var emptyWidth = header.ActualWidth;

        // One that fits in the indent, then one that does not.
        foreach (var width in new[] { 30d, 60d })
        {
            tableView.RowHeaderTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                $"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='{width}' Height='10' /></DataTemplate>");
            await WaitForLayoutAsync(tableView);
            await WaitForLayoutAsync(tableView);

            header = GetTemplatePart<TableViewRowHeader>(tableView.Rows[0], "RowHeader");
            var used = header.ActualWidth - header.MinWidth;
            var indent = Math.Max(0d, 24d - used);

            Assert.IsTrue(used > 0d, "the template should make the header wider than its minimum");
            Assert.AreEqual(indent, GetTemplatePart<Border>(tableView.Rows[0], "IndentPlaceholder").Width, 0.5, $"indent with a {width} px template");
            // The grid line was inside the indent and follows the header once the indent is used up.
            Assert.AreEqual(before + (header.ActualWidth - emptyWidth) - (24d - indent), CellsLeft(), 1.5, $"cells with a {width} px template");
        }

        await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
    }

    [UITestMethod]
    public async Task NestedGrouping_DetailsButton_FillsTheIndent()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        await GroupAsync(tableView, TableViewGroupState.Expanded, nameof(GroupItem.Category), nameof(GroupItem.Kind));

        tableView.RowDetailsTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
            "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='Details' /></DataTemplate>");
        // The mode is the default; changing it and back shows the button for the template set after load.
        tableView.RowDetailsVisibilityMode = TableViewRowDetailsVisibilityMode.Collapsed;
        tableView.RowDetailsVisibilityMode = TableViewRowDetailsVisibilityMode.VisibleWhenExpanded;
        await WaitForLayoutAsync(tableView);
        await WaitForLayoutAsync(tableView);

        var row = tableView.Rows[0];
        var button = GetTemplatePart<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>(row, "DetailsToggleButton");
        var buttonWidth = button.DesiredSize.Width;

        Assert.AreEqual(Visibility.Visible, button.Visibility);
        Assert.IsTrue(buttonWidth > 0d);
        Assert.AreEqual(Math.Max(0d, 24d - buttonWidth), GetTemplatePart<Border>(row, "IndentPlaceholder").Width, 0.5);

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
    public async Task GroupHeaders_KeepTheirGroup_AcrossExpandAndCollapse()
    {
        foreach (var toggledKey in new[] { "A", "B", "C" })
        {
            var tableView = await CreateTableViewAsync(CreateItems(6));
            await GroupAsync(tableView, TableViewGroupState.Collapsed);

            var before = tableView.GroupHeaderRows.ToDictionary(x => (string)x.GroupInfo!.Key!);

            for (var i = 0; i < 2; i++)
            {
                before[toggledKey].ToggleExpanded();
                await WaitForLayoutAsync(tableView);

                foreach (var header in tableView.GroupHeaderRows)
                {
                    Assert.AreSame(before[(string)header.GroupInfo!.Key!], header, $"group {header.GroupInfo.Key}, toggled {toggledKey}, toggle {i + 1}");
                }
            }

            await UnitTestApp.Current.MainWindow.UnloadTestContentAsync(tableView);
        }
    }

    [UITestMethod]
    public async Task GroupHeader_StaysAsWideAsTheViewport_WhenTheColumnsAreWider()
    {
        var tableView = await CreateTableViewAsync(CreateItems(6));
        tableView.Columns[0].Width = new GridLength(900);
        await WaitForLayoutAsync(tableView);
        await GroupAsync(tableView, TableViewGroupState.Expanded);

        var scrollViewer = GetScrollViewer(tableView);
        Assert.IsTrue(scrollViewer.ScrollableWidth > 0d, "the columns should be wider than the table");

        foreach (var header in tableView.GroupHeaderRows)
        {
            // The expander button has a presenter of the same name; the header has the last one.
            var content = header.FindDescendants().OfType<ContentPresenter>().Last(x => x.Name == "ContentPresenter");
            var right = content.TransformToVisual(scrollViewer).TransformPoint(new Windows.Foundation.Point(content.ActualWidth, 0d)).X;

            Assert.IsTrue(right <= scrollViewer.ViewportWidth + 0.5, $"the header content ends at {right}, past the viewport's {scrollViewer.ViewportWidth}");
        }

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

    private static bool IsStickyHeaderShown(TableView tableView)
    {
        return tableView.StickyGroupHeader is { Visibility: Visibility.Visible };
    }

    private static void AssertStickyHeader(TableView tableView, ScrollViewer scrollViewer, string expectedKey)
    {
        Assert.IsTrue(IsStickyHeaderShown(tableView), "the sticky header should be shown");
        Assert.AreEqual(expectedKey, tableView.StickyGroupHeader!.GroupInfo!.Key);
        Assert.IsFalse(tableView.GroupHeaderRows.Any(x => x.IsPinned), "headers in the rows are never pinned");

        // The sticky header is not in the scrolled content, so its position in the viewport is direct.
        var top = tableView.StickyGroupHeader.TransformToVisual(scrollViewer).TransformPoint(default).Y;
        Assert.AreEqual(0d, top, 1d, "the sticky header should be at the top of the viewport");
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
