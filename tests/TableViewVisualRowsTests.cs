using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WinUI.TableView.Tests;

[TestClass]
public class TableViewVisualRowsTests
{
    [UITestMethod]
    public void Ungrouped_IsAOneToOneProjection()
    {
        var view = new CollectionView(CreateItems());
        var rows = new TableViewVisualRows(view, _ => false);

        Assert.IsFalse(rows.HasGroups);
        Assert.AreEqual(5, rows.Count);

        for (var i = 0; i < 5; i++)
        {
            Assert.AreEqual(i, rows.GetItemIndex(i));
            Assert.AreEqual(i, rows.GetVisualIndex(i));
            Assert.AreSame(view[i], rows[i]);
            Assert.IsNull(rows.GetGroup(i));
        }

        Assert.AreEqual(-1, rows.GetItemIndex(5));
        Assert.AreEqual(-1, rows.GetVisualIndex(5));
        Assert.AreEqual(-1, rows.GetVisualIndex(-1));
    }

    [UITestMethod]
    public void Grouped_InterleavesHeaderRows()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, _ => false);

        // header(Fruit) 3 items header(Veg) 2 items
        Assert.IsTrue(rows.HasGroups);
        Assert.AreEqual(7, rows.Count);

        Assert.IsNotNull(rows.GetGroup(0));
        Assert.AreEqual("Fruit", rows.GetGroup(0)!.Key);
        Assert.AreEqual(-1, rows.GetItemIndex(0));

        Assert.AreEqual(0, rows.GetItemIndex(1));
        Assert.AreEqual(1, rows.GetItemIndex(2));
        Assert.AreEqual(2, rows.GetItemIndex(3));

        Assert.IsNotNull(rows.GetGroup(4));
        Assert.AreEqual("Veg", rows.GetGroup(4)!.Key);

        Assert.AreEqual(3, rows.GetItemIndex(5));
        Assert.AreEqual(4, rows.GetItemIndex(6));

        // And back the other way.
        Assert.AreEqual(1, rows.GetVisualIndex(0));
        Assert.AreEqual(3, rows.GetVisualIndex(2));
        Assert.AreEqual(5, rows.GetVisualIndex(3));
        Assert.AreEqual(6, rows.GetVisualIndex(4));
    }

    [UITestMethod]
    public void MultiLevel_EmitsAHeaderPerLevel()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Colour)));

        var rows = new TableViewVisualRows(view, _ => false);

        // Fruit > Green(1) Red(2) ; Veg > Green(1) Red(1)  =>  6 headers + 5 items
        Assert.AreEqual(11, rows.Count);

        Assert.AreEqual(0, rows.GetGroup(0)!.Level);
        Assert.AreEqual(1, rows.GetGroup(1)!.Level);
        Assert.AreEqual(0, rows.GetItemIndex(2));

        // Every item still maps to exactly one visual row and back.
        for (var itemIndex = 0; itemIndex < view.Count; itemIndex++)
        {
            var visualIndex = rows.GetVisualIndex(itemIndex);
            Assert.IsTrue(visualIndex >= 0, $"item {itemIndex} should be visible");
            Assert.AreEqual(itemIndex, rows.GetItemIndex(visualIndex));
        }
    }

    [UITestMethod]
    public void CollapsedGroup_HidesItsItemsButKeepsItsHeader()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, group => Equals(group.Key, "Fruit"));

        // header(Fruit) header(Veg) 2 items
        Assert.AreEqual(4, rows.Count);
        Assert.AreEqual("Fruit", rows.GetGroup(0)!.Key);
        Assert.AreEqual("Veg", rows.GetGroup(1)!.Key);
        Assert.AreEqual(3, rows.GetItemIndex(2));
        Assert.AreEqual(4, rows.GetItemIndex(3));

        // The hidden items map to no visual row.
        Assert.AreEqual(-1, rows.GetVisualIndex(0));
        Assert.AreEqual(-1, rows.GetVisualIndex(1));
        Assert.AreEqual(-1, rows.GetVisualIndex(2));
        Assert.AreEqual(2, rows.GetVisualIndex(3));
    }

    [UITestMethod]
    public void CollapsingAnOuterGroup_AlsoHidesItsNestedHeaders()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Colour)));

        var rows = new TableViewVisualRows(view, group => group.Level is 0 && Equals(group.Key, "Fruit"));

        // header(Fruit) then header(Veg) header(Green) item header(Red) item
        Assert.AreEqual(6, rows.Count);
        Assert.AreEqual("Fruit", rows.GetGroup(0)!.Key);
        Assert.AreEqual("Veg", rows.GetGroup(1)!.Key);
        Assert.AreEqual(1, rows.GetGroup(2)!.Level);
    }

    [UITestMethod]
    public void EverythingCollapsed_LeavesOnlyTopLevelHeaders()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, _ => true);

        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(-1, rows.GetItemIndex(0));
        Assert.AreEqual(-1, rows.GetItemIndex(1));
    }

    [UITestMethod]
    public void EmptyView_ProjectsNothing()
    {
        var view = new CollectionView(new ObservableCollection<GroupItem>());
        var rows = new TableViewVisualRows(view, _ => false);

        Assert.AreEqual(0, rows.Count);
        Assert.IsFalse(rows.HasGroups);
        Assert.AreEqual(-1, rows.GetItemIndex(0));
        Assert.AreEqual(-1, rows.GetVisualIndex(0));
    }

    [UITestMethod]
    public void IndexOf_FindsItemsAndGroupHeaders()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, _ => false);
        var vegGroup = view.Groups[1];

        Assert.AreEqual(4, rows.IndexOf(vegGroup));
        Assert.AreEqual(1, rows.IndexOf(view[0]));
        Assert.AreEqual(-1, rows.IndexOf(new GroupItem()));
    }

    [UITestMethod]
    public void OnItemInserted_IntoAnExistingGroup_ReportsASingleAdd()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, _ => false);
        var events = Record(rows);

        source.Add(new GroupItem { Category = "Fruit", Colour = "Red", Name = "Plum" });
        rows.OnItemInserted(view.IndexOf(source[^1]));

        Assert.AreEqual(8, rows.Count);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Add, events[0].Action);
    }

    [UITestMethod]
    public void OnItemInserted_ThatCreatesAGroup_ReportsAReset()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var rows = new TableViewVisualRows(view, _ => false);
        var events = Record(rows);

        source.Add(new GroupItem { Category = "Grain", Colour = "Brown", Name = "Rice" });
        rows.OnItemInserted(view.IndexOf(source[^1]));

        // A new header row plus the item: two more rows, so a single add cannot describe it.
        Assert.AreEqual(9, rows.Count);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
    }

    [UITestMethod]
    public void OnItemInserted_Ungrouped_ReportsASingleAdd()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        var rows = new TableViewVisualRows(view, _ => false);
        var events = Record(rows);

        source.Add(new GroupItem { Category = "Fruit", Colour = "Red", Name = "Plum" });
        rows.OnItemInserted(view.IndexOf(source[^1]));

        Assert.AreEqual(6, rows.Count);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Add, events[0].Action);
        Assert.AreEqual(5, events[0].NewStartingIndex);
    }

    [UITestMethod]
    public void OnItemRemoved_Ungrouped_ReportsASingleRemove()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        var rows = new TableViewVisualRows(view, _ => false);
        var events = Record(rows);

        var removed = view[2];
        var visualIndex = rows.GetVisualIndex(2);
        source.Remove((GroupItem)removed!);
        rows.OnItemRemoved(visualIndex, removed);

        Assert.AreEqual(4, rows.Count);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Remove, events[0].Action);
        Assert.AreEqual(2, events[0].OldStartingIndex);
    }

    [UITestMethod]
    public void Reset_RebuildsAfterCollapseStateChanges()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var collapsed = new HashSet<object?>();
        var rows = new TableViewVisualRows(view, group => collapsed.Contains(group.Key));
        var events = Record(rows);

        Assert.AreEqual(7, rows.Count);

        collapsed.Add("Fruit");
        rows.Reset();

        Assert.AreEqual(4, rows.Count);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(NotifyCollectionChangedAction.Reset, events[0].Action);
    }

    private static List<NotifyCollectionChangedEventArgs> Record(TableViewVisualRows rows)
    {
        List<NotifyCollectionChangedEventArgs> events = [];
        rows.CollectionChanged += (_, e) => events.Add(e);

        return events;
    }

    private static ObservableCollection<GroupItem> CreateItems()
    {
        return
        [
            new GroupItem { Category = "Veg", Colour = "Red", Name = "Tomato" },
            new GroupItem { Category = "Fruit", Colour = "Red", Name = "Cherry" },
            new GroupItem { Category = "Fruit", Colour = "Green", Name = "Apple" },
            new GroupItem { Category = "Veg", Colour = "Green", Name = "Broccoli" },
            new GroupItem { Category = "Fruit", Colour = "Red", Name = "Banana" },
        ];
    }
}
