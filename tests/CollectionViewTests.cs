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
