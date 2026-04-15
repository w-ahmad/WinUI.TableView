using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using WinUI.TableView;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewHierarchyTests
{
    // ────────────────────────────────────── Defaults ──────────────────────

    [UITestMethod]
    public void IsHierarchicalEnabled_DefaultsFalse()
    {
        var tv = new TableView();
        Assert.IsFalse(tv.IsHierarchicalEnabled);
    }

    [UITestMethod]
    public void HierarchyIndent_DefaultIs16()
    {
        var tv = new TableView();
        Assert.AreEqual(16d, tv.HierarchyIndent);
    }

    [UITestMethod]
    public void ChildrenPath_And_HierarchyItemsSourcePath_ShareSameDP()
    {
        var tv = new TableView();
        tv.HierarchyItemsSourcePath = "Children";
        Assert.AreEqual("Children", tv.ChildrenPath);
        tv.ChildrenPath = "Kids";
        Assert.AreEqual("Kids", tv.HierarchyItemsSourcePath);
    }

    [UITestMethod]
    public void IndentSize_And_HierarchyIndent_ShareSameDP()
    {
        var tv = new TableView();
        tv.HierarchyIndent = 24d;
        Assert.AreEqual(24d, tv.IndentSize);
        tv.IndentSize = 8d;
        Assert.AreEqual(8d, tv.HierarchyIndent);
    }

    // ────────────────────────────────── HasChildItems ─────────────────────

    [UITestMethod]
    public void HasChildItems_ReturnsFalse_WhenHierarchyDisabled()
    {
        var tv = new TableView { ChildrenPath = "Children" };
        var item = new TreeNode("A", new TreeNode("B"));
        Assert.IsFalse(tv.HasChildItems(item));
    }

    [UITestMethod]
    public void HasChildItems_ReturnsFalse_ForNull()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        Assert.IsFalse(tv.HasChildItems(null));
    }

    [UITestMethod]
    public void HasChildItems_ReturnsTrue_WhenChildrenExist()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("Parent", new TreeNode("Child"));
        Assert.IsTrue(tv.HasChildItems(item));
    }

    [UITestMethod]
    public void HasChildItems_ReturnsFalse_WhenNoChildren()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("Leaf");
        Assert.IsFalse(tv.HasChildItems(item));
    }

    [UITestMethod]
    public void HasChildItems_UsesHasChildrenPath_WhenSet()
    {
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            HasChildrenPath = "HasChildren"
        };
        var withFlag = new TreeNodeWithFlag("A", hasChildren: true);
        var withoutFlag = new TreeNodeWithFlag("B", hasChildren: false);
        Assert.IsTrue(tv.HasChildItems(withFlag));
        Assert.IsFalse(tv.HasChildItems(withoutFlag));
    }

    [UITestMethod]
    public void HasChildItems_WorksWithChildrenSelector()
    {
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenSelector = item => item is TreeNode n ? n.Children : null
        };
        Assert.IsTrue(tv.HasChildItems(new TreeNode("P", new TreeNode("C"))));
        Assert.IsFalse(tv.HasChildItems(new TreeNode("Leaf")));
    }

    // ────────────────────────────────── IsItemExpanded ────────────────────

    [UITestMethod]
    public void IsItemExpanded_ReturnsFalse_WhenHierarchyDisabled()
    {
        var tv = new TableView { ChildrenPath = "Children" };
        var item = new TreeNode("A", new TreeNode("B"));
        Assert.IsFalse(tv.IsItemExpanded(item));
    }

    [UITestMethod]
    public void IsItemExpanded_ReturnsFalse_ForNull()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        Assert.IsFalse(tv.IsItemExpanded(null));
    }

    [UITestMethod]
    public void IsItemExpanded_ReturnsFalse_ForLeafNode()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        Assert.IsFalse(tv.IsItemExpanded(new TreeNode("Leaf")));
    }

    [UITestMethod]
    public void IsItemExpanded_ReturnsTrue_ByDefault_ForParentNode()
    {
        // Items not in _collapsedHierarchyItems are considered expanded
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));
        Assert.IsTrue(tv.IsItemExpanded(item));
    }

    [UITestMethod]
    public void IsItemExpanded_ReturnsFalse_AfterCollapsing()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));
        tv.CollapseItem(item);
        Assert.IsFalse(tv.IsItemExpanded(item));
    }

    [UITestMethod]
    public void IsItemExpanded_UsesIsExpandedPath_WhenSet()
    {
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            IsExpandedPath = "IsExpanded"
        };
        var expanded = new ExpandableNode("P", isExpanded: true, new TreeNode("C"));
        var collapsed = new ExpandableNode("Q", isExpanded: false, new TreeNode("C"));
        Assert.IsTrue(tv.IsItemExpanded(expanded));
        Assert.IsFalse(tv.IsItemExpanded(collapsed));
    }

    // ─────────────────────────────── SetItemExpanded ──────────────────────

    [UITestMethod]
    public void SetItemExpanded_True_FiresRowExpandedEvent()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));

        // Collapse first so we can expand
        tv.CollapseItem(item);

        TableViewRowExpansionChangedEventArgs? args = null;
        tv.RowExpanded += (_, e) => args = e;
        tv.SetItemExpanded(item, true);

        Assert.IsNotNull(args);
        Assert.AreEqual(item, args.Item);
        Assert.IsTrue(args.IsExpanded);
    }

    [UITestMethod]
    public void SetItemExpanded_False_FiresRowCollapsedEvent()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));

        TableViewRowExpansionChangedEventArgs? args = null;
        tv.RowCollapsed += (_, e) => args = e;
        tv.SetItemExpanded(item, false);

        Assert.IsNotNull(args);
        Assert.AreEqual(item, args.Item);
        Assert.IsFalse(args.IsExpanded);
    }

    [UITestMethod]
    public void SetItemExpanded_DoesNothing_ForLeafNode()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("Leaf");

        bool eventFired = false;
        tv.RowCollapsed += (_, _) => eventFired = true;
        tv.RowExpanded += (_, _) => eventFired = true;

        tv.SetItemExpanded(item, false);
        tv.SetItemExpanded(item, true);

        Assert.IsFalse(eventFired);
    }

    [UITestMethod]
    public void SetItemExpanded_WritesIsExpandedPath_WhenSet()
    {
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            IsExpandedPath = "IsExpanded"
        };
        var item = new ExpandableNode("P", isExpanded: true, new TreeNode("C"));
        tv.SetItemExpanded(item, false);
        Assert.IsFalse(item.IsExpanded);
    }

    // ──────────────────────────── ToggleItemExpansion ─────────────────────

    [UITestMethod]
    public void ToggleItemExpansion_ExpandsCollapsedNode()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));
        tv.CollapseItem(item);

        tv.ToggleItemExpansion(item);

        Assert.IsTrue(tv.IsItemExpanded(item));
    }

    [UITestMethod]
    public void ToggleItemExpansion_CollapsesExpandedNode()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        var item = new TreeNode("P", new TreeNode("C"));

        tv.ToggleItemExpansion(item);

        Assert.IsFalse(tv.IsItemExpanded(item));
    }

    [UITestMethod]
    public void ToggleItemExpansion_DoesNothing_ForNull()
    {
        var tv = new TableView { IsHierarchicalEnabled = true, ChildrenPath = "Children" };
        tv.ToggleItemExpansion(null); // should not throw
    }

    // ──────────────────────── ExpandItem / CollapseItem ───────────────────

    [UITestMethod]
    public void ExpandItem_ExpandsCollapsedNode()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SetItemExpanded(root, false);
        Assert.IsFalse(tv.Items.Contains(child));

        tv.ExpandItem(root);

        Assert.IsTrue(tv.IsItemExpanded(root));
        Assert.IsTrue(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void CollapseItem_CollapsesExpandedNode()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.IsTrue(tv.Items.Contains(child));

        tv.CollapseItem(root);

        Assert.IsFalse(tv.IsItemExpanded(root));
        Assert.IsFalse(tv.Items.Contains(child));
    }

    // ──────────────────────────── ExpandAll / CollapseAll ─────────────────

    [UITestMethod]
    public void ExpandAll_ExpandsAllCollapsedNodes()
    {
        var grandchild = new TreeNode("Grandchild");
        var child = new TreeNode("Child", grandchild);
        var root = new TreeNode("Root", child);
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SetItemExpanded(root, false);
        tv.SetItemExpanded(child, false);
        Assert.AreEqual(1, tv.Items.Count);

        tv.ExpandAll();

        Assert.AreEqual(3, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(child));
        Assert.IsTrue(tv.Items.Contains(grandchild));
    }

    [UITestMethod]
    public void CollapseAll_CollapsesAllExpandedNodes()
    {
        var grandchild = new TreeNode("Grandchild");
        var child = new TreeNode("Child", grandchild);
        var root = new TreeNode("Root", child);
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(3, tv.Items.Count);

        tv.CollapseAll();

        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsFalse(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void ExpandAll_DoesNothing_WhenHierarchyDisabled()
    {
        var tv = new TableView { IsHierarchicalEnabled = false };
        tv.ExpandAll(); // should not throw
    }

    [UITestMethod]
    public void CollapseAll_DoesNothing_WhenHierarchyDisabled()
    {
        var tv = new TableView { IsHierarchicalEnabled = false };
        tv.CollapseAll(); // should not throw
    }

    // ────────────────────────────── GetHierarchyLevel ─────────────────────

    [UITestMethod]
    public void GetHierarchyLevel_ReturnsZero_ForUnknownItem()
    {
        var tv = new TableView();
        Assert.AreEqual(0, tv.GetHierarchyLevel(new TreeNode("X")));
    }

    [UITestMethod]
    public void GetHierarchyLevel_ReturnsZero_ForNull()
    {
        var tv = new TableView();
        Assert.AreEqual(0, tv.GetHierarchyLevel(null));
    }

    [UITestMethod]
    public void GetHierarchyLevel_Returns_Correct_Levels_After_BuildProcessedSource()
    {
        var child1 = new TreeNode("C1");
        var child2 = new TreeNode("C2");
        var grandchild = new TreeNode("GC");
        child1 = new TreeNode("C1", grandchild);
        var root = new TreeNode("Root", child1, child2);
        var items = new ObservableCollection<TreeNode> { root };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        Assert.AreEqual(0, tv.GetHierarchyLevel(root));
        Assert.AreEqual(1, tv.GetHierarchyLevel(child1));
        Assert.AreEqual(1, tv.GetHierarchyLevel(child2));
        Assert.AreEqual(2, tv.GetHierarchyLevel(grandchild));
    }

    // ─────────────────── Visible items reflect expand/collapse state ──────

    [UITestMethod]
    public void CollapsedItemsChildren_AreNotInDisplayedItems_AfterSourceSet()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        var items = new ObservableCollection<TreeNode> { root };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        // Root is expanded by default — child should be in Items
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(child));

        // Collapse root
        tv.SetItemExpanded(root, false);

        // After collapse, child should not be visible
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsFalse(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void ExpandingItem_AddsChildrenToDisplayedItems()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        var items = new ObservableCollection<TreeNode> { root };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        // Collapse then re-expand
        tv.SetItemExpanded(root, false);
        Assert.IsFalse(tv.Items.Contains(child));

        tv.SetItemExpanded(root, true);
        Assert.IsTrue(tv.Items.Contains(child));
    }

    // ─────────────────────── ChildrenSelector alternative ─────────────────

    [UITestMethod]
    public void ChildrenSelector_WorksAsAlternative_ToChildrenPath()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        var items = new ObservableCollection<TreeNode> { root };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenSelector = item => item is TreeNode n ? n.Children : null,
            ItemsSource = items
        };

        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(child));
        Assert.AreEqual(0, tv.GetHierarchyLevel(root));
        Assert.AreEqual(1, tv.GetHierarchyLevel(child));
    }

    // ───────────────────── Dynamic source mutations ────────────────────────

    [UITestMethod]
    public void AddingRootItem_ToSource_UpdatesDisplayedItems()
    {
        var items = new ObservableCollection<TreeNode>();
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        Assert.AreEqual(0, tv.Items.Count);

        var root = new TreeNode("Root");
        items.Add(root);
        tv.RefreshView(); // source mutation triggers async queue; flush synchronously

        Assert.IsTrue(tv.Items.Contains(root));
    }

    [UITestMethod]
    public void RemovingRootItem_FromSource_UpdatesDisplayedItems()
    {
        var root = new TreeNode("Root");
        var items = new ObservableCollection<TreeNode> { root };
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        Assert.IsTrue(tv.Items.Contains(root));

        items.Remove(root);
        tv.RefreshView(); // source mutation triggers async queue; flush synchronously

        Assert.IsFalse(tv.Items.Contains(root));
    }

    [UITestMethod]
    public void AddingExpandedParent_WithChildren_ToSource_ShowsAllNodes()
    {
        var items = new ObservableCollection<TreeNode>();
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);
        items.Add(root);
        tv.RefreshView(); // source mutation triggers async queue; flush synchronously

        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void DynamicSource_NotObserved_WhenHierarchyDisabled()
    {
        var items = new ObservableCollection<TreeNode>();
        var tv = new TableView
        {
            IsHierarchicalEnabled = false,
            ChildrenPath = "Children",
            ItemsSource = items
        };

        var root = new TreeNode("Root");
        items.Add(root);
        tv.RefreshView(); // source mutation triggers async queue; flush synchronously

        // Non-hierarchy mode goes through _collectionView which watches the source directly
        Assert.IsTrue(tv.Items.Contains(root));
    }

    // ─────────────────────────────── Filtering ────────────────────────────

    [UITestMethod]
    public void Filter_MatchingLeaf_ShowsLeafAndAllAncestors()
    {
        // Root → Parent → Child ("Child" matches filter)
        var child = new TreeNode("Child");
        var parent = new TreeNode("Parent", child);
        var root = new TreeNode("Root", parent);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Child"));

        Assert.IsTrue(tv.Items.Contains(root), "Root (ancestor) should be visible");
        Assert.IsTrue(tv.Items.Contains(parent), "Parent (ancestor) should be visible");
        Assert.IsTrue(tv.Items.Contains(child), "Matching leaf should be visible");
    }

    [UITestMethod]
    public void Filter_NonMatchingSubtree_IsHidden()
    {
        var matchingChild = new TreeNode("Match");
        var matchingParent = new TreeNode("MatchParent", matchingChild);
        var otherParent = new TreeNode("OtherParent", new TreeNode("OtherChild"));
        var root = new TreeNode("Root", matchingParent, otherParent);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Match"));

        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(matchingParent));
        Assert.IsTrue(tv.Items.Contains(matchingChild));
        Assert.IsFalse(tv.Items.Contains(otherParent), "Subtree with no match should be hidden");
    }

    [UITestMethod]
    public void Filter_ClearingFilter_RestoresAllItems()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Child"));

        Assert.AreEqual(2, tv.Items.Count);

        tv.FilterDescriptions.Clear();

        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void Filter_CollapsedAncestor_IsForceExpandedToShowMatch()
    {
        var child = new TreeNode("Match");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        // Collapse root before applying filter
        tv.SetItemExpanded(root, false);
        Assert.AreEqual(1, tv.Items.Count, "Only root visible when collapsed");

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Match"));

        Assert.IsTrue(tv.Items.Contains(root), "Collapsed ancestor should appear");
        Assert.IsTrue(tv.Items.Contains(child), "Matching child should be force-expanded into view");
    }

    [UITestMethod]
    public void Filter_DirectlyMatchingParent_ShowsWithChildren_WhenExpanded()
    {
        // Both parent and child have the same name — both pass the filter independently.
        var matchingChild = new TreeNode("Match");
        var root = new TreeNode("Match", matchingChild);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Match"));

        // Both root and matchingChild pass the filter directly; both are visible.
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(matchingChild));
    }

    [UITestMethod]
    public void Filter_DirectlyMatchingParent_CollapsedShowsOnlyParent()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Match", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SetItemExpanded(root, false);

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "Match"));

        Assert.IsTrue(tv.Items.Contains(root));
        // root matches directly and is collapsed — children not force-expanded
        Assert.IsFalse(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void Filter_NoMatch_ShowsEmptyList()
    {
        var root = new TreeNode("Root", new TreeNode("Child"));

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name), item =>
            item is TreeNode n && n.Name == "NoSuchNode"));

        Assert.AreEqual(0, tv.Items.Count);
    }

    [UITestMethod]
    public void Filter_GetAllHierarchyItemsFlat_ReturnsAllNodesIncludingCollapsed()
    {
        var grandchild = new TreeNode("Grandchild");
        var child = new TreeNode("Child", grandchild);
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        // Collapse root so grandchild is hidden from the display
        tv.SetItemExpanded(root, false);
        Assert.AreEqual(1, tv.Items.Count);

        var all = tv.GetAllHierarchyItemsFlat().ToList();
        Assert.AreEqual(3, all.Count);
        Assert.IsTrue(all.Contains(root));
        Assert.IsTrue(all.Contains(child));
        Assert.IsTrue(all.Contains(grandchild));
    }

    [UITestMethod]
    public void Filter_MultipleDescriptions_AndsLogic()
    {
        // Both filters must pass; only "AB" contains both "A" and "B".
        var nodeAB = new TreeNode("AB");
        var nodeA = new TreeNode("A");
        var nodeB = new TreeNode("B");
        var nodeC = new TreeNode("C");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { nodeC, nodeB, nodeA, nodeAB }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            item => item is TreeNode n && n.Name.Contains("A")));
        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            item => item is TreeNode n && n.Name.Contains("B")));

        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(nodeAB));
    }

    [UITestMethod]
    public void ClearAllFilters_InHierarchyMode_RemovesFilter()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        // Filter to only root; child doesn't match and has no matching descendants.
        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            item => item is TreeNode n && n.Name == "Root"));

        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsFalse(tv.Items.Contains(child));

        tv.ClearAllFilters();

        Assert.AreEqual(2, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsTrue(tv.Items.Contains(child));
    }

    // ─────────────────────────────── Sorting ──────────────────────────────

    [UITestMethod]
    public void Sort_Ascending_OrdersRootItemsByName()
    {
        var rootC = new TreeNode("C");
        var rootA = new TreeNode("A");
        var rootB = new TreeNode("B");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { rootC, rootA, rootB }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        var names = tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(new List<string> { "A", "B", "C" }, names);
    }

    [UITestMethod]
    public void Sort_Descending_OrdersRootItemsByName()
    {
        var rootC = new TreeNode("C");
        var rootA = new TreeNode("A");
        var rootB = new TreeNode("B");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { rootA, rootC, rootB }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Descending));

        var names = tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(new List<string> { "C", "B", "A" }, names);
    }

    [UITestMethod]
    public void Sort_AppliedPerLevel_NotGlobally()
    {
        // Children of each parent are sorted within their own sibling group,
        // not mixed together with children of other parents.
        var childZA = new TreeNode("Z_of_A");
        var childAA = new TreeNode("A_of_A");
        var parentA = new TreeNode("ParentA", childZA, childAA);

        var childZB = new TreeNode("Z_of_B");
        var childAB = new TreeNode("A_of_B");
        var parentB = new TreeNode("ParentB", childZB, childAB);

        // Roots inserted in reverse order to also verify root-level sorting.
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { parentB, parentA }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        // Roots sorted: ParentA < ParentB.
        // Children of ParentA sorted independently: A_of_A < Z_of_A.
        // Children of ParentB sorted independently: A_of_B < Z_of_B.
        var names = tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(
            new List<string> { "ParentA", "A_of_A", "Z_of_A", "ParentB", "A_of_B", "Z_of_B" },
            names);
    }

    [UITestMethod]
    public void Sort_PreservesHierarchyLevels()
    {
        var childZ = new TreeNode("Z");
        var childA = new TreeNode("A");
        var root = new TreeNode("Root", childZ, childA);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        Assert.AreEqual(0, tv.GetHierarchyLevel(root));
        Assert.AreEqual(1, tv.GetHierarchyLevel(childA));
        Assert.AreEqual(1, tv.GetHierarchyLevel(childZ));
    }

    [UITestMethod]
    public void ClearAllSorting_InHierarchyMode_RestoresInsertionOrder()
    {
        var rootC = new TreeNode("C");
        var rootA = new TreeNode("A");
        var rootB = new TreeNode("B");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { rootC, rootA, rootB }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));
        CollectionAssert.AreEqual(
            new List<string> { "A", "B", "C" },
            tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList());

        tv.ClearAllSorting();

        // Original insertion order restored: C, A, B
        CollectionAssert.AreEqual(
            new List<string> { "C", "A", "B" },
            tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList());
    }

    [UITestMethod]
    public void Sort_WithFilter_BothApplied()
    {
        // Filter excludes one root; the remaining roots should be sorted.
        var nodeExcluded = new TreeNode("Excluded");
        var nodeC = new TreeNode("C");
        var nodeA = new TreeNode("A");
        var nodeB = new TreeNode("B");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { nodeExcluded, nodeC, nodeA, nodeB }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            item => item is TreeNode n && n.Name != "Excluded"));
        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        var names = tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(new List<string> { "A", "B", "C" }, names);
    }

    [UITestMethod]
    public void Sort_WithChildrenSelector_WorksCorrectly()
    {
        var childZ = new TreeNode("Z");
        var childA = new TreeNode("A");
        var root = new TreeNode("Root");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenSelector = item => item is TreeNode n && n.Name == "Root"
                ? new TreeNode[] { childZ, childA }
                : System.Array.Empty<TreeNode>(),
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        // root's children [childZ, childA] should be sorted ascending to [childA, childZ]
        var names = tv.Items.OfType<TreeNode>().Select(x => x.Name).ToList();
        CollectionAssert.AreEqual(new List<string> { "Root", "A", "Z" }, names);
    }

    // ─────────────────── Multi-level and toggle scenarios ──────────────────

    [UITestMethod]
    public void MultiLevel_CollapsingRoot_HidesAllDescendants()
    {
        // 3-level hierarchy: root → child → grandchild
        var grandchild = new TreeNode("Grandchild");
        var child = new TreeNode("Child", grandchild);
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(3, tv.Items.Count);

        tv.SetItemExpanded(root, false);

        // Collapsing root hides child AND grandchild
        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsFalse(tv.Items.Contains(child));
        Assert.IsFalse(tv.Items.Contains(grandchild));
    }

    [UITestMethod]
    public void DisablingHierarchy_AfterEnable_ResetsToFlatView()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(2, tv.Items.Count);

        tv.IsHierarchicalEnabled = false;

        // Flat mode: only top-level source items; child lives inside root.Children, not the source.
        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(root));
        Assert.IsFalse(tv.Items.Contains(child));
    }

    // ──────────────────── Edge Cases / Null and Empty ─────────────────────

    [UITestMethod]
    public void EmptySource_InHierarchyMode_ShowsNothing()
    {
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode>()
        };

        Assert.AreEqual(0, tv.Items.Count);
    }

    [UITestMethod]
    public void ChildrenPropertyNull_TreatedAsLeaf()
    {
        var item = new NullableChildrenNode("Leaf"); // Children property = null

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<NullableChildrenNode> { item }
        };

        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsFalse(tv.HasChildItems(item));
    }

    [UITestMethod]
    public void ChildrenSelector_ReturningEmpty_TreatedAsLeaf()
    {
        var item = new TreeNode("A");

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenSelector = _ => System.Array.Empty<object>(),
            ItemsSource = new List<TreeNode> { item }
        };

        Assert.AreEqual(1, tv.Items.Count);
        Assert.IsFalse(tv.HasChildItems(item));
    }

    [UITestMethod]
    public void HasChildItems_ReturnsFalse_WhenHasChildrenPathReturnsFalse_EvenWithChildren()
    {
        // HasChildrenPath flag = false should override the actual children collection.
        var item = new TreeNodeWithFlag("Parent", hasChildren: false);
        item.Children.Add(new TreeNode("ShouldNotAppear"));

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            HasChildrenPath = nameof(TreeNodeWithFlag.HasChildren),
            ItemsSource = new List<TreeNodeWithFlag> { item }
        };

        Assert.IsFalse(tv.HasChildItems(item));
        Assert.AreEqual(1, tv.Items.Count, "Child should not appear when HasChildrenPath returns false");
    }

    // ──────────────────────── Circular Reference ──────────────────────────

    [UITestMethod]
    public void CircularReference_DoesNotHang()
    {
        var node = new TreeNode("A");
        node.Children.Add(node); // self-referential circular node

        // FlattenHierarchy has a path-based cycle guard; this must not hang.
        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { node }
        };

        Assert.IsTrue(tv.Items.Count >= 1, "Node should appear at least once; cycle guard stops infinite recursion");
    }

    // ──────────────────── IsExpandedPath initial state ────────────────────

    [UITestMethod]
    public void IsExpandedPath_InitiallyFalse_ItemStartsCollapsed()
    {
        var child = new TreeNode("Child");
        var parent = new ExpandableNode("Parent", isExpanded: false, child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            IsExpandedPath = nameof(ExpandableNode.IsExpanded),
            ItemsSource = new List<ExpandableNode> { parent }
        };

        Assert.AreEqual(1, tv.Items.Count, "Parent with IsExpanded=false should start collapsed");
        Assert.IsFalse(tv.IsItemExpanded(parent));
        Assert.IsFalse(tv.Items.Contains(child));
    }

    // ──────────────────────── 3-Level Hierarchy ───────────────────────────

    [UITestMethod]
    public void DeepNesting_3Levels_AllVisible_WhenAllExpanded()
    {
        var grandchild = new TreeNode("GC");
        var child = new TreeNode("C", grandchild);
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        // Depth-first order: root → child → grandchild
        Assert.AreEqual(3, tv.Items.Count);
        Assert.AreSame(root, tv.Items[0]);
        Assert.AreSame(child, tv.Items[1]);
        Assert.AreSame(grandchild, tv.Items[2]);
    }

    [UITestMethod]
    public void GetHierarchyLevel_ReturnsCorrectLevels_For3LevelHierarchy()
    {
        var grandchild = new TreeNode("GC");
        var child = new TreeNode("C", grandchild);
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(0, tv.GetHierarchyLevel(root));
        Assert.AreEqual(1, tv.GetHierarchyLevel(child));
        Assert.AreEqual(2, tv.GetHierarchyLevel(grandchild));
    }

    [UITestMethod]
    public void GetHierarchyLevel_ReturnsZero_ForCollapsedItem_NotInDisplay()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(1, tv.GetHierarchyLevel(child)); // visible → level 1

        tv.SetItemExpanded(root, false); // collapse; child removed from the level map

        Assert.AreEqual(0, tv.GetHierarchyLevel(child), "Collapsed item not in display map; default level is 0");
    }

    // ──────────────── Dynamic Nested ObservableCollection ─────────────────

    [UITestMethod]
    public void DynamicChild_AddToNestedObservableCollection_UpdatesDisplay()
    {
        var parent = new ObservableTreeNode("Parent");
        var source = new ObservableCollection<ObservableTreeNode> { parent };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = source
        };

        Assert.AreEqual(1, tv.Items.Count); // only parent, no children yet

        parent.Children.Add(new ObservableTreeNode("NewChild"));
        tv.RefreshView(); // flush async rebuild

        Assert.AreEqual(2, tv.Items.Count);
        Assert.AreEqual("NewChild", ((ObservableTreeNode)tv.Items[1]).Name);
    }

    [UITestMethod]
    public void DynamicChild_RemoveFromNestedObservableCollection_UpdatesDisplay()
    {
        var child = new ObservableTreeNode("Child");
        var parent = new ObservableTreeNode("Parent");
        parent.Children.Add(child);

        var source = new ObservableCollection<ObservableTreeNode> { parent };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = source
        };

        Assert.AreEqual(2, tv.Items.Count); // parent + child

        parent.Children.Remove(child);
        tv.RefreshView();

        Assert.AreEqual(1, tv.Items.Count);
        Assert.AreSame(parent, tv.Items[0]);
    }

    // ──────────────────────── Filter extended ─────────────────────────────

    [UITestMethod]
    public void Filter_ForceExpand_DoesNotPermanentlyExpandCollapsedParent()
    {
        var child = new TreeNode("FilterTarget");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SetItemExpanded(root, false);
        Assert.AreEqual(1, tv.Items.Count); // only root (collapsed)

        // Filter force-expands root because child matches.
        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            x => x is TreeNode n && n.Name == "FilterTarget"));

        Assert.AreEqual(2, tv.Items.Count, "Force-expand: matching child must be visible");
        Assert.IsTrue(tv.Items.Contains(child));

        // Clearing the filter must restore the manually-collapsed state.
        tv.ClearAllFilters();

        Assert.AreEqual(1, tv.Items.Count, "Root should be collapsed again after filter is cleared");
        Assert.IsFalse(tv.Items.Contains(child));
    }

    [UITestMethod]
    public void Filter_3Level_GrandchildMatch_ShowsEntireAncestorChain()
    {
        var grandchild = new TreeNode("Target");
        var child = new TreeNode("Mid", grandchild);
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.FilterDescriptions.Add(new FilterDescription(nameof(TreeNode.Name),
            x => x is TreeNode n && n.Name == "Target"));

        Assert.AreEqual(3, tv.Items.Count);
        Assert.IsTrue(tv.Items.Contains(root), "Root (ancestor) must be force-shown");
        Assert.IsTrue(tv.Items.Contains(child), "Mid (ancestor) must be force-shown");
        Assert.IsTrue(tv.Items.Contains(grandchild), "Matching grandchild must be visible");
    }

    // ──────────────────────── Sort extended ───────────────────────────────

    [UITestMethod]
    public void Sort_CollapseAndReExpand_ChildrenRemainSorted()
    {
        var b = new TreeNode("B");
        var a = new TreeNode("A");
        var root = new TreeNode("Root", b, a); // inserted B before A

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        tv.SortDescriptions.Add(new SortDescription(nameof(TreeNode.Name), SortDirection.Ascending));

        // After sort: Root[0], A[1], B[2]
        Assert.AreSame(a, tv.Items[1]);
        Assert.AreSame(b, tv.Items[2]);

        tv.SetItemExpanded(root, false);
        Assert.AreEqual(1, tv.Items.Count);

        tv.SetItemExpanded(root, true);

        // Sort must be reapplied on re-expand.
        Assert.AreEqual(3, tv.Items.Count);
        Assert.AreSame(a, tv.Items[1], "Sort order must be preserved after collapse+re-expand");
        Assert.AreSame(b, tv.Items[2]);
    }

    // ─────────────────────── Re-enabling Hierarchy ────────────────────────

    [UITestMethod]
    public void ReEnablingHierarchy_AfterDisable_ShowsExpandedTree()
    {
        var child = new TreeNode("Child");
        var root = new TreeNode("Root", child);

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new List<TreeNode> { root }
        };

        Assert.AreEqual(2, tv.Items.Count);

        tv.IsHierarchicalEnabled = false;
        Assert.AreEqual(1, tv.Items.Count, "Flat mode: only root-level source items");

        tv.IsHierarchicalEnabled = true;
        Assert.AreEqual(2, tv.Items.Count, "Hierarchy restored: root + child visible");
        Assert.IsTrue(tv.Items.Contains(child));
    }

    // ─────────────── Reset action cleans up collapsed state ───────────────

    [UITestMethod]
    public void SourceReset_ClearsCollapsedState_AndRebuildsView()
    {
        var child = new ObservableTreeNode("Child");
        var root = new ObservableTreeNode("Root");
        root.Children.Add(child);
        var source = new ObservableCollection<ObservableTreeNode> { root };

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = source
        };

        Assert.AreEqual(2, tv.Items.Count);

        tv.SetItemExpanded(root, false);
        Assert.AreEqual(1, tv.Items.Count, "Root is collapsed");

        // Replacing source via Clear+Add simulates a Reset via CollectionChanged.
        // After Clear the new root is expanded by default so child should be visible.
        var newChild = new ObservableTreeNode("NewChild");
        var newRoot = new ObservableTreeNode("NewRoot");
        newRoot.Children.Add(newChild);
        source.Clear();
        source.Add(newRoot);
        tv.RefreshView();

        Assert.AreEqual(2, tv.Items.Count, "New root must be expanded (stale collapsed state cleared)");
        Assert.AreSame(newRoot, tv.Items[0]);
        Assert.AreSame(newChild, tv.Items[1]);
    }

    // ───────── Dynamically added item's children collection subscribed ─────

    [UITestMethod]
    public void DynamicAdd_NewItemWithChildren_ChildCollectionChangesAreObserved()
    {
        var source = new ObservableCollection<ObservableTreeNode>();

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = source
        };

        Assert.AreEqual(0, tv.Items.Count);

        // Add a root with a pre-existing child.
        var newRoot = new ObservableTreeNode("NewRoot");
        var existingChild = new ObservableTreeNode("Existing");
        newRoot.Children.Add(existingChild);
        source.Add(newRoot);
        tv.RefreshView();

        Assert.AreEqual(2, tv.Items.Count, "NewRoot + Existing child");

        // Now add a child to the newly-added root's collection.
        // The subscription fix means this change should be detected.
        var laterChild = new ObservableTreeNode("Later");
        newRoot.Children.Add(laterChild);
        tv.RefreshView();

        Assert.AreEqual(3, tv.Items.Count, "NewRoot + Existing + Later");
        Assert.IsTrue(tv.Items.Contains(laterChild), "Child added after root insertion must be visible");
    }

    // ─────────────────────────── Circular ref in flat enumeration ─────────

    [UITestMethod]
    public void GetAllHierarchyItemsFlat_WithCircularRef_DoesNotStackOverflow()
    {
        // Build a circular reference: A -> B -> A
        var nodeA = new ObservableTreeNode("A");
        var nodeB = new ObservableTreeNode("B");
        nodeA.Children.Add(nodeB);
        nodeB.Children.Add(nodeA); // circular!

        var tv = new TableView
        {
            IsHierarchicalEnabled = true,
            ChildrenPath = "Children",
            ItemsSource = new ObservableCollection<ObservableTreeNode> { nodeA }
        };

        // Should not stack overflow — the fix adds cycle detection to GetAllItemsFlatRecursive.
        var allItems = tv.GetAllHierarchyItemsFlat().ToList();

        Assert.IsTrue(allItems.Count >= 2, "A and B should both appear");
        Assert.IsTrue(allItems.Contains(nodeA));
        Assert.IsTrue(allItems.Contains(nodeB));
    }
}

