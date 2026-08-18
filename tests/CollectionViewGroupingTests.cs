using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace WinUI.TableView.Tests;

[TestClass]
public class CollectionViewGroupingTests
{
    [UITestMethod]
    public void NoGroupDescriptions_MeansNoGroups()
    {
        var view = new CollectionView(CreateItems());

        Assert.IsFalse(view.IsGrouped);
        Assert.AreEqual(0, view.Groups.Count);
    }

    [UITestMethod]
    public void SingleLevel_GroupsItemsAndOrdersThemByKey()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        Assert.IsTrue(view.IsGrouped);

        var groups = view.Groups;
        Assert.AreEqual(2, groups.Count);

        Assert.AreEqual("Fruit", groups[0].Key);
        Assert.AreEqual(0, groups[0].Level);
        Assert.AreEqual(0, groups[0].FirstItemIndex);
        Assert.AreEqual(3, groups[0].ItemCount);
        Assert.AreEqual(2, groups[0].LastItemIndex);
        Assert.IsNull(groups[0].Parent);

        Assert.AreEqual("Veg", groups[1].Key);
        Assert.AreEqual(3, groups[1].FirstItemIndex);
        Assert.AreEqual(2, groups[1].ItemCount);

        // Items are laid out grouped, so every item in a group sits inside that group's span.
        for (var i = 0; i < 3; i++)
        {
            Assert.AreEqual("Fruit", ((GroupItem)view[i]!).Category);
        }

