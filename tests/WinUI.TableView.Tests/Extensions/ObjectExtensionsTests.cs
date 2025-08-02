using System.Collections;
using WinUI.TableView.Extensions;

namespace WinUI.TableView.Tests.Extensions;

public class ObjectExtensionsTests
{
    [Fact]
    public void IsNumeric_WithNumericTypes_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(((byte)1).IsNumeric());
        Assert.True(((sbyte)1).IsNumeric());
        Assert.True(((short)1).IsNumeric());
        Assert.True(((ushort)1).IsNumeric());
        Assert.True(1.IsNumeric());
        Assert.True(1u.IsNumeric());
        Assert.True(1L.IsNumeric());
        Assert.True(1UL.IsNumeric());
        Assert.True(1.0f.IsNumeric());
        Assert.True(1.0d.IsNumeric());
        Assert.True(1.0m.IsNumeric());
    }

    [Fact]
    public void IsNumeric_WithNonNumericTypes_ReturnsFalse()
    {
        // Arrange & Act & Assert
        Assert.False("string".IsNumeric());
        Assert.False(true.IsNumeric());
        Assert.False(DateTime.Now.IsNumeric());
        Assert.False(new object().IsNumeric());
        Assert.False('c'.IsNumeric());
        Assert.False(Guid.NewGuid().IsNumeric());
    }

    [Fact]
    public void GetItemType_WithGenericList_ReturnsItemType()
    {
        // Arrange
        var intList = new List<int> { 1, 2, 3 };
        var stringList = new List<string> { "a", "b", "c" };

        // Act
        var intType = intList.GetItemType();
        var stringType = stringList.GetItemType();

        // Assert
        Assert.Equal(typeof(int), intType);
        Assert.Equal(typeof(string), stringType);
    }

    [Fact]
    public void GetItemType_WithArray_ReturnsItemType()
    {
        // Arrange
        var intArray = new int[] { 1, 2, 3 };
        var stringArray = new string[] { "a", "b", "c" };

        // Act
        var intType = intArray.GetItemType();
        var stringType = stringArray.GetItemType();

        // Assert
        Assert.Equal(typeof(int), intType);
        Assert.Equal(typeof(string), stringType);
    }

    [Fact]
    public void GetItemType_WithEmptyEnumerable_ReturnsExpectedType()
    {
        // Arrange
        var emptyIntList = new List<int>();
        var emptyStringList = new List<string>();

        // Act
        var intType = emptyIntList.GetItemType();
        var stringType = emptyStringList.GetItemType();

        // Assert
        Assert.Equal(typeof(int), intType);
        Assert.Equal(typeof(string), stringType);
    }

    [Fact]
    public void GetItemType_WithNonGenericEnumerable_ReturnsTypeOfFirstItem()
    {
        // Arrange
        var arrayList = new ArrayList { 1, "string", 3.14 };

        // Act
        var type = arrayList.GetItemType();

        // Assert
        Assert.Equal(typeof(int), type);
    }

    [Fact]
    public void GetItemType_WithEmptyNonGenericEnumerable_ReturnsNull()
    {
        // Arrange
        var emptyArrayList = new ArrayList();

        // Act
        var type = emptyArrayList.GetItemType();

        // Assert
        Assert.Null(type);
    }

    [Fact]
    public void GetCustomOrCLRType_WithRegularObject_ReturnsObjectType()
    {
        // Arrange
        var obj = "test string";
        var number = 42;

        // Act
        var stringType = obj.GetCustomOrCLRType();
        var intType = number.GetCustomOrCLRType();

        // Assert
        Assert.Equal(typeof(string), stringType);
        Assert.Equal(typeof(int), intType);
    }

    [Fact]
    public void GetCustomOrCLRType_WithNullObject_ReturnsNull()
    {
        // Arrange
        object? nullObj = null;

        // Act
        var type = nullObj.GetCustomOrCLRType();

        // Assert
        Assert.Null(type);
    }

    // Note: Testing GetValue methods with property paths would require complex setup
    // since they work with reflection and property navigation. These are integration
    // tests that would require more elaborate test objects with properties.
    [Fact]
    public void GetValue_WithNullObject_ReturnsNull()
    {
        // Arrange
        object? nullObj = null;
        var emptyPis = Array.Empty<(System.Reflection.PropertyInfo pi, object? index)>();

        // Act
        var result = nullObj.GetValue(emptyPis);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValue_WithEmptyPropertyPath_ReturnsSameObject()
    {
        // Arrange
        var testObj = "test";
        var emptyPis = Array.Empty<(System.Reflection.PropertyInfo pi, object? index)>();

        // Act
        var result = testObj.GetValue(emptyPis);

        // Assert
        Assert.Equal(testObj, result);
    }

    // Test object for property navigation tests
    public class TestObject
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public TestObject? Child { get; set; }
        public List<string> Items { get; set; } = new();
        public string[] Array { get; set; } = System.Array.Empty<string>();
    }

    [Fact]
    public void GetValue_WithPropertyPath_ReturnsPropertyValue()
    {
        // Arrange
        var testObj = new TestObject { Name = "Test", Value = 42 };

        // Act
        var nameResult = testObj.GetValue(typeof(TestObject), "Name", out _);
        var valueResult = testObj.GetValue(typeof(TestObject), "Value", out _);

        // Assert
        Assert.Equal("Test", nameResult);
        Assert.Equal(42, valueResult);
    }

    [Fact]
    public void GetValue_WithInvalidPropertyPath_ReturnsNull()
    {
        // Arrange
        var testObj = new TestObject();

        // Act
        var result = testObj.GetValue(typeof(TestObject), "NonExistentProperty", out var pis);

        // Assert
        Assert.Null(result);
        Assert.Null(pis);
    }

    [Fact]
    public void GetValue_WithNullOrEmptyPath_ReturnsOriginalObject()
    {
        // Arrange
        var testObj = new TestObject();

        // Act
        var nullResult = testObj.GetValue(typeof(TestObject), null, out var nullPis);
        var emptyResult = testObj.GetValue(typeof(TestObject), "", out var emptyPis);
        var whitespaceResult = testObj.GetValue(typeof(TestObject), "   ", out var whitespacePis);

        // Assert
        Assert.Equal(testObj, nullResult);
        Assert.Equal(testObj, emptyResult);
        Assert.Equal(testObj, whitespaceResult);
        Assert.Empty(nullPis);
        Assert.Empty(emptyPis);
        Assert.Empty(whitespacePis);
    }

    [Fact]
    public void GetValue_WithArrayIndex_ReturnsIndexedValue()
    {
        // Arrange
        var testObj = new TestObject 
        { 
            Array = new[] { "first", "second", "third" }
        };

        // Act
        var result = testObj.GetValue(typeof(TestObject), "Array[1]", out _);

        // Assert
        Assert.Equal("second", result);
    }

    [Fact]
    public void GetValue_WithListIndex_ReturnsIndexedValue()
    {
        // Arrange
        var testObj = new TestObject 
        { 
            Items = new List<string> { "item1", "item2", "item3" }
        };

        // Act
        var result = testObj.GetValue(typeof(TestObject), "Items[0]", out _);

        // Assert
        Assert.Equal("item1", result);
    }
}