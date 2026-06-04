using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests;

[TestClass]
public class ObjectExtensionsTests
{
    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessSimpleProperty()
    {
        var testItem = new TestItem { Number = 7 };
        var func = testItem.GetCompiledValueGetter("Number");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(7, result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessSimpleNullableProperty()
    {
        var today = DateTimeOffset.Now;
        var testItem = new TestItem { CompletedDate = today };
        var func = testItem.GetCompiledValueGetter("CompletedDate");
        Assert.IsNotNull(func);

        var result = func(testItem);
        Assert.AreEqual(today, result);

        testItem.CompletedDate = null;
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessNestedProperty()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetCompiledValueGetter("SubItems[0].SubSubItems[0].Name");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("NestedValue", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessArrayElement()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var func = testItem.GetCompiledValueGetter("IntArray[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccess2DArrayElement()
    {
        var testItem = new TestItem { Int2DArray = new int[,] {{1, 2, 3}, {10, 20, 30}} };
        var func = testItem.GetCompiledValueGetter("Int2DArray[1,1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessMultiDimensionalIndexer()
    {
        var testItem = new TestItem();
        testItem[2, "foo"] = "bar";
        var func = testItem.GetCompiledValueGetter("[2,foo]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("bar", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessDictionaryByStringKey()
    {
        var testItem = new TestItem { Dictionary1 = new() { { "key1", "value1" } } };
        var func = testItem.GetCompiledValueGetter("Dictionary1[key1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessDictionaryByIntKey()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        var func = testItem.GetCompiledValueGetter("Dictionary2[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("value1", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidProperty()
    {
        var testItem = new TestItem();
        var func = testItem.GetCompiledValueGetter("NonExistent.Property.Path");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidProperty2()
    {
        var testItem = new TestItem();
        var func = testItem.GetCompiledValueGetter("SubItems[0].SubSubItems[0].Invalid");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidProperty3()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetCompiledValueGetter("SubItems[0].SubSubItems[0].Invalid");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidProperty4()
    {
        var testItem = new TestItem();
        var func = testItem.GetCompiledValueGetter("Dictionary[123]");
        Assert.IsNull(func);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidDictionaryIndexer()
    {
        var testItem = new TestItem { Dictionary2 = new() { { 1, "value1" } } };
        var func = testItem.GetCompiledValueGetter("Dictionary2[1]");
        Assert.IsNotNull(func);

        var result = func(testItem);
        Assert.AreEqual("value1", result);

        testItem = new TestItem { Dictionary2 = new() { { 2, "value2" } } };
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForInvalidArrayIndex()
    {
        var testItem = new TestItem { SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }] };
        var func = testItem.GetCompiledValueGetter("SubItems[0].SubSubItems[0].Name");
        Assert.IsNotNull(func);

        var result = func(testItem);
        Assert.AreEqual("NestedValue", result);

        testItem = new TestItem { SubItems = [new() { SubSubItems = null! }] };
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForOutOfBoundsArrayIndex()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var func = testItem.GetCompiledValueGetter("IntArray[2]");
        Assert.IsNotNull(func);
        var testItem2 = new TestItem { IntArray = [1] };
        var result = func(testItem2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForOutOfBoundsMultiDimArrayIndex()
    {
        var testItem3x3 = new TestItem { Int2DArray = new int[,] { { 1, 2, 3 }, { 10, 20, 30 } } };
        var func = testItem3x3.GetCompiledValueGetter("Int2DArray[2,2]");
        Assert.IsNotNull(func);
        var result = func(testItem3x3);

        var testItem2x2 = new TestItem { Int2DArray = new int[,] { { 1, 2 }, { 10, 30 } } };
        result = func(testItem2x2);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessListByIndex()
    {
        var testItem = new TestItem { StringList = ["item0", "item1", "item2"] };
        var func = testItem.GetCompiledValueGetter("StringList[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("item1", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessPropertyOnString()
    {
        var testItem = new TestItem { StringList = ["item0", "item1 long text", "item2"] };
        var func = testItem.GetCompiledValueGetter("StringList[1].Length");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(15, result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForOutOfBoundsListIndex()
    {
        var testItem = new TestItem { StringList = ["item0", "item1", "item2"] };
        var func = testItem.GetCompiledValueGetter("StringList[2]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("item2", result);

        // Test with different data that has fewer items - should return null
        testItem = new TestItem { StringList = ["item0"] };
        result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForDictionaryKeyTypeMismatch()
    {
        // Dictionary<string, string> accessed with int key
        var testItem = new TestItem { Dictionary1 = new() { { "key1", "value1" } } };
        var func = testItem.GetCompiledValueGetter("Dictionary1[123]"); // int key for string-keyed dictionary
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessValueTypeProperty()
    {
        var testItem = new TestItem { ValueTypeStruct = new TestStruct { Value = 42 } };
        var func = testItem.GetCompiledValueGetter("ValueTypeStruct.Value");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForNegativeArrayIndex()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };
        var func = testItem.GetCompiledValueGetter("IntArray[-1]");
        Assert.IsNull(func); // Should fail during expression building
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForNegativeListIndex()
    {
        var testItem = new TestItem { StringList = ["item0", "item1"] };
        var func = testItem.GetCompiledValueGetter("StringList[-1]");
        Assert.IsNull(func); // Should fail during expression building
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForWrongArrayDimensions()
    {
        var testItem = new TestItem { Int2DArray = new int[,] { { 1, 2 }, { 3, 4 } } };
        var func = testItem.GetCompiledValueGetter("Int2DArray[1]"); // 2D array with 1D index
        Assert.IsNull(func); // Should fail during expression building
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForThrowingIndexer()
    {
        var testItem = new TestItem();
        // This should trigger the generic indexer path with try-catch
        var func = testItem.GetCompiledValueGetter("[999,nonexistent]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.IsNull(result); // Custom indexer returns empty string, but expression should handle gracefully
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldAccessNonGenericList()
    {
        var testItem = new TestItem { NonGenericList = new System.Collections.ArrayList { "item0", "item1", "item2" } };
        var func = testItem.GetCompiledValueGetter("NonGenericList[1]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.AreEqual("item1", result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForOutOfBoundsNonGenericList()
    {
        var testItem = new TestItem { NonGenericList = new System.Collections.ArrayList { "item0" } };
        var func = testItem.GetCompiledValueGetter("NonGenericList[5]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueGetter_ShouldReturnNull_ForEmptyList()
    {
        var testItem = new TestItem { StringList = [] };
        var func = testItem.GetCompiledValueGetter("StringList[0]");
        Assert.IsNotNull(func);
        var result = func(testItem);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetSimpleProperty()
    {
        var testItem = new TestItem { Number = 7 };

        var setter = testItem.GetCompiledValueSetter("Number");

        Assert.IsNotNull(setter);

        setter(testItem, 42);

        Assert.AreEqual(42, testItem.Number);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetSimpleNullableProperty()
    {
        var today = DateTimeOffset.Now;
        var testItem = new TestItem();

        var setter = testItem.GetCompiledValueSetter("CompletedDate");

        Assert.IsNotNull(setter);

        setter(testItem, today);

        Assert.AreEqual(today, testItem.CompletedDate);

        setter(testItem, null);

        Assert.IsNull(testItem.CompletedDate);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetNestedProperty()
    {
        var testItem = new TestItem
        {
            SubItems = [new() { SubSubItems = [new() { Name = "OldValue" }] }]
        };

        var setter = testItem.GetCompiledValueSetter("SubItems[0].SubSubItems[0].Name");

        Assert.IsNotNull(setter);

        setter(testItem, "NewValue");

        Assert.AreEqual("NewValue", testItem.SubItems[0].SubSubItems[0].Name);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetArrayElement()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };

        var setter = testItem.GetCompiledValueSetter("IntArray[1]");

        Assert.IsNotNull(setter);

        setter(testItem, 99);

        Assert.AreEqual(99, testItem.IntArray[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSet2DArrayElement()
    {
        var testItem = new TestItem
        {
            Int2DArray = new int[,] { { 1, 2, 3 }, { 10, 20, 30 } }
        };

        var setter = testItem.GetCompiledValueSetter("Int2DArray[1,1]");

        Assert.IsNotNull(setter);

        setter(testItem, 99);

        Assert.AreEqual(99, testItem.Int2DArray[1, 1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetMultiDimensionalIndexer()
    {
        var testItem = new TestItem();

        var setter = testItem.GetCompiledValueSetter("[2,foo]");

        Assert.IsNotNull(setter);

        setter(testItem, "bar");

        Assert.AreEqual("bar", testItem[2, "foo"]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetDictionaryByStringKey()
    {
        var testItem = new TestItem
        {
            Dictionary1 = new() { { "key1", "value1" } }
        };

        var setter = testItem.GetCompiledValueSetter("Dictionary1[key1]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("updated", testItem.Dictionary1["key1"]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldAddDictionaryValue_WhenStringKeyDoesNotExist()
    {
        var testItem = new TestItem
        {
            Dictionary1 = []
        };

        var setter = testItem.GetCompiledValueSetter("Dictionary1[key1]");

        Assert.IsNotNull(setter);

        setter(testItem, "value1");

        Assert.AreEqual("value1", testItem.Dictionary1["key1"]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetDictionaryByIntKey()
    {
        var testItem = new TestItem
        {
            Dictionary2 = new() { { 1, "value1" } }
        };

        var setter = testItem.GetCompiledValueSetter("Dictionary2[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("updated", testItem.Dictionary2[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldAddDictionaryValue_WhenIntKeyDoesNotExist()
    {
        var testItem = new TestItem
        {
            Dictionary2 = []
        };

        var setter = testItem.GetCompiledValueSetter("Dictionary2[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "value1");

        Assert.AreEqual("value1", testItem.Dictionary2[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForInvalidProperty()
    {
        var testItem = new TestItem();

        var setter = testItem.GetCompiledValueSetter("NonExistent.Property.Path");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForInvalidNestedProperty()
    {
        var testItem = new TestItem
        {
            SubItems = [new() { SubSubItems = [new() { Name = "NestedValue" }] }]
        };

        var setter = testItem.GetCompiledValueSetter("SubItems[0].SubSubItems[0].Invalid");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForNullIntermediateProperty()
    {
        var testItem = new TestItem
        {
            SubItems = [new() { SubSubItems = null! }]
        };

        var setter = testItem.GetCompiledValueSetter("SubItems[0].SubSubItems[0].Name");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForInvalidDictionaryProperty()
    {
        var testItem = new TestItem();

        var setter = testItem.GetCompiledValueSetter("Dictionary[123]");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotThrow_ForDictionaryKeyTypeMismatch()
    {
        var testItem = new TestItem
        {
            Dictionary1 = new() { { "key1", "value1" } }
        };

        var setter = testItem.GetCompiledValueSetter("Dictionary1[123]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("value1", testItem.Dictionary1["key1"]);
        Assert.IsFalse(testItem.Dictionary1.ContainsKey("123"));
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForOutOfBoundsArrayIndex()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };

        var setter = testItem.GetCompiledValueSetter("IntArray[5]");

        Assert.IsNotNull(setter);

        setter(testItem, 99);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, testItem.IntArray);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForOutOfBoundsMultiDimArrayIndex()
    {
        var testItem = new TestItem
        {
            Int2DArray = new int[,] { { 1, 2 }, { 10, 30 } }
        };

        var setter = testItem.GetCompiledValueSetter("Int2DArray[2,2]");

        Assert.IsNotNull(setter);

        setter(testItem, 99);

        Assert.AreEqual(1, testItem.Int2DArray[0, 0]);
        Assert.AreEqual(2, testItem.Int2DArray[0, 1]);
        Assert.AreEqual(10, testItem.Int2DArray[1, 0]);
        Assert.AreEqual(30, testItem.Int2DArray[1, 1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetListByIndex()
    {
        var testItem = new TestItem
        {
            StringList = ["item0", "item1", "item2"]
        };

        var setter = testItem.GetCompiledValueSetter("StringList[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("updated", testItem.StringList[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetNonGenericListByIndex()
    {
        var testItem = new TestItem
        {
            NonGenericList = new System.Collections.ArrayList { "item0", "item1", "item2" }
        };

        var setter = testItem.GetCompiledValueSetter("NonGenericList[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("updated", testItem.NonGenericList[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForNegativeListIndex()
    {
        var testItem = new TestItem { StringList = ["item0", "item1"] };

        var setter = testItem.GetCompiledValueSetter("StringList[-1]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        CollectionAssert.AreEqual(new[] { "item0", "item1" }, testItem.StringList);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForOutOfBoundsListIndex()
    {
        var testItem = new TestItem { StringList = ["item0"] };

        var setter = testItem.GetCompiledValueSetter("StringList[5]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        CollectionAssert.AreEqual(new[] { "item0" }, testItem.StringList);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForOutOfBoundsNonGenericListIndex()
    {
        var testItem = new TestItem
        {
            NonGenericList = new System.Collections.ArrayList { "item0" }
        };

        var setter = testItem.GetCompiledValueSetter("NonGenericList[5]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual("item0", testItem.NonGenericList[0]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_ForEmptyList()
    {
        var testItem = new TestItem { StringList = [] };

        var setter = testItem.GetCompiledValueSetter("StringList[0]");

        Assert.IsNotNull(setter);

        setter(testItem, "updated");

        Assert.AreEqual(0, testItem.StringList.Count);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForNegativeArrayIndex()
    {
        var testItem = new TestItem { IntArray = [10, 20, 30] };

        var setter = testItem.GetCompiledValueSetter("IntArray[-1]");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForWrongArrayDimensions()
    {
        var testItem = new TestItem
        {
            Int2DArray = new int[,] { { 1, 2 }, { 3, 4 } }
        };

        var setter = testItem.GetCompiledValueSetter("Int2DArray[1]");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldReturnNull_ForReadOnlyProperty()
    {
        var testItem = new TestItem
        {
            StringList = ["item0", "item1 long text", "item2"]
        };

        var setter = testItem.GetCompiledValueSetter("StringList[1].Length");

        Assert.IsNull(setter);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertStringToInt()
    {
        var testItem = new TestItem { Number = 7 };

        var setter = testItem.GetCompiledValueSetter("Number");

        Assert.IsNotNull(setter);

        setter(testItem, "42");

        Assert.AreEqual(42, testItem.Number);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSet_WhenStringCannotConvertToInt()
    {
        var testItem = new TestItem { Number = 7 };

        var setter = testItem.GetCompiledValueSetter("Number");

        Assert.IsNotNull(setter);

        setter(testItem, "invalid");

        Assert.AreEqual(7, testItem.Number);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertStringToNullableDateTimeOffset()
    {
        var testItem = new TestItem();

        var setter = testItem.GetCompiledValueSetter("CompletedDate");

        Assert.IsNotNull(setter);

        var value = DateTimeOffset.Now;

        setter(testItem, value.ToString(CultureInfo.CurrentCulture));

        Assert.IsNotNull(testItem.CompletedDate);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldSetNullablePropertyToNull_WhenValueIsEmptyString()
    {
        var testItem = new TestItem
        {
            CompletedDate = DateTimeOffset.Now
        };

        var setter = testItem.GetCompiledValueSetter("CompletedDate");

        Assert.IsNotNull(setter);

        setter(testItem, "");

        Assert.IsNull(testItem.CompletedDate);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertStringToArrayIntElement()
    {
        var testItem = new TestItem
        {
            IntArray = [1, 2, 3]
        };

        var setter = testItem.GetCompiledValueSetter("IntArray[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "42");

        Assert.AreEqual(42, testItem.IntArray[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldNotSetArrayIntElement_WhenConversionFails()
    {
        var testItem = new TestItem
        {
            IntArray = [1, 2, 3]
        };

        var setter = testItem.GetCompiledValueSetter("IntArray[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "not-a-number");

        Assert.AreEqual(2, testItem.IntArray[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertStringToListIntElement()
    {
        var testItem = new TestItem
        {
            IntList = [1, 2, 3]
        };

        var setter = testItem.GetCompiledValueSetter("IntList[1]");

        Assert.IsNotNull(setter);

        setter(testItem, "99");

        Assert.AreEqual(99, testItem.IntList[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertIntToStringListElement()
    {
        var testItem = new TestItem
        {
            StringList = ["A", "B", "C"]
        };

        var setter = testItem.GetCompiledValueSetter("StringList[1]");

        Assert.IsNotNull(setter);

        setter(testItem, 123);

        Assert.AreEqual("123", testItem.StringList[1]);
    }

    [TestMethod]
    public void GetCompiledValueSetter_ShouldConvertStringToDictionaryIntValue()
    {
        var testItem = new TestItem
        {
            IntDictionary = new()
            {
                ["A"] = 1
            }
        };

        var setter = testItem.GetCompiledValueSetter("IntDictionary[A]");

        Assert.IsNotNull(setter);

        setter(testItem, "500");

        Assert.AreEqual(500, testItem.IntDictionary["A"]);
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
        public int Number { get; set; } = 0;
        public DateTimeOffset? CompletedDate { get; set; }
        public TestStruct ValueTypeStruct { get; set; }
        public IList NonGenericList { get; set; } = new ArrayList();
        public List<SubItem> SubItems { get; set; } = [];
        public Dictionary<string, string> Dictionary1 { get; set; } = [];
        public Dictionary<int, string> Dictionary2 { get; set; } = [];
        public Dictionary<string, int> IntDictionary { get; set; } = [];
        public int[] IntArray { get; set; } = [];
        public int[,] Int2DArray { get; set; } = new int[0, 0];
        public List<string> StringList { get; set; } = [];
        public List<int> IntList { get; set; } = [];

        // Multi-dimensional indexer
        private readonly Dictionary<(int, string), string> _multiIndex = new();
        public string this[int i, string key]
        {
            get => _multiIndex[(i, key)];
            set => _multiIndex[(i, key)] = value;
        }
    }

    public struct TestStruct
    {
        public int Value { get; set; }
    }
}

