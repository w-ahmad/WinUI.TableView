using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class ObjectExtensionsTests
{
    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldAccessSimpleNestedProperty()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetFuncCompiledPropertyPath("SubItems[0].SubSubItems[0].Name");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("NestedValue", result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldAccessArrayElement()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var func = testItem.GetFuncCompiledPropertyPath("IntArray[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldAccessDictionaryByStringKey()
    {
        var testItem = new TestItem { Dictionary1 = new() { { "key1", "value1" } } };
        var func = testItem.GetFuncCompiledPropertyPath("Dictionary1[key1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldAccessDictionaryByIntKey()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        var func = testItem.GetFuncCompiledPropertyPath("Dictionary2[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidProperty()
    {
        var testItem = new TestItem();
        var func = testItem.GetFuncCompiledPropertyPath("NonExistent.Property.Path");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidProperty2()
    {
        var testItem = new TestItem();
        var func = testItem.GetFuncCompiledPropertyPath("SubItems[0].SubSubItems[0].Invalid");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidProperty3()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetFuncCompiledPropertyPath("SubItems[0].SubSubItems[0].Invalid");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidIndexer()
    {
        var testItem = new TestItem();
        var func = testItem.GetFuncCompiledPropertyPath("Dictionary[123]");
        Assert.IsNull(func);
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
