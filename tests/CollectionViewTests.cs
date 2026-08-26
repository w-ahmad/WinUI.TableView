using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUI.TableView;
using WinUI.TableView.Helpers;

namespace WinUI.TableView.Tests;

[TestClass]
public class CollectionViewTests
{
    [UITestMethod]
    public void Initializes_With_Source()
    {
        var src = CreateItems(5);
        var view = new CollectionView(src);
        Assert.AreEqual(src.Count, view.Count);
        foreach (var item in src)
        {
            Assert.IsTrue(view.Contains(item));
        }
    }

    [UITestMethod]
    public void Initializes_With_Pre_Sorted_Source()
    {
        var src = CreateItems(6).OrderBy(x => x.Value).ToList();
        var view = new CollectionView(src);
        Assert.AreEqual(src.Count, view.Count);
        var orderedValues = view.Select(i => ((TestItem)i).Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, orderedValues);
    }

    [UITestMethod]
    public void Filter_Description_Filters_Items()
    {
        var src = CreateItems(6);
        var view = new CollectionView(src);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(3));
        Assert.AreEqual(3, view.Count);
        foreach (var item in view)
        {
            Assert.IsTrue(((TestItem)item).Value > 3);
        }
    }

    [UITestMethod]
    public void Sorting_Description_Sorts_Items()
    {
        var src = CreateItems(4);
        var view = new CollectionView(src);
        view.SortDescriptions.Add(SortByValueAscending());
        var orderedValues = view.Select(i => ((TestItem)i).Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, orderedValues);
    }

    [UITestMethod]
    public void LiveShaping_Reacts_To_Property_Changes_Filter()
    {
        var src = CreateItems(3);
        var view = new CollectionView(src, liveShapingEnabled: true);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(1)); // keep Value > 1

        Assert.AreEqual(2, view.Count);

        var item0 = src[0];
        item0.Value = 1;
        Assert.IsFalse(view.Contains(item0));

        var item2 = src[2];
        item2.Value = 2;
        Assert.IsTrue(view.Contains(item2));
    }

    [UITestMethod]
    public void LiveShaping_Reacts_To_Property_Changes_Sort()
    {
        var src = CreateItems(3); // Values: 3,2,1
        var view = new CollectionView(src, liveShapingEnabled: true);
        view.SortDescriptions.Add(SortByValueAscending());
        var valuesBefore = view.Select(i => ((TestItem)i).Value).ToArray();
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, valuesBefore);

        var item0 = src[0];
        item0.Value = 0;
        var valuesAfter = view.Select(i => ((TestItem)i).Value).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 1, 2 }, valuesAfter);
    }

    [TestMethod]
    public void Add_Remove_Operations_Update_View_With_Filter()
    {
        var src = CreateItems(0);
        var view = new CollectionView(src);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(5));

        var a = new TestItem { Id = 1, Name = "A", Value = 10 };
        var b = new TestItem { Id = 2, Name = "B", Value = 3 };

        src.Add(a); // passes filter
        src.Add(b); // filtered out
        Assert.IsTrue(view.Contains(a));
        Assert.IsFalse(view.Contains(b));

        src.Remove(a);
        Assert.IsFalse(view.Contains(a));
    }

    [UITestMethod]
    public void Current_Item_Navigation_Works()
    {
        var src = CreateItems(3);
        var view = new CollectionView(src);

        Assert.IsTrue(view.MoveCurrentToFirst());
        Assert.IsTrue(Equals(view.CurrentItem, src[0]));

        Assert.IsTrue(view.MoveCurrentToNext());
        Assert.IsTrue(Equals(view.CurrentItem, src[1]));

        Assert.IsTrue(view.MoveCurrentToLast());
        Assert.IsTrue(Equals(view.CurrentItem, src[2]));

        Assert.IsTrue(view.MoveCurrentToPrevious());
        Assert.IsTrue(Equals(view.CurrentItem, src[1]));

        Assert.IsTrue(view.MoveCurrentTo(src[0]));
        Assert.IsTrue(Equals(view.CurrentItem, src[0]));
    }

    [UITestMethod]
    public void Refresh_Methods_Rebuild_View()
    {
        var src = CreateItems(4);
        var view = new CollectionView(src);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(2));
        view.RefreshFilter();
        foreach (var item in view)
        {
            Assert.IsTrue(((TestItem)item).Value > 2);
        }

        view.SortDescriptions.Add(SortByNameDescending());
        view.RefreshSorting();
        var names = view.Select(i => ((TestItem)i).Name).ToArray();
        var expected = names.OrderByDescending(n => n).ToArray();
        CollectionAssert.AreEqual(expected, names);

        src.Add(new TestItem { Id = 99, Name = "ZZ", Value = 100 });
        Assert.IsTrue(view.Contains(src[4]));
    }

    [UITestMethod]
    public void Insert_RemoveAt_Use_View_Indexing()
    {
        var src = CreateItems(3);
        var view = new CollectionView(src);
        view.Insert(1, new TestItem { Id = 10, Name = "X", Value = 50 });
        Assert.AreEqual(4, src.Count);
        Assert.AreEqual(4, view.Count);

        var itemAt1 = view.ElementAt(1);
        view.RemoveAt(1);
        Assert.IsFalse(src.Contains(itemAt1));
        Assert.IsFalse(view.Contains(itemAt1));
    }

    [UITestMethod]
    public void Compare_Uses_SortDescriptions()
    {
        var sd = SortByValueAscending();
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 5 },
            new() { Id = 2, Name = "B", Value = 3 },
        };
        var view = new CollectionView(src);
        view.SortDescriptions.Add(sd);

        var cmp = view.Compare(src[0], src[1]);
        Assert.IsTrue(cmp > 0);
    }

    [UITestMethod]
    public void LiveShaping_Disabled_Ignores_Property_Changes()
    {
        var src = CreateItems(4);
        var view = new CollectionView(src, liveShapingEnabled: false);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(2));
        // With live shaping disabled, changing Value should not trigger view update automatically
        Assert.AreEqual(2, view.Count);
        var first = src[0];
        first.Value = 1; // would fail filter if live shaping was enabled
        Assert.IsTrue(view.Contains(first)); // still there
        view.RefreshFilter();
        Assert.IsFalse(view.Contains(first));
    }

    [UITestMethod]
    public void Multi_Sort_Tie_Breaking_Works()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "Beta", Value = 10 },
            new() { Id = 2, Name = "Alpha", Value = 10 },
            new() { Id = 3, Name = "Gamma", Value = 5 },
        };
        var view = new CollectionView(src);
        view.SortDescriptions.Add(new SortDescription(nameof(TestItem.Value), SortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription(nameof(TestItem.Name), SortDirection.Descending));

        var names = view.Select(i => ((TestItem)i).Name).ToArray();
        // Value ascending puts Gamma (5) first, then Value=10 items with Name descending: Beta, Alpha
        CollectionAssert.AreEqual(new[] { "Gamma", "Beta", "Alpha" }, names);
    }

    [UITestMethod]
    public void Filter_With_PropertyName_Only_Responds_To_Relevant_Changes()
    {
        var src = CreateItems(3);
        var view = new CollectionView(src, liveShapingEnabled: true);
        // Filter applies only when Value changes
        view.FilterDescriptions.Add(new FilterDescription(nameof(TestItem.Value), item => item is TestItem ti && ti.Value >= 2));
        Assert.AreEqual(2, view.Count);

        var item1 = src[1]; // Value=2
        item1.Name = "Changed"; // irrelevant change
        Assert.IsTrue(view.Contains(item1));

        item1.Value = 1; // relevant change, should drop
        Assert.IsFalse(view.Contains(item1));
    }

    [UITestMethod]
    public void Source_Move_Replace_Reset_Are_Handled()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 0, Name = "A", Value = 3 },
            new() { Id = 1, Name = "B", Value = 2 },
            new() { Id = 2, Name = "C", Value = 1 },
        };
        var view = new CollectionView(src);

        // Move last to first
        src.Move(2, 0);
        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, view.Select(x => ((TestItem)x).Value).ToArray());

        // Replace at index 1
        var replacement = new TestItem { Id = 99, Name = "Z", Value = 99 };
        src[1] = replacement;
        Assert.AreEqual(replacement, view.ElementAt(1));

        // Reset
        src.Clear();
        Assert.AreEqual(0, view.Count);
    }

    [UITestMethod]
    public void CurrentPosition_Adjusts_On_Insert_Remove_Before_Current()
    {
        var src = CreateItems(4);
        var view = new CollectionView(src);
        Assert.IsTrue(view.MoveCurrentToPosition(2));
        var cur = view.CurrentItem;
        Assert.AreEqual(src[2], cur);

        // Insert before current
        view.Insert(1, new TestItem { Id = 10, Name = "X", Value = 50 });
        Assert.AreEqual(3, view.CurrentPosition);
        Assert.AreEqual(cur, view.CurrentItem);

        // Remove before current
        view.RemoveAt(1);
        Assert.AreEqual(2, view.CurrentPosition);
        Assert.AreEqual(cur, view.CurrentItem);
    }

    [UITestMethod]
    public void Duplicates_Are_Supported_And_Filtered_Individually()
    {
        var a = new TestItem { Id = 1, Name = "Dup", Value = 5 };
        var src = new ObservableCollection<TestItem> { a, a, new() { Id = 2, Name = "Other", Value = 1 } };
        var view = new CollectionView(src);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(2));
        Assert.AreEqual(2, view.Count);
        a.Value = 1; // drops both duplicates
        Assert.AreEqual(0, view.Count);
    }

    [UITestMethod]
    public void Empty_Source_Produces_Empty_View()
    {
        var src = new ObservableCollection<TestItem>();
        var view = new CollectionView(src);
        Assert.AreEqual(0, view.Count);
        view.FilterDescriptions.Add(FilterByValueGreaterThan(0));
        view.Refresh();
        Assert.AreEqual(0, view.Count);
    }

    [UITestMethod]
    public void Single_Group_Description_Groups_Items()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
            new() { Id = 3, Name = "C", Value = 1 },
            new() { Id = 4, Name = "D", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        Assert.AreEqual(2, view.CollectionGroups!.Count);

        var group1 = (CollectionViewGroup)view.CollectionGroups[0];
        var group2 = (CollectionViewGroup)view.CollectionGroups[1];
        Assert.AreEqual(1, ((TableViewGroupInfo)group1.Group!).Key);
        Assert.AreEqual(2, ((TableViewGroupInfo)group2.Group!).Key);
        CollectionAssert.AreEqual(new[] { "A", "C" }, group1.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
        CollectionAssert.AreEqual(new[] { "B", "D" }, group2.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());

        // The flat view is pruned/reordered to match what's currently visible - with everything expanded,
        // that's every item, flattened in group order (Value=1's A, C, then Value=2's B, D), not source order.
        CollectionAssert.AreEqual(new[] { "A", "C", "B", "D" }, view.Select(i => ((TestItem)i).Name).ToArray());
    }

    [UITestMethod]
    public void Multi_Level_Group_Descriptions_Create_Nested_Flattened_Groups()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
            new() { Id = 3, Name = "C", Value = 1 },
            new() { Id = 4, Name = "D", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());
        view.GroupDescriptions.Add(GroupByName());

        // Depth-first flattened order: Value-1 (parent, empty), Name-A (leaf), Name-C (leaf),
        // Value-2 (parent, empty), Name-B (leaf), Name-D (leaf).
        Assert.AreEqual(6, view.CollectionGroups!.Count);

        var parent1 = (CollectionViewGroup)view.CollectionGroups[0];
        Assert.AreEqual(1, ((TableViewGroupInfo)parent1.Group!).Key);
        Assert.AreEqual(0, parent1.GroupItems!.Count);

        var leafA = (CollectionViewGroup)view.CollectionGroups[1];
        Assert.AreEqual("A", ((TableViewGroupInfo)leafA.Group!).Key);
        CollectionAssert.AreEqual(new[] { "A" }, leafA.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());

        var leafC = (CollectionViewGroup)view.CollectionGroups[2];
        Assert.AreEqual("C", ((TableViewGroupInfo)leafC.Group!).Key);

        var parent2 = (CollectionViewGroup)view.CollectionGroups[3];
        Assert.AreEqual(2, ((TableViewGroupInfo)parent2.Group!).Key);
        Assert.AreEqual(0, parent2.GroupItems!.Count);

        var leafB = (CollectionViewGroup)view.CollectionGroups[4];
        Assert.AreEqual("B", ((TableViewGroupInfo)leafB.Group!).Key);

        var leafD = (CollectionViewGroup)view.CollectionGroups[5];
        Assert.AreEqual("D", ((TableViewGroupInfo)leafD.Group!).Key);

        // With everything expanded, the flat view holds every item flattened in depth-first group order.
        CollectionAssert.AreEqual(new[] { "A", "C", "B", "D" }, view.Select(i => ((TestItem)i).Name).ToArray());
    }

    [UITestMethod]
    public void Group_Descriptions_Direct_Add_Without_Deferral_Updates_Groups()
    {
        var src = CreateItems(4); // Values: 4,3,2,1
        var view = new CollectionView(src);

        // Adding directly (no DeferRefresh) must still rebuild CollectionGroups.
        view.GroupDescriptions.Add(GroupByValue());

        Assert.AreEqual(4, view.CollectionGroups!.Count);
    }

    [UITestMethod]
    public void Default_Group_State_Collapsed_Starts_Groups_Collapsed()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Collapsed;
        view.GroupDescriptions.Add(GroupByValue());

        Assert.AreEqual(2, view.CollectionGroups!.Count);
        var group1 = (CollectionViewGroup)view.CollectionGroups[0];
        Assert.IsFalse(((TableViewGroupInfo)group1.Group!).IsExpanded);
        Assert.AreEqual(0, group1.GroupItems!.Count); // Collapsed - no items surfaced through this group.
        Assert.AreEqual(0, view.Count); // Both groups collapsed - nothing is currently visible.
    }

    [UITestMethod]
    public void Changing_Default_Group_State_Reapplies_To_Non_Overridden_Groups()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.GroupDescriptions.Add(GroupByValue()); // CollectionView's own default is Collapsed.

        Assert.IsTrue(view.CollectionGroups!.Cast<CollectionViewGroup>().All(g => g.GroupItems!.Count == 0));

        view.DefaultGroupState = TableViewGroupState.Expanded;

        Assert.IsTrue(view.CollectionGroups!.Cast<CollectionViewGroup>().All(g => g.GroupItems!.Count == 1));
        Assert.AreEqual(2, view.Count); // Both groups are now expanded, so both items are visible.
    }

    [UITestMethod]
    public void Explicitly_Toggled_Group_Ignores_Later_Default_State_Changes()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.GroupDescriptions.Add(GroupByValue()); // Both groups start collapsed.

        ToggleGroup((CollectionViewGroup)view.CollectionGroups![0]); // Explicitly expand just this one.
        Assert.AreEqual(1, ((CollectionViewGroup)view.CollectionGroups![0]).GroupItems!.Count);
        Assert.AreEqual(0, ((CollectionViewGroup)view.CollectionGroups![1]).GroupItems!.Count);

        view.DefaultGroupState = TableViewGroupState.Expanded; // Both groups now show.
        Assert.AreEqual(1, ((CollectionViewGroup)view.CollectionGroups![0]).GroupItems!.Count);
        Assert.AreEqual(1, ((CollectionViewGroup)view.CollectionGroups![1]).GroupItems!.Count);

        view.DefaultGroupState = TableViewGroupState.Collapsed; // The explicitly-expanded group stays expanded.
        Assert.AreEqual(1, ((CollectionViewGroup)view.CollectionGroups![0]).GroupItems!.Count);
        Assert.AreEqual(0, ((CollectionViewGroup)view.CollectionGroups![1]).GroupItems!.Count);
    }

    [UITestMethod]
    public void Ungrouping_And_Regrouping_Does_Not_Resurrect_Prior_Expanded_State()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src); // Default group state is Collapsed.
        var firstGrouping = GroupByValue();
        view.GroupDescriptions.Add(firstGrouping);

        ToggleGroup((CollectionViewGroup)view.CollectionGroups![0]); // Explicitly expand just this one.
        Assert.AreEqual(1, ((CollectionViewGroup)view.CollectionGroups![0]).GroupItems!.Count);

        view.GroupDescriptions.Remove(firstGrouping); // Ungroup.

        // Re-group: a brand new GroupDescription instance, exactly like the ColumnGroupDescription
        // TableViewColumnHeader.Group() creates when a column is grouped again after being ungrouped.
        view.GroupDescriptions.Add(GroupByValue());

        // The prior expand override must not resurrect under the new GroupDescription instance - both
        // groups should start at the (still Collapsed) default again, not "remember" the old expand state.
        Assert.IsTrue(view.CollectionGroups!.Cast<CollectionViewGroup>().All(g => g.GroupItems!.Count == 0));
    }

    [UITestMethod]
    public void Collapsing_A_Leaf_Group_Hides_Its_Items()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
            new() { Id = 3, Name = "C", Value = 1 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        ToggleGroup((CollectionViewGroup)view.CollectionGroups![0]);

        // ToggleGroup rebuilds CollectionGroups, so re-fetch rather than reuse the pre-toggle instance.
        var group1 = (CollectionViewGroup)view.CollectionGroups![0];
        Assert.AreEqual(0, group1.GroupItems!.Count); // Collapsed - no items surfaced through this group.
        Assert.AreEqual(1, view.Count); // Only the still-expanded Value=2 group's item ("B") is visible.

        ToggleGroup((CollectionViewGroup)view.CollectionGroups![0]);
        group1 = (CollectionViewGroup)view.CollectionGroups![0];
        Assert.AreEqual(2, group1.GroupItems!.Count); // Re-expanded - both Value=1 items surfaced again.
        Assert.AreEqual(3, view.Count); // Both groups now expanded - everything is visible again.
    }

    [UITestMethod]
    public void Collapsing_A_Parent_Group_Hides_Descendant_Subgroups_And_Items()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
            new() { Id = 3, Name = "C", Value = 1 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());
        view.GroupDescriptions.Add(GroupByName());

        // Flattened order: Value-1 (parent), Name-A (leaf), Name-C (leaf), Value-2 (parent), Name-B (leaf).
        Assert.AreEqual(5, view.CollectionGroups!.Count);

        var parent1 = (CollectionViewGroup)view.CollectionGroups[0];
        ToggleGroup(parent1);

        // Collapsing the parent removes its subgroup headers entirely, not just their items.
        Assert.AreEqual(3, view.CollectionGroups!.Count);
        Assert.AreEqual(1, view.Count); // Only Value=2's "B" remains visible - Value=1's subtree is hidden.

        var remainingLeaf = (CollectionViewGroup)view.CollectionGroups[2];
        Assert.AreEqual("B", ((TableViewGroupInfo)remainingLeaf.Group!).Key);
        CollectionAssert.AreEqual(new[] { "B" }, remainingLeaf.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
    }

    [UITestMethod]
    public void Collapsed_State_Survives_A_Rebuild_Triggered_By_Something_Else()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        var group1 = (CollectionViewGroup)view.CollectionGroups![0];
        ToggleGroup(group1);
        group1 = (CollectionViewGroup)view.CollectionGroups![0];
        Assert.AreEqual(0, group1.GroupItems!.Count);

        // Adding a single item goes through HandleItemAdded (not HandleSourceChanged), which rebuilds
        // CollectionGroups via HandleGroupChanged and so constructs brand new TableViewGroupInfo
        // instances - the collapsed state must still stick.
        src.Add(new TestItem { Id = 3, Name = "E", Value = 1 });

        // Still collapsed - the new Value=1 item stays hidden too, even under a brand new
        // TableViewGroupInfo instance for the (still collapsed) Value=1 group.
        group1 = (CollectionViewGroup)view.CollectionGroups![0];
        Assert.AreEqual(0, group1.GroupItems!.Count);
        Assert.AreEqual(1, view.Count); // Only Value=2's "B" is visible - Value=1's group (A, E) stays collapsed.
    }

    [UITestMethod]
    public void Adding_An_Item_Places_It_In_Its_Existing_Group()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        src.Add(new TestItem { Id = 3, Name = "C", Value = 1 }); // Joins the existing Value=1 group.

        Assert.AreEqual(2, view.CollectionGroups!.Count); // Still just two groups - no new one created.
        var group1 = (CollectionViewGroup)view.CollectionGroups[0];
        CollectionAssert.AreEqual(new[] { "A", "C" }, group1.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
        Assert.AreEqual(2, ((TableViewGroupInfo)group1.Group!).Count);
        Assert.AreEqual(3, view.Count);
    }

    [UITestMethod]
    public void Adding_An_Item_With_A_New_Key_Creates_A_New_Group()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        Assert.AreEqual(1, view.CollectionGroups!.Count);

        src.Add(new TestItem { Id = 2, Name = "B", Value = 2 }); // A brand new Value=2 group.

        Assert.AreEqual(2, view.CollectionGroups!.Count);
        var newGroup = (CollectionViewGroup)view.CollectionGroups[1];
        Assert.AreEqual(2, ((TableViewGroupInfo)newGroup.Group!).Key);
        CollectionAssert.AreEqual(new[] { "B" }, newGroup.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
        Assert.AreEqual(2, view.Count);
    }

    [UITestMethod]
    public void Removing_An_Item_Updates_Its_Group()
    {
        var a = new TestItem { Id = 1, Name = "A", Value = 1 };
        var c = new TestItem { Id = 3, Name = "C", Value = 1 };
        var src = new ObservableCollection<TestItem>
        {
            a,
            new() { Id = 2, Name = "B", Value = 2 },
            c,
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        src.Remove(a);

        Assert.AreEqual(2, view.CollectionGroups!.Count); // Value=1 group still exists - C remains.
        var group1 = (CollectionViewGroup)view.CollectionGroups[0];
        CollectionAssert.AreEqual(new[] { "C" }, group1.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
        Assert.AreEqual(1, ((TableViewGroupInfo)group1.Group!).Count);
        Assert.AreEqual(2, view.Count);
    }

    [UITestMethod]
    public void Removing_The_Last_Item_In_A_Group_Removes_The_Group_Header()
    {
        var a = new TestItem { Id = 1, Name = "A", Value = 1 };
        var src = new ObservableCollection<TestItem>
        {
            a,
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        Assert.AreEqual(2, view.CollectionGroups!.Count);

        src.Remove(a); // The only Value=1 item - its group should disappear entirely, not just empty out.

        Assert.AreEqual(1, view.CollectionGroups!.Count);
        Assert.AreEqual(2, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[0]).Group!).Key);
        Assert.AreEqual(1, view.Count);
    }

    [UITestMethod]
    public void Removing_An_Item_Hidden_In_A_Collapsed_Group_Updates_The_Group()
    {
        var a = new TestItem { Id = 1, Name = "A", Value = 1 };
        var c = new TestItem { Id = 3, Name = "C", Value = 1 };
        var src = new ObservableCollection<TestItem> { a, c };
        var view = new CollectionView(src); // Groups start collapsed per CollectionView's own default.
        view.GroupDescriptions.Add(GroupByValue());

        var group1 = (CollectionViewGroup)view.CollectionGroups![0];
        Assert.AreEqual(0, group1.GroupItems!.Count); // Collapsed - nothing surfaced through the group yet.
        Assert.AreEqual(0, view.Count); // The only group is collapsed - nothing is currently visible.

        src.Remove(a); // Removing an item that's currently hidden inside the collapsed group.

        group1 = (CollectionViewGroup)view.CollectionGroups![0];
        ToggleGroup(group1); // Expand to check what's actually left inside.

        var expandedGroup = (CollectionViewGroup)view.CollectionGroups![0];
        CollectionAssert.AreEqual(new[] { "C" }, expandedGroup.GroupItems!.Select(i => ((TestItem)i!).Name).ToArray());
        Assert.AreEqual(1, view.Count);
    }

    [UITestMethod]
    public void Adding_An_Item_Places_It_In_The_Correct_Nested_Group()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());
        view.GroupDescriptions.Add(GroupByName());

        // Flattened: Value-1 (parent), Name-A (leaf), Value-2 (parent), Name-B (leaf).
        Assert.AreEqual(4, view.CollectionGroups!.Count);

        src.Add(new TestItem { Id = 3, Name = "A", Value = 1 }); // Joins the existing Value=1/Name=A leaf group.

        Assert.AreEqual(4, view.CollectionGroups!.Count); // No new group - same Value/Name combo.
        var leafA = (CollectionViewGroup)view.CollectionGroups[1];
        Assert.AreEqual("A", ((TableViewGroupInfo)leafA.Group!).Key);
        Assert.AreEqual(2, leafA.GroupItems!.Count);
        Assert.AreEqual(3, view.Count);

        src.Add(new TestItem { Id = 4, Name = "D", Value = 1 }); // A brand new Value=1/Name=D leaf group.

        Assert.AreEqual(5, view.CollectionGroups!.Count);
        Assert.AreEqual(4, view.Count);
    }

    [UITestMethod]
    public void CollectionView_Add_And_Remove_Respect_Grouping()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        view.GroupDescriptions.Add(GroupByValue());

        view.Add(new TestItem { Id = 2, Name = "B", Value = 2 }); // Via CollectionView.Add, not the source directly.

        Assert.AreEqual(2, view.CollectionGroups!.Count);
        Assert.AreEqual(2, view.Count);

        view.RemoveAt(0); // Removes whatever is first in the (grouped/reordered) view - item "A".

        Assert.AreEqual(1, view.CollectionGroups!.Count);
        Assert.AreEqual(1, view.Count);
    }

    [UITestMethod]
    public void Changing_A_Group_Descriptions_Direction_Reorders_Groups()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        var groupDescription = GroupByValue();
        view.GroupDescriptions.Add(groupDescription);

        // Ascending by default.
        Assert.AreEqual(1, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);

        groupDescription.Direction = SortDirection.Descending;
        view.RefreshGrouping();

        // Mutating the same GroupDescription's Direction and refreshing reorders groups in place -
        // this is exactly what TableViewColumnHeader.SortGroupDescription does when toggling a grouped
        // column's sort direction, instead of adding a redundant SortDescription.
        Assert.AreEqual(2, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);
    }

    [UITestMethod]
    public void Group_Sort_Mode_Count_Orders_Groups_By_Size_Not_Key()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 1 },
            new() { Id = 3, Name = "C", Value = 1 },
            new() { Id = 4, Name = "D", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        var groupDescription = GroupByValue();
        groupDescription.SortMode = GroupSortMode.Count;
        view.GroupDescriptions.Add(groupDescription);

        // Value=1 has 3 items, Value=2 has 1 - ascending-by-key (the default mode) would put Value=1
        // first; ascending-by-count puts the SMALLER group first instead, so Value=2 comes first here.
        var first = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!;
        Assert.AreEqual(2, first.Key);
        Assert.AreEqual(1, first.Count);

        var second = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[1]).Group!;
        Assert.AreEqual(1, second.Key);
        Assert.AreEqual(3, second.Count);
    }

    [UITestMethod]
    public void Changing_A_Group_Descriptions_Sort_Mode_Reorders_Groups()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "A", Value = 1 },
            new() { Id = 2, Name = "B", Value = 1 },
            new() { Id = 3, Name = "C", Value = 1 },
            new() { Id = 4, Name = "D", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        var groupDescription = GroupByValue();
        view.GroupDescriptions.Add(groupDescription);

        // Default: Key mode, ascending - Value=1 (the smaller key) first, same as today.
        Assert.AreEqual(1, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);

        groupDescription.SortMode = GroupSortMode.Count;
        view.RefreshGrouping();

        // Switching to Count mode (still Ascending) puts the smaller GROUP first instead - Value=2 (1 item).
        Assert.AreEqual(2, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);

        groupDescription.Direction = SortDirection.Descending;
        view.RefreshGrouping();

        // The two toggles compose: Count mode + Descending puts the BIGGER group first - Value=1 (3 items).
        Assert.AreEqual(1, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);
    }

    [UITestMethod]
    public void Group_Sort_Mode_Count_Breaks_Ties_By_Key()
    {
        var src = new ObservableCollection<TestItem>
        {
            // Value=2's item is encountered first, but ties should break ascending by key (Value=1
            // first), not by source encounter order.
            new() { Id = 1, Name = "A", Value = 2 },
            new() { Id = 2, Name = "B", Value = 1 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;
        var groupDescription = GroupByValue();
        groupDescription.SortMode = GroupSortMode.Count;
        view.GroupDescriptions.Add(groupDescription);

        // Both groups have exactly 1 item - a tie. The tie-break is always ascending by key, so Value=1
        // comes first despite Value=2 appearing first in the source.
        Assert.AreEqual(1, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!).Key);
        Assert.AreEqual(2, ((TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[1]).Group!).Key);
    }

    [UITestMethod]
    public void Multi_Level_Group_Sort_Mode_Applies_Independently_Per_Level()
    {
        var src = new ObservableCollection<TestItem>
        {
            new() { Id = 1, Name = "Z", Value = 1 },
            new() { Id = 2, Name = "M", Value = 2 },
            new() { Id = 3, Name = "A", Value = 2 },
        };
        var view = new CollectionView(src);
        view.DefaultGroupState = TableViewGroupState.Expanded;

        var outerGroupDescription = GroupByValue();
        outerGroupDescription.SortMode = GroupSortMode.Count;
        outerGroupDescription.Direction = SortDirection.Descending; // biggest outer group first
        view.GroupDescriptions.Add(outerGroupDescription);
        view.GroupDescriptions.Add(GroupByName()); // inner level: default Key mode, ascending

        // Outer: Value=2 has 2 items, Value=1 has 1 - Count+Descending puts Value=2 (bigger) first,
        // the opposite of the default key-ascending order.
        var outerFirst = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups![0]).Group!;
        Assert.AreEqual(2, outerFirst.Key);

        // Inner (still Key mode) keeps ordering alphabetically within that outer group: "A" before "M".
        var innerFirst = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[1]).Group!;
        Assert.AreEqual("A", innerFirst.Key);

        var innerSecond = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[2]).Group!;
        Assert.AreEqual("M", innerSecond.Key);

        // Outer: Value=1 (1 item) comes last.
        var outerSecond = (TableViewGroupInfo)((CollectionViewGroup)view.CollectionGroups[3]).Group!;
        Assert.AreEqual(1, outerSecond.Key);
    }

    /// <summary>
    /// Flips a group's expanded/collapsed state, the same DependencyProperty the header row's expand/collapse
    /// button toggles - a stand-in for the removed CollectionView.ToggleGroup convenience method.
    /// </summary>
    private static void ToggleGroup(CollectionViewGroup group)
    {
        var info = (TableViewGroupInfo)group.Group!;
        info.IsExpanded = !info.IsExpanded;
    }

    private static GroupDescription GroupByValue()
    {
        return new GroupDescription(null, valueDelegate: item => ((TestItem)item!).Value);
    }

    private static GroupDescription GroupByName()
    {
        return new GroupDescription(null, valueDelegate: item => ((TestItem)item!).Name);
    }

    private static ObservableCollection<TestItem> CreateItems(int count)
    {
        var list = new ObservableCollection<TestItem>();
        for (var i = 0; i < count; i++)
        {
            list.Add(new TestItem { Id = i, Name = $"Item{i}", Value = count - i });
        }
        return list;
    }

    private static FilterDescription FilterByValueGreaterThan(int min)
    {
        return new FilterDescription(null, item => item is TestItem ti && ti.Value > min);
    }

    private static SortDescription SortByValueAscending()
    {
        return new SortDescription(nameof(TestItem.Value), SortDirection.Ascending);
    }

    private static SortDescription SortByNameDescending()
    {
        return new SortDescription(nameof(TestItem.Name), SortDirection.Descending);
    }
}

internal partial class TestItem : INotifyPropertyChanged
{
    private int _id;
    private string _name = string.Empty;
    private int _value;

    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value; OnPropertyChanged(nameof(Id));
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value; OnPropertyChanged(nameof(Name));
            }
        }
    }

    public int Value
    {
        get => _value; set
        {
            if (_value != value)
            {
                _value = value; OnPropertyChanged(nameof(Value));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString()
    {
        return $"{Id}:{Name}:{Value}";
    }
}

internal class IncrementalItems : ObservableCollection<TestItem>, ISupportIncrementalLoading
{
    public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
    {
        return CreateLoadOperation(count);
    }

    private static IAsyncOperation<LoadMoreItemsResult> CreateLoadOperation(uint count)
    {
        return Task.Run(async () =>
        {
            var items = new List<TestItem>();
            // This Task will be replaced by AsAsyncOperation by MSTest AppContainer runtime automatically
            return new LoadMoreItemsResult { Count = count };
        }).AsAsyncOperation();
    }

    public bool HasMoreItems => true;
}
