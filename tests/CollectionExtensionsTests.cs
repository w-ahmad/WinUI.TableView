using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class CollectionExtensionsTests
{
    [TestMethod]
    public void AddRange_AddsAllItemsToList()
    {
        var list = new Collection<int> { 1 };
        var toAdd = new[] { 2, 3, 4 };
        list.AddRange(toAdd);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list);
    }

    [TestMethod]
    public void AddRange_EmptyItems_DoesNothing()
    {
        var list = new Collection<string> { "a" };
        var toAdd = new[] { "b", "c", "d" };
        list.AddRange(toAdd);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, list);
    }

    [TestMethod]
    public void RemoveWhere_RemovesMatchingItems()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        list.RemoveWhere(x => x % 2 == 0); // Remove even
        CollectionAssert.AreEqual(new[] { 1, 3, 5 }, list);
    }

    [TestMethod]
    public void RemoveWhere_NoMatches_DoesNothing()
    {
        var list = new List<int> { 1, 2, 3 };
        list.RemoveWhere(x => x > 10);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
    }

    [TestMethod]
    public void RemoveWhere_AllMatch_RemovesAll()
    {
        var list = new List<int> { 1, 1, 1 };
        list.RemoveWhere(x => x == 1);
        CollectionAssert.AreEqual(new int[0], list);
    }

    [TestMethod]
    public void RemoveWhere_EmptyCollection_DoesNothing()
    {
        var list = new List<int>();
        list.RemoveWhere(x => true);
        CollectionAssert.AreEqual(new int[0], list);
    }

    #region IndexOf Tests

    [TestMethod]
    public void IndexOf_IList_FindsItemAtCorrectIndex()
    {
        var list = new List<string> { "a", "b", "c" };
        Assert.AreEqual(1, list.IndexOf("b"));
    }

    [TestMethod]
    public void IndexOf_IList_ReturnsNegativeOneWhenNotFound()
    {
        var list = new List<string> { "a", "b", "c" };
        Assert.AreEqual(-1, list.IndexOf("d"));
    }

    [TestMethod]
    public void IndexOf_IList_FindsNullItem()
    {
        var list = new List<string?> { "a", null, "c" };
        Assert.AreEqual(1, list.IndexOf(null));
    }

    [TestMethod]
    public void IndexOf_IList_ReturnsFirstOccurrence()
    {
        var list = new List<int> { 1, 2, 3, 2, 4 };
        Assert.AreEqual(1, list.IndexOf(2));
    }

    [TestMethod]
    public void IndexOf_IList_EmptyList_ReturnsNegativeOne()
    {
        var list = new List<string>();
        Assert.AreEqual(-1, list.IndexOf("a"));
    }

    [TestMethod]
    public void IndexOf_ArrayList_FindsItemAtCorrectIndex()
    {
        var list = new ArrayList { "a", "b", "c" };
        Assert.AreEqual(1, ((IEnumerable)list).IndexOf("b"));
    }

    [TestMethod]
    public void IndexOf_GenericEnumerable_FindsItemAtCorrectIndex()
    {
        var enumerable = Enumerable.Range(0, 5).Select(x => x * 2);
        Assert.AreEqual(2, enumerable.IndexOf(4));
    }

    [TestMethod]
    public void IndexOf_GenericEnumerable_ReturnsNegativeOneWhenNotFound()
    {
        var enumerable = Enumerable.Range(0, 5).Select(x => x * 2);
        Assert.AreEqual(-1, enumerable.IndexOf(5));
    }

    [TestMethod]
    public void IndexOf_GenericEnumerable_EmptyEnumerable_ReturnsNegativeOne()
    {
        var enumerable = Enumerable.Empty<int>();
        Assert.AreEqual(-1, enumerable.IndexOf(1));
    }

    [TestMethod]
    public void IndexOf_GenericEnumerable_WithNull_FindsNull()
    {
        var enumerable = new[] { "a", null, "c" }.AsEnumerable();
        Assert.AreEqual(1, enumerable.IndexOf(null));
    }

    #endregion

    #region IsReadOnly Tests

    [TestMethod]
    public void IsReadOnly_MutableList_ReturnsFalse()
    {
        var list = new List<int> { 1, 2, 3 };
        Assert.IsFalse(list.IsReadOnly());
    }

    [TestMethod]
    public void IsReadOnly_ReadOnlyCollection_ReturnsTrue()
    {
        var list = new List<int> { 1, 2, 3 };
        var readOnly = new ReadOnlyCollection<int>(list);
        Assert.IsTrue(readOnly.IsReadOnly());
    }

    [TestMethod]
    public void IsReadOnly_Array_ReturnsFalse()
    {
        var array = new[] { 1, 2, 3 };
        Assert.IsFalse(array.IsReadOnly());
    }

    [TestMethod]
    public void IsReadOnly_ArrayList_ReturnsFalse()
    {
        var list = new ArrayList { 1, 2, 3 };
        Assert.IsFalse(list.IsReadOnly());
    }

    [TestMethod]
    public void IsReadOnly_GenericEnumerable_ReturnsTrue()
    {
        var enumerable = Enumerable.Range(1, 3);
        Assert.IsTrue(enumerable.IsReadOnly());
    }

    [TestMethod]
    public void IsReadOnly_Collection_ReturnsFalse()
    {
        var collection = new Collection<string> { "a", "b", "c" };
        Assert.IsFalse(collection.IsReadOnly());
    }

    #endregion

    #region Add Tests

    [TestMethod]
    public void Add_IList_AddsItem()
    {
        var list = new List<string> { "a", "b" };
        ((IEnumerable)list).Add("c");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void Add_IList_AddsNullItem()
    {
        var list = new List<string?> { "a", "b" };
        ((IEnumerable)list).Add(null);
        CollectionAssert.AreEqual(new[] { "a", "b", null }, list);
    }

    [TestMethod]
    public void Add_ArrayList_AddsItem()
    {
        var list = new ArrayList { "a", "b" };
        ((IEnumerable)list).Add("c");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void Add_EmptyList_AddsItem()
    {
        var list = new List<int>();
        ((IEnumerable)list).Add(1);
        CollectionAssert.AreEqual(new[] { 1 }, list);
    }

    [TestMethod]
    public void Add_Collection_AddsItem()
    {
        var collection = new Collection<int> { 1, 2 };
        ((IEnumerable)collection).Add(3);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collection);
    }

    [TestMethod]
    public void Add_GenericEnumerable_DoesNothing()
    {
        // This tests that calling Add on a non-IList/ICollectionView enumerable doesn't throw
        var enumerable = Enumerable.Range(1, 3);
        enumerable.Add(4); // Should not throw
    }

    #endregion

    #region Insert Tests

    [TestMethod]
    public void Insert_IList_InsertsItemAtIndex()
    {
        var list = new List<string> { "a", "c" };
        ((IEnumerable)list).Insert(1, "b");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void Insert_IList_InsertsAtBeginning()
    {
        var list = new List<int> { 2, 3 };
        ((IEnumerable)list).Insert(0, 1);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
    }

    [TestMethod]
    public void Insert_IList_InsertsAtEnd()
    {
        var list = new List<int> { 1, 2 };
        ((IEnumerable)list).Insert(2, 3);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, list);
    }

    [TestMethod]
    public void Insert_IList_InsertsNull()
    {
        var list = new List<string?> { "a", "b" };
        ((IEnumerable)list).Insert(1, null);
        CollectionAssert.AreEqual(new[] { "a", null, "b" }, list);
    }

    [TestMethod]
    public void Insert_ArrayList_InsertsItemAtIndex()
    {
        var list = new ArrayList { "a", "c" };
        ((IEnumerable)list).Insert(1, "b");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void Insert_EmptyList_InsertsAtZero()
    {
        var list = new List<int>();
        ((IEnumerable)list).Insert(0, 1);
        CollectionAssert.AreEqual(new[] { 1 }, list);
    }

    [TestMethod]
    public void Insert_Collection_InsertsItemAtIndex()
    {
        var collection = new Collection<int> { 1, 3 };
        ((IEnumerable)collection).Insert(1, 2);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, collection);
    }

    [TestMethod]
    public void Insert_GenericEnumerable_DoesNothing()
    {
        // This tests that calling Insert on a non-IList/ICollectionView enumerable doesn't throw
        var enumerable = Enumerable.Range(1, 3);
        enumerable.Insert(0, 4); // Should not throw
    }

    #endregion

    #region Remove Tests

    [TestMethod]
    public void Remove_IList_RemovesItem()
    {
        var list = new List<string> { "a", "b", "c" };
        ((IEnumerable)list).Remove("b");
        CollectionAssert.AreEqual(new[] { "a", "c" }, list);
    }

    [TestMethod]
    public void Remove_IList_RemovesFirstOccurrence()
    {
        var list = new List<int> { 1, 2, 3, 2, 4 };
        ((IEnumerable)list).Remove(2);
        CollectionAssert.AreEqual(new[] { 1, 3, 2, 4 }, list);
    }

    [TestMethod]
    public void Remove_IList_RemovesNull()
    {
        var list = new List<string?> { "a", null, "c" };
        ((IEnumerable)list).Remove(null);
        CollectionAssert.AreEqual(new[] { "a", "c" }, list);
    }

    [TestMethod]
    public void Remove_IList_NonExistentItem_DoesNothing()
    {
        var list = new List<string> { "a", "b", "c" };
        ((IEnumerable)list).Remove("d");
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    [TestMethod]
    public void Remove_ArrayList_RemovesItem()
    {
        var list = new ArrayList { "a", "b", "c" };
        ((IEnumerable)list).Remove("b");
        CollectionAssert.AreEqual(new[] { "a", "c" }, list);
    }

    [TestMethod]
    public void Remove_Collection_RemovesItem()
    {
        var collection = new Collection<int> { 1, 2, 3 };
        ((IEnumerable)collection).Remove(2);
        CollectionAssert.AreEqual(new[] { 1, 3 }, collection);
    }

    [TestMethod]
    public void Remove_EmptyList_DoesNothing()
    {
        var list = new List<int>();
        ((IEnumerable)list).Remove(1);
        CollectionAssert.AreEqual(new int[0], list);
    }

    [TestMethod]
    public void Remove_GenericEnumerable_DoesNothing()
    {
        // This tests that calling Remove on a non-IList/ICollectionView enumerable doesn't throw
        var enumerable = Enumerable.Range(1, 3);
        enumerable.Remove(2); // Should not throw
    }

    #endregion

    #region Clear Tests

    [TestMethod]
    public void Clear_IList_RemovesAllItems()
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };
        ((IEnumerable)list).Clear();
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Clear_ArrayList_RemovesAllItems()
    {
        var list = new ArrayList { "a", "b", "c" };
        ((IEnumerable)list).Clear();
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Clear_Collection_RemovesAllItems()
    {
        var collection = new Collection<string> { "a", "b", "c" };
        ((IEnumerable)collection).Clear();
        Assert.AreEqual(0, collection.Count);
    }

    [TestMethod]
    public void Clear_EmptyList_DoesNothing()
    {
        var list = new List<int>();
        ((IEnumerable)list).Clear();
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Clear_ListWithNulls_RemovesAllItems()
    {
        var list = new List<string?> { "a", null, "c", null };
        ((IEnumerable)list).Clear();
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Clear_GenericEnumerable_DoesNothing()
    {
        // This tests that calling Clear on a non-IList/ICollectionView enumerable doesn't throw
        var enumerable = Enumerable.Range(1, 3);
        enumerable.Clear(); // Should not throw
    }

    #endregion
}
