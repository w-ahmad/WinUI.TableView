using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using WinUI.TableView.Collections;

namespace WinUI.TableView.Tests;

[TestClass]
public class ObjectBackedTypedSetTests
{
    [TestMethod]
    public void Construct_FromObjects_CastsToType()
    {
        var source = new object?[] { 1, 2, 3, null };
        var set = new ObjectBackedTypedSet<int?>(source);
        Assert.AreEqual(4, set.Count);
        Assert.IsTrue(set.Contains(1));
        Assert.IsTrue(set.Contains(2));
        Assert.IsTrue(set.Contains(3));
        Assert.IsTrue(set.Contains(null));
    }

    [TestMethod]
    public void Add_TypedItem_IncreasesCount()
    {
        var set = new ObjectBackedTypedSet<string>(["a"]);
        Assert.AreEqual(1, set.Count);
        set.Add("b");
        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains("b"));
    }

    [TestMethod]
    public void Add_Null_AddsDefault()
    {
        var set = new ObjectBackedTypedSet<string>([]);
        Assert.AreEqual(0, set.Count);
        set.Add(null);
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(null));
    }

    [TestMethod]
    public void Remove_TypedItem_Removes()
    {
        var set = new ObjectBackedTypedSet<int>([1, 2]);
        Assert.AreEqual(2, set.Count);
        var removed = set.Remove(1);
        Assert.IsTrue(removed);
        Assert.AreEqual(1, set.Count);
        Assert.IsFalse(set.Contains(1));
    }

    [TestMethod]
    public void Remove_Null_RemovesDefault()
    {
        var set = new ObjectBackedTypedSet<string>([null, "x"]);
        var removed = set.Remove(null);
        Assert.IsTrue(removed);
        Assert.AreEqual(1, set.Count);
        Assert.IsFalse(set.Contains(null));
    }

    [TestMethod]
    public void CopyTo_CopiesAllElements_InOrderOfEnumeration()
    {
        var set = new ObjectBackedTypedSet<int>([1, 2, 3]);
        var arr = new object?[5];
        set.CopyTo(arr, 1);
        // HashSet iteration order is unspecified; validate membership and positions filled
        var nonNullCount = 0;
        for (var i = 1; i <= 3; i++) if (arr[i] != null) nonNullCount++;
        Assert.AreEqual(3, nonNullCount);
        CollectionAssert.Contains(arr, 1);
        CollectionAssert.Contains(arr, 2);
        CollectionAssert.Contains(arr, 3);
    }

    [TestMethod]
    public void Enumerator_YieldsObjects()
    {
        var set = new ObjectBackedTypedSet<int>([7, 8]);
        var collected = new List<object?>();
        foreach (var o in set)
        {
            collected.Add(o);
        }
        Assert.AreEqual(2, collected.Count);
        CollectionAssert.Contains(collected, 7);
        CollectionAssert.Contains(collected, 8);
    }

    [TestMethod]
    public void Clear_EmptiesSet()
    {
        var set = new ObjectBackedTypedSet<int>([1, 2]);
        set.Clear();
        Assert.AreEqual(0, set.Count);
    }
}