        for (var i = 3; i < 5; i++)
        {
            Assert.AreEqual("Veg", ((GroupItem)view[i]!).Category);
        }
    }

    [UITestMethod]
    public void GroupDescriptionDirection_OrdersTheGroups()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category), SortDirection.Descending));

        Assert.AreEqual("Veg", view.Groups[0].Key);
        Assert.AreEqual("Fruit", view.Groups[1].Key);
    }

    [UITestMethod]
    public void MultiLevel_ProducesNestedGroupsInDocumentOrder()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Colour)));

        var groups = view.Groups;

        // Fruit(3) > Green(1), Red(2) ; Veg(2) > Green(1), Red(1)
        Assert.AreEqual(6, groups.Count);

        Assert.AreEqual("Fruit", groups[0].Key);
        Assert.AreEqual(0, groups[0].Level);
        Assert.AreEqual(3, groups[0].ItemCount);

        Assert.AreEqual("Green", groups[1].Key);
        Assert.AreEqual(1, groups[1].Level);
        Assert.AreSame(groups[0], groups[1].Parent);
        Assert.AreEqual(1, groups[1].ItemCount);

        Assert.AreEqual("Red", groups[2].Key);
        Assert.AreEqual(1, groups[2].Level);
        Assert.AreSame(groups[0], groups[2].Parent);
        Assert.AreEqual(2, groups[2].ItemCount);

        Assert.AreEqual("Veg", groups[3].Key);
        Assert.AreEqual(0, groups[3].Level);

        // The inner "Green" key repeats under a different outer group, which must be a separate group.
        Assert.AreEqual("Green", groups[4].Key);
        Assert.AreEqual(1, groups[4].Level);
        Assert.AreSame(groups[3], groups[4].Parent);
        Assert.AreNotSame(groups[1], groups[4]);

        CollectionAssert.AreEqual(new object?[] { "Veg", "Green" }, groups[4].KeyPath.ToArray());
    }

    [UITestMethod]
    public void SortDescriptions_OrderItemsWithinEachGroup()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.SortDescriptions.Add(new SortDescription(nameof(GroupItem.Name), SortDirection.Ascending));

        var names = view.Cast<GroupItem>().Select(x => x.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "Apple", "Banana", "Cherry", "Broccoli", "Tomato" }, names);
    }

    [UITestMethod]
    public void Filtering_ExcludesItemsAndDropsEmptyGroups()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.FilterDescriptions.Add(new FilterDescription(nameof(GroupItem.Category), o => ((GroupItem)o!).Category == "Veg"));

        Assert.AreEqual(2, view.Count);
        Assert.AreEqual(1, view.Groups.Count);
        Assert.AreEqual("Veg", view.Groups[0].Key);
        Assert.AreEqual(0, view.Groups[0].FirstItemIndex);
        Assert.AreEqual(2, view.Groups[0].ItemCount);
    }

    [UITestMethod]
    public void AddingAnItemToAnExistingGroup_GrowsItAndShiftsLaterGroups()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        source.Add(new GroupItem { Category = "Fruit", Colour = "Red", Name = "Plum" });

        Assert.AreEqual(2, view.Groups.Count);
        Assert.AreEqual(4, view.Groups[0].ItemCount);
        Assert.AreEqual(0, view.Groups[0].FirstItemIndex);
        Assert.AreEqual(4, view.Groups[1].FirstItemIndex);
        Assert.AreEqual(2, view.Groups[1].ItemCount);
    }

    [UITestMethod]
    public void AddingAnItemThatStartsANewGroup_CreatesTheGroup()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        source.Add(new GroupItem { Category = "Grain", Colour = "Brown", Name = "Rice" });

        Assert.AreEqual(3, view.Groups.Count);
        Assert.AreEqual("Fruit", view.Groups[0].Key);
        Assert.AreEqual("Grain", view.Groups[1].Key);
        Assert.AreEqual(1, view.Groups[1].ItemCount);
        Assert.AreEqual("Veg", view.Groups[2].Key);
        Assert.AreEqual(4, view.Groups[2].FirstItemIndex);
    }

    [UITestMethod]
    public void RemovingTheLastItemOfAGroup_RemovesTheGroup()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        foreach (var item in source.Where(x => x.Category == "Veg").ToList())
        {
            source.Remove(item);
        }

        Assert.AreEqual(1, view.Groups.Count);
        Assert.AreEqual("Fruit", view.Groups[0].Key);
        Assert.AreEqual(3, view.Groups[0].ItemCount);
    }

    [UITestMethod]
    public void RemovingAnItemFromTheMiddle_ShrinksItsGroupAndShiftsLaterGroups()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        source.Remove(source.First(x => x.Category == "Fruit"));

        Assert.AreEqual(2, view.Groups.Count);
        Assert.AreEqual(2, view.Groups[0].ItemCount);
        Assert.AreEqual(2, view.Groups[1].FirstItemIndex);
        Assert.AreEqual(2, view.Groups[1].ItemCount);
    }

    [UITestMethod]
    public void ChangingAnItemsGroupKey_MovesItBetweenGroups()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        var apple = source.First(x => x.Name == "Apple");
        apple.Category = "Veg";

        Assert.AreEqual(2, view.Groups.Count);
        Assert.AreEqual("Fruit", view.Groups[0].Key);
        Assert.AreEqual(2, view.Groups[0].ItemCount);
        Assert.AreEqual("Veg", view.Groups[1].Key);
        Assert.AreEqual(3, view.Groups[1].ItemCount);

        // The item really moved into the Veg span.
        Assert.IsTrue(view.IndexOf(apple) >= view.Groups[1].FirstItemIndex);
    }

    [UITestMethod]
    public void ChangingTheLastItemOfAGroup_RemovesTheEmptiedGroup()
    {
        var source = CreateItems();
        var view = new CollectionView(source);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        foreach (var item in source.Where(x => x.Category == "Veg").ToList())
        {
            item.Category = "Fruit";
        }

        Assert.AreEqual(1, view.Groups.Count);
        Assert.AreEqual("Fruit", view.Groups[0].Key);
        Assert.AreEqual(5, view.Groups[0].ItemCount);
    }

    [UITestMethod]
    public void ClearingGroupDescriptions_RemovesAllGroups()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        Assert.AreEqual(2, view.Groups.Count);

        view.GroupDescriptions.Clear();

        Assert.IsFalse(view.IsGrouped);
        Assert.AreEqual(0, view.Groups.Count);
    }

    [UITestMethod]
    public void DeferRefresh_DelaysGroupingUntilDisposed()
    {
        var view = new CollectionView(CreateItems());

        using (view.DeferRefresh())
        {
            view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
            Assert.AreEqual(0, view.Groups.Count, "grouping should not have been applied yet");
        }

        Assert.AreEqual(2, view.Groups.Count);
    }

    [UITestMethod]
    public void RefreshGrouping_RebuildsGroupsAfterAnUntrackedChange()
    {
        var source = CreateItems();
        var view = new CollectionView(source, liveShapingEnabled: false);
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));

        source.First(x => x.Name == "Apple").Category = "Veg";
        Assert.AreEqual(3, view.Groups[0].ItemCount, "live shaping is off, so nothing moved yet");

        view.RefreshGrouping();

        Assert.AreEqual(2, view.Groups[0].ItemCount);
        Assert.AreEqual(3, view.Groups[1].ItemCount);
    }

    [UITestMethod]
    public void GroupSpansStayContiguousAndCoverEveryItem()
    {
        var view = new CollectionView(CreateItems());
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Category)));
        view.GroupDescriptions.Add(new GroupDescription(nameof(GroupItem.Colour)));

        // Every top-level group's span, concatenated, must exactly cover the view.
        var expectedStart = 0;

        foreach (var group in view.Groups.Where(g => g.Level is 0))
        {
            Assert.AreEqual(expectedStart, group.FirstItemIndex);
            expectedStart += group.ItemCount;
        }

        Assert.AreEqual(view.Count, expectedStart);
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

internal sealed class GroupItem : INotifyPropertyChanged
{
    private string _category = string.Empty;

    public string Category
    {
        get => _category;
        set
        {
            if (_category != value)
            {
                _category = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Category)));
            }
        }
    }

    public string Colour { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => $"{Category}/{Colour}/{Name}";
}
