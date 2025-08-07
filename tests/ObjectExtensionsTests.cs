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
    public void GetFuncCompiledPropertyPath_ShouldAccess2DArrayElement()
    {
        var testItem = new TestItem { Int2DArray = new int[,] {{1, 2, 3}, {10, 20, 30}} };
        var func = testItem.GetFuncCompiledPropertyPath("Int2DArray[1,1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldAccessMultiDimensionalIndexer()
    {
        var testItem = new TestItem();
        testItem[2, "foo"] = "bar";
        var func = testItem.GetFuncCompiledPropertyPath("[2,foo]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("bar", result);
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
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidProperty4()
    {
        var testItem = new TestItem();
        var func = testItem.GetFuncCompiledPropertyPath("Dictionary[123]");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidDictionaryIndexer()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        var func = testItem.GetFuncCompiledPropertyPath("Dictionary2[1]");
        Assert.IsNotNull(func);

        var result = func(testItem);
        Assert.AreEqual("value1", result);

        testItem = new TestItem { Dictionary2 = new() { { 2, "value2" } } };
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForInvalidArrayIndex()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetFuncCompiledPropertyPath("SubItems[0].SubSubItems[0].Name");
        Assert.IsNotNull(func);

        var result = func(testItem);
        Assert.AreEqual("NestedValue", result);

        testItem = new TestItem { SubItems = [new() { SubSubItems = null! }] };
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForOutOfBoundsArrayIndex()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var func = testItem.GetFuncCompiledPropertyPath("IntArray[2]");
        Assert.IsNotNull(func);
        var testItem2 = new TestItem { IntArray = [1] };
        var result = func(testItem2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetFuncCompiledPropertyPath_ShouldReturnNull_ForOutOfBoundsMultiDimArrayIndex()
    {
        var testItem3x3 = new TestItem { Int2DArray = new int[,] { { 1, 2, 3 }, { 10, 20, 30 } } };
        var func = testItem3x3.GetFuncCompiledPropertyPath("Int2DArray[2,2]");
        Assert.IsNotNull(func);
        var result = func(testItem3x3);

        var testItem2x2 = new TestItem { Int2DArray = new int[,] { { 1, 2 }, { 10, 30 } } };
        result = func(testItem2x2);
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
        public int[,] Int2DArray { get; set; } = new int[0, 0];

        // Multi-dimensional indexer
        private readonly Dictionary<(int, string), string> _multiIndex = new();
        public string this[int i, string key]
        {
            get => _multiIndex.TryGetValue((i, key), out var value) ? value : string.Empty;
            set => _multiIndex[(i, key)] = value;
        }
    }
}
