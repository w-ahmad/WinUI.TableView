using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class ObjectExtensionsTests
{
    [TestMethod]
    public void GetValue_ShouldAccessSimpleNestedProperty_UsingPathString()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var result = testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Name", out var _);
        Assert.IsNotNull(result);
        Assert.AreEqual("NestedValue", result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessSimpleNestedProperty_UsingParsedPath()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Name", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNotNull(result);
        Assert.AreEqual("NestedValue", result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessArrayElement_UsingPathString()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var result = testItem.GetValue(typeof(TestItem), "IntArray[1]", out var _);
        Assert.IsNotNull(result);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessArrayElement_UsingParsedPath()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        testItem.GetValue(typeof(TestItem), "IntArray[1]", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNotNull(result);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessDictionaryByStringKey_UsingPathString()
    {
        var testItem = new TestItem { Dictionary1 = new() { { "key1", "value1" } } };
        var result = testItem.GetValue(typeof(TestItem), "Dictionary1[key1]", out var _);
        Assert.IsNotNull(result);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessDictionaryByStringKey_UsingParsedPath()
    {
        var testItem = new TestItem { Dictionary1 = new() { { "key1", "value1" } } };
        testItem.GetValue(typeof(TestItem), "Dictionary1[key1]", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNotNull(result);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessDictionaryByIntKey_UsingPathString()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        var result = testItem.GetValue(typeof(TestItem), "Dictionary2[1]", out var _);
        Assert.IsNotNull(result);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetValue_ShouldAccessDictionaryByIntKey_UsingParsedPath()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        testItem.GetValue(typeof(TestItem), "Dictionary2[1]", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNotNull(result);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty_UsingPathString()
    {
        var testItem = new TestItem();
        var result = testItem.GetValue(typeof(TestItem), "NonExistent.Property.Path", out var _);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty_UsingParsedPath()
    {
        var testItem = new TestItem();
        testItem.GetValue(typeof(TestItem), "NonExistent.Property.Path", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty2_UsingPathString()
    {
        var testItem = new TestItem();
        var result = testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Invalid", out var _);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty2_UsingParsedPath()
    {
        var testItem = new TestItem();
        testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Invalid", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty3_UsingPathString()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var result = testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Invalid", out var _);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidProperty3_UsingParsedPath()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        testItem.GetValue(typeof(TestItem), "SubItems[0].SubSubItems[0].Invalid", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidIndexer_UsingPathString()
    {
        var testItem = new TestItem();
        var result = testItem.GetValue(typeof(TestItem), "Dictionary[123]", out var _);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetValue_ShouldReturnNull_ForInvalidIndexer_UsingParsedPath()
    {
        var testItem = new TestItem();
        testItem.GetValue(typeof(TestItem), "Dictionary[123]", out var pis);
        var result = testItem.GetValue(pis);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetItemType_ShouldReturnCorrectType_ForGenericEnumerable()
    {
        var list = new List<int> { 1, 2, 3 };
        var itemType = list.GetItemType();
        Assert.AreEqual(typeof(int), itemType);
    }

    [TestMethod]
    public void GetCustomOrCLRType_ShouldReturnCLRType_ForNonICustomTypeProvider()
    {
        var obj = new object();
        var type = obj.GetCustomOrCLRType();
        Assert.AreEqual(typeof(object), type);
    }

    [TestMethod]
    public void GetCustomOrCLRType_ShouldReturnNull_ForNullInstance()
    {
        object? obj = null;
        var type = obj.GetCustomOrCLRType();
        Assert.IsNull(type);
    }

    [TestMethod]
    public void GetCustomOrCLRType_ShouldReturnType_ForStringInstance()
    {
        var obj = "TestString";
        var type = obj.GetCustomOrCLRType();
        Assert.AreEqual(typeof(string), type);
    }

    [TestMethod]
    public void GetCustomOrCLRType_ShouldReturnType_ForNumericInstance()
    {
        var obj = 123;
        var type = obj.GetCustomOrCLRType();
        Assert.AreEqual(typeof(int), type);
    }

    [TestMethod]
    public void GetCustomOrCLRType_ShouldReturnType_ForCustomObjectInstance()
    {
        var obj = new TestItem();
        var type = obj.GetCustomOrCLRType();
        Assert.AreEqual(typeof(TestItem), type);
    }

    private class SubSubItem
    {
        public string Name { get; set; } = string.Empty;
    }

    private class SubItem
    {
        public List<SubSubItem> SubSubItems { get; set; } = [];
    }

    private class TestItem
    {
        public List<SubItem> SubItems { get; set; } = [];
        public Dictionary<string, string> Dictionary1 { get; set; } = [];
        public Dictionary<int, string> Dictionary2 { get; set; } = [];
        public int[] IntArray { get; set; } = [];
    }
}
