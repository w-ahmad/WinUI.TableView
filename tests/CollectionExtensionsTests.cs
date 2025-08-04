using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
}