// ─────────────────────────────── Test helpers ─────────────────────────────

internal class TreeNode
{
    public string Name { get; }
    public List<TreeNode> Children { get; } = [];

    public TreeNode(string name, params TreeNode[] children)
    {
        Name = name;
        Children.AddRange(children);
    }

    public override string ToString() => Name;
}

internal class TreeNodeWithFlag
{
    public string Name { get; }
    public bool HasChildren { get; }
    public List<TreeNode> Children { get; } = [];

    public TreeNodeWithFlag(string name, bool hasChildren)
    {
        Name = name;
        HasChildren = hasChildren;
    }
}

internal class ExpandableNode : INotifyPropertyChanged
{
    private bool _isExpanded;

    public string Name { get; }
    public List<TreeNode> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ExpandableNode(string name, bool isExpanded, params TreeNode[] children)
    {
        Name = name;
        _isExpanded = isExpanded;
        Children.AddRange(children);
    }
}

internal class NullableChildrenNode
{
    public string Name { get; }
    public List<NullableChildrenNode>? Children { get; }

    public NullableChildrenNode(string name, List<NullableChildrenNode>? children = null)
    {
        Name = name;
        Children = children;
    }
}

internal class ObservableTreeNode
{
    public string Name { get; }
    public ObservableCollection<ObservableTreeNode> Children { get; } = new();

    public ObservableTreeNode(string name)
    {
        Name = name;
    }

    public override string ToString() => Name;
}
